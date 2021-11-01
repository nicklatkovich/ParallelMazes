import { MazeMap } from "./maze-map";
import { ParallelMazesClient } from "./parallel-mazes-client";

export class ExpertClient {
	private parmClient = new ParallelMazesClient();
	private readonly consoleDiv: HTMLDivElement;
	private readonly loginDiv: HTMLDivElement;
	private readonly loginInput: HTMLInputElement;
	private readonly loginButton: HTMLButtonElement;
	private readonly errorDiv: HTMLDivElement;
	private readonly errorLabel: HTMLDivElement;
	private readonly retryButton: HTMLButtonElement;
	private readonly mapTableContainer: HTMLElement;
	private readonly playerDiv: HTMLElement;
	private readonly finishDiv: HTMLElement;
	private readonly lockerDiv: HTMLDivElement;
	private readonly rightButton: HTMLElement;
	private readonly upButton: HTMLElement;
	private readonly leftButton: HTMLElement;
	private readonly downButton: HTMLElement;
	private readonly map = new MazeMap("map-table");
	private gameId: string = "";
	private expertId: string = "";

	constructor() {
		this.consoleDiv = document.getElementById("console-div") as HTMLDivElement;
		this.loginDiv = document.getElementById("login-div") as HTMLDivElement;
		this.loginInput = document.getElementById("login-input") as HTMLInputElement;
		this.loginButton = document.getElementById("login-button") as HTMLButtonElement;
		this.errorDiv = document.getElementById("error-div") as HTMLDivElement;
		this.errorLabel = document.getElementById("error-label") as HTMLDivElement;
		this.retryButton = document.getElementById("retry-button") as HTMLButtonElement;
		this.mapTableContainer = document.getElementById("map-table-container") as HTMLElement;
		this.playerDiv = document.getElementById("player") as HTMLElement;
		this.finishDiv = document.getElementById("finish") as HTMLElement;
		this.lockerDiv = document.getElementById("locker") as HTMLDivElement;
		this.rightButton = document.getElementById("right-button") as HTMLElement;
		this.upButton = document.getElementById("up-button") as HTMLElement;
		this.leftButton = document.getElementById("left-button") as HTMLElement;
		this.downButton = document.getElementById("down-button") as HTMLElement;
		this.consoleDiv.innerText = "CONNECTION";
		this.run();
	}

	private login(): void {
		this.consoleDiv.innerText = "LOGIN";
		this.loginDiv.style.display = "flex";
		this.loginButton.disabled = false;
		this.loginButton.onclick = async () => {
			this.loginButton.disabled = true;
			this.gameId = this.loginInput.value;
			try {
				this.expertId = await this.parmClient.loginExpert(this.gameId).then((res) => res.pass);
			} catch (error) {
				this.loginInput.value = "";
				this.loginDiv.style.display = "none";
				this.errorDiv.style.display = "flex";
				this.errorLabel.innerText = "GAME NOT FOUND";
				this.retryButton.onclick = () => {
					this.errorDiv.style.display = "none";
					this.login();
				};
				return;
			}
			this.loginDiv.style.display = "none";
			this.consoleDiv.innerText = `GAME ID: ${this.gameId}\nEXPERT: ${this.expertId}`;
		};
	}

	private async onMove(direction: string) {
		if (this.lockerDiv.innerText !== "MOVE") return;
		this.lockerDiv.innerText = "STOP";
		this.loginDiv.style.color = "red";
		const result = await this.parmClient.makeAMove(this.gameId, direction);
		this.lockerDiv.innerText = result.move === "expert" ? "MOVE" : "STOP";
		this.lockerDiv.style.color = result.move === "expert" ? "#0f0" : "red";
		if (!result.strike) {
			this.playerDiv.style.left = `${result.new_pos.x * 40 + 16}px`;
			this.playerDiv.style.top = `${result.new_pos.y * 40 + 16}px`;
		}
	}

	private async run(): Promise<void> {
		this.parmClient.onModuleConnected((data) => {
			this.mapTableContainer.style.display = "flex";
			this.map.data = data.module_maze;
			this.lockerDiv.innerText = data.move === "expert" ? "MOVE" : "STOP";
			this.lockerDiv.style.color = data.move === "expert" ? "#0f0" : "red";
			this.map.render();
			this.playerDiv.style.left = `${data.expert_pos.x * 40 + 16}px`;
			this.playerDiv.style.top = `${data.expert_pos.y * 40 + 16}px`;
			this.finishDiv.style.left = `${data.expert_finish.x * 40 + 12}px`;
			this.finishDiv.style.top = `${data.expert_finish.y * 40 + 12}px`;
			this.rightButton.onclick = () => this.onMove("right");
			this.upButton.onclick = () => this.onMove("up");
			this.leftButton.onclick = () => this.onMove("left");
			this.downButton.onclick = () => this.onMove("down");
		});
		this.parmClient.onModuleMoved((data) => {
			console.log(data);
			this.lockerDiv.innerText = data.move === "expert" ? "MOVE" : "STOP";
			this.lockerDiv.style.color = data.move === "expert" ? "#0f0" : "red";
		});
		await this.parmClient.connect();
		this.login();
	}
}
