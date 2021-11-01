export type Coord = { x: number; y: number };

export type Json = string | number | boolean | null | Json[] | { [x: string]: Json };

export type ServerResponse = FailedServerResponse | SuccessfulServerResponse | EventServerResponse;

export type Move = "expert" | "module" | "none";
export const AllMoves = ["expert", "module", "none"];

export interface FailedServerResponse<TReason = unknown> {
	readonly type: "error",
	readonly id: number;
	readonly reason: TReason;
};

export interface SuccessfulServerResponse<TResult = unknown> {
	readonly type: "success",
	readonly id: number,
	readonly success: true,
	readonly result: TResult,
};

export interface EventServerResponse<TEvent extends string = string, TData = unknown> {
	readonly type: "event",
	readonly event: TEvent,
	readonly data: TData,
}

export enum Method {
	CREATE_GAME = "create_game",
	CONNECT_TO_GAME = "connect_to_game",
	EXPERT_MOVE = "expert_move",
	PING = "ping",
}

export type Args<T extends Method> = T extends any ? {
	[Method.CREATE_GAME]: {},
	[Method.CONNECT_TO_GAME]: { game_id: string },
	[Method.EXPERT_MOVE]: { game_id: string, direction: string },
	[Method.PING]: null,
}[T] : never;

export type Result<T extends Method> = T extends any ? {
	[Method.CREATE_GAME]: { game_id: string, module_key: string },
	[Method.CONNECT_TO_GAME]: { pass: string },
	[Method.EXPERT_MOVE]: { move: Move, new_pos: Coord },
	[Method.PING]: boolean,
}[T] : never;
