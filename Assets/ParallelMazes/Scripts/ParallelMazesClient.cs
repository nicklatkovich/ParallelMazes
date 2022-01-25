using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ParallelMazesClient {
	public sealed class GameIdArguments {
		[JsonProperty(Required = Required.Always, PropertyName = "game_id")] public string GameId;
	}

	public sealed class CreateGameResponse {
		[JsonProperty(Required = Required.Always, PropertyName = "game_id")] public string GameId;
		[JsonProperty(Required = Required.Always, PropertyName = "module_key")] public string ModuleKey;
	}

	public sealed class Coord {
		[JsonProperty(Required = Required.Always, PropertyName = "x")] public int X;
		[JsonProperty(Required = Required.Always, PropertyName = "y")] public int Y;
	}

	public sealed class ConnectToExpertArguments {
		[JsonProperty(Required = Required.Always, PropertyName = "game_id")] public string GameId;
		[JsonProperty(Required = Required.Always, PropertyName = "expert_id")] public string ExpertId;
	}

	public sealed class ConnectToExpertResponse {
		[JsonProperty(Required = Required.Always, PropertyName = "module_maze")] public int[][] ModuleMaze;
		[JsonProperty(Required = Required.Always, PropertyName = "module_pos")] public Coord ModulePos;
		[JsonProperty(Required = Required.Always, PropertyName = "module_finish")] public Coord ModuleFinish;
		[JsonProperty(Required = Required.Always, PropertyName = "expert_maze")] public int[][] ExpertMaze;
		[JsonProperty(Required = Required.Always, PropertyName = "expert_pos")] public Coord ExpertPos;
		[JsonProperty(Required = Required.Always, PropertyName = "expert_finish")] public Coord ExpertFinish;
		[JsonProperty(Required = Required.Always, PropertyName = "move")] public string Move;
	}

	public sealed class ModuleMoveArguments {
		[JsonProperty(Required = Required.Always, PropertyName = "game_id")] public string GameId;
		[JsonProperty(Required = Required.Always, PropertyName = "direction")] public string Direction;
	}

	public sealed class ModuleMoveResponse {
		[JsonProperty(Required = Required.Always, PropertyName = "move")] public string Move;
		[JsonProperty(Required = Required.Always, PropertyName = "new_pos")] public Coord NewPosition;
	}

	public sealed class ModuleMoveErrorReason {
		[JsonProperty(Required = Required.Always, PropertyName = "message")] public string Message;
		[JsonProperty(Required = Required.Always, PropertyName = "move")] public string Move;
	}

	public sealed class ExpertMovedEvent {
		[JsonProperty(Required = Required.Always, PropertyName = "game_id")] public string GameId;
		[JsonProperty(Required = Required.Always, PropertyName = "move")] public string Move;
		[JsonProperty(Required = Required.Always, PropertyName = "strike")] public bool Strike;
		[JsonProperty(Required = Required.Always, PropertyName = "direction")] public string Direction;
		[JsonProperty(Required = Required.Always, PropertyName = "new_expert_pos")] public Coord NewExpertPosition;
	}

	public WSClient WS;

	// public ParallelMazesClient() { WS = new WSClient("ws://warm-wildwood-46578.herokuapp.com"); }
	public ParallelMazesClient() { WS = new WSClient("ws://127.0.0.1:3000"); }
	public void Connect() { WS.Connect(); }
	public void Ping() { WS.Call("ping", null, (_) => {}, (_) => {}); }

	public void CreateGame(System.Action<CreateGameResponse> success, System.Action<object> failure) {
		WS.Call("create_game", null, (result) => CastResponse<CreateGameResponse>(result, failure, success), failure);
	}

	public void ConnectToExpert(string gameId, string expertId, System.Action<ConnectToExpertResponse> success, System.Action<object> failure) {
		ConnectToExpertArguments args = new ConnectToExpertArguments();
		args.GameId = gameId;
		args.ExpertId = expertId;
		WS.Call("connect_to_expert", args, (result) => CastResponse<ConnectToExpertResponse>(result, failure, success), failure);
	}

	public void KickExpert(string gameId, System.Action<bool> success, System.Action<object> failure) {
		GameIdArguments args = new GameIdArguments();
		args.GameId = gameId;
		WS.Call("kick_expert", args, (_) => success(true), failure);
	}

	public void ModuleMove(string gameId, string direction, System.Action<ModuleMoveResponse> success, System.Action<ModuleMoveErrorReason> failure) {
		ModuleMoveArguments args = new ModuleMoveArguments();
		args.GameId = gameId;
		args.Direction = direction;
		System.Action<object> failureParser = (reason) => {
			CastResponse<ModuleMoveErrorReason>(reason, (reason1) => Debug.LogErrorFormat("module_move: {0}/{1}", reason, reason1), failure);
		};
		WS.Call("module_move", args, (result) => CastResponse<ModuleMoveResponse>(result, (reason) => {
			Debug.LogErrorFormat("module_move success parser error: {0}", reason);
		}, success), failureParser);
	}

	public void OnExpertMoved(string gameId, System.Action<ExpertMovedEvent> cb) {
		WS.On("expert_moved", (data) => CastResponse<ExpertMovedEvent>(data, (reason) => Debug.LogErrorFormat("on expert_moved: {0}", reason), cb));
	}

	private void CastResponse<T>(object response, System.Action<object> failure, System.Action<T> success) {
		JObject jobject = response as JObject;
		if (jobject == null) {
			failure("Unable to cast to JObject");
			return;
		}
		T result = jobject.ToObject<T>();
		if (result == null) failure("Unable cast JObject to expected type");
		else success(result);
	}
}
