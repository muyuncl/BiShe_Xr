using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    [Header("History 渲染图回显（PreviewImage / Volcano 等）")]
    [Tooltip("在等待 GLB 的同一轮询里，若 history 中出现下列节点的 images 输出则尝试下载（按顺序优先）")]
    public bool enableRenderImagePolling = true;

    [Tooltip("默认：66=SaveImage（history 最稳），57=PreviewImage，58=Volcano。若你图里 SaveImage 不是 66 请改；勿与 Rodin 等节点 id 冲突")]
    public string[] renderImageHistoryNodeIds = { "66", "57", "58" };

    [Tooltip("勾选后：当 history 已有 outputs 但仍解析不到图片时，打一条一次性提示（含节点 id），便于排查")]
    public bool logRenderImageHistoryDebug = true;

    [Tooltip("与 history 并行：扫描 outputDirectory 下在本次任务开始后新生成的 PNG（不依赖 /history 的 images 字段）")]
    public bool enableRenderImageOutputDirectoryPoll = true;

    [Tooltip("只认该前缀的文件名，如 SaveImage 默认前缀 ComfyUI_ → ComfyUI_00001_.png；留空则任意 PNG（更易误匹配）")]
    public string renderImageFilenamePrefixFilter = "ComfyUI_";

    [Header("Comfy.org API 节点（Rodin / Tripo 等）")]
    [Tooltip("网页端登录后，浏览器 F12 → Network → 点一次「Queue Prompt」→ 请求体里的 extra_data.auth_token_comfy_org（不要带 Bearer 前缀）。勿提交到公开仓库。")]
    public string authTokenComfyOrg;

    [Tooltip("可选：Comfy.org 账户 API Key，对应 extra_data.api_key_comfy_org；与上面二选一即可。")]
    public string apiKeyComfyOrg;

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
        var extraData = new JObject();
        if (!string.IsNullOrWhiteSpace(authTokenComfyOrg))
            extraData["auth_token_comfy_org"] = authTokenComfyOrg.Trim();
        if (!string.IsNullOrWhiteSpace(apiKeyComfyOrg))
            extraData["api_key_comfy_org"] = apiKeyComfyOrg.Trim();
        if (extraData.Count > 0)
            body["extra_data"] = extraData;

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
    /// 轮询任务历史，等待并获取生成的GLB文件。
    /// 可选：同一轮询中解析 PreviewImage / Volcano 等节点的 <c>images</c> 输出，下载 PNG/JPEG 后回调（用于 UI 回显）。
    /// </summary>
    public IEnumerator WaitForPromptOutputGLB(
        string promptId,
        float maxWaitTime,
        Action<ComfyGeneratedFileInfo> onSuccess,
        Action<string> onError,
        Action<byte[]> onRenderImageBytesDownloaded = null)
    {
        if (string.IsNullOrEmpty(promptId))
        {
            onError?.Invoke("prompt_id 为空，无法查询任务状态");
            yield break;
        }

        Debug.Log($"🔍 开始轮询 ComfyUI 历史任务，prompt_id={promptId}，最多等待 {maxWaitTime} 秒");
        float startTime = Time.time;
        DateTime pollStartUtc = DateTime.UtcNow;
        HashSet<string> existingGlbAtStart = SnapshotExistingGlbNames();
        HashSet<string> existingPngAtStart = SnapshotExistingPngPaths();
        string lastEmittedRenderSignature = null;
        string lastShownRenderImageContentKey = null;
        bool loggedMissingImageHint = false;

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

                    if (enableRenderImagePolling && onRenderImageBytesDownloaded != null)
                    {
                        if (TryExtractFirstImageFileInfoFromHistory(response, renderImageHistoryNodeIds, out ComfyGeneratedFileInfo imageInfo))
                        {
                            string sig = $"{imageInfo.filename}|{imageInfo.subfolder}|{imageInfo.type}";
                            if (!string.Equals(sig, lastEmittedRenderSignature, StringComparison.Ordinal))
                            {
                                byte[] imgBytes = null;
                                string imgErr = null;
                                yield return DownloadHistoryArtifactBytes(imageInfo, b => imgBytes = b, e => imgErr = e);
                                if (imgBytes != null && imgBytes.Length > 0 && LooksLikeImageFile(imgBytes))
                                {
                                    lastEmittedRenderSignature = sig;
                                    string contentKey = $"{imageInfo.filename.ToLowerInvariant()}|{imgBytes.Length}";
                                    if (!string.Equals(contentKey, lastShownRenderImageContentKey, StringComparison.Ordinal))
                                    {
                                        lastShownRenderImageContentKey = contentKey;
                                        onRenderImageBytesDownloaded.Invoke(imgBytes);
                                        Debug.Log($"[ComfyUIClient] 渲染图已下载(history): {imageInfo.filename} ({imgBytes.Length} bytes)");
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(imgErr))
                                        Debug.LogWarning($"渲染图下载失败: {imgErr}");
                                    else if (imgBytes != null && imgBytes.Length > 0)
                                        Debug.LogWarning($"渲染图内容与图片魔数不符（可能为错误页/HTML），len={imgBytes.Length}");
                                }
                            }
                        }
                        else if (logRenderImageHistoryDebug &&
                                 TryGetNonEmptyHistoryOutputNodeKeys(response, out string nodeKeys))
                        {
                            if (!loggedMissingImageHint)
                            {
                                loggedMissingImageHint = true;
                                Debug.Log($"[ComfyUIClient] history 已有 outputs（节点: {nodeKeys}），但未解析到可下载的渲染图。请核对 renderImageHistoryNodeIds 与 Comfy 中 SaveImage 节点 id 一致，并在 VRPhotoTo3DController 绑定 RawImage。");
                            }
                        }
                    }

                    if (TryExtractGLBFileInfoFromHistory(response, out ComfyGeneratedFileInfo fileInfo))
                    {
                        Debug.Log($"✅ 检测到GLB输出: {fileInfo.filename}");
                        onSuccess?.Invoke(fileInfo);
                        yield break;
                    }
                    // history 成功返回但未解析到 GLB，兜底扫描输出目录
                    if (TryFindLatestGlbFromOutputDir(pollStartUtc, existingGlbAtStart, out ComfyGeneratedFileInfo dirFileInfo))
                    {
                        Debug.Log($"✅ history 未命中，目录兜底命中GLB: {dirFileInfo.filename}");
                        onSuccess?.Invoke(dirFileInfo);
                        yield break;
                    }
                }
                else
                {
                    Debug.LogWarning($"查询 history 失败: {request.error}");
                    if (TryFindLatestGlbFromOutputDir(pollStartUtc, existingGlbAtStart, out ComfyGeneratedFileInfo dirFileInfo))
                    {
                        Debug.Log($"✅ history 请求失败，目录兜底命中GLB: {dirFileInfo.filename}");
                        onSuccess?.Invoke(dirFileInfo);
                        yield break;
                    }
                }
            }

            if (enableRenderImagePolling && onRenderImageBytesDownloaded != null && enableRenderImageOutputDirectoryPoll &&
                TryReadLatestNewRenderImageBytesFromOutputDir(
                    pollStartUtc, existingPngAtStart, renderImageFilenamePrefixFilter,
                    lastShownRenderImageContentKey, out byte[] diskPng, out string diskContentKey))
            {
                lastShownRenderImageContentKey = diskContentKey;
                onRenderImageBytesDownloaded.Invoke(diskPng);
                Debug.Log($"[ComfyUIClient] 渲染图已从 output 目录读取: {diskContentKey.Split('|')[0]} ({diskPng.Length} bytes)");
            }

            float elapsed = Time.time - startTime;
            Debug.Log($"⏳ 任务未完成，已等待 {Mathf.FloorToInt(elapsed)} 秒");
            yield return new WaitForSeconds(2f);
        }
        onError?.Invoke($"等待任务输出超时（{maxWaitTime} 秒），prompt_id={promptId}");
    }

    private HashSet<string> SnapshotExistingPngPaths()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
                return set;
            foreach (var file in Directory.GetFiles(outputDirectory, "*.png", SearchOption.AllDirectories))
                set.Add(Path.GetFullPath(file));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"扫描输出目录 PNG 快照失败: {e.Message}");
        }
        return set;
    }

    /// <summary>
    /// 在 Comfy output 目录中查找「本次任务开始后」写入的、文件名前缀匹配的 PNG，读取字节。
    /// </summary>
    private bool TryReadLatestNewRenderImageBytesFromOutputDir(
        DateTime pollStartUtc,
        HashSet<string> existingPngAtStart,
        string filenamePrefix,
        string lastShownContentKey,
        out byte[] data,
        out string contentKey)
    {
        data = null;
        contentKey = null;
        try
        {
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
                return false;

            string prefix = filenamePrefix?.Trim() ?? "";

            FileInfo candidate = Directory.GetFiles(outputDirectory, "*.png", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .Where(fi => fi.Exists)
                .Where(fi => string.IsNullOrEmpty(prefix) || fi.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Where(fi => existingPngAtStart == null || !existingPngAtStart.Contains(fi.FullName) || fi.LastWriteTimeUtc >= pollStartUtc.AddSeconds(-2))
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .FirstOrDefault();

            if (candidate == null)
                return false;

            string tentativeKey = $"{candidate.Name.ToLowerInvariant()}|{candidate.Length}";
            if (string.Equals(tentativeKey, lastShownContentKey, StringComparison.Ordinal))
                return false;

            byte[] raw = File.ReadAllBytes(candidate.FullName);
            if (raw == null || raw.Length == 0 || !LooksLikeImageFile(raw))
                return false;

            contentKey = $"{candidate.Name.ToLowerInvariant()}|{raw.Length}";
            if (string.Equals(contentKey, lastShownContentKey, StringComparison.Ordinal))
                return false;

            data = raw;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"输出目录读取渲染图 PNG 失败: {e.Message}");
            return false;
        }
    }

    private HashSet<string> SnapshotExistingGlbNames()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
                return set;
            foreach (var file in Directory.GetFiles(outputDirectory, "*.glb", SearchOption.AllDirectories))
                set.Add(Path.GetFullPath(file));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"扫描输出目录初始快照失败: {e.Message}");
        }
        return set;
    }

    private bool TryFindLatestGlbFromOutputDir(DateTime pollStartUtc, HashSet<string> existingGlbAtStart, out ComfyGeneratedFileInfo fileInfo)
    {
        fileInfo = null;
        try
        {
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
                return false;

            var candidate = Directory.GetFiles(outputDirectory, "*.glb", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .Where(fi => fi.Exists)
                .Where(fi => existingGlbAtStart == null || !existingGlbAtStart.Contains(fi.FullName) || fi.LastWriteTimeUtc >= pollStartUtc.AddSeconds(-2))
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .FirstOrDefault();

            if (candidate == null)
                return false;

            fileInfo = BuildFileInfoFromAbsolutePath(candidate.FullName);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"目录兜底查找GLB失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 直接读取输出目录里最新的 glb 文件字节（本地兜底，不走 /view 下载）。
    /// </summary>
    public bool TryReadLatestGlbBytesFromOutputDir(out byte[] glbData, out string filePath, out string error)
    {
        glbData = null;
        filePath = null;
        error = null;
        try
        {
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
            {
                error = $"输出目录不可用: {outputDirectory}";
                return false;
            }

            var latest = Directory.GetFiles(outputDirectory, "*.glb", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .Where(fi => fi.Exists)
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latest == null)
            {
                error = "输出目录中未找到 .glb 文件";
                return false;
            }

            byte[] data = File.ReadAllBytes(latest.FullName);
            if (!LooksLikeGlb(data))
            {
                error = $"最新文件不是有效GLB: {latest.FullName}";
                return false;
            }

            glbData = data;
            filePath = latest.FullName;
            return true;
        }
        catch (Exception e)
        {
            error = $"读取最新GLB失败: {e.Message}";
            return false;
        }
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

        // ComfyUI 不同节点/版本的 view 参数组合有差异，这里多策略兜底。
        List<string> urls = BuildViewCandidateUrls(fileInfo);
        string lastError = null;
        foreach (var url in urls)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 60;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    lastError = $"下载失败: {request.error}, url={url}";
                    continue;
                }

                byte[] data = request.downloadHandler.data;
                if (!LooksLikeGlb(data))
                {
                    string preview = SafePreviewText(data, 120);
                    lastError = $"下载内容不是GLB二进制, url={url}, 预览={preview}";
                    Debug.LogWarning(lastError);
                    continue;
                }

                Debug.Log($"文件下载成功，大小: {data.Length} bytes, 文件: {fileInfo.filename}");
                onSuccess?.Invoke(data);
                yield break;
            }
        }

        Debug.LogError(lastError ?? "下载失败：所有候选 URL 都未成功返回 GLB");
        onError?.Invoke(lastError ?? "下载失败：所有候选 URL 都未成功返回 GLB");
    }

    private List<string> BuildViewCandidateUrls(ComfyGeneratedFileInfo fileInfo)
    {
        string escapedName = UnityWebRequest.EscapeURL(fileInfo.filename);
        string escapedSub = UnityWebRequest.EscapeURL(fileInfo.subfolder ?? "");
        string escapedType = UnityWebRequest.EscapeURL(fileInfo.type ?? "");
        var list = new List<string>();

        // 1) 完整参数
        if (!string.IsNullOrEmpty(fileInfo.subfolder) && !string.IsNullOrEmpty(fileInfo.type))
            list.Add($"{serverUrl}/view?filename={escapedName}&subfolder={escapedSub}&type={escapedType}");

        // 2) 常见 output
        list.Add($"{serverUrl}/view?filename={escapedName}&type=output");

        // 3) 仅 filename（许多本地默认可用）
        list.Add($"{serverUrl}/view?filename={escapedName}");

        // 去重
        return list.Distinct().ToList();
    }

    private ComfyGeneratedFileInfo BuildFileInfoFromAbsolutePath(string fullPath)
    {
        string filename = Path.GetFileName(fullPath);
        string subfolder = "";
        try
        {
            string outRoot = Path.GetFullPath(outputDirectory);
            string abs = Path.GetFullPath(fullPath);
            if (abs.StartsWith(outRoot, StringComparison.OrdinalIgnoreCase))
            {
                string rel = abs.Substring(outRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string relDir = Path.GetDirectoryName(rel);
                subfolder = string.IsNullOrWhiteSpace(relDir) ? "" : relDir.Replace('\\', '/');
            }
        }
        catch
        {
            // ignore and fallback to empty subfolder
        }

        return new ComfyGeneratedFileInfo
        {
            filename = filename,
            subfolder = subfolder,
            type = "output"
        };
    }

    private static bool LooksLikeGlb(byte[] data)
    {
        if (data == null || data.Length < 4) return false;
        // GLB magic = 0x67 0x6C 0x54 0x46 => 'g''l''T''F'
        return data[0] == 0x67 && data[1] == 0x6C && data[2] == 0x54 && data[3] == 0x46;
    }

    private static string SafePreviewText(byte[] data, int maxBytes)
    {
        if (data == null || data.Length == 0) return "<empty>";
        int count = Mathf.Min(maxBytes, data.Length);
        try
        {
            return Encoding.UTF8.GetString(data, 0, count).Replace("\n", "\\n").Replace("\r", "");
        }
        catch
        {
            return "<binary>";
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

    /// <summary>
    /// history 里第一条 prompt 的 outputs 是否已有非空节点（用于一次性调试日志）。
    /// </summary>
    private static bool TryGetNonEmptyHistoryOutputNodeKeys(string historyJson, out string nodeKeysCsv)
    {
        nodeKeysCsv = null;
        if (string.IsNullOrWhiteSpace(historyJson))
            return false;
        try
        {
            var root = JObject.Parse(historyJson);
            foreach (var promptEntry in root.Properties())
            {
                var outputs = promptEntry.Value?["outputs"] as JObject;
                if (outputs == null || outputs.Count == 0)
                    return false;
                nodeKeysCsv = string.Join(", ", outputs.Properties().Select(p => p.Name));
                return true;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    /// <summary>
    /// 从 history JSON 的 outputs 中取出首张图片文件信息（优先按节点 id，再扫描其余节点）。
    /// 兼容 Comfy 常见 <c>images</c> 数组结构（PreviewImage、SaveImage、自定义 API 节点等）。
    /// </summary>
    public static bool TryExtractFirstImageFileInfoFromHistory(string historyJson, string[] priorityNodeIds, out ComfyGeneratedFileInfo fileInfo)
    {
        fileInfo = null;
        if (string.IsNullOrWhiteSpace(historyJson))
            return false;

        try
        {
            var root = JObject.Parse(historyJson);
            foreach (var promptEntry in root.Properties())
            {
                var outputs = promptEntry.Value?["outputs"] as JObject;
                if (outputs == null)
                    continue;

                if (priorityNodeIds != null)
                {
                    foreach (var nodeId in priorityNodeIds)
                    {
                        if (string.IsNullOrWhiteSpace(nodeId))
                            continue;
                        if (TryGetFirstImageFileFromNodeOutput(outputs, nodeId.Trim(), out fileInfo))
                            return true;
                    }
                }

                foreach (var nodeProp in outputs.Properties())
                {
                    if (priorityNodeIds != null && Array.Exists(priorityNodeIds, id => string.Equals(id?.Trim(), nodeProp.Name, StringComparison.Ordinal)))
                        continue;
                    if (TryGetFirstImageFileFromNodeOutput(outputs, nodeProp.Name, out fileInfo))
                        return true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"解析 history 中的图片输出失败: {e.Message}");
        }

        return false;
    }

    private static bool TryGetFirstImageFileFromNodeOutput(JObject outputs, string nodeId, out ComfyGeneratedFileInfo fileInfo)
    {
        fileInfo = null;
        if (!outputs.TryGetValue(nodeId, out JToken nodeTok) || nodeTok is not JObject nodeOutput)
            return false;

        foreach (var prop in nodeOutput.Properties())
        {
            if (prop.Value is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (TryParseHistoryImageItem(item, out fileInfo))
                        return true;
                }
            }
        }

        if (nodeOutput["image"] is JObject single && TryParseHistoryImageItem(single, out fileInfo))
            return true;

        return false;
    }

    private static bool TryParseHistoryImageItem(JToken item, out ComfyGeneratedFileInfo fileInfo)
    {
        fileInfo = null;
        if (item is not JObject io)
            return false;
        string filename = io["filename"]?.ToString();
        if (string.IsNullOrEmpty(filename) || !IsRasterImageFileName(filename))
            return false;
        string sub = io["subfolder"]?.ToString();
        string typ = io["type"]?.ToString();
        if (string.IsNullOrEmpty(typ))
            typ = "output";
        fileInfo = new ComfyGeneratedFileInfo
        {
            filename = filename,
            subfolder = sub,
            type = typ
        };
        return true;
    }

    private static bool IsRasterImageFileName(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;
        string lower = filename.ToLowerInvariant();
        return lower.EndsWith(".png", StringComparison.Ordinal) ||
               lower.EndsWith(".jpg", StringComparison.Ordinal) ||
               lower.EndsWith(".jpeg", StringComparison.Ordinal) ||
               lower.EndsWith(".webp", StringComparison.Ordinal);
    }

    private static bool LooksLikeImageFile(byte[] data)
    {
        if (data == null || data.Length < 3)
            return false;
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return true;
        if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return true;
        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return true;
        return false;
    }

    /// <summary>
    /// 从 /view 下载 history 中的任意产物（不做 GLB 魔数校验），用于渲染图等。
    /// </summary>
    public IEnumerator DownloadHistoryArtifactBytes(ComfyGeneratedFileInfo fileInfo, Action<byte[]> onSuccess, Action<string> onError)
    {
        if (fileInfo == null || string.IsNullOrEmpty(fileInfo.filename))
        {
            onError?.Invoke("下载失败：文件信息为空");
            yield break;
        }

        List<string> urls = BuildViewCandidateUrls(fileInfo);
        string lastError = null;
        foreach (var url in urls)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 60;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    lastError = $"下载失败: {request.error}, url={url}";
                    continue;
                }

                byte[] data = request.downloadHandler.data;
                if (data == null || data.Length == 0)
                {
                    lastError = $"下载内容为空, url={url}";
                    continue;
                }

                onSuccess?.Invoke(data);
                yield break;
            }
        }

        onError?.Invoke(lastError ?? "下载失败：所有候选 URL 都未成功");
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

                    // 兜底1：遍历节点输出中所有数组字段，找对象里的 filename=.glb
                    foreach (var child in nodeOutput.Properties())
                    {
                        if (child.Value is not JArray array) continue;
                        foreach (var item in array)
                        {
                            if (item is not JObject objItem) continue;
                            string filename = objItem["filename"]?.ToString();
                            if (!string.IsNullOrEmpty(filename) && filename.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                            {
                                fileInfo = new ComfyGeneratedFileInfo
                                {
                                    filename = filename,
                                    subfolder = objItem["subfolder"]?.ToString(),
                                    type = objItem["type"]?.ToString()
                                };
                                return true;
                            }
                        }
                    }

                    // 兜底2：某些节点（如 Preview3D）会在 result 数组直接给出 glb 路径字符串
                    foreach (var child in nodeOutput.Properties())
                    {
                        if (child.Value is not JArray arr) continue;
                        foreach (var item in arr)
                        {
                            if (item?.Type != JTokenType.String) continue;
                            string s = item.ToString();
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
