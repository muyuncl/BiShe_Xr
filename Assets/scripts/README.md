# VR拍照生成3D模型 - 使用说明

## 已创建的脚本

1. **ComfyUIClient.cs** - ComfyUI API通信客户端
2. **WebcamCapture.cs** - 摄像头拍照管理器
3. **RuntimeOBJLoader.cs** - OBJ模型加载器
4. **VRPhotoTo3DController.cs** - 主控制器

## Unity场景设置步骤

### 1. 创建主控制器对象

1. 在Hierarchy中创建空物体，命名为 `PhotoTo3DController`
2. 添加 `VRPhotoTo3DController` 组件
3. 组件会自动添加其他三个依赖组件

### 2. 配置ComfyUI设置

在 `ComfyUIClient` 组件中：
- **Server Url**: `http://127.0.0.1:8188` (默认)
- **Output Directory**: `D:\comfyui\ComfyUI-aki-v3\ComfyUI\output`

### 3. 创建UI界面

#### 创建Canvas（World Space）：
```
1. 右键 Hierarchy → UI → Canvas
2. Canvas 设置：
   - Render Mode: World Space
   - 位置调整到VR中合适的位置
   - Scale: 0.001, 0.001, 0.001
```

#### 创建生成按钮：
```
1. 在Canvas下创建 Button，命名为 "GenerateButton"
2. 调整大小和位置
3. 按钮文字改为 "拍照生成3D"
4. 在Button的OnClick事件中：
   - 拖入 PhotoTo3DController 对象
   - 选择函数: VRPhotoTo3DController.OnGenerateButtonClick()
```

#### 创建状态文本：
```
1. 在Canvas下创建 Text，命名为 "StatusText"
2. 设置字体大小、颜色
3. 拖入到 VRPhotoTo3DController 的 Status Text 字段
```

#### 创建加载面板（可选）：
```
1. 在Canvas下创建 Panel，命名为 "LoadingPanel"
2. 添加子物体：旋转的加载图标或进度条
3. 拖入到 VRPhotoTo3DController 的 Loading Panel 字段
```

### 4. 配置VR交互

#### 使用XR Interaction Toolkit：
```
1. 确保Button有 XR Simple Interactable 组件
2. 或者使用 XR Ray Interactor 射线交互
```

### 5. 设置模型生成位置

#### 方法A：使用Spawn Point
```
1. 创建空物体，命名为 "ModelSpawnPoint"
2. 放置在想要生成模型的位置
3. 拖入到 VRPhotoTo3DController 的 Model Spawn Point 字段
```

#### 方法B：自动放置
```
不设置Spawn Point，模型会自动出现在摄像机前方2米处
```

### 6. 配置材质（可选）

在 `RuntimeOBJLoader` 组件中：
- **Default Material**: 拖入一个Material（留空则使用Standard材质）

### 7. 调整参数

在 `VRPhotoTo3DController` 组件中：
- **Generation Wait Time**: 35秒（TripoSR生成时间，根据实际调整）
- **Model Scale**: 1.0（模型缩放比例）
- **Photo Width/Height**: 1024x1024（摄像头分辨率）

## 使用流程

1. **启动ComfyUI**
   - 运行秋叶整合包，确保服务在 `http://127.0.0.1:8188` 运行
   - 测试工作流能正常生成模型

2. **运行Unity项目**
   - 连接Quest/Pico设备（通过Link或串流）
   - 运行场景

3. **生成模型**
   - 在VR中点击"拍照生成3D"按钮
   - 等待约35秒
   - 模型会出现在场景中

## 常见问题

### Q: 提示"ComfyUI连接失败"
**A:** 检查ComfyUI是否启动，浏览器访问 `http://127.0.0.1:8188` 测试

### Q: 提示"未检测到摄像头"
**A:** 确保电脑有摄像头，Unity有摄像头权限

### Q: 模型生成失败
**A:** 
- 检查ComfyUI的output目录路径是否正确
- 手动在ComfyUI界面测试工作流
- 查看Unity Console的详细错误信息

### Q: 模型显示不正常
**A:** 
- 调整 Model Scale 参数
- 检查模型的Spawn Point位置
- 为模型添加合适的材质和光照

### Q: VR按钮点击无反应
**A:** 
- 检查XR Interaction Toolkit是否正确配置
- 确保Button有Collider和XR Interactable组件
- 检查OnClick事件是否正确绑定

## 进阶功能

### 添加模型交互
在生成的模型上添加：
- XR Grab Interactable（抓取）
- Rigidbody（物理）
- Collider（碰撞）

### 优化性能
- 降低摄像头分辨率
- 在ComfyUI工作流中添加减面节点
- 使用LOD系统

### 保存模型
可以扩展代码，将生成的模型保存到本地：
```csharp
System.IO.File.WriteAllBytes("path/to/save.obj", objData);
```

## 测试清单

- [ ] ComfyUI服务正常运行
- [ ] Unity能连接到ComfyUI
- [ ] 摄像头能正常拍照
- [ ] 图片能上传到ComfyUI
- [ ] 工作流能生成OBJ文件
- [ ] Unity能下载并加载OBJ
- [ ] VR按钮交互正常
- [ ] 模型显示位置正确

## 技术支持

如遇到问题，检查：
1. Unity Console的错误日志
2. ComfyUI终端的输出信息
3. output目录是否有生成的OBJ文件
