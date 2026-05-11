using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时环境切换：切换场景模型（Prefab）与基础灯光/天空盒等。
/// 不会影响生成的模型（只管理 environmentParent 下的实例）。
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentRuntimeSwitcher : MonoBehaviour
{
    [Serializable]
    public class EnvironmentEntry
    {
        public string displayName;
        public Sprite thumbnail;

        [Tooltip("环境场景模型（展厅/展台/背景等）。可为空：只切换灯光/天空盒。")]
        public GameObject environmentPrefab;

        [Header("Lighting (optional)")]
        [Tooltip("切换到该环境时启用这些灯光，其他 managedLights 会被禁用。留空则不改灯光。")]
        public Light[] lightsToEnable;

        [Tooltip("可选：切换天空盒材质；留空则不改。")]
        public Material skyboxMaterial;
    }

    [Header("Environment list")]
    [SerializeField] private EnvironmentEntry[] environments;

    [Header("Instantiation")]
    [Tooltip("环境实例的父物体。建议放一个空物体 EnvironmentRoot。")]
    [SerializeField] private Transform environmentParent;

    [Header("Optional: managed lights")]
    [Tooltip("若配置了此列表，则切环境时先禁用这些灯光，再按 entry.lightsToEnable 逐个启用。")]
    [SerializeField] private Light[] managedLights;

    [Header("Runtime")]
    [Tooltip("启动时自动应用第一个环境，并确保只显示一个环境。")]
    [SerializeField] private bool applyFirstEnvironmentOnStart = true;

    public int CurrentIndex { get; private set; } = -1;
    public EnvironmentEntry Current => (environments != null && CurrentIndex >= 0 && CurrentIndex < environments.Length)
        ? environments[CurrentIndex]
        : null;

    // 仅对 Prefab 资产按 index 缓存实例；场景对象不实例化，只做显隐切换。
    private readonly Dictionary<int, GameObject> _runtimeInstances = new Dictionary<int, GameObject>();

    public int Count => environments?.Length ?? 0;

    public EnvironmentEntry Get(int index)
    {
        if (environments == null || index < 0 || index >= environments.Length)
            return null;
        return environments[index];
    }

    private void Start()
    {
        if (environments == null || environments.Length == 0)
            return;

        // 启动先全隐藏，避免“多个场景同时可见”。
        SetAllEnvironmentVisualsActive(false);

        if (applyFirstEnvironmentOnStart)
            Apply(0);
    }

    public void Apply(int index)
    {
        if (environments == null || environments.Length == 0)
        {
            Debug.LogWarning("[EnvironmentRuntimeSwitcher] 未配置 environments。", this);
            return;
        }

        index = Mathf.Clamp(index, 0, environments.Length - 1);
        var entry = environments[index];
        bool sameIndex = index == CurrentIndex;
        CurrentIndex = index;
        Debug.Log($"[EnvironmentRuntimeSwitcher] 应用环境 index={index}, name={entry?.displayName}, prefab={(entry?.environmentPrefab != null ? entry.environmentPrefab.name : "<null>")}", this);

        // 1) 场景模型：先全部隐藏，再只显示当前。
        SetAllEnvironmentVisualsActive(false);
        SetEnvironmentVisualActive(index, true);

        // 2) 灯光
        if (managedLights != null && managedLights.Length > 0)
        {
            for (int i = 0; i < managedLights.Length; i++)
            {
                if (managedLights[i] != null)
                    managedLights[i].enabled = false;
            }

            if (entry.lightsToEnable != null)
            {
                for (int i = 0; i < entry.lightsToEnable.Length; i++)
                {
                    if (entry.lightsToEnable[i] != null)
                        entry.lightsToEnable[i].enabled = true;
                }
            }
        }

        // 3) Skybox
        if (entry.skyboxMaterial != null)
            RenderSettings.skybox = entry.skyboxMaterial;

        if (entry.environmentPrefab == null && (entry.lightsToEnable == null || entry.lightsToEnable.Length == 0) && entry.skyboxMaterial == null)
            Debug.LogWarning($"[EnvironmentRuntimeSwitcher] 环境 \"{entry?.displayName}\" 未配置 prefab/灯光/skybox，视觉上不会变化。", this);
        else if (sameIndex)
            Debug.Log($"[EnvironmentRuntimeSwitcher] 重复点击同一环境 index={index}，已仅做可见性校正。", this);
    }

    private void SetAllEnvironmentVisualsActive(bool active)
    {
        if (environments == null) return;
        for (int i = 0; i < environments.Length; i++)
            SetEnvironmentVisualActive(i, active);
    }

    private void SetEnvironmentVisualActive(int index, bool active)
    {
        if (environments == null || index < 0 || index >= environments.Length)
            return;

        var entry = environments[index];
        if (entry == null || entry.environmentPrefab == null)
            return;

        var source = entry.environmentPrefab;
        if (IsSceneObject(source))
        {
            source.SetActive(active);
            return;
        }

        // Prefab 资产：按 index 懒加载一次，后续仅显隐，不重复实例化。
        if (!_runtimeInstances.TryGetValue(index, out var instance) || instance == null)
        {
            if (!active)
                return; // 需要显示时再创建

            Transform parent = environmentParent != null ? environmentParent : transform;
            instance = Instantiate(source, parent);
            instance.name = source.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            _runtimeInstances[index] = instance;
        }

        instance.SetActive(active);
    }

    private static bool IsSceneObject(GameObject go)
    {
        // 场景内对象：scene 有效且不是持久化资产
        return go != null && go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.name);
    }
}

