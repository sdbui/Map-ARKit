using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(TogglePlacementButton))]
public class TogglePlacementButtonEditor : ButtonEditor {
    private TogglePlacementButton _button;

    protected override void OnEnable()
    {
        _button = (TogglePlacementButton)target;
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Locked Image");
        _button.lockedImage = (Sprite)EditorGUILayout.ObjectField(_button.lockedImage, typeof(Sprite));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Unlocked Image");
        _button.unlockedImage = (Sprite)EditorGUILayout.ObjectField(_button.unlockedImage, typeof(Sprite));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        base.OnInspectorGUI();
    }
}
