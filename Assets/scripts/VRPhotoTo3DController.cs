using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR拍照生成3D模型主控制器
/// 协调摄像头、ComfyUI通信和模型加载
/// </summary>
public class VRPhotoTo3DController : MonoBehaviour
{
    private enum FlowPhase
    {
        Idle,
        Uploading,
        Rendering,
        Modeling,
        Completed,
        Failed,
    }

    [Header("组件引用")]
    [Tooltip("摄像头拍照组件")]
    public WebcamCapture webcamCapture;
    
    [Tooltip("ComfyUI客户端组件")]
    public ComfyUIClient comfyUIClient;
    
    [Tooltip("GLB加载器组件")]
    public GLBLoader glbLoader;

    [Header("UI引用")]
    [Tooltip("生成按钮")]
    public Button generateButton;
    
    [Tooltip("状态提示文本（TextMeshPro）")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("网络诊断文本（可选）")]
    public TextMeshProUGUI networkDiagnosticText;
    
    [Tooltip("加载UI面板")]
    public GameObject loadingPanel;

    [Header("渲染图回显（Comfy history → 下载 → UI）")]
    [Tooltip("可选；整块预览区（含底色的父物体）。有则加载前 SetActive(false)，出图后再 true。子级需挂 RawImage")]
    public GameObject renderPreviewHost;

    [Tooltip("轮询到 SaveImage / PreviewImage 等节点的 images 输出后显示；留空则无法贴图（仍会轮询）")]
    public RawImage renderPreviewRawImage;

    [Tooltip("可选；若使用 Image 显示而非 RawImage，赋此字段（会动态创建 Sprite）")]
    public Image renderPreviewImage;

    [Header("模型生成设置")]
    [Tooltip("模型生成等待时间（秒）")]
    public float generationWaitTime = 35f;
    
    [Tooltip("生成的模型放置位置")]
    public Transform modelSpawnPoint;
    
    [Tooltip("在「按高度适配」时作为额外倍率；关闭适配时等同旧版整体缩放")]
    public float modelScale = 1f;

    [Tooltip("将模型世界空间包围盒高度缩放到该值（米），例如 0.5 = 50cm，等比不变形")]
    public float targetModelHeightMeters = 0.5f;

    [Tooltip("生成后按 Renderer 包围盒把高度缩放到 targetModelHeightMeters")]
    public bool fitModelHeightToTarget = true;

    [Header("VR交互")]
    [Tooltip("是否在生成过程中禁用按钮")]
    public bool disableButtonDuringGeneration = true;

    [Tooltip("生成后按 Interaction SDK「Add Grab Interaction」：Rigidbody + Grabbable + 子物体 HandGrab/Grab Interactable（与面板向导一致）")]
    public bool enableGrabOnSpawnedModel = true;

    [Tooltip("生成后短暂自转展示；与抓取同时开可能抢旋转，建议抓取时关闭")]
    public bool enableSpawnRotationAnimation = false;

    [Header("调试功能")]
    [Tooltip("启用详细日志输出")]
    public bool enableDetailedLogs = true;
    
    [Tooltip("按空格键测试拍照（非VR模式调试用）")]
    public bool enableKeyboardTest = true;

    [Header("草图调试（跳过拍照）")]
    [Tooltip("勾选后不调用摄像头，改用下方本地文件或纹理作为草图上传 ComfyUI")]
    public bool useDebugSketchFromLocal;

    [Tooltip("本地图片绝对路径，例如 D:/shots/sketch.png（优先于下方纹理）")]
    public string debugSketchAbsolutePath;

    [Tooltip("路径为空时使用；不可读贴图会通过 RenderTexture 拷一份再导出 PNG")]
    public Texture2D debugSketchTexture;

    private bool isGenerating = false;
    private GameObject currentModel;
    private string selectedMaterialName;
    private Texture2D selectedMaterialReference;
    private Texture2D _runtimeRenderPreviewTexture;
    private Sprite _runtimeRenderPreviewSprite;
    private FlowPhase _phase = FlowPhase.Idle;
    private bool _renderPreviewSeen;

    private void Start()
    {
        // 初始化组件
        if (webcamCapture == null)
            webcamCapture = gameObject.AddComponent<WebcamCapture>();
        
        if (comfyUIClient == null)
            comfyUIClient = gameObject.AddComponent<ComfyUIClient>();
        
        if (glbLoader == null)
            glbLoader = gameObject.AddComponent<GLBLoader>();

        // 隐藏加载UI
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // 测试ComfyUI连接
        StartCoroutine(TestComfyUIConnection());
        
        LogDebug("=== VR拍照生成3D系统已启动 ===");
        LogDebug("按空格键测试拍照功能（如果启用键盘测试）");

        SetRenderPreviewShellVisible(false);
        SetPhase(FlowPhase.Idle);
    }

    private void OnDestroy()
    {
        ClearRenderPreviewTargets();
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
        
        yield return comfyUIClient.TestConnectionDetailed((success, details) =>
        {
            if (success)
            {
                UpdateStatus("ComfyUI连接成功，准备就绪");
                LogDebug("✅ ComfyUI连接成功！");
                if (networkDiagnosticText != null)
                    networkDiagnosticText.text = $"服务可达: {comfyUIClient.serverUrl}";
            }
            else
            {
                UpdateStatus("ComfyUI连接失败，请检查服务是否启动");
                LogDebug($"❌ ComfyUI连接失败！\n{details}");
                if (networkDiagnosticText != null)
                    networkDiagnosticText.text = details;
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

    public void SetSelectedMaterialReference(string materialName, Texture2D referenceImage)
    {
        selectedMaterialName = materialName;
        selectedMaterialReference = referenceImage;
        LogDebug($"🎨 已选择材质: {selectedMaterialName}, 参考图: {(selectedMaterialReference != null ? "已配置" : "未配置")}");
    }

    /// <summary>
    /// 完整的生成流程协程
    /// </summary>
    private IEnumerator GenerateModelFromPhoto()
    {
        isGenerating = true;
        _renderPreviewSeen = false;
        SetPhase(FlowPhase.Idle);
        ClearRenderPreviewTargets();

        // 禁用生成按钮
        if (generateButton != null)
            generateButton.interactable = false;
        
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // ========== 步骤1: 草图（拍照或调试本地图） ==========
        byte[] photoData = null;

        if (useDebugSketchFromLocal)
        {
            UpdateStatus("调试模式：加载本地草图...");
            LogDebug("📂 [步骤1/7] 调试模式：跳过拍照，使用本地草图");
            photoData = TryLoadDebugSketchBytes();
            if (photoData == null)
            {
                UpdateStatus("调试草图读取失败（检查路径或纹理）");
                LogDebug("❌ 调试草图读取失败：请填写有效绝对路径，或指定 Texture2D");
                yield return new WaitForSeconds(2f);
                FinishGeneration();
                yield break;
            }

            LogDebug($"✅ 本地草图载入成功！大小: {photoData.Length / 1024}KB ({photoData.Length} bytes)");
            yield return null;
        }
        else
        {
            UpdateStatus("正在拍照...");
            LogDebug("📷 [步骤1/7] 开始拍照");
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
        }

        if (selectedMaterialReference == null)
        {
            UpdateStatus("请先选择材质（需要材质参考图）");
            LogDebug("❌ 未选择材质：请在材质栏选择一项后再生成");
            yield return new WaitForSeconds(2f);
            FinishGeneration();
            yield break;
        }

        byte[] materialBytes = TextureToPngBytes(selectedMaterialReference);
        if (materialBytes == null || materialBytes.Length == 0)
        {
            UpdateStatus("材质参考图无法编码为图片");
            LogDebug("❌ 材质 Texture2D 转 PNG 失败");
            yield return new WaitForSeconds(2f);
            FinishGeneration();
            yield break;
        }

        // ========== 步骤2: 上传草图与材质图（仅替换 API 中两个 LoadImage） ==========
        SetPhase(FlowPhase.Uploading, "上传草图与材质图…");
        LogDebug("📤 [步骤2/7] 上传草图与材质参考图");

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string sketchFile = $"unity_sketch_{stamp}.png";
        string materialFile = $"unity_material_{stamp}.png";

        string sketchUploadedName = null;
        string materialUploadedName = null;
        bool sketchOk = false;
        bool materialOk = false;

        yield return comfyUIClient.UploadImage(
            photoData,
            sketchFile,
            "image/png",
            (name) => { sketchUploadedName = name; sketchOk = true; LogDebug($"✅ 草图已上传: {name}"); },
            (error) => { SetPhase(FlowPhase.Failed, $"草图上传失败：{error}"); LogDebug($"❌ {error}"); sketchOk = false; }
        );

        if (!sketchOk)
        {
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        yield return comfyUIClient.UploadImage(
            materialBytes,
            materialFile,
            "image/png",
            (name) => { materialUploadedName = name; materialOk = true; LogDebug($"✅ 材质图已上传: {name}"); },
            (error) => { SetPhase(FlowPhase.Failed, $"材质图上传失败：{error}"); LogDebug($"❌ {error}"); materialOk = false; }
        );

        if (!materialOk)
        {
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        // ========== 步骤3: 提交工作流 ==========
        SetPhase(FlowPhase.Rendering, "渲染中…（已提交任务）");
        LogDebug("🚀 [步骤3/7] 提交工作流到ComfyUI（仅替换线稿/材质文件名）");
        
        bool queueSuccess = false;
        string promptId = null;

        yield return comfyUIClient.QueuePromptDualLoadImages(
            sketchUploadedName,
            materialUploadedName,
            (queuedPromptId) => 
            { 
                queueSuccess = true;
                promptId = queuedPromptId;
                LogDebug($"✅ 工作流提交成功！prompt_id: {promptId}");
            },
            (error) => 
            { 
                SetPhase(FlowPhase.Failed, $"任务提交失败：{error}");
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
        SetPhase(FlowPhase.Rendering, "渲染中…");
        LogDebug($"⏳ [步骤4/7] 等待ComfyUI生成3D模型（最多 {generationWaitTime} 秒）");
        LogDebug("💡 提示：系统会按 prompt_id 轮询 ComfyUI history 接口");

        // ========== 步骤5: 轮询任务并获取GLB文件信息 ==========
        SetPhase(FlowPhase.Rendering, "渲染中…（等待出图/建模结果）");
        LogDebug($"🔍 [步骤5/7] 查询 ComfyUI history，prompt_id={promptId}");

        if (comfyUIClient != null && comfyUIClient.enableRenderImagePolling &&
            renderPreviewRawImage == null && renderPreviewImage == null)
            LogDebug("⚠️ 渲染图轮询已开启，但未绑定 renderPreviewRawImage / renderPreviewImage，无法回显。请在 Inspector 绑定。");
        
        ComfyUIClient.ComfyGeneratedFileInfo glbFileInfo = null;
        bool findSuccess = false;

        bool hasRenderUi = (renderPreviewRawImage != null || renderPreviewImage != null) &&
                           comfyUIClient != null && comfyUIClient.enableRenderImagePolling;
        yield return comfyUIClient.WaitForPromptOutputGLB(
            promptId,
            generationWaitTime,
            (fileInfo) => 
            { 
                glbFileInfo = fileInfo;
                findSuccess = true;
                LogDebug($"✅ 找到GLB文件: {fileInfo.filename}");
            },
            (error) => 
            { 
                SetPhase(FlowPhase.Failed, $"未找到模型文件：{error}");
                LogDebug($"❌ 未找到模型文件: {error}");
                LogDebug("💡 请检查ComfyUI是否成功生成了GLB文件");
                findSuccess = false;
            },
            hasRenderUi ? (Action<byte[]>)ApplyRenderPreviewBytes : null
        );

        if (!findSuccess)
        {
            yield return new WaitForSeconds(3f);
            FinishGeneration();
            yield break;
        }

        // ========== 步骤6: 下载GLB文件 ==========
        SetPhase(FlowPhase.Modeling, "建模中…（下载模型文件）");
        LogDebug($"📥 [步骤6/7] 下载GLB文件");
        
        byte[] glbData = null;
        bool downloadSuccess = false;

        yield return comfyUIClient.DownloadFile(
            glbFileInfo,
            (data) => 
            { 
                glbData = data;
                downloadSuccess = true;
                LogDebug($"✅ GLB文件下载完成！大小: {data.Length / 1024}KB");
            },
            (error) => 
            { 
                SetPhase(FlowPhase.Failed, $"模型下载失败：{error}");
                LogDebug($"❌ 下载失败: {error}");
                downloadSuccess = false;
            }
        );

        if (!downloadSuccess || glbData == null)
        {
            LogDebug("⚠️ 在线下载GLB失败，尝试读取输出目录最新GLB作为兜底...");
            string fallbackErr = "未知错误";
            if (comfyUIClient != null && comfyUIClient.TryReadLatestGlbBytesFromOutputDir(out byte[] fallbackData, out string fallbackPath, out fallbackErr))
            {
                glbData = fallbackData;
                downloadSuccess = true;
                SetPhase(FlowPhase.Modeling, "建模中…（切换本地输出目录模型）");
                LogDebug($"✅ 本地兜底GLB读取成功: {fallbackPath}, 大小: {glbData.Length / 1024}KB");
            }
            else
            {
                LogDebug($"❌ 本地兜底GLB读取失败: {fallbackErr}");
                SetPhase(FlowPhase.Failed, $"读取本地模型失败：{fallbackErr}");
                yield return new WaitForSeconds(3f);
                FinishGeneration();
                yield break;
            }
        }

        // ========== 步骤7: 加载GLB模型 ==========
        SetPhase(FlowPhase.Modeling, "建模中…（加载模型）");
        LogDebug("🎨 [步骤7/7] 加载GLB模型");
        
        GameObject model = null;
        bool loadSuccess = false;

        yield return glbLoader.LoadGLBFromBytes(
            glbData,
            "GeneratedModel",
            (loadedModel) =>
            {
                model = loadedModel;
                loadSuccess = true;
            },
            (error) =>
            {
                SetPhase(FlowPhase.Failed, $"模型加载失败：{error}");
                LogDebug($"❌ 模型加载失败: {error}");
            }
        );

        if (!loadSuccess || model == null)
        {
            LogDebug("⚠️ 当前GLB加载失败，尝试改用输出目录最新GLB再次加载...");
            string fallbackErr = "未知错误";
            if (comfyUIClient != null && comfyUIClient.TryReadLatestGlbBytesFromOutputDir(out byte[] fallbackData, out string fallbackPath, out fallbackErr))
            {
                GameObject retryModel = null;
                bool retryLoadSuccess = false;
                yield return glbLoader.LoadGLBFromBytes(
                    fallbackData,
                    "GeneratedModel_Fallback",
                    (loadedModel) =>
                    {
                        retryModel = loadedModel;
                        retryLoadSuccess = true;
                    },
                    (error) =>
                    {
                        LogDebug($"❌ 兜底GLB二次加载失败: {error}");
                    }
                );

                if (retryLoadSuccess && retryModel != null)
                {
                    model = retryModel;
                    loadSuccess = true;
                    SetPhase(FlowPhase.Modeling, "建模中…（已切换本地模型并加载成功）");
                    LogDebug($"✅ 兜底GLB加载成功: {fallbackPath}");
                }
                else
                {
                    SetPhase(FlowPhase.Failed, "模型加载失败（兜底也失败）");
                    LogDebug($"❌ 模型加载失败！兜底也失败: {fallbackErr}");
                    yield return new WaitForSeconds(3f);
                    FinishGeneration();
                    yield break;
                }
            }
            else
            {
                SetPhase(FlowPhase.Failed, $"模型加载失败：{fallbackErr}");
                LogDebug($"❌ 模型加载失败！且无法读取兜底GLB: {fallbackErr}");
                yield return new WaitForSeconds(3f);
                FinishGeneration();
                yield break;
            }
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

        model.transform.localScale = Vector3.one;
        if (fitModelHeightToTarget)
        {
            GeneratedModelPostProcess.ApplyUniformWorldHeight(model, targetModelHeightMeters, modelScale);
            LogDebug($"📏 模型高度适配为 {targetModelHeightMeters}m（等比，额外倍率 modelScale={modelScale}）");
        }
        else
        {
            model.transform.localScale = Vector3.one * modelScale;
            LogDebug($"📏 模型缩放: {modelScale}");
        }

        if (enableGrabOnSpawnedModel)
            GeneratedModelPostProcess.AddGrabInteraction(model);

        if (enableSpawnRotationAnimation)
            StartCoroutine(RotateModel(model));

        SetPhase(FlowPhase.Completed, "完成");
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

    private void SetRenderPreviewShellVisible(bool visible)
    {
        if (renderPreviewHost != null)
        {
            renderPreviewHost.SetActive(visible);
            return;
        }
        if (renderPreviewRawImage != null)
        {
            renderPreviewRawImage.gameObject.SetActive(visible);
            renderPreviewRawImage.enabled = visible;
        }
        if (renderPreviewImage != null)
        {
            renderPreviewImage.gameObject.SetActive(visible);
            renderPreviewImage.enabled = visible;
        }
    }

    private void ClearRenderPreviewTargets()
    {
        if (renderPreviewRawImage != null)
            renderPreviewRawImage.texture = null;
        if (renderPreviewImage != null)
            renderPreviewImage.sprite = null;
        if (_runtimeRenderPreviewSprite != null)
        {
            Destroy(_runtimeRenderPreviewSprite);
            _runtimeRenderPreviewSprite = null;
        }
        if (_runtimeRenderPreviewTexture != null)
        {
            Destroy(_runtimeRenderPreviewTexture);
            _runtimeRenderPreviewTexture = null;
        }
        SetRenderPreviewShellVisible(false);
    }

    /// <summary>
    /// Comfy history 轮询中下载到的渲染图字节（PNG/JPEG/WebP）显示到 UI。
    /// </summary>
    private void ApplyRenderPreviewBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return;
        if (renderPreviewRawImage == null && renderPreviewImage == null)
            return;

        SetRenderPreviewShellVisible(true);
        if (!_renderPreviewSeen)
        {
            _renderPreviewSeen = true;
            if (_phase == FlowPhase.Rendering || _phase == FlowPhase.Uploading || _phase == FlowPhase.Idle)
                SetPhase(FlowPhase.Modeling, "建模中…（渲染图已生成）");
        }

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Destroy(tex);
            LogDebug("❌ 渲染图字节无法解码为纹理");
            return;
        }

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(false, false);

        if (_runtimeRenderPreviewSprite != null)
        {
            Destroy(_runtimeRenderPreviewSprite);
            _runtimeRenderPreviewSprite = null;
        }
        if (_runtimeRenderPreviewTexture != null)
            Destroy(_runtimeRenderPreviewTexture);

        _runtimeRenderPreviewTexture = tex;

        if (renderPreviewRawImage != null)
            renderPreviewRawImage.texture = tex;

        if (renderPreviewImage != null)
        {
            _runtimeRenderPreviewSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            renderPreviewImage.sprite = _runtimeRenderPreviewSprite;
        }

        LogDebug($"✅ 渲染图已回显 ({tex.width}x{tex.height})");
    }

    /// <summary>
    /// 完成生成流程
    /// </summary>
    private void FinishGeneration()
    {
        isGenerating = false;
        
        // 启用生成按钮
        if (generateButton != null)
            generateButton.interactable = true;
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // 保留「完成/失败」终态文案，直到下一次生成开始时覆盖
        if (_phase != FlowPhase.Completed && _phase != FlowPhase.Failed)
            SetPhase(FlowPhase.Idle, "准备就绪");
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

    private void SetPhase(FlowPhase phase, string detail = null)
    {
        _phase = phase;
        string msg = phase switch
        {
            FlowPhase.Idle => string.IsNullOrWhiteSpace(detail) ? "准备就绪" : detail,
            FlowPhase.Uploading => string.IsNullOrWhiteSpace(detail) ? "上传中…" : $"上传中：{detail}",
            FlowPhase.Rendering => string.IsNullOrWhiteSpace(detail) ? "渲染中…" : detail,
            FlowPhase.Modeling => string.IsNullOrWhiteSpace(detail) ? "建模中…" : detail,
            FlowPhase.Completed => string.IsNullOrWhiteSpace(detail) ? "完成" : detail,
            FlowPhase.Failed => string.IsNullOrWhiteSpace(detail) ? "失败" : detail,
            _ => detail ?? "处理中…"
        };
        UpdateStatus(msg);
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
    /// 调试模式：从磁盘路径（优先）或 Texture2D 得到 PNG/JPEG 等原始字节；纹理会导出为 PNG。
    /// </summary>
    private byte[] TryLoadDebugSketchBytes()
    {
        if (!string.IsNullOrWhiteSpace(debugSketchAbsolutePath))
        {
            var path = debugSketchAbsolutePath.Trim().Trim('"');
            try
            {
                if (!File.Exists(path))
                {
                    LogDebug($"❌ 调试草图文件不存在: {path}");
                    return null;
                }

                return File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                LogDebug($"❌ 读取调试草图失败: {e.Message}");
                return null;
            }
        }

        if (debugSketchTexture != null)
            return TextureToPngBytes(debugSketchTexture);

        LogDebug("❌ 未配置调试草图：路径为空且 debugSketchTexture 为空");
        return null;
    }

    private static byte[] TextureToPngBytes(Texture2D src)
    {
        if (src == null)
            return null;

        if (src.isReadable)
            return src.EncodeToPNG();

        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(src.width, src.height, TextureFormat.RGB24, false);
        copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        var png = copy.EncodeToPNG();
        UnityEngine.Object.Destroy(copy);
        return png;
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
