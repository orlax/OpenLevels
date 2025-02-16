using UnityEngine;
using UnityEditor;

public class GroupSelection
{
    // Adds a menu item under GameObject and binds it to Cmd+G (or Ctrl+G on Windows)
    [MenuItem("GameObject/Group %g", false, 0)]
    static void GroupSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
            return;

        // Calculate the center position of the selection
        Vector3 center = Vector3.zero;
        foreach (GameObject obj in selectedObjects)
        {
            center += obj.transform.position;
        }
        center /= selectedObjects.Length;

        // Create a new empty GameObject at the center
        GameObject group = new GameObject("Group");
        group.transform.position = center;
        Undo.RegisterCreatedObjectUndo(group, "Create Group");

        // Parent all selected objects to the new group, preserving their world positions
        foreach (GameObject obj in selectedObjects)
        {
            Undo.SetTransformParent(obj.transform, group.transform, "Group Objects");
        }

        // Set the new group as the active selection
        Selection.activeGameObject = group;
    }

    // Validate that there is at least one selected object before enabling the menu item
    [MenuItem("GameObject/Group %g", true)]
    static bool ValidateGroupSelectedObjects()
    {
        return Selection.activeTransform != null;
    }
}
