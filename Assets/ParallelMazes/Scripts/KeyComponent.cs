using UnityEngine;

public class KeyComponent : MonoBehaviour {
	public KMSelectable Selectable;
	public TextMesh LabelComponent;

	private char _label = '?'; public char Label { get { return _label; } set { if (_label == value) return; _label = value; UpdateLabel(); } }

	private void Start() {
		UpdateLabel();
	}

	private void UpdateLabel() {
		LabelComponent.text = "" + Label;
	}
}
