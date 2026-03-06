using System;
using UnityEngine;

/// <summary>
/// 摄像头拍照管理器
/// 负责访问电脑摄像头并拍照
/// </summary>
public class WebcamCapture : MonoBehaviour
{
    private WebCamTexture webCamTexture;
    
    [Header("摄像头设置")]
    [Tooltip("拍照分辨率宽度")]
    public int photoWidth = 1024;
    
    [Tooltip("拍照分辨率高度")]
    public int photoHeight = 1024;
    
    [Tooltip("摄像头启动等待时间（秒）")]
    public float cameraStartupTime = 2f;

    private bool isInitialized = false;

    /// <summary>
    /// 初始化摄像头
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
            return;

        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("❌ 未检测到摄像头设备！");
            return;
        }

        // 列出所有摄像头
        Debug.Log($"📷 检测到 {devices.Length} 个摄像头:");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"  [{i}] {devices[i].name}");
        }

        // 使用第一个可用的摄像头
        string deviceName = devices[0].name;
        Debug.Log($"✅ 使用摄像头: {deviceName}");

        webCamTexture = new WebCamTexture(deviceName, photoWidth, photoHeight, 30);
        isInitialized = true;
    }

    /// <summary>
    /// 异步拍照（使用协程，避免卡顿）- 改进版
    /// </summary>
    public System.Collections.IEnumerator TakePhotoAsync(Action<byte[]> callback)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (webCamTexture == null)
        {
            Debug.LogError("❌ 摄像头未初始化！");
            callback?.Invoke(null);
            yield break;
        }

        Debug.Log("📷 启动摄像头...");
        
        // 启动摄像头
        webCamTexture.Play();

        // 等待摄像头真正启动并获取画面
        Debug.Log($"⏳ 等待摄像头启动（最多 {cameraStartupTime} 秒）...");
        float elapsed = 0f;
        
        while (!webCamTexture.didUpdateThisFrame && elapsed < cameraStartupTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!webCamTexture.didUpdateThisFrame)
        {
            Debug.LogError($"❌ 摄像头启动超时！等待了 {elapsed} 秒");
            Debug.LogError("💡 提示：请检查摄像头是否被其他程序占用");
            webCamTexture.Stop();
            callback?.Invoke(null);
            yield break;
        }

        Debug.Log($"✅ 摄像头已启动！分辨率: {webCamTexture.width}x{webCamTexture.height}");
        
        // 额外等待几帧，确保画面稳定
        Debug.Log("⏳ 等待画面稳定...");
        yield return new WaitForSeconds(0.5f);

        try
        {
            // 创建Texture2D并复制像素
            Debug.Log("📸 正在捕获画面...");
            Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            photo.SetPixels(webCamTexture.GetPixels());
            photo.Apply();

            // 停止摄像头
            webCamTexture.Stop();
            Debug.Log("🛑 摄像头已停止");

            // 编码为PNG
            Debug.Log("💾 正在编码为PNG...");
            byte[] pngData = photo.EncodeToPNG();
            
            // 清理
            Destroy(photo);

            Debug.Log($"✅ 拍照成功！图片大小: {pngData.Length / 1024}KB ({pngData.Length} bytes)");
            
            // 可选：保存到本地用于调试
            #if UNITY_EDITOR
            string debugPath = System.IO.Path.Combine(Application.dataPath, "../debug_photo.png");
            System.IO.File.WriteAllBytes(debugPath, pngData);
            Debug.Log($"🔍 调试：图片已保存到 {debugPath}");
            #endif
            
            callback?.Invoke(pngData);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 拍照过程出错: {e.Message}");
            Debug.LogError($"堆栈: {e.StackTrace}");
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
            callback?.Invoke(null);
        }
    }

    /// <summary>
    /// 停止摄像头
    /// </summary>
    public void Stop()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
            Debug.Log("🛑 摄像头已停止");
        }
    }

    private void OnDestroy()
    {
        Stop();
        if (webCamTexture != null)
        {
            Destroy(webCamTexture);
        }
    }
}
