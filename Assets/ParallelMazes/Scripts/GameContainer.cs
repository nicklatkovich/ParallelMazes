using UnityEngine;

public class GameContainer : MonoBehaviour {
	public TextMesh Locker;
	public MazeComponent MazeComponent;
	public KMSelectable DirectionButtonPrefab;
	public KMSelectable DisconnectButton;

	public bool Move = false;
	public KMSelectable RightButton;
	public KMSelectable UpButton;
	public KMSelectable LeftButton;
	public KMSelectable DownButton;

	private void Start() {
		Init();
	}

	public void Init() {
		if (RightButton != null) return;
		RightButton = CreateDirectionButton(new Vector2(7.5f, 3), 0);
		UpButton = CreateDirectionButton(new Vector2(3, -1.5f), -90);
		LeftButton = CreateDirectionButton(new Vector2(-1.5f, 3), 180);
		DownButton = CreateDirectionButton(new Vector2(3, 7.5f), 90);
		UpdateLocker();
	}

	private KMSelectable CreateDirectionButton(Vector2 pos, float angle) {
		KMSelectable result = Instantiate(DirectionButtonPrefab);
		result.transform.parent = transform;
		result.transform.localPosition = MazeComponent.transform.localPosition + new Vector3(pos.x * MazeComponent.CELL_SIZE, 0, -pos.y * MazeComponent.CELL_SIZE);
		result.transform.localScale = Vector3.one;
		result.transform.localRotation = Quaternion.Euler(0, angle, 0);
		return result;
	}

	public void UpdateLocker() {
		Locker.text = Move ? "MOVE" : "STOP";
		Locker.color = Move ? Color.green : Color.red;
	}
}
