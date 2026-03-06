using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ComfyUI API 通信客户端
/// 负责与本地ComfyUI服务器进行HTTP通信
/// </summary>
public class ComfyUIClient : MonoBehaviour
{
    [Header("ComfyUI 设置")]
    [Tooltip("ComfyUI服务器地址")]
    public string serverUrl = "http://127.0.0.1:8188";
    
    [Tooltip("ComfyUI输出目录的绝对路径")]
    public string outputDirectory = @"D:\comfyui\ComfyUI-aki-v3\ComfyUI\output";

    // 工作流JSON模板（从你提供的workflow复制）
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
  ""13"": {
    ""inputs"": {
      ""preview3d"": null,
      ""mesh"": [""12"", 0]
    },
    ""class_type"": ""TripoSRViewer""
  },
  ""14"": {
    ""inputs"": {
      ""model"": ""triposrmodel.ckpt"",
      ""chunk_size"": 8192
    },
    ""class_type"": ""TripoSRModelLoader""
  },
  ""15"": {
    ""inputs"": {
      ""image"": ""{IMAGE_NAME}""
    },
    ""class_type"": ""LoadImage""
  },
  ""17"": {
    ""inputs"": {
      ""rembg_session"": [""18"", 0],
      ""image"": [""15"", 0]
    },
    ""class_type"": ""ImageRemoveBackground+""
  },
  ""18"": {
    ""inputs"": {
      ""model"": ""u2net: general purpose"",
      ""providers"": ""CPU""
    },
    ""class_type"": ""RemBGSession+""
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
                onSuccess?.Invoke(response);
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
    /// 智能等待并获取最新生成的OBJ文件（支持实时检测）
    /// </summary>
    public IEnumerator WaitAndGetLatestOBJ(float maxWaitTime, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log($"🔍 开始监控output目录，最多等待 {maxWaitTime} 秒");
        
        if (!System.IO.Directory.Exists(outputDirectory))
        {
            onError?.Invoke($"输出目录不存在: {outputDirectory}");
            yield break;
        }

        // 记录开始时间和初始文件列表
        float startTime = Time.time;
        DateTime checkStartTime = DateTime.Now.AddSeconds(-2); // 往前推2秒，确保捕获到文件
        
        string foundFile = null;
        
        // 循环检测，每2秒检查一次
        while (Time.time - startTime < maxWaitTime)
        {
            string[] objFiles = System.IO.Directory.GetFiles(outputDirectory, "*.obj");
            
            // 查找在检测开始后创建的文件
            foreach (string file in objFiles)
            {
                DateTime fileTime = System.IO.File.GetLastWriteTime(file);
                if (fileTime > checkStartTime)
                {
                    foundFile = file;
                    Debug.Log($"✅ 检测到新生成的OBJ文件: {System.IO.Path.GetFileName(file)}");
                    Debug.Log($"📅 文件时间: {fileTime}");
                    break;
                }
            }
            
            if (foundFile != null)
                break;
            
            // 显示等待进度
            float elapsed = Time.time - startTime;
            Debug.Log($"⏳ 等待中... 已等待 {Mathf.FloorToInt(elapsed)} 秒");
            
            yield return new WaitForSeconds(2f);
        }
        
        // 如果没找到新文件，尝试获取最新的文件
        if (foundFile == null)
        {
            Debug.LogWarning("⚠️ 未检测到新文件，尝试获取最新的OBJ文件");
            
            try
            {
                string[] objFiles = System.IO.Directory.GetFiles(outputDirectory, "*.obj");
                
                if (objFiles.Length == 0)
                {
                    onError?.Invoke("未找到任何OBJ文件");
                    yield break;
                }

                // 找到最新的文件
                foundFile = objFiles[0];
                DateTime latestTime = System.IO.File.GetLastWriteTime(foundFile);

                foreach (string file in objFiles)
                {
                    DateTime fileTime = System.IO.File.GetLastWriteTime(file);
                    if (fileTime > latestTime)
                    {
                        latestTime = fileTime;
                        foundFile = file;
                    }
                }
                
                Debug.Log($"📁 使用最新的OBJ文件: {System.IO.Path.GetFileName(foundFile)}");
            }
            catch (Exception e)
            {
                onError?.Invoke($"查找OBJ文件时出错: {e.Message}");
                yield break;
            }
        }

        string fileName = System.IO.Path.GetFileName(foundFile);
        onSuccess?.Invoke(fileName);
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

    /// <summary>
    /// 下载相关的材质和贴图文件
    /// </summary>
    public IEnumerator DownloadRelatedFiles(string objFileName, Action<Dictionary<string, byte[]>> onSuccess, Action<string> onError)
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        
        // 下载OBJ文件
        bool objSuccess = false;
        yield return DownloadFile(objFileName, (data) => 
        {
            files["obj"] = data;
            objSuccess = true;
        }, onError);
        
        if (!objSuccess)
        {
            yield break;
        }
        
        // 尝试下载MTL文件
        string mtlFileName = objFileName.Replace(".obj", ".mtl");
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
        string baseFileName = System.IO.Path.GetFileNameWithoutExtension(objFileName);
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
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/system_stats"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success;
            if (success)
            {
                Debug.Log("ComfyUI连接成功！");
            }
            else
            {
                Debug.LogError($"ComfyUI连接失败: {request.error}");
            }
            callback?.Invoke(success);
        }
    }
}
