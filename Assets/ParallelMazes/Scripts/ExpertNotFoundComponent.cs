using UnityEngine;

public class ExpertNotFoundComponent : MonoBehaviour {
	public TextMesh TextComponent;
	public KMSelectable RetryButton;

	private string _expertId = "";
	public string ExpertId { get { return _expertId; } set { if (_expertId == value) return; _expertId = value; UpdateText(); } }

	private void Start() {
		UpdateText();
	}

	private void UpdateText() {
		TextComponent.text = string.Format("EXPERT {0}\nNOT FOUND", ExpertId);
	}
}
