using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldSpaceGlassPanel))]
public class WorldSpaceGlassPanelEditor : Editor
{
    SerializedProperty targetGraphic;
    SerializedProperty lightAngle;
    SerializedProperty lightIntensityPercent;
    SerializedProperty refraction;
    SerializedProperty depth;
    SerializedProperty dispersion;
    SerializedProperty frostPercent;
    SerializedProperty splay;
    SerializedProperty tint;

    void OnEnable()
    {
        targetGraphic = serializedObject.FindProperty("targetGraphic");
        lightAngle = serializedObject.FindProperty("lightAngle");
        lightIntensityPercent = serializedObject.FindProperty("lightIntensityPercent");
        refraction = serializedObject.FindProperty("refraction");
        depth = serializedObject.FindProperty("depth");
        dispersion = serializedObject.FindProperty("dispersion");
        frostPercent = serializedObject.FindProperty("frostPercent");
        splay = serializedObject.FindProperty("splay");
        tint = serializedObject.FindProperty("tint");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(targetGraphic);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("光（方向 + 强度）", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(lightAngle, new GUIContent("方向 (°)"));
        EditorGUILayout.Slider(lightIntensityPercent, 0f, 100f, new GUIContent("强度 (%)"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("折射", EditorStyles.boldLabel);
        EditorGUILayout.Slider(refraction, 0f, 1f, new GUIContent("强度"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("深度感", EditorStyles.boldLabel);
        EditorGUILayout.Slider(depth, 0f, 2f, new GUIContent("视差放大"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("色散", EditorStyles.boldLabel);
        EditorGUILayout.Slider(dispersion, 0f, 10f, new GUIContent("RGB 分离"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("磨砂模糊", EditorStyles.boldLabel);
        EditorGUILayout.Slider(frostPercent, 0f, 100f, new GUIContent("模糊 (%)"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("扩散", EditorStyles.boldLabel);
        EditorGUILayout.Slider(splay, 0f, 3f, new GUIContent("采样扩散"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("底色", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tint, new GUIContent("Tint (RGBA)"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "请在与 UI 相同的渲染相机上挂载 WorldSpaceGlassBackgroundCapture，并为本对象的 Image 指定材质（Shader：UI/World Space Glass）。Canvas 建议为 World Space，且 Sort Order 保证毛玻璃在希望看到的场景之后绘制。",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("Assets/Create/UI/World Space Glass Material", false, 201)]
    static void CreateGlassMaterial()
    {
        const string defaultPath = "Assets/Materials/UI_WorldSpaceGlass.mat";
        var shader = Shader.Find("UI/World Space Glass");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("World Space Glass", "未找到 Shader「UI/World Space Glass」。请确认 Assets/Shaders/UI/WorldSpaceGlassUI.shader 已导入。", "确定");
            return;
        }

        var mat = new Material(shader) { name = "UI_WorldSpaceGlass" };
        string path = EditorUtility.SaveFilePanelInProject("保存毛玻璃材质", mat.name, "mat", "", defaultPath);
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(mat);
    }
}
