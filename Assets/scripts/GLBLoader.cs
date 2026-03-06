using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using GLTFast;

/// <summary>
/// GLB模型加载器
/// 使用GLTFast插件加载GLB格式的3D模型
/// </summary>
public class GLBLoader : MonoBehaviour
{
    /// <summary>
    /// 从字节数组异步加载GLB模型
    /// </summary>
    public IEnumerator LoadGLBFromBytes(byte[] glbData, string modelName, Action<GameObject> onSuccess, Action<string> onError)
    {
        if (glbData == null || glbData.Length == 0)
        {
            Debug.LogError("❌ GLB数据为空！");
            onError?.Invoke("GLB数据为空");
            yield break;
        }

        Debug.Log($"📦 开始加载GLB模型，数据大小: {glbData.Length / 1024}KB");

        // 创建GLTFast实例
        var gltf = new GltfImport();

        // 启动加载任务
        Task<bool> loadTask = gltf.Load(glbData);
        
        // 等待任务完成
        while (!loadTask.IsCompleted)
        {
            yield return null;
        }

        if (!loadTask.Result)
        {
            Debug.LogError("❌ GLB加载失败！");
            onError?.Invoke("GLB加载失败");
            yield break;
        }

        Debug.Log("✅ GLB数据解析成功，正在实例化模型...");

        // 创建父物体
        GameObject modelParent = new GameObject(modelName);

        // 实例化模型到场景中
        var instantiator = new GameObjectInstantiator(gltf, modelParent.transform);
        Task<bool> instantiateTask = gltf.InstantiateMainSceneAsync(instantiator);
        
        // 等待实例化完成
        while (!instantiateTask.IsCompleted)
        {
            yield return null;
        }

        if (!instantiateTask.Result)
        {
            Debug.LogError("❌ GLB实例化失败！");
            Destroy(modelParent);
            onError?.Invoke("GLB实例化失败");
            yield break;
        }

        Debug.Log($"✅ GLB模型加载成功: {modelName}");
        
        // 输出模型信息
        MeshRenderer[] renderers = modelParent.GetComponentsInChildren<MeshRenderer>();
        MeshFilter[] filters = modelParent.GetComponentsInChildren<MeshFilter>();
        Debug.Log($"📊 模型包含: {renderers.Length} 个渲染器, {filters.Length} 个网格");

        onSuccess?.Invoke(modelParent);
    }

    /// <summary>
    /// 从本地文件路径加载GLB模型
    /// </summary>
    public IEnumerator LoadGLBFromFile(string filePath, string modelName, Action<GameObject> onSuccess, Action<string> onError)
    {
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"❌ 文件不存在: {filePath}");
            onError?.Invoke($"文件不存在: {filePath}");
            yield break;
        }

        Debug.Log($"📂 从文件加载GLB: {filePath}");

        byte[] glbData = null;
        try
        {
            glbData = System.IO.File.ReadAllBytes(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 读取文件失败: {e.Message}");
            onError?.Invoke($"读取文件失败: {e.Message}");
            yield break;
        }

        yield return LoadGLBFromBytes(glbData, modelName, onSuccess, onError);
    }
}
