using System.Linq;
using UnityEngine;
using KeepCoding;
using KModkit;

public class ParallelMazesModule : ModuleScript {
	enum State { CONNECTON, DISCONNECTED, REGISTRATION, WAITING_FOR_EXPERT, IN_GAME, EXPERT_NOT_FOUND }

	public TextMesh Console;
	public KMSelectable Selectable;
	public KMSelectable SolveButton;
	public ExpertIdInput ExpertIdInput;
	public ExpertNotFoundComponent ExpertNotFoundComponent;
	public GameContainer GameContainer;

	private class Updating {
		public bool ShouldUpdate = false;
		public ParallelMazesClient.Coord NewModulePosition;
		public bool Move = false;
		public bool Solve = false;
		public bool Strike = false;
	}

	private bool _connected = false;
	private string _gameId;
	private string _moduleKey;
	private float _lastPingTime;
	private State _prevState = State.CONNECTON;
	private State _state = State.CONNECTON;
	private Updating _updating = new Updating();
	private ParallelMazesClient _client = new ParallelMazesClient();

	private void Start() {
		_client.WS.OnOpen += OnConnect;
		_client.WS.OnError += (e) => Debug.LogFormat("Error: {0}", e);
		_client.WS.OnClose += OnDisconnected;
	}

	public override void OnActivate() {
		base.OnActivate();
		SolveButton.OnInteract += () => { if (!IsSolved) Solve(); return false; };
		ExpertIdInput.SubmitButton.OnInteract += () => { if (!IsSolved && !ExpertIdInput.Disabled) OnExpertIdSubmit(); return false; };
		ExpertNotFoundComponent.RetryButton.OnInteract += () => { if (!IsSolved) OnExpertNotFoundRetryPressed(); return false; };
		GameContainer.DisconnectButton.OnInteract += () => { if (!IsSolved) OnDisconnectButtonPressed(); return false; };
		GameContainer.RightButton.OnInteract += () => { OnMove("right"); return false; };
		GameContainer.UpButton.OnInteract += () => { OnMove("up"); return false; };
		GameContainer.LeftButton.OnInteract += () => { OnMove("left"); return false; };
		GameContainer.DownButton.OnInteract += () => { OnMove("down"); return false; };
		foreach (KeyComponent key in ExpertIdInput.Keys) key.Selectable.Parent = Selectable;
		ExpertNotFoundComponent.RetryButton.Parent = Selectable;
		KMSelectable[] dirButtons = new[] { GameContainer.RightButton, GameContainer.UpButton, GameContainer.LeftButton, GameContainer.DownButton };
		foreach (KMSelectable dirBtn in dirButtons) dirBtn.Parent = Selectable;
		Selectable.Children = new[] {
			SolveButton,
			ExpertIdInput.ClearButton,
			ExpertIdInput.SubmitButton,
			ExpertNotFoundComponent.RetryButton,
			GameContainer.DisconnectButton,
		}.Concat(ExpertIdInput.Keys.Select(k => k.Selectable)).Concat(dirButtons).ToArray();
		Selectable.UpdateChildren();
		ExpertIdInput.OnSelectableUpdated();
		foreach (Component cmp in new Component[] { SolveButton, ExpertIdInput, ExpertNotFoundComponent, GameContainer }) cmp.gameObject.SetActive(false);
		_client.Connect();
	}

	public override void OnDestruct() {
		base.OnDestruct();
		_client.WS.Close();
	}

	private void Update() {
		if (IsSolved) return;
		if (_state != _prevState) UpdateState();
		if (_updating.ShouldUpdate) {
			_lastPingTime = Time.time;
			GameContainer.Move = _updating.Move;
			GameContainer.UpdateLocker();
			if (_updating.NewModulePosition != null) {
				GameContainer.MazeComponent.PlayerPosition = _updating.NewModulePosition;
				GameContainer.MazeComponent.RenderPlayer();
				Log("Moved to ({0};{1})", _updating.NewModulePosition.X, _updating.NewModulePosition.Y);
			}
			if (_updating.Strike) {
				Strike();
				Log("Moved into the wall. Strike!");
			}
			if (_updating.Solve) {
				_client.WS.Close();
				Solve();
				Log("Module solved!");
			}
			_updating.ShouldUpdate = false;
		}
		if (Time.time - _lastPingTime > 1f) {
			if (_connected) _client.Ping();
			_lastPingTime = Time.time;
		}
	}

	private void UpdateState() {
		_lastPingTime = Time.time;
		if (_state == State.DISCONNECTED) {
			Console.text = "DISCONNECTED";
			SolveButton.gameObject.SetActive(true);
		} else if (_state == State.REGISTRATION) Console.text = "REGISTRATION";
		else if (_state == State.WAITING_FOR_EXPERT) {
			GameContainer.gameObject.SetActive(false);
			ExpertNotFoundComponent.gameObject.SetActive(false);
			Console.text = string.Format("GAME ID: {0}", _gameId);
			ExpertIdInput.gameObject.SetActive(true);
		} else if (_state == State.IN_GAME) {
			Console.text = string.Format("GAME ID: {0}\nEXPERT: {1}", _gameId, ExpertIdInput.ExpertId);
			ExpertIdInput.Disabled = false;
			ExpertIdInput.gameObject.SetActive(false);
			GameContainer.gameObject.SetActive(true);
			GameContainer.UpdateLocker();
			GameContainer.MazeComponent.Render();
		} else if (_state == State.EXPERT_NOT_FOUND) {
			Log("Expert {0} not found", ExpertIdInput.ExpertId);
			ExpertIdInput.Disabled = false;
			ExpertIdInput.gameObject.SetActive(false);
			ExpertNotFoundComponent.gameObject.SetActive(true);
			ExpertNotFoundComponent.ExpertId = ExpertIdInput.ExpertId;
		}
		_prevState = _state;
	}

	private void OnMove(string direction) {
		if (IsSolved || !GameContainer.Move) return;
		GameContainer.Move = false;
		GameContainer.UpdateLocker();
		_client.ModuleMove(_gameId, direction, (result) => {
			_updating = new Updating();
			_updating.ShouldUpdate = true;
			_updating.Move = result.Move == "module";
			_updating.NewModulePosition = result.NewPosition;
			_updating.Solve = result.Move == "none";
		}, (reason) => {
			if (reason.Message != "moved into wall") {
				Debug.LogError(reason);
				return;
			}
			_updating = new Updating();
			_updating.ShouldUpdate = true;
			_updating.Move = reason.Move == "module";
			_updating.Strike = true;
		});
	}

	private void OnDisconnectButtonPressed() {
		_client.KickExpert(_gameId, (_) => _state = State.WAITING_FOR_EXPERT, (reason) => {
			Debug.LogErrorFormat("kick_expert throws: {0}", reason);
			_state = State.WAITING_FOR_EXPERT;
		});
	}

	private void OnExpertNotFoundRetryPressed() {
		_state = State.WAITING_FOR_EXPERT;
		ExpertIdInput.ExpertId = "";
	}

	private void OnExpertIdSubmit() {
		ExpertIdInput.Disabled = true;
		_client.ConnectToExpert(_gameId, ExpertIdInput.ExpertId, (result) => {
			Log("Expert maze: {0}", result.ExpertMaze.SelectMany(s => s).Join(" "));
			Log("Module start position: ({0};{1})", result.ModulePos.X, result.ModulePos.Y);
			Log("Module finish position: ({0};{1})", result.ModuleFinish.X, result.ModuleFinish.Y);
			GameContainer.MazeComponent.Map = result.ExpertMaze;
			GameContainer.MazeComponent.PlayerPosition = result.ModulePos;
			GameContainer.MazeComponent.FinishPosition = result.ModuleFinish;
			GameContainer.Move = result.Move == "module";
			_state = State.IN_GAME;
		}, (reason) => {
			if (reason as string == "expert not found") _state = State.EXPERT_NOT_FOUND;
			else Debug.LogErrorFormat("Unexpected error reason on expert id submit: {0}", reason);
		});
	}

	private void OnConnect() {
		_connected = true;
		switch (_state) {
			case State.CONNECTON:
				Log("Connected to server. Registration...");
				_state = State.REGISTRATION;
				_client.CreateGame(OnGameCreated, (reason) => Debug.LogError(reason));
				break;
			default: Debug.LogError("Unexpected connection"); break;
		}
	}

	private void OnGameCreated(ParallelMazesClient.CreateGameResponse game) {
		if (_state != State.REGISTRATION) {
			Debug.LogError("Game created not on registration");
			return;
		}
		Log("Registered: {0} / {1}", game.GameId, game.ModuleKey);
		_gameId = game.GameId;
		_moduleKey = game.ModuleKey;
		_state = State.WAITING_FOR_EXPERT;
		_client.OnExpertMoved(_gameId, (e) => {
			_updating = new Updating();
			_updating.ShouldUpdate = true;
			_updating.Move = e.Move == "module";
			_updating.Strike = e.Strike;
			_updating.Solve = e.Move == "none";
		});
	}

	private void OnDisconnected() {
		_connected = false;
		switch (_state) {
			case State.CONNECTON:
				Log("Unable to connect to server");
				_state = State.DISCONNECTED;
				break;
			default: Debug.LogError("Connection closed"); break;
		}
	}
}
