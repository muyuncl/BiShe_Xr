using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIFrameData))]
public class UIFrameDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var frame = (UIFrameData)target;
        var rounded = frame.GetComponent<RoundedGraphic>();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("同步圆角到 RoundedGraphic"))
        {
            if (rounded == null)
                rounded = Undo.AddComponent<RoundedGraphic>(frame.gameObject);

            Undo.RecordObject(rounded, "Sync Rounded Graphic");
            rounded.CornerRadius = frame.CornerRadius;
            EditorUtility.SetDirty(rounded);
        }
    }
}
