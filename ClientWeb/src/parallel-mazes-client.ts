import { Args, Method, Result } from "./type";
import { WSClient } from "./ws-client";

export class ParallelMazesClient {
	private ws = new WSClient("ws://127.0.0.1:3000");

	public async connect(): Promise<void> { await this.ws.connect(); }
	public async createGame(): Promise<{ gameId: string, moduleKey: string }> {
		return this.call(Method.CREATE_GAME, {}).then((res) => ({ gameId: res.game_id, moduleKey: res.module_key }));
	}

	private async call<T extends Method>(method: T, args: Args<T>): Promise<Result<T>> {
		return this.ws.call(method, args);
	}
}
