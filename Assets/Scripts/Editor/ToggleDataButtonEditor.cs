using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ToggleDataButton))]
public class ToggleDataButtonEditor : ButtonEditor {
	private ToggleDataButton _button;

	protected override void OnEnable()
	{
		_button = (ToggleDataButton)target;
		base.OnEnable();
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Enabled");
		_button.isEnabled = EditorGUILayout.Toggle(_button.isEnabled);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Visualizer Name");
		_button.visualizerName = EditorGUILayout.TextField(_button.visualizerName);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Enabled Image");
		_button.enabledImage = (Sprite)EditorGUILayout.ObjectField(_button.enabledImage, typeof(Sprite));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Disabled Image");
		_button.disabledImage = (Sprite)EditorGUILayout.ObjectField(_button.disabledImage, typeof(Sprite));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		base.OnInspectorGUI();
	}
}
