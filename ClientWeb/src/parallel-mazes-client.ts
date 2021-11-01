import { AllMoves, Args, Coord, Method, Move, Result } from "./type";
import { WSClient } from "./ws-client";

export const ON_MODULE_CONNECTED_EVENT = "connected_to_game";
export const ON_MODULE_MOVED_EVENT = "module_moved";

export interface OnModuleConnectedEventData {
	game_id: string;
	expert_id: string;
	module_maze: number[][];
	expert_pos: Coord;
	expert_finish: Coord;
	move: Move;
}

export interface OnModuleMovedEventData {
	game_id: string;
	move: Move;
	strike: boolean;
}

export class ParallelMazesClient {
	private ws = new WSClient("wss://warm-wildwood-46578.herokuapp.com/");

	public async connect(): Promise<void> { await this.ws.connect(); this.pingJob(); }
	public async createGame(): Promise<{ gameId: string, moduleKey: string }> {
		return this.call(Method.CREATE_GAME, {}).then((res) => ({ gameId: res.game_id, moduleKey: res.module_key }));
	}

	public async loginExpert(gameId: string): Promise<{ pass: string }> {
		return this.call(Method.CONNECT_TO_GAME, { game_id: gameId });
	}

	public async makeAMove(gameId: string, direction: string): Promise<{ move: Move, new_pos: Coord, strike: false } | { move: Move, strike: true }> {
		try {
			return await this.call(Method.EXPERT_MOVE, { game_id: gameId, direction }).then((res) => ({ move: res.move, new_pos: res.new_pos, strike: false }));
		} catch (error) {
			if (typeof error !== "object" || !error || (error as any).message !== "moved into wall" || AllMoves.indexOf((error as any).move) < 0) throw error;
			return { move: (error as any).move as Move, strike: true };
		}
	}

	private async call<T extends Method>(method: T, args: Args<T>): Promise<Result<T>> {
		return this.ws.call(method, args);
	}

	public async onModuleConnected(f: (data: OnModuleConnectedEventData) => unknown) {
		this.ws.onEvent(ON_MODULE_CONNECTED_EVENT, (data) => f(data as OnModuleConnectedEventData));
	}

	public async onModuleMoved(f: (data: OnModuleMovedEventData) => unknown) {
		this.ws.onEvent(ON_MODULE_MOVED_EVENT, (data) => f(data as OnModuleMovedEventData));
	}

	private async pingJob() {
		while (true) {
			await new Promise<void>((resolve) => setTimeout(() => resolve(), 1e3));
			this.call(Method.PING, null);
		}
	}
}
