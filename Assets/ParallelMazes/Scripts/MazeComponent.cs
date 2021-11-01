using System.Collections.Generic;
using UnityEngine;

public class MazeComponent : MonoBehaviour {
	public const int WIDTH = 7;
	public const int HEIGHT = 7;
	public const float PLAYER_Y_OFFSET = 0.0001f;
	public const float CELL_SIZE = 0.012f;
	public const float WALL_SIZE = 0.003f;

	public GameObject WallPrefab;
	public GameObject Player;
	public GameObject Finish;

	public int[][] Map;
	public ParallelMazesClient.Coord FinishPosition;
	public ParallelMazesClient.Coord PlayerPosition;

	private List<GameObject> _walls = new List<GameObject>();

	public void Render() {
		foreach (GameObject wall in _walls) Destroy(wall);
		_walls = new List<GameObject>();
		for (int x = 0; x < WIDTH; x++) {
			for (int z = 0; z < HEIGHT; z++) {
				int v = Map[x][z];
				if ((v & (1 << 1)) == 0) CreateWall(new Vector3(x * CELL_SIZE, 0f, -(z - 0.5f) * CELL_SIZE), false);
				if ((v & (1 << 2)) == 0) CreateWall(new Vector3((x - 0.5f) * CELL_SIZE, 0f, -z * CELL_SIZE), true);
				if (x + 1 == WIDTH && (v & (1 << 0)) == 0) CreateWall(new Vector3((x + 0.5f) * CELL_SIZE, 0f, -z * CELL_SIZE), true);
				if (z + 1 == HEIGHT && (v & (1 << 3)) == 0) CreateWall(new Vector3(x * CELL_SIZE, 0f, -(z + 0.5f) * CELL_SIZE), false);
			}
		}
		Finish.transform.localPosition = new Vector3(FinishPosition.X * CELL_SIZE, PLAYER_Y_OFFSET, -FinishPosition.Y * CELL_SIZE);
		RenderPlayer();
	}

	public void RenderPlayer() {
		Player.transform.localPosition = new Vector3(PlayerPosition.X * CELL_SIZE, PLAYER_Y_OFFSET, -PlayerPosition.Y * CELL_SIZE);
	}

	private void CreateWall(Vector3 pos, bool vert) {
		GameObject wall = Instantiate(WallPrefab);
		wall.transform.parent = transform;
		wall.transform.localPosition = pos;
		wall.transform.localScale = (vert ? new Vector3(WALL_SIZE, 1f, CELL_SIZE + WALL_SIZE) : new Vector3(CELL_SIZE + WALL_SIZE, 1f, WALL_SIZE)) / 10f;
		wall.transform.localRotation = Quaternion.identity;
		_walls.Add(wall);
	}
}
