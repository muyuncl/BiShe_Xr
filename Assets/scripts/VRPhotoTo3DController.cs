using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VR拍照生成3D模型主控制器
/// 协调摄像头、ComfyUI通信和模型加载
/// </summary>
public class VRPhotoTo3DController : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("摄像头拍照组件")]
    public WebcamCapture webcamCapture;
    
    [Tooltip("ComfyUI客户端组件")]
    public ComfyUIClient comfyUIClient;
    
    [Tooltip("OBJ加载器组件")]
    public RuntimeOBJLoader objLoader;

    [Header("UI引用")]
    [Tooltip("状态提示文本")]
    public Text statusText;
    
    [Tooltip("加载UI面板")]
    public GameObject loadingPanel;

    [Header("模型生成设置")]
    [Tooltip("模型生成等待时间（秒）")]
    public float generationWaitTime = 35f;
    
    [Tooltip("生成的模型放置位置")]
    public Transform modelSpawnPoint;
    
    [Tooltip("模型缩放比例")]
    public float modelScale = 1f;

    [Header("VR交互")]
    [Tooltip("是否在生成过程中禁用按钮")]
    public bool disableButtonDuringGeneration = true;

    [Header("调试功能")]
    [Tooltip("启用详细日志输出")]
    public bool enableDetailedLogs = true;
    
    [Tooltip("按空格键测试拍照（非VR模式调试用）")]
    public bool enableKeyboardTest = true;

    private bool isGenerating = false;
    private GameObject currentModel;

    private void Start()
    {
        // 初始化组件
        if (webcamCapture == null)
            webcamCapture = gameObject.AddComponent<WebcamCapture>();
        
        if (comfyUIClient == null)
            comfyUIClient = gameObject.AddComponent<ComfyUIClient>();
        
        if (objLoader == null)
            objLoader = gameObject.AddComponent<RuntimeOBJLoader>();

        // 隐藏加载UI
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // 测试ComfyUI连接
        StartCoroutine(TestComfyUIConnection());
        
        LogDebug("=== VR拍照生成3D系统已启动 ===");
        LogDebug("按空格键测试拍照功能（如果启用键盘测试）");
    }

    private void Update()
    {
        // 键盘测试功能（方便调试）
        if (enableKeyboardTest && Input.GetKeyDown(KeyCode.Space))
        {
            LogDebug("🎮 检测到空格键按下，开始测试生成流程");
            OnGenerateButtonClick();
        }
    }

    /// <summary>
    /// 测试ComfyUI连接
    /// </summary>
    private IEnumerator TestComfyUIConnection()
    {
        UpdateStatus("正在连接ComfyUI...");
        LogDebug("🔌 开始测试ComfyUI连接...");
        
        yield return comfyUIClient.TestConnection((success) =>
        {
            if (success)
            {
                UpdateStatus("ComfyUI连接成功，准备就绪");
                LogDebug("✅ ComfyUI连接成功！");
            }
            else
            {
                UpdateStatus("ComfyUI连接失败，请检查服务是否启动");
                LogDebug("❌ ComfyUI连接失败！请检查服务是否在 http://127.0.0.1:8188 运行");
            }
        });
    }

    /// <summary>
    /// VR按钮点击事件（公开方法，供XR按钮调用）
    /// </summary>
    public void OnGenerateButtonClick()
    {
        if (isGenerating)
        {
            Debug.LogWarning("⚠️ 正在生成中，请稍候...");
            UpdateStatus("正在生成中，请稍候...");
            return;
        }

        LogDebug("========================================");
        LogDebug("🎯 用户触发生成按钮，开始完整流程");
        LogDebug("========================================");
        StartCoroutine(GenerateModelFromPhoto());
    }

    /// <summary>
    /// 完整的生成流程协程
    /// </summary>
    private IEnumerator GenerateModelFromPhoto()
    {
        isGenerating = true;
        
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // ========== 步骤1: 拍照 ==========
        UpdateStatus("正在拍照...");
        LogDebug("📷 [步骤1/8] 开始拍照");

        byte[] photoData = null;
        yield return webcamCapture.TakePhotoAsync((data) => { photoData = data; });

        if (photoData == null)
        {
            UpdateStatus("拍照失败！");
            LogDebug("❌ 拍照失败！请检查摄像头是否可用");
            yield return new WaitForSeconds(2f);
            FinishGeneration();
            yield break;
        }

        LogDebug($"✅ 拍照成功！图片大小: {photoData.Length / 1024}KB ({photoData.Length} bytes)");

        // ========== 步骤2: 上传图片 ==========
        UpdateStatus("正在上传图片到ComfyUI...");
        LogDebug("📤 [步骤2/8] 开始上传图片到ComfyUI");
        
        string uploadedFileName = null;
        bool uploadSuccess = false;

        yield return comfyUIClient.UploadImage(
            photoData,
            (fileName) => 
            { 
                uploadedFileName = fileName;
                uploadSuccess = true;
                LogDebug($"✅ 图片上传成功！文件名: {fileName}");
            },
            (error) => 
            { 
                UpdateStatus($"上传失败: {error}");
                LogDebug($"❌ 上传失败: {error}");
                uploadSuccess = false;
            }
        );

        if (!uploadSuccess)
        {
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        // ========== 步骤3: 提交工作流 ==========
        UpdateStatus("正在提交3D生成任务...");
        LogDebug("🚀 [步骤3/8] 提交TripoSR工作流到ComfyUI");
        
        bool queueSuccess = false;

        yield return comfyUIClient.QueuePrompt(
            uploadedFileName,
            (response) => 
            { 
                queueSuccess = true;
                LogDebug($"✅ 工作流提交成功！ComfyUI开始处理");
            },
            (error) => 
            { 
                UpdateStatus($"任务提交失败: {error}");
                LogDebug($"❌ 任务提交失败: {error}");
                queueSuccess = false;
            }
        );

        if (!queueSuccess)
        {
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        // ========== 步骤4: 等待生成 ==========
        UpdateStatus($"正在生成3D模型，请稍候...");
        LogDebug($"⏳ [步骤4/8] 智能等待ComfyUI生成3D模型（最多 {generationWaitTime} 秒）");
        LogDebug("💡 提示：系统会自动检测文件生成，无需等待全部时间");

        // ========== 步骤5: 智能检测生成的OBJ文件 ==========
        UpdateStatus("正在查找生成的模型文件...");
        LogDebug($"🔍 [步骤5/8] 智能监控output目录");
        LogDebug($"📁 监控路径: {comfyUIClient.outputDirectory}");
        
        string objFileName = null;
        bool findSuccess = false;

        yield return comfyUIClient.WaitAndGetLatestOBJ(
            generationWaitTime, // 最大等待时间
            (fileName) => 
            { 
                objFileName = fileName;
                findSuccess = true;
                LogDebug($"✅ 找到OBJ文件: {fileName}");
            },
            (error) => 
            { 
                UpdateStatus($"未找到模型文件: {error}");
                LogDebug($"❌ 未找到模型文件: {error}");
                LogDebug("💡 请检查ComfyUI是否成功生成了OBJ文件");
                findSuccess = false;
            }
        );

        if (!findSuccess)
        {
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        // ========== 步骤6: 下载OBJ和相关文件（材质、贴图） ==========
        UpdateStatus("正在下载模型和贴图文件...");
        LogDebug($"📥 [步骤6/8] 下载OBJ、MTL和贴图文件");
        
        Dictionary<string, byte[]> modelFiles = null;
        bool downloadSuccess = false;

        yield return comfyUIClient.DownloadRelatedFiles(
            objFileName,
            (files) => 
            { 
                modelFiles = files;
                downloadSuccess = true;
                LogDebug($"✅ 文件下载完成！共 {files.Count} 个文件");
                foreach (var key in files.Keys)
                {
                    if (key != "textureName")
                        LogDebug($"  - {key}: {files[key].Length / 1024}KB");
                }
            },
            (error) => 
            { 
                UpdateStatus($"下载失败: {error}");
                LogDebug($"❌ 下载失败: {error}");
                downloadSuccess = false;
            }
        );

        if (!downloadSuccess)
        {
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        // ========== 步骤7: 加载模型（带贴图） ==========
        UpdateStatus("正在加载3D模型...");
        LogDebug("🎨 [步骤7/8] 解析OBJ文件并应用材质贴图");
        
        GameObject model = objLoader.LoadOBJWithTexture(modelFiles, "GeneratedModel");

        if (model == null)
        {
            UpdateStatus("模型加载失败！");
            LogDebug("❌ 模型加载失败！OBJ文件可能损坏");
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        LogDebug("✅ 模型加载成功！");

        // ========== 步骤8: 在场景中显示模型 ==========
        LogDebug("🎭 [步骤8/8] 在VR场景中显示模型");
        
        // 删除之前的模型
        if (currentModel != null)
        {
            LogDebug("🗑️ 删除之前的模型");
            Destroy(currentModel);
        }

        currentModel = model;

        // 设置模型位置和缩放
        if (modelSpawnPoint != null)
        {
            model.transform.position = modelSpawnPoint.position;
            model.transform.rotation = modelSpawnPoint.rotation;
            LogDebug($"📍 模型放置在Spawn Point: {modelSpawnPoint.position}");
        }
        else
        {
            // 默认放在摄像机前方2米处
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                model.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 2f;
                model.transform.LookAt(mainCamera.transform);
                LogDebug($"📍 模型放置在摄像机前方2米: {model.transform.position}");
            }
        }

        model.transform.localScale = Vector3.one * modelScale;
        LogDebug($"📏 模型缩放: {modelScale}");

        // 可选：添加旋转动画
        StartCoroutine(RotateModel(model));

        UpdateStatus("3D模型生成成功！");
        LogDebug("========================================");
        LogDebug("🎉 模型生成完成！所有步骤执行成功！");
        LogDebug("========================================");

        yield return new WaitForSeconds(2f);
        FinishGeneration();
    }

    /// <summary>
    /// 模型旋转动画（可选）
    /// </summary>
    private IEnumerator RotateModel(GameObject model)
    {
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (model != null)
            {
                model.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 完成生成流程
    /// </summary>
    private void FinishGeneration()
    {
        isGenerating = false;
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        UpdateStatus("准备就绪");
        LogDebug("🔄 系统准备就绪，可以开始新的生成");
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[状态] {message}");
    }

    /// <summary>
    /// 输出调试日志
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDetailedLogs)
        {
            Debug.Log($"[PhotoTo3D] {message}");
        }
    }

    /// <summary>
    /// 删除当前模型（可供外部调用）
    /// </summary>
    public void DeleteCurrentModel()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
            UpdateStatus("模型已删除");
            LogDebug("🗑️ 当前模型已删除");
        }
    }
}
