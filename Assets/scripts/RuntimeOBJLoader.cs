using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// 运行时OBJ模型加载器
/// 负责解析OBJ文件并创建Unity GameObject（支持材质和贴图）
/// </summary>
public class RuntimeOBJLoader : MonoBehaviour
{
    [Header("模型设置")]
    [Tooltip("默认材质")]
    public Material defaultMaterial;

    /// <summary>
    /// 从字节数组加载OBJ模型（带材质和贴图）
    /// </summary>
    public GameObject LoadOBJWithTexture(Dictionary<string, byte[]> files, string modelName = "GeneratedModel")
    {
        if (files == null || !files.ContainsKey("obj"))
        {
            Debug.LogError("❌ OBJ数据为空！");
            return null;
        }

        try
        {
            // 解析OBJ
            string objText = Encoding.UTF8.GetString(files["obj"]);
            GameObject model = ParseOBJ(objText, modelName);
            
            if (model == null)
                return null;

            // 如果有贴图，应用到模型
            if (files.ContainsKey("texture"))
            {
                Debug.Log("🎨 检测到贴图文件，正在应用...");
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(files["texture"]))
                {
                    Debug.Log($"✅ 贴图加载成功！尺寸: {texture.width}x{texture.height}");
                    
                    // 创建材质并应用贴图
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.mainTexture = texture;
                    
                    // 应用到所有MeshRenderer
                    MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.material = mat;
                    }
                    
                    Debug.Log($"✅ 贴图已应用到 {renderers.Length} 个渲染器");
                }
                else
                {
                    Debug.LogWarning("⚠️ 贴图加载失败");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到贴图文件，使用默认材质");
            }
            
            return model;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 加载OBJ失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从字节数组加载OBJ模型（简单版本，无贴图）
    /// </summary>
    public GameObject LoadOBJFromBytes(byte[] objData, string modelName = "GeneratedModel")
    {
        if (objData == null || objData.Length == 0)
        {
            Debug.LogError("❌ OBJ数据为空！");
            return null;
        }

        try
        {
            // 将字节转换为文本
            string objText = Encoding.UTF8.GetString(objData);
            
            // 解析OBJ
            GameObject model = ParseOBJ(objText, modelName);
            
            return model;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 加载OBJ失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析OBJ文本格式
    /// </summary>
    private GameObject ParseOBJ(string objText, string modelName)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // 用于重建索引
        Dictionary<string, int> vertexIndexMap = new Dictionary<string, int>();
        List<Vector3> finalVertices = new List<Vector3>();
        List<Vector3> finalNormals = new List<Vector3>();
        List<Vector2> finalUVs = new List<Vector2>();

        string[] lines = objText.Split('\n');

        Debug.Log($"📄 开始解析OBJ文件，共 {lines.Length} 行");

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            string[] parts = trimmedLine.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0)
                continue;

            switch (parts[0])
            {
                case "v": // 顶点坐标
                    if (parts.Length >= 4)
                    {
                        float x = ParseFloat(parts[1]);
                        float y = ParseFloat(parts[2]);
                        float z = ParseFloat(parts[3]);
                        // Unity使用左手坐标系，需要翻转Z轴
                        vertices.Add(new Vector3(-x, y, z));
                    }
                    break;

                case "vn": // 法线
                    if (parts.Length >= 4)
                    {
                        float x = ParseFloat(parts[1]);
                        float y = ParseFloat(parts[2]);
                        float z = ParseFloat(parts[3]);
                        normals.Add(new Vector3(-x, y, z));
                    }
                    break;

                case "vt": // UV坐标
                    if (parts.Length >= 3)
                    {
                        float u = ParseFloat(parts[1]);
                        float v = ParseFloat(parts[2]);
                        uvs.Add(new Vector2(u, v));
                    }
                    break;

                case "f": // 面
                    if (parts.Length >= 4)
                    {
                        // OBJ面可以是三角形或四边形，这里处理三角形
                        int[] faceIndices = new int[parts.Length - 1];
                        
                        for (int i = 1; i < parts.Length; i++)
                        {
                            string vertexData = parts[i];
                            
                            if (!vertexIndexMap.ContainsKey(vertexData))
                            {
                                // 解析 v/vt/vn 格式
                                string[] indices = vertexData.Split('/');
                                
                                int vIndex = int.Parse(indices[0]) - 1; // OBJ索引从1开始
                                int vtIndex = indices.Length > 1 && !string.IsNullOrEmpty(indices[1]) ? int.Parse(indices[1]) - 1 : -1;
                                int vnIndex = indices.Length > 2 && !string.IsNullOrEmpty(indices[2]) ? int.Parse(indices[2]) - 1 : -1;

                                // 添加顶点数据
                                finalVertices.Add(vertices[vIndex]);
                                
                                if (vnIndex >= 0 && vnIndex < normals.Count)
                                    finalNormals.Add(normals[vnIndex]);
                                else
                                    finalNormals.Add(Vector3.up);

                                if (vtIndex >= 0 && vtIndex < uvs.Count)
                                    finalUVs.Add(uvs[vtIndex]);
                                else
                                    finalUVs.Add(Vector2.zero);

                                vertexIndexMap[vertexData] = finalVertices.Count - 1;
                            }

                            faceIndices[i - 1] = vertexIndexMap[vertexData];
                        }

                        // 添加三角形（注意Unity的顺时针顺序）
                        if (faceIndices.Length == 3)
                        {
                            triangles.Add(faceIndices[0]);
                            triangles.Add(faceIndices[2]);
                            triangles.Add(faceIndices[1]);
                        }
                        else if (faceIndices.Length == 4) // 四边形转两个三角形
                        {
                            triangles.Add(faceIndices[0]);
                            triangles.Add(faceIndices[2]);
                            triangles.Add(faceIndices[1]);

                            triangles.Add(faceIndices[0]);
                            triangles.Add(faceIndices[3]);
                            triangles.Add(faceIndices[2]);
                        }
                    }
                    break;
            }
        }

        Debug.Log($"📊 解析完成: {finalVertices.Count} 顶点, {triangles.Count / 3} 三角形, {finalUVs.Count} UV坐标");

        // 创建Mesh
        Mesh mesh = new Mesh();
        mesh.name = modelName;
        mesh.vertices = finalVertices.ToArray();
        mesh.normals = finalNormals.ToArray();
        mesh.uv = finalUVs.ToArray();
        mesh.triangles = triangles.ToArray();

        // 如果没有法线，自动计算
        if (finalNormals.Count == 0 || finalNormals[0] == Vector3.up)
        {
            Debug.Log("🔧 重新计算法线");
            mesh.RecalculateNormals();
        }

        mesh.RecalculateBounds();

        // 创建GameObject
        GameObject modelObject = new GameObject(modelName);
        MeshFilter meshFilter = modelObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = modelObject.AddComponent<MeshRenderer>();
        
        // 使用默认材质或创建新材质
        if (defaultMaterial != null)
        {
            meshRenderer.material = defaultMaterial;
        }
        else
        {
            meshRenderer.material = new Material(Shader.Find("Standard"));
            meshRenderer.material.color = Color.white;
        }

        Debug.Log($"✅ OBJ加载成功: {finalVertices.Count} 顶点, {triangles.Count / 3} 三角形");

        return modelObject;
    }

    /// <summary>
    /// 安全解析浮点数（处理不同的小数点格式）
    /// </summary>
    private float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }
}
