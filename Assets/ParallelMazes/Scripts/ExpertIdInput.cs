using System.Linq;
using UnityEngine;

public class ExpertIdInput : MonoBehaviour {
	public const int EXPERT_ID_LENGTH = 7;
	public const float INTERVAL = 0.02f;
	public const float OFFSET = 0.04f;

	public TextMesh ExpertIdComponent;
	public KMSelectable ClearButton;
	public KMSelectable SubmitButton;
	public KeyComponent KeyPrefab;

	public bool Disabled = false;
	public KeyComponent[] Keys;

	private string _expertId = "";
	public string ExpertId { get { return _expertId; } set { if (_expertId == value) return; _expertId = value; UpdateExpertIdInputText(); } }

	private void Start() {
		Init();
	}

	public void Init() {
		if (Keys != null) return;
		Keys = Enumerable.Range(0, 10).Select(i => {
			KeyComponent key = Instantiate(KeyPrefab);
			key.transform.parent = transform;
			int posIndex = (i + 9) % 10;
			float x = (posIndex % 5 - 2) * INTERVAL;
			float z = INTERVAL * (posIndex / 5);
			key.transform.localPosition = new Vector3(x, 0, -(OFFSET + z));
			key.transform.localRotation = Quaternion.identity;
			key.transform.localScale = Vector3.one;
			char c = (char)('0' + i);
			key.Label = c;
			key.Selectable.OnInteract += () => { OnKeyPressed(c); return false; };
			return key;
		}).ToArray();
		ClearButton.OnInteract += () => { OnClearPressed(); return false; };
	}

	private void OnKeyPressed(char c) {
		if (Disabled) return;
		if (ExpertId.Length >= EXPERT_ID_LENGTH) return;
		ExpertId += c;
	}

	private void OnClearPressed() {
		if (Disabled) return;
		ExpertId = "";
	}

	private void UpdateExpertIdInputText() {
		ExpertIdComponent.text = ExpertId.PadRight(EXPERT_ID_LENGTH, '.');
		ClearButton.gameObject.SetActive(ExpertId.Length > 0);
		SubmitButton.gameObject.SetActive(ExpertId.Length == EXPERT_ID_LENGTH);
	}
}
