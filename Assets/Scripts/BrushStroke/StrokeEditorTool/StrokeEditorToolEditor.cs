using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StrokeEditorTool))]
public class StrokeEditorToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StrokeEditorTool tool = (StrokeEditorTool)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Colliders"))
        {
            tool.GenerateStroke();
        }

        if (GUILayout.Button("Clear Children"))
        {
            tool.ClearChildren();
        }
    }
}
