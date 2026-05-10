#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VRPhotoTo3DController))]
public class VRPhotoTo3DControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ctrl = (VRPhotoTo3DController)target;
        if (!ctrl.useDebugSketchFromLocal)
            return;

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("选择本地草图文件…", GUILayout.Height(24)))
            {
                string path = EditorUtility.OpenFilePanel(
                    "选择草图图片",
                    "",
                    "png,jpg,jpeg,bmp,webp");

                if (!string.IsNullOrEmpty(path))
                {
                    Undo.RecordObject(ctrl, "Set Debug Sketch Path");
                    ctrl.debugSketchAbsolutePath = path;
                    EditorUtility.SetDirty(ctrl);
                }
            }
        }

        EditorGUILayout.HelpBox(
            "勾选「Use Debug Sketch From Local」后：优先使用绝对路径原文件字节上传；" +
            "路径为空时使用 Debug Sketch Texture（会自动转为 PNG）。正式场景请关闭此项。",
            MessageType.Info);
    }
}
#endif
