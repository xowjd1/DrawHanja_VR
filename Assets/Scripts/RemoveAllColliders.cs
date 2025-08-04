using UnityEngine;
using UnityEditor;

public class RemoveAllColliders : EditorWindow
{
    [MenuItem("Tools/Remove All Colliders From Selection %#r")]
    static void RemoveColliders()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        GameObject selected = Selection.activeGameObject;

        // 자식 + 자기 자신 + 부모 전부 포함한 Transform 리스트
        var allTransforms = selected.GetComponentsInChildren<Transform>(true);
        var current = selected.transform.parent;
        while (current != null)
        {
            System.Array.Resize(ref allTransforms, allTransforms.Length + 1);
            allTransforms[allTransforms.Length - 1] = current;
            current = current.parent;
        }

        int count = 0;
        foreach (Transform t in allTransforms)
        {
            Collider[] colliders = t.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                Undo.DestroyObjectImmediate(col);
                count++;
            }
        }

        Debug.Log($"Removed {count} colliders from hierarchy of: {selected.name}");
    }
}
