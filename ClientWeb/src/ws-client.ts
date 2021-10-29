import { Json, ServerResponse } from "./type";

export class WSClient {
	public get socket(): WebSocket { if (!this._socket) throw new Error("Not connected"); return this._socket; }
	public get whenConnected(): Promise<void> {
		if (!this.connectionPromise) throw new Error("Not connected");
		return this.connectionPromise;
	}

	private _socket: WebSocket | null = null;
	private nextRequestId: number = 0;
	private connectionPromise: Promise<void> | null = null;
	private onConnect: (() => void) | null = null;
	private callHandlers = new Map<number, { resolve: (response: unknown) => any, reject: (reason: unknown) => any }>();
	private eventHandlers = new Map<string, Set<(data: unknown) => any>>();

	constructor(
		public readonly url: string,
	) { }

	public async connect() {
		if (this._socket) return this.connectionPromise;
		this._socket = new WebSocket(this.url);
		this.connectionPromise = new Promise<void>((resolve) => this.onConnect = resolve);
		this._socket.onopen = () => this.onConnect!();
		this._socket.onmessage = (response) => {
			const raw = response.data?.toString();
			if (!raw) return;
			let json: ServerResponse;
			try { json = JSON.parse(raw); } catch (_) { return; }
			if (typeof json !== "object" || !json) return;
			if (json.type === "event") {
				if (!this.eventHandlers.has(json.event)) return;
				for (const handler of this.eventHandlers.get(json.event)!) handler(json.data);
			} else if (json.type === "success" || json.type === "error") {
				const { id } = json;
				if (!this.callHandlers.has(id)) return;
				const { resolve, reject } = this.callHandlers.get(id)!;
				this.callHandlers.delete(id);
				if (json.type === "success") resolve(json.result);
				else reject(json.reason);
			} else return;
		};
		await this.connectionPromise;
	}

	public async call<TResult = unknown>(method: string, args: Json): Promise<TResult> {
		const id = this.nextRequestId;
		this.nextRequestId += 1;
		const promise = new Promise((resolve, reject) => this.callHandlers.set(id, { resolve, reject }));
		await this.whenConnected;
		this._socket!.send(JSON.stringify({ id, method, args }));
		return promise as Promise<TResult>;
	}

	public async onEvent<TResult = unknown>(event: string, handler: (data: TResult) => any) {
		if (!this.eventHandlers.has(event)) this.eventHandlers.set(event, new Set());
		this.eventHandlers.get(event)!.add(handler as (data: unknown) => any);
	}

	public async removeEventHandler(event: string, handler: (data: unknown) => any) {
		if (!this.eventHandlers.has(event)) return;
		this.eventHandlers.get(event)!.delete(handler);
	}
}
