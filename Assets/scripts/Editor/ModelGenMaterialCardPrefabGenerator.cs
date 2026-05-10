using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成 / 更新材质卡片预制体，便于在 Prefab 模式下微调布局。
/// </summary>
public static class ModelGenMaterialCardPrefabGenerator
{
    [MenuItem("Tools/UI/Create or Update ModelGen Material Card Prefab")]
    public static void CreateOrUpdatePrefab()
    {
        EnsurePrefabExists(forceRebuild: true);
        Debug.Log($"已写入预制体: {ModelGenMaterialCardPrefabBuilder.PrefabPath}");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelGenMaterialCardPrefabBuilder.PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    /// <summary>
    /// 工程中尚无预制体时创建一份；forceRebuild 时用当前 Builder 覆盖预制体。
    /// </summary>
    public static void EnsurePrefabExists(bool forceRebuild = false)
    {
        string path = ModelGenMaterialCardPrefabBuilder.PrefabPath;
        string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        if (!forceRebuild && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;

        var root = ModelGenMaterialCardPrefabBuilder.BuildCard(parent: null);
        root.name = "ModelGenMaterialCard";

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
