using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

sealed class ServerResponse {
	[JsonProperty(Required = Required.Always, PropertyName = "type")] public string Type;
	[JsonProperty("id")] public System.Nullable<int> Id;
	[JsonProperty("result")] public object Result;
	[JsonProperty("reason")] public object Reason;
	[JsonProperty("event")] public string Event;
	[JsonProperty("data")] public object Data;
}

sealed class ServerCall {
	[JsonProperty(Required = Required.Always, PropertyName = "id")] public int Id;
	[JsonProperty(Required = Required.Always, PropertyName = "method")] public string Method;
	[JsonProperty("args")] public object Args;
}

sealed class ResponseHandler {
	public System.Action<object> Success;
	public System.Action<object> Failure;
}

public class WSClient {
	public event OnOpenHandler OnOpen; public delegate void OnOpenHandler();
	public event OnCloseHandler OnClose; public delegate void OnCloseHandler();
	public event OnErrorHandler OnError; public delegate void OnErrorHandler(string message);

	private int _nextRequestId = 0;
	private WebSocket _socket;
	private Dictionary<int, ResponseHandler> _callHandlers = new Dictionary<int, ResponseHandler>();
	public readonly Dictionary<string, List<System.Action<object>>> _eventHandlers = new Dictionary<string, List<System.Action<object>>>();

	public WSClient(string url) {
		_socket = new WebSocket(url);
		// Debug.Log(_socket.SslConfiguration.EnabledSslProtocols);
		// _socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Ssl3;
		// Debug.Log(_socket.SslConfiguration.EnabledSslProtocols);
		_socket.OnOpen += (s, e) => { OnOpen.Invoke(); };
		_socket.OnClose += (s, e) => { Debug.LogError(e.Reason); Debug.LogError(e.Code); Debug.LogError(e.Code); Debug.LogError(e.WasClean); OnClose.Invoke(); };
		_socket.OnError += (s, e) => { OnError.Invoke(e.Message); };
		_socket.OnMessage += (s, e) => {
			try {
				if (e.IsBinary) throw new System.Exception("Unexpected binary server response");
				ServerResponse response;
				response = JsonConvert.DeserializeObject<ServerResponse>(e.Data);
				if (response.Type == "event") {
					if (!_eventHandlers.ContainsKey(response.Event)) {
						Debug.LogErrorFormat("Unprocessable event {0}", response.Event);
						return;
					}
					foreach (System.Action<object> handler in _eventHandlers[response.Event]) handler(response.Data);
				} else if (response.Type == "success" || response.Type == "error") {
					if (response.Id == null) return;
					if (!_callHandlers.ContainsKey(response.Id.Value)) return;
					ResponseHandler handler = _callHandlers[response.Id.Value];
					if (handler == null) return;
					if (response.Type == "success") handler.Success(response.Result);
					else handler.Failure(response.Reason);
				} else throw new System.Exception("Unexpected type of server response");
			} catch (System.Exception exc) {
				Debug.LogError(exc);
			}
		};
	}

	public void Close() {
		_socket.CloseAsync();
	}

	public void Ping() {
		_socket.Ping();
	}

	public void Call(string method, object args, System.Action<object> success, System.Action<object> failure) {
		ServerCall callParams = new ServerCall();
		callParams.Id = _nextRequestId;
		_nextRequestId += 1;
		callParams.Method = method;
		callParams.Args = args;
		ResponseHandler handler = new ResponseHandler();
		handler.Success = success;
		handler.Failure = failure;
		_callHandlers.Add(callParams.Id, handler);
		_socket.SendAsync(JsonConvert.SerializeObject(callParams), (sent) => { });
	}

	public void Connect() { _socket.ConnectAsync(); }

	public void On(string eventName, System.Action<object> cb) {
		if (!_eventHandlers.ContainsKey(eventName)) _eventHandlers.Add(eventName, new List<System.Action<object>>());
		_eventHandlers[eventName].Add(cb);
	}
}
