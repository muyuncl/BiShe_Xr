using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 模型生成 World Space UI：材质横滑、中央状态、右侧生成；与 <see cref="VRPhotoTo3DController"/> 对接。
/// </summary>
public class ModelGenUIController : MonoBehaviour
{
    [Serializable]
    public class MaterialEntry
    {
        public string id = "material_id";
        public string displayName = "材质";
        public string englishName = "Material Name";
        [Tooltip("可选；留空则使用下方 Preview Color 作纯色底")]
        public Sprite previewImage;
        [Tooltip("上传到 ComfyUI 的材质参考图")]
        public Texture2D referenceImage;
        public Color previewColor = new Color(0.25f, 0.3f, 0.4f, 1f);
    }

    [Header("Optional: 照片转3D")]
    [SerializeField] private VRPhotoTo3DController photoTo3D;

    [Header("UI")]
    [SerializeField] private Button generateButton;
    [SerializeField] private Button libraryButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("材质配表")]
    [SerializeField] private ModelGenMaterialCatalog materialCatalog;
    [SerializeField] private List<MaterialEntry> materials = new List<MaterialEntry>();

    [Header("引用（由生成器或手动绑定）")]
    [SerializeField] private Transform materialContent;
    [SerializeField] private ModelGenMaterialCardUI materialCardTemplate;

    private readonly List<ModelGenMaterialCardUI> _matCards = new List<ModelGenMaterialCardUI>();
    private readonly List<MaterialEntry> _resolvedMaterials = new List<MaterialEntry>();

    private int _matIndex;

    private void Reset()
    {
        ApplyDefaultTablesIfEmpty();
    }

    private void Start()
    {
        ApplyDefaultTablesIfEmpty();
        WirePhotoTo3D();
        WireGenerateClick();
        BuildMaterialUI();
    }

    private void WirePhotoTo3D()
    {
        if (photoTo3D == null)
            return;
        if (generateButton != null)
            photoTo3D.generateButton = generateButton;
        if (statusText != null)
            photoTo3D.statusText = statusText;
    }

    private void WireGenerateClick()
    {
        if (generateButton == null || photoTo3D == null)
            return;
        generateButton.onClick.RemoveListener(photoTo3D.OnGenerateButtonClick);
        generateButton.onClick.AddListener(photoTo3D.OnGenerateButtonClick);
    }


    public void SetPhotoController(VRPhotoTo3DController c)
    {
        photoTo3D = c;
        WirePhotoTo3D();
    }

    private void ApplyDefaultTablesIfEmpty()
    {
        if (materialCatalog != null && materialCatalog.Materials != null && materialCatalog.Materials.Count > 0)
            return;

        if (materials == null || materials.Count == 0)
        {
            materials = new List<MaterialEntry>
            {
                new MaterialEntry { displayName = "青花瓷", previewColor = new Color(0.2f, 0.28f, 0.6f) },
                new MaterialEntry { displayName = "铜胎", previewColor = new Color(0.55f, 0.38f, 0.22f) },
                new MaterialEntry { displayName = "原木", previewColor = new Color(0.42f, 0.3f, 0.22f) },
                new MaterialEntry { displayName = "玉质", previewColor = new Color(0.25f, 0.55f, 0.45f) },
                new MaterialEntry { displayName = "哑光釉", previewColor = new Color(0.45f, 0.47f, 0.5f) },
            };
        }
    }

    private void BuildMaterialUI()
    {
        _matCards.Clear();
        _resolvedMaterials.Clear();
        ResolveMaterials();

        if (materialContent != null)
        {
            for (int i = materialContent.childCount - 1; i >= 0; i--)
            {
                var child = materialContent.GetChild(i);
                if (materialCardTemplate != null && child == materialCardTemplate.transform)
                    continue;
                Destroy(child.gameObject);
            }
        }

        if (materialCardTemplate != null && materialContent != null)
        {
            for (int i = 0; i < _resolvedMaterials.Count; i++)
            {
                int idx = i;
                var m = _resolvedMaterials[i];
                var inst = Instantiate(materialCardTemplate, materialContent);
                inst.gameObject.SetActive(true);
                inst.SetData(m.previewImage, m.displayName, m.englishName, m.previewColor);
                inst.Clicked += _ => SelectMat(idx);
                _matCards.Add(inst);
            }

            SelectMat(0);
        }

        if (statusText != null && string.IsNullOrEmpty(statusText.text))
            statusText.text = "准备就绪";
    }

    private void SelectMat(int i)
    {
        if (_matCards.Count == 0)
            return;
        _matIndex = Mathf.Clamp(i, 0, _matCards.Count - 1);
        for (int j = 0; j < _matCards.Count; j++)
            _matCards[j].SetSelected(j == _matIndex);

        if (_matIndex >= 0 && _matIndex < _resolvedMaterials.Count && photoTo3D != null)
        {
            var selected = _resolvedMaterials[_matIndex];
            photoTo3D.SetSelectedMaterialReference(selected.displayName, selected.referenceImage);
            if (statusText != null)
                statusText.text = $"已选择材质：{selected.displayName}";
        }
    }

    private void ResolveMaterials()
    {
        if (materialCatalog != null && materialCatalog.Materials != null && materialCatalog.Materials.Count > 0)
        {
            foreach (var item in materialCatalog.Materials)
            {
                if (item == null) continue;
                _resolvedMaterials.Add(new MaterialEntry
                {
                    id = item.id,
                    displayName = item.displayName,
                    englishName = item.englishName,
                    previewImage = item.previewSprite,
                    referenceImage = item.referenceImage,
                    previewColor = item.fallbackPreviewColor
                });
            }
            return;
        }

        _resolvedMaterials.AddRange(materials);
    }
}
