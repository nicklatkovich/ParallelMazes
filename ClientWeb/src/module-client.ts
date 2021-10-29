import { ParallelMazesClient } from "./parallel-mazes-client";

enum State { NOT_INITED, CONNECTING, CREATING_GAME, WAITING_FOR_EXPERT }

export class ModuleClient {
	private parmClient = new ParallelMazesClient();
	private state: State = State.NOT_INITED;
	private gameId: string = "";
	private readonly displayText: HTMLDivElement;

	constructor() {
		this.state = State.CONNECTING;
		this.displayText = document.getElementById("display-text") as HTMLDivElement;
		this.render();
		this.run();
	}

	private async run(): Promise<void> {
		await this.parmClient.connect();
		this.state = State.CREATING_GAME;
		this.render();
		const { gameId } = await this.parmClient.createGame();
		this.state = State.WAITING_FOR_EXPERT;
		this.gameId = gameId;
		this.render();
	}

	private render(): void {
		switch (this.state) {
			case State.CONNECTING: this.displayText.innerText = "Connecting..."; break;
			case State.CREATING_GAME: this.displayText.innerText = "Creating game..."; break;
			case State.WAITING_FOR_EXPERT: this.displayText.innerText = `Game Id: ${this.gameId}`; break;
		}
	}
}
