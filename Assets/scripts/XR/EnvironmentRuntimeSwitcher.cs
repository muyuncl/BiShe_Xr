using System;
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

    public int CurrentIndex { get; private set; } = -1;
    public EnvironmentEntry Current => (environments != null && CurrentIndex >= 0 && CurrentIndex < environments.Length)
        ? environments[CurrentIndex]
        : null;

    private GameObject _spawnedEnvironment;

    public int Count => environments?.Length ?? 0;

    public EnvironmentEntry Get(int index)
    {
        if (environments == null || index < 0 || index >= environments.Length)
            return null;
        return environments[index];
    }

    public void Apply(int index)
    {
        if (environments == null || environments.Length == 0)
        {
            Debug.LogWarning("[EnvironmentRuntimeSwitcher] 未配置 environments。", this);
            return;
        }

        index = Mathf.Clamp(index, 0, environments.Length - 1);
        if (index == CurrentIndex)
            return;

        var entry = environments[index];
        CurrentIndex = index;

        // 1) 场景模型
        if (_spawnedEnvironment != null)
            Destroy(_spawnedEnvironment);
        _spawnedEnvironment = null;

        if (entry.environmentPrefab != null)
        {
            Transform parent = environmentParent != null ? environmentParent : transform;
            _spawnedEnvironment = Instantiate(entry.environmentPrefab, parent);
            _spawnedEnvironment.name = entry.environmentPrefab.name;
            _spawnedEnvironment.transform.localPosition = Vector3.zero;
            _spawnedEnvironment.transform.localRotation = Quaternion.identity;
            _spawnedEnvironment.transform.localScale = Vector3.one;
        }

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
    }
}

