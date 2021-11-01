using UnityEngine;

public class FinishComponent : MonoBehaviour {
	private float _angle;

	private void Start() {
		_angle = Random.Range(0f, 360f);
	}

	private void Update() {
		_angle += Time.deltaTime * 90f;
		transform.localRotation = Quaternion.Euler(0, _angle, 0);
	}
}
