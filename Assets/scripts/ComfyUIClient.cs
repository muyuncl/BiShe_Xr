using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ComfyUI API 通信客户端
/// 负责与本地ComfyUI服务器进行HTTP通信
/// </summary>
public class ComfyUIClient : MonoBehaviour
{
    [Serializable]
    public class ComfyGeneratedFileInfo
    {
        public string filename;
        public string subfolder;
        public string type;
    }

    [Header("ComfyUI 设置")]
    [Tooltip("ComfyUI服务器地址")]
    public string serverUrl = "http://127.0.0.1:8188";
    
    [Tooltip("ComfyUI输出目录的绝对路径")]
    public string outputDirectory = @"D:\comfyui\ComfyUI-aki-v3\ComfyUI\output";

    [Header("API 工作流（火山 + Tripo 双图）")]
    [Tooltip("ComfyUI 导出的 API 格式 JSON（.json TextAsset）。仅运行时会改写其中两个 LoadImage 的 image 文件名。")]
    public TextAsset workflowApiJson;

    [Tooltip("线稿 LoadImage 节点 ID（API JSON 中的 key）")]
    public string sketchLoadImageNodeId = "59";

    [Tooltip("材质参考 LoadImage 节点 ID")]
    public string materialLoadImageNodeId = "61";

    private void Awake()
    {
#if UNITY_EDITOR
        if (workflowApiJson == null)
        {
            workflowApiJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Workflows/ComfySketchToModelApi.json");
        }
#endif
    }

    private void Reset()
    {
#if UNITY_EDITOR
        if (workflowApiJson == null)
            workflowApiJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Workflows/ComfySketchToModelApi.json");
#endif
    }

    /// <summary>
    /// 上传图片到 ComfyUI input；fileName 须含扩展名，mime 与内容一致。
    /// </summary>
    public IEnumerator UploadImage(byte[] imageData, string fileName, string mimeType, Action<string> onSuccess, Action<string> onError)
    {
        if (imageData == null || imageData.Length == 0)
        {
            onError?.Invoke("上传失败：图片数据为空");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            onError?.Invoke("上传失败：文件名为空");
            yield break;
        }

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("image", imageData, fileName, mimeType));

        using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/upload/image", formData))
        {
            request.timeout = 60;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                string usedName = fileName;
                if (TryParseUploadResponseName(response, out string serverName) && !string.IsNullOrEmpty(serverName))
                    usedName = serverName;

                Debug.Log($"图片上传成功: {usedName}");
                onSuccess?.Invoke(usedName);
            }
            else
            {
                string error = $"上传失败: {request.error}";
                Debug.LogError(error);
                onError?.Invoke(error);
            }
        }
    }

    /// <summary>
    /// 上传 PNG（默认草图）
    /// </summary>
    public IEnumerator UploadImage(byte[] imageData, Action<string> onSuccess, Action<string> onError)
    {
        string fileName = $"unity_photo_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        yield return UploadImage(imageData, fileName, "image/png", onSuccess, onError);
    }

    /// <summary>
    /// 仅替换两个 LoadImage 的 image 文件名，其余节点（含提示词、种子）保持 TextAsset 原样。
    /// </summary>
    public IEnumerator QueuePromptDualLoadImages(string sketchUploadedName, string materialUploadedName, Action<string> onSuccess, Action<string> onError)
    {
        if (workflowApiJson == null || string.IsNullOrWhiteSpace(workflowApiJson.text))
        {
            onError?.Invoke("未配置 workflowApiJson（请拖入 ComfySketchToModelApi.json 或你的 API 导出）");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(sketchUploadedName) || string.IsNullOrWhiteSpace(materialUploadedName))
        {
            onError?.Invoke("草图或材质上传文件名为空");
            yield break;
        }

        JObject workflow;
        try
        {
            workflow = JObject.Parse(workflowApiJson.text);
        }
        catch (Exception e)
        {
            onError?.Invoke($"解析工作流 JSON 失败: {e.Message}");
            yield break;
        }

        StripMetaRecursive(workflow);

        if (!TrySetLoadImageFileName(workflow, sketchLoadImageNodeId, sketchUploadedName, out string errSketch))
        {
            onError?.Invoke(errSketch);
            yield break;
        }

        if (!TrySetLoadImageFileName(workflow, materialLoadImageNodeId, materialUploadedName, out string errMat))
        {
            onError?.Invoke(errMat);
            yield break;
        }

        var body = new JObject { ["prompt"] = workflow };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body.ToString(Formatting.None));

        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/prompt", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 60;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"任务提交成功: {response}");
                string promptId = ExtractPromptId(response);
                if (!string.IsNullOrEmpty(promptId))
                    onSuccess?.Invoke(promptId);
                else
                    onError?.Invoke($"任务提交成功但未解析到 prompt_id，响应: {response}");
            }
            else
            {
                string err = $"任务提交失败: {request.error}\n{request.downloadHandler?.text}";
                Debug.LogError(err);
                onError?.Invoke(err);
            }
        }
    }

    private static void StripMetaRecursive(JToken token)
    {
        if (token is JObject obj)
        {
            obj.Remove("_meta");
            foreach (var p in obj.Properties().ToList())
                StripMetaRecursive(p.Value);
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
                StripMetaRecursive(item);
        }
    }

    private static bool TrySetLoadImageFileName(JObject workflow, string nodeId, string fileName, out string error)
    {
        error = null;
        if (!workflow.TryGetValue(nodeId, out JToken nodeTok) || nodeTok is not JObject node)
        {
            error = $"工作流中缺少节点 {nodeId}";
            return false;
        }

        if (node["class_type"]?.ToString() != "LoadImage")
        {
            error = $"节点 {nodeId} 不是 LoadImage";
            return false;
        }

        var inputs = node["inputs"] as JObject;
        if (inputs == null)
        {
            error = $"节点 {nodeId} 无 inputs";
            return false;
        }

        inputs["image"] = fileName;
        return true;
    }

    private static bool TryParseUploadResponseName(string responseJson, out string name)
    {
        name = null;
        if (string.IsNullOrWhiteSpace(responseJson))
            return false;
        try
        {
            var o = JObject.Parse(responseJson);
            name = o["name"]?.ToString();
            return !string.IsNullOrEmpty(name);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 轮询任务历史，等待并获取生成的GLB文件
    /// </summary>
    public IEnumerator WaitForPromptOutputGLB(string promptId, float maxWaitTime, Action<ComfyGeneratedFileInfo> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(promptId))
        {
            onError?.Invoke("prompt_id 为空，无法查询任务状态");
            yield break;
        }

        Debug.Log($"🔍 开始轮询 ComfyUI 历史任务，prompt_id={promptId}，最多等待 {maxWaitTime} 秒");
        float startTime = Time.time;

        while (Time.time - startTime < maxWaitTime)
        {
            string historyUrl = $"{serverUrl}/history/{UnityWebRequest.EscapeURL(promptId)}";
            using (UnityWebRequest request = UnityWebRequest.Get(historyUrl))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    if (TryExtractGLBFileInfoFromHistory(response, out ComfyGeneratedFileInfo fileInfo))
                    {
                        Debug.Log($"✅ 检测到GLB输出: {fileInfo.filename}");
                        onSuccess?.Invoke(fileInfo);
                        yield break;
                    }
                }
                else
                {
                    Debug.LogWarning($"查询 history 失败: {request.error}");
                }
            }

            float elapsed = Time.time - startTime;
            Debug.Log($"⏳ 任务未完成，已等待 {Mathf.FloorToInt(elapsed)} 秒");
            yield return new WaitForSeconds(2f);
        }
        onError?.Invoke($"等待任务输出超时（{maxWaitTime} 秒），prompt_id={promptId}");
    }

    /// <summary>
    /// 从ComfyUI下载文件
    /// </summary>
    public IEnumerator DownloadFile(string fileName, Action<byte[]> onSuccess, Action<string> onError)
    {
        string url = $"{serverUrl}/view?filename={UnityWebRequest.EscapeURL(fileName)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 60;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] data = request.downloadHandler.data;
                Debug.Log($"文件下载成功，大小: {data.Length} bytes");
                onSuccess?.Invoke(data);
            }
            else
            {
                string error = $"下载失败: {request.error}";
                Debug.LogError(error);
                onError?.Invoke(error);
            }
        }
    }

    public IEnumerator DownloadFile(ComfyGeneratedFileInfo fileInfo, Action<byte[]> onSuccess, Action<string> onError)
    {
        if (fileInfo == null || string.IsNullOrEmpty(fileInfo.filename))
        {
            onError?.Invoke("下载失败：文件信息为空");
            yield break;
        }

        string url = $"{serverUrl}/view?filename={UnityWebRequest.EscapeURL(fileInfo.filename)}";
        if (!string.IsNullOrEmpty(fileInfo.subfolder))
            url += $"&subfolder={UnityWebRequest.EscapeURL(fileInfo.subfolder)}";
        if (!string.IsNullOrEmpty(fileInfo.type))
            url += $"&type={UnityWebRequest.EscapeURL(fileInfo.type)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 60;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] data = request.downloadHandler.data;
                Debug.Log($"文件下载成功，大小: {data.Length} bytes, 文件: {fileInfo.filename}");
                onSuccess?.Invoke(data);
            }
            else
            {
                string error = $"下载失败: {request.error}";
                Debug.LogError(error);
                onError?.Invoke(error);
            }
        }
    }

    /// <summary>
    /// 下载相关的材质和贴图文件
    /// </summary>
    public IEnumerator DownloadRelatedFiles(string GLBFileName, Action<Dictionary<string, byte[]>> onSuccess, Action<string> onError)
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        
        // 下载GLB文件
        bool GLBSuccess = false;
        yield return DownloadFile(GLBFileName, (data) => 
        {
            files["GLB"] = data;
            GLBSuccess = true;
        }, onError);
        
        if (!GLBSuccess)
        {
            yield break;
        }
        
        // 尝试下载MTL文件
        string mtlFileName = GLBFileName.Replace(".glb", ".mtl");
        Debug.Log($"🔍 尝试下载材质文件: {mtlFileName}");
        
        yield return DownloadFile(mtlFileName, (data) => 
        {
            files["mtl"] = data;
            Debug.Log($"✅ MTL文件下载成功");
        }, (error) => 
        {
            Debug.LogWarning($"⚠️ MTL文件不存在或下载失败: {error}");
        });
        
        // 尝试下载贴图文件（常见的命名方式）
        string baseFileName = System.IO.Path.GetFileNameWithoutExtension(GLBFileName);
        string[] possibleTextures = new string[]
        {
            $"{baseFileName}.png",
            $"{baseFileName}_albedo.png",
            $"{baseFileName}_diffuse.png",
            $"{baseFileName}.jpg",
            "texture.png",
            "albedo.png"
        };
        
        foreach (string texName in possibleTextures)
        {
            Debug.Log($"🔍 尝试下载贴图: {texName}");
            bool found = false;
            
            yield return DownloadFile(texName, (data) => 
            {
                files["texture"] = data;
                files["textureName"] = System.Text.Encoding.UTF8.GetBytes(texName);
                Debug.Log($"✅ 贴图文件下载成功: {texName}");
                found = true;
            }, (error) => 
            {
                // 静默失败，继续尝试下一个
            });
            
            if (found)
                break;
        }
        
        onSuccess?.Invoke(files);
    }

    /// <summary>
    /// 测试ComfyUI连接
    /// </summary>
    public IEnumerator TestConnection(Action<bool> callback)
    {
        yield return TestConnectionDetailed((success, _) => callback?.Invoke(success));
    }

    /// <summary>
    /// 测试连接并返回详细错误信息（用于演示现场排障提示）
    /// </summary>
    public IEnumerator TestConnectionDetailed(Action<bool, string> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/system_stats"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success;
            if (success)
            {
                Debug.Log("ComfyUI连接成功！");
                callback?.Invoke(true, string.Empty);
            }
            else
            {
                Debug.LogError($"ComfyUI连接失败: {request.error}");
                string details = BuildConnectionHint(request.error);
                callback?.Invoke(false, details);
            }
        }
    }

    private string BuildConnectionHint(string requestError)
    {
        string baseMsg = $"地址: {serverUrl}\n错误: {requestError}";
        bool isAndroid = Application.platform == RuntimePlatform.Android;
        if (IsLoopbackUrl(serverUrl) && isAndroid)
        {
            return $"{baseMsg}\n检测到你在使用 localhost/127.0.0.1。Quest 真机请改成电脑局域网 IP，例如 http://192.168.1.100:8188";
        }

        return $"{baseMsg}\n请确认：1) ComfyUI 正在运行；2) 端口 8188 可访问；3) serverUrl 与当前运行环境匹配。";
    }

    private static bool IsLoopbackUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (url.Contains("localhost")) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return false;
        return uri.Host == "127.0.0.1" || uri.Host == "::1";
    }

    private static string ExtractPromptId(string responseJson)
    {
        try
        {
            var obj = JObject.Parse(responseJson);
            var token = obj["prompt_id"];
            return token?.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"解析 prompt_id 失败: {e.Message}");
            return null;
        }
    }

    private static bool TryExtractGLBFileInfoFromHistory(string historyJson, out ComfyGeneratedFileInfo fileInfo)
    {
        fileInfo = null;

        try
        {
            var root = JObject.Parse(historyJson);
            foreach (var promptEntry in root.Properties())
            {
                var outputs = promptEntry.Value?["outputs"] as JObject;
                if (outputs == null) continue;

                foreach (var node in outputs.Properties())
                {
                    var nodeOutput = node.Value as JObject;
                    if (nodeOutput == null) continue;

                    // 常见 Save Mesh as GLB 输出键：meshes
                    var meshArray = nodeOutput["meshes"] as JArray;
                    if (meshArray != null)
                    {
                        foreach (var item in meshArray)
                        {
                            string filename = item?["filename"]?.ToString();
                            if (!string.IsNullOrEmpty(filename) && filename.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                            {
                                fileInfo = new ComfyGeneratedFileInfo
                                {
                                    filename = filename,
                                    subfolder = item?["subfolder"]?.ToString(),
                                    type = item?["type"]?.ToString()
                                };
                                return true;
                            }
                        }
                    }

                    // 兜底：遍历节点输出中所有数组字段，找 filename=.glb
                    foreach (var child in nodeOutput.Properties())
                    {
                        if (child.Value is not JArray array) continue;
                        foreach (var item in array)
                        {
                            string filename = item?["filename"]?.ToString();
                            if (!string.IsNullOrEmpty(filename) && filename.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                            {
                                fileInfo = new ComfyGeneratedFileInfo
                                {
                                    filename = filename,
                                    subfolder = item?["subfolder"]?.ToString(),
                                    type = item?["type"]?.ToString()
                                };
                                return true;
                            }
                        }
                    }

                    // Tripo 等节点可能直接输出 .glb 路径字符串
                    foreach (var child in nodeOutput.Properties())
                    {
                        if (child.Value?.Type != JTokenType.String) continue;
                        string s = child.Value.ToString();
                        if (string.IsNullOrEmpty(s) || !s.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)) continue;
                        fileInfo = new ComfyGeneratedFileInfo
                        {
                            filename = System.IO.Path.GetFileName(s),
                            subfolder = "",
                            type = "output"
                        };
                        return true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析 history 输出失败: {e.Message}");
        }

        return false;
    }
}
