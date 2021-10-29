export type Json = string | number | boolean | null | Json[] | { [x: string]: Json };

export type ServerResponse = FailedServerResponse | SuccessfulServerResponse | EventServerResponse;

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
	CREATE_GAME = "create_game"
}

export type Args<T extends Method> = T extends any ? {
	[Method.CREATE_GAME]: {},
}[T] : never;

export type Result<T extends Method> = T extends any ? {
	[Method.CREATE_GAME]: { game_id: string, module_key: string },
}[T] : never;
