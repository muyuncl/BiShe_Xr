using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModelGenMaterialCatalog", menuName = "ModelGen/Material Catalog")]
public class ModelGenMaterialCatalog : ScriptableObject
{
    [Serializable]
    public class MaterialConfig
    {
        [Tooltip("材质唯一ID，可选")]
        public string id = "material_id";

        [Tooltip("UI展示名")]
        public string displayName = "材质";

        [Tooltip("英文副标题（显示在材质名下方）")]
        public string englishName = "Material Name";

        [Tooltip("卡片预览图（可为空）")]
        public Sprite previewSprite;

        [Tooltip("上传到ComfyUI的材质参考图（建议使用Texture2D）")]
        public Texture2D referenceImage;

        [Tooltip("无预览图时使用该底色")]
        public Color fallbackPreviewColor = new Color(0.25f, 0.3f, 0.4f, 1f);
    }

    [SerializeField] private List<MaterialConfig> materials = new List<MaterialConfig>();

    public IReadOnlyList<MaterialConfig> Materials => materials;
}
