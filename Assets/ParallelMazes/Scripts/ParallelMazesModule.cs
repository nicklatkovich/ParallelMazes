using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KeepCoding;

public class ParallelMazesModule : ModuleScript {
	enum State { CONNECTON, CONNECTION_ERROR, REGISTRATION, WAITING_FOR_EXPERT, IN_GAME, EXPERT_NOT_FOUND, DISCONNECTED }

	public readonly string TwitchHelpMessage = new[] {
		"\"!{0} expert 123456\" - connect expert",
		"\"!{0} move u\" - press movement button",
		"\"!{0} retry\" - press retry button",
		"\"!{0} disconnect\" - disconnect expert",
		"\"!{0} reconnect\" - reconnect if connection closed",
		"\"!{0} offline\" - solve module if server is unavailable",
	}.Join(" | ");

	public TextMesh Console;
	public KMSelectable Selectable;
	public KMSelectable SolveButton;
	public KMSelectable ReconnectButton;
	public ExpertIdInput ExpertIdInput;
	public ExpertNotFoundComponent ExpertNotFoundComponent;
	public GameContainer GameContainer;

	private class GameMoveInfo {
		public bool IsNew;
		public bool Expert;
		public string Direction;
		public ParallelMazesClient.Coord Position;
		public bool CanMove;
		public bool Solve;
		public bool Strike;
		public float Time;
	}

	private bool _startLogged = false;
	private bool _connected = false;
	private string _gameId;
	private string _moduleKey;
	private float _lastPingTime;
	private float _lastTime;
	private State _prevState = State.CONNECTON;
	private State _state = State.CONNECTON;
	private GameMoveInfo _lastMoveInfo = new GameMoveInfo();
	private ParallelMazesClient _client = new ParallelMazesClient();
	private object _lastException = null;
	private Queue<string> _logs = new Queue<string>();

	private void Start() {
		_lastTime = Time.time;
		_client.WS.OnOpen += OnConnect;
		_client.WS.OnError += (e) => _lastException = e;
		_client.WS.OnClose += OnDisconnected;
	}

	public override void OnActivate() {
		base.OnActivate();
		SolveButton.OnInteract += () => { if (!IsSolved) Solve(); return false; };
		ReconnectButton.OnInteract += () => { Reconnect(); return false; };
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
			ReconnectButton,
			ExpertIdInput.ClearButton,
			ExpertIdInput.SubmitButton,
			ExpertNotFoundComponent.RetryButton,
			GameContainer.DisconnectButton,
		}.Concat(ExpertIdInput.Keys.Select(k => k.Selectable)).Concat(dirButtons).ToArray();
		Selectable.UpdateChildren();
		ExpertIdInput.OnSelectableUpdated();
		foreach (Component cmp in new Component[] { SolveButton, ReconnectButton, ExpertIdInput, ExpertNotFoundComponent, GameContainer }) cmp.gameObject.SetActive(false);
		Log("Connecting to server...");
		_client.Connect();
	}

	public override void OnDestruct() {
		base.OnDestruct();
		_client.WS.Close();
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		command = command.Trim().ToLower();
		if (Regex.IsMatch(command, @"^expert +[0-9]{7}$")) {
			yield return null;
			if (_state != State.WAITING_FOR_EXPERT) {
				yield return "sendtochaterror {0}, !{1}: expert id cannot be entered right now";
				yield break;
			}
			string code = command.Split(' ').Last();
			List<KMSelectable> buttons = code.Select(c => ExpertIdInput.Keys[c - '0'].Selectable).ToList();
			buttons.Add(ExpertIdInput.SubmitButton);
			if (ExpertIdInput.ExpertId.Length > 0) buttons.Insert(0, ExpertIdInput.ClearButton);
			yield return buttons.ToArray();
			yield return new WaitUntil(() => _state != State.WAITING_FOR_EXPERT);
			yield break;
		}
		if (Regex.IsMatch(command, @"^move +(up?|d(own)?|l(eft)?|r(ight)?)$")) {
			yield return null;
			if (_state != State.IN_GAME) {
				yield return "sendtochaterror {0}, !{1}: cannot move: not in game";
				yield break;
			}
			if (!GameContainer.Move) {
				yield return "sendtochaterror {0}, !{1}: cannot move: not your turn";
				yield break;
			}
			char dirC = command.Split(' ').Last().First();
			float lastUpdatingTime = _lastMoveInfo.Time;
			float time = _lastTime;
			if (dirC == 'u') yield return new[] { GameContainer.UpButton };
			else if (dirC == 'd') yield return new[] { GameContainer.DownButton };
			else if (dirC == 'l') yield return new[] { GameContainer.LeftButton };
			else if (dirC == 'r') yield return new[] { GameContainer.RightButton };
			else throw new System.Exception(string.Format("Move cannot be made in {0} direction", dirC));
			yield return new WaitUntil(() => lastUpdatingTime != _lastMoveInfo.Time || _lastTime - time > 1f);
			yield break;
		}
		if (command == "retry") {
			yield return null;
			if (_state != State.EXPERT_NOT_FOUND) {
				yield return "sendtochaterror {0}, !{1}: retry button not found";
				yield break;
			}
			yield return new[] { ExpertNotFoundComponent.RetryButton };
			yield break;
		}
		if (command == "disconnect") {
			yield return null;
			if (_state != State.IN_GAME) {
				yield return "sendtochaterror {0}, !{1}: disconnect button not found";
				yield break;
			}
			yield return new[] { GameContainer.DisconnectButton };
			yield break;
		}
		if (command == "offline") {
			yield return null;
			if (_state != State.CONNECTION_ERROR) {
				yield return "sendtochaterror {0}, !{1}: module seems to be working correctly";
				yield break;
			}
			yield return new[] { SolveButton };
			yield break;
		}
	}

	private void Update() {
		_lastTime = Time.time;
		if (IsSolved) return;
		if (_lastException != null) {
			Log("ERROR: {0}", _lastException);
			_lastException = null;
		}
		while (_logs.Count > 0) Log(_logs.Dequeue());
		if (_state != _prevState) UpdateState();
		if (_lastMoveInfo.IsNew) {
			_lastMoveInfo.IsNew = false;
			_lastPingTime = Time.time;
			string prevMover = _lastMoveInfo.Expert ? "Expert" : "Defuser";
			string nextMover = _lastMoveInfo.CanMove ? "defuser" : "expert";
			if (_lastMoveInfo.Strike) Log("{0} moves {1} into the wall. Strike! Next move: {2}", prevMover, _lastMoveInfo.Direction, nextMover);
			else if (_lastMoveInfo.Solve) {
				Log("{0} moves {1} to {2}{3}. Module solved!", prevMover, _lastMoveInfo.Direction, (char)('A' + _lastMoveInfo.Position.X), _lastMoveInfo.Position.Y + 1);
			} else {
				Log("{0} moves {1} to {2}{3}. Next move: {4}", prevMover, _lastMoveInfo.Direction, (char)('A' + _lastMoveInfo.Position.X), _lastMoveInfo.Position.Y + 1,
					nextMover);
			}
			GameContainer.Move = _lastMoveInfo.CanMove;
			GameContainer.UpdateLocker();
			if (!_lastMoveInfo.Expert && _lastMoveInfo.Position != null) {
				GameContainer.MazeComponent.PlayerPosition = _lastMoveInfo.Position;
				GameContainer.MazeComponent.RenderPlayer();
			}
			if (_lastMoveInfo.Strike) Strike();
			if (_lastMoveInfo.Solve) {
				Solve();
				_client.WS.Close();
			}
		}
		if (Time.time - _lastPingTime > 1f) {
			if (_connected) _client.Ping();
			_lastPingTime = Time.time;
		}
	}

	private void UpdateState() {
		_lastPingTime = Time.time;
		if (_state == State.CONNECTION_ERROR) {
			Console.text = "NO CONNECTION";
			SolveButton.gameObject.SetActive(true);
		} else if (_state == State.REGISTRATION) {
			Console.text = "REGISTRATION";
		} else if (_state == State.WAITING_FOR_EXPERT) {
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
		} else if (_state == State.DISCONNECTED) {
			ExpertIdInput.gameObject.SetActive(false);
			GameContainer.gameObject.SetActive(false);
			ReconnectButton.gameObject.SetActive(true);
			Console.text = "DISCONNECTED";
		}
		_prevState = _state;
	}

	private void Reconnect() {
		if (IsSolved || _state != State.DISCONNECTED) return;
		ReconnectButton.gameObject.SetActive(false);
		_state = State.CONNECTON;
		Console.text = "RECONNECT";
		_client.WS.Connect();
	}

	private void OnMove(string direction) {
		if (IsSolved || !GameContainer.Move) return;
		GameContainer.Move = false;
		GameContainer.UpdateLocker();
		Log("Moving {0}...", direction);
		_client.ModuleMove(_gameId, direction, (result) => {
			_lastMoveInfo = new GameMoveInfo();
			_lastMoveInfo.IsNew = true;
			_lastMoveInfo.Expert = false;
			_lastMoveInfo.Direction = direction;
			_lastMoveInfo.Position = result.NewPosition;
			_lastMoveInfo.CanMove = result.Move == "module";
			_lastMoveInfo.Solve = result.Move == "none";
			_lastMoveInfo.Strike = false;
			_lastMoveInfo.Time = _lastTime;
		}, (reason) => {
			if (reason.Message != "moved into wall") {
				_lastException = reason;
				return;
			}
			_lastMoveInfo = new GameMoveInfo();
			_lastMoveInfo.IsNew = true;
			_lastMoveInfo.Expert = false;
			_lastMoveInfo.Direction = direction;
			_lastMoveInfo.Position = null;
			_lastMoveInfo.CanMove = reason.Move == "module";
			_lastMoveInfo.Solve = false;
			_lastMoveInfo.Strike = true;
			_lastMoveInfo.Time = _lastTime;
		});
	}

	private void OnDisconnectButtonPressed() {
		Log("Disconnecting expert...");
		_client.KickExpert(_gameId, (_) => {
			_logs.Enqueue("Expert disconnected");
			_state = State.WAITING_FOR_EXPERT;
		}, (reason) => {
			_lastException = reason;
			_state = State.WAITING_FOR_EXPERT;
		});
	}

	private void OnExpertNotFoundRetryPressed() {
		_state = State.WAITING_FOR_EXPERT;
		ExpertIdInput.ExpertId = "";
	}

	private void OnExpertIdSubmit() {
		ExpertIdInput.Disabled = true;
		Log("Connecting to expert {0}...", ExpertIdInput.ExpertId);
		_client.ConnectToExpert(_gameId, ExpertIdInput.ExpertId, (result) => {
			_logs.Enqueue("Expert connected");
			if (!_startLogged) {
				_logs.Enqueue(string.Format("Defuser maze: {0}", result.ModuleMaze.Select(s => s.Join(" ")).Join(";")));
				_logs.Enqueue(string.Format("Defuser start position: {0}{1}", (char)('A' + result.ModulePos.X), result.ModulePos.Y + 1));
				_logs.Enqueue(string.Format("Defuser finish position: {0}{1}", (char)('A' + result.ModuleFinish.X), result.ModuleFinish.Y + 1));
				_logs.Enqueue(string.Format("Expert maze: {0}", result.ExpertMaze.Select(s => s.Join(" ")).Join(";")));
				_logs.Enqueue(string.Format("Expert start position: {0}{1}", (char)('A' + result.ExpertPos.X), result.ExpertPos.Y + 1));
				_logs.Enqueue(string.Format("Expert finish position: {0}{1}", (char)('A' + result.ExpertFinish.X), result.ExpertFinish.Y + 1));
				_logs.Enqueue(string.Format("Next move: {0}", result.Move == "module" ? "defuser" : "expert"));
				_startLogged = true;
			}
			GameContainer.MazeComponent.Map = result.ExpertMaze;
			GameContainer.MazeComponent.PlayerPosition = result.ModulePos;
			GameContainer.MazeComponent.FinishPosition = result.ModuleFinish;
			GameContainer.Move = result.Move == "module";
			_state = State.IN_GAME;
		}, (reason) => {
			if (reason as string == "expert not found") _state = State.EXPERT_NOT_FOUND;
			else _lastException = reason;
		});
	}

	private void OnConnect() {
		_connected = true;
		switch (_state) {
			case State.CONNECTON:
				_logs.Enqueue("Connected to server. Registration...");
				_state = State.REGISTRATION;
				_client.CreateGame(OnGameCreated, (reason) => _lastException = reason);
				break;
			default: _lastException = "Unexpected connection"; break;
		}
	}

	private void OnGameCreated(ParallelMazesClient.CreateGameResponse game) {
		if (_state != State.REGISTRATION) {
			_lastException = "Unexpected game creation event";
			return;
		}
		_logs.Enqueue(string.Format("Game created: {0}", game.GameId));
		_gameId = game.GameId;
		_moduleKey = game.ModuleKey;
		_state = State.WAITING_FOR_EXPERT;
		_client.OnExpertMoved(_gameId, (e) => {
			_lastMoveInfo = new GameMoveInfo();
			_lastMoveInfo.IsNew = true;
			_lastMoveInfo.Expert = true;
			_lastMoveInfo.Direction = e.Direction;
			_lastMoveInfo.Position = e.NewExpertPosition;
			_lastMoveInfo.CanMove = e.Move == "module";
			_lastMoveInfo.Solve = e.Move == "none";
			_lastMoveInfo.Strike = e.Strike;
			_lastMoveInfo.Time = _lastTime;
		});
	}

	private void OnDisconnected() {
		_connected = false;
		switch (_state) {
			case State.CONNECTON:
				_lastException = "Unable to connect to server";
				_state = State.CONNECTION_ERROR;
				break;
			default:
				if (IsSolved) break;
				_lastException = "Connection closed";
				_state = State.DISCONNECTED;
				break;
		}
	}
}
