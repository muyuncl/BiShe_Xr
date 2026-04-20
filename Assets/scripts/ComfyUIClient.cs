using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

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
    public string serverUrl = "http://192.168.1.100:8188";
    
    [Tooltip("ComfyUI输出目录的绝对路径")]
    public string outputDirectory = @"D:\comfyui\ComfyUI-aki-v3\ComfyUI\output";

    // 工作流JSON模板（输出GLB格式）
    private const string WORKFLOW_TEMPLATE = @"{
  ""12"": {
    ""inputs"": {
      ""geometry_resolution"": 256,
      ""threshold"": 25,
      ""model"": [""14"", 0],
      ""reference_image"": [""17"", 0],
      ""reference_mask"": [""17"", 1]
    },
    ""class_type"": ""TripoSRSampler""
  },
  ""14"": {
    ""inputs"": {
      ""model"": ""triposrmodel.ckpt"",
      ""chunk_size"": 8192
    },
    ""class_type"": ""TripoSRModelLoader""
  },
  ""17"": {
    ""inputs"": {
      ""rembg_session"": [""18"", 0],
      ""image"": [""23"", 0]
    },
    ""class_type"": ""ImageRemoveBackground+""
  },
  ""18"": {
    ""inputs"": {
      ""model"": ""u2net: general purpose"",
      ""providers"": ""CUDA""
    },
    ""class_type"": ""RemBGSession+""
  },
  ""23"": {
    ""inputs"": {
      ""image"": ""{IMAGE_NAME}""
    },
    ""class_type"": ""LoadImage""
  },
  ""26"": {
    ""inputs"": {
      ""filename_prefix"": ""unity_output"",
      ""mesh"": [""12"", 0]
    },
    ""class_type"": ""Save Mesh as GLB""
  }
}";

    /// <summary>
    /// 上传图片到ComfyUI
    /// </summary>
    public IEnumerator UploadImage(byte[] imageData, Action<string> onSuccess, Action<string> onError)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"unity_photo_{timestamp}.png";

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("image", imageData, fileName, "image/png"));

        using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/upload/image", formData))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"图片上传成功: {fileName}");
                onSuccess?.Invoke(fileName);
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
    /// 提交工作流任务到ComfyUI
    /// </summary>
    public IEnumerator QueuePrompt(string imageName, Action<string> onSuccess, Action<string> onError)
    {
        // 替换工作流中的图片名称
        string workflow = WORKFLOW_TEMPLATE.Replace("{IMAGE_NAME}", imageName);

        // 构建prompt请求体
        string promptJson = $"{{\"prompt\": {workflow}}}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(promptJson);

        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/prompt", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"任务提交成功: {response}");
                string promptId = ExtractPromptId(response);
                if (!string.IsNullOrEmpty(promptId))
                {
                    onSuccess?.Invoke(promptId);
                }
                else
                {
                    string error = $"任务提交成功但未解析到 prompt_id，响应: {response}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
            }
            else
            {
                string error = $"任务提交失败: {request.error}";
                Debug.LogError(error);
                onError?.Invoke(error);
            }
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
        if (IsLoopbackUrl(serverUrl))
        {
            return $"{baseMsg}\n检测到你在使用 localhost/127.0.0.1。Quest 真机请改成电脑局域网 IP，例如 http://192.168.1.100:8188";
        }

        return $"{baseMsg}\n请确认：1) Quest 与电脑同一局域网；2) ComfyUI 正在运行；3) 防火墙放行 8188 端口。";
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
