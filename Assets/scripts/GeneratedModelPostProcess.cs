using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;

/// <summary>
/// 生成模型放置后处理：按高度等比缩放、按 Interaction SDK「Add Grab Interaction」向导逻辑添加抓取。
/// </summary>
public static class GeneratedModelPostProcess
{
    const string HandGrabChildName = "ISDK_HandGrabInteraction";

    /// <summary>
    /// 等比缩放，使所有 Renderer 的世界空间 AABB 高度等于 targetHeightMeters。
    /// </summary>
    public static void ApplyUniformWorldHeight(GameObject root, float targetHeightMeters, float extraScaleMultiplier = 1f)
    {
        if (root == null || targetHeightMeters <= 1e-5f)
            return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[GeneratedModelPostProcess] 无 Renderer，跳过高度缩放");
            return;
        }

        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled) continue;
            world.Encapsulate(renderers[i].bounds);
        }

        float h = world.size.y;
        if (h < 1e-5f)
        {
            Debug.LogWarning("[GeneratedModelPostProcess] 包围盒高度为 0，跳过缩放");
            return;
        }

        float uniform = (targetHeightMeters / h) * Mathf.Max(0.001f, extraScaleMultiplier);
        root.transform.localScale = root.transform.localScale * uniform;
    }

    /// <summary>
    /// 与 Meta Interaction SDK 菜单「Add Grab Interaction」向导（GrabWizard）一致：
    /// Rigidbody（无重力、运动学）、<see cref="Grabbable"/>、子物体上的
    /// <see cref="HandGrabInteractable"/> + <see cref="GrabInteractable"/>；若无 Collider 则按 Renderer 包络生成触发器 BoxCollider。
    /// </summary>
    public static void AddGrabInteraction(GameObject root)
    {
        if (root == null)
            return;

        // 与 XRI 抓取二选一：去掉旧组件避免双套交互
        var xriGrab = root.GetComponent<XRGrabInteractable>();
        if (xriGrab != null)
            Object.Destroy(xriGrab);

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
            rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        if (!HasColliderUnderRigidbody(rb))
        {
            if (TryEncapsulateRenderersLikeGrabWizard(root, out Bounds localBounds))
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = localBounds.center;
                box.size = localBounds.size;
                box.isTrigger = true;
            }
            else if (root.TryGetComponent<RectTransform>(out var rect))
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = rect.rect.center;
                box.size = new Vector3(rect.rect.size.x, rect.rect.size.y, 0f);
                box.isTrigger = true;
            }
            else
            {
                var sphere = root.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
            }
        }

        Grabbable grabbable = root.GetComponent<Grabbable>();
        if (grabbable == null)
            grabbable = root.AddComponent<Grabbable>();
        grabbable.InjectOptionalTargetTransform(root.transform);
        grabbable.InjectOptionalRigidbody(rb);

        Transform existingChild = root.transform.Find(HandGrabChildName);
        if (existingChild != null &&
            existingChild.GetComponent<HandGrabInteractable>() != null &&
            existingChild.GetComponent<GrabInteractable>() != null)
            return;

        if (existingChild != null)
            Object.Destroy(existingChild.gameObject);

        var handGo = new GameObject(HandGrabChildName);
        handGo.transform.SetParent(root.transform, false);
        handGo.transform.localPosition = Vector3.zero;
        handGo.transform.localRotation = Quaternion.identity;
        handGo.transform.localScale = Vector3.one;

        var handInteractable = handGo.AddComponent<HandGrabInteractable>();
        var grabInteractable = handGo.AddComponent<GrabInteractable>();

        handInteractable.InjectRigidbody(rb);
        handInteractable.InjectSupportedGrabTypes(GrabTypeFlags.All);
        handInteractable.InjectOptionalPointableElement(grabbable);

        grabInteractable.InjectRigidbody(rb);
        grabInteractable.InjectOptionalPointableElement(grabbable);
    }

    private static bool HasColliderUnderRigidbody(Rigidbody rb)
    {
        return rb != null && rb.gameObject.GetComponentInChildren<Collider>(true) != null;
    }

    /// <summary>
    /// 对齐 Editor QuickActions <c>Utils.TryEncapsulateRenderers</c>（仅 Mesh / Skinned / Sprite）。
    /// </summary>
    private static bool TryEncapsulateRenderersLikeGrabWizard(GameObject obj, out Bounds localBounds)
    {
        var filtered = new List<Renderer>();
        foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer || r is SpriteRenderer)
                filtered.Add(r);
        }

        if (filtered.Count == 0)
        {
            localBounds = default;
            return false;
        }

        Transform GetRendererTransform(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.rootBone != null)
                return skinned.rootBone;
            return renderer.transform;
        }

        void EncapsulateRenderer(ref Bounds bounds, Renderer renderer)
        {
            void Encapsulate(ref Bounds b, Vector3 point)
            {
                b.Encapsulate(obj.transform.InverseTransformPoint(
                    GetRendererTransform(renderer).TransformPoint(point)));
            }

            Vector3 center = renderer.localBounds.center;
            Vector3 extents = renderer.localBounds.extents;

            Encapsulate(ref bounds, center + extents);
            Encapsulate(ref bounds, center + new Vector3(-extents.x, extents.y, extents.z));
            Encapsulate(ref bounds, center + new Vector3(extents.x, extents.y, -extents.z));
            Encapsulate(ref bounds, center + new Vector3(-extents.x, extents.y, -extents.z));
            Encapsulate(ref bounds, center + new Vector3(extents.x, -extents.y, extents.z));
            Encapsulate(ref bounds, center + new Vector3(-extents.x, -extents.y, extents.z));
            Encapsulate(ref bounds, center + new Vector3(extents.x, -extents.y, -extents.z));
            Encapsulate(ref bounds, center - extents);
        }

        localBounds = new Bounds(
            obj.transform.InverseTransformPoint(
                GetRendererTransform(filtered[0]).TransformPoint(filtered[0].localBounds.center)),
            Vector3.zero);

        for (int i = 0; i < filtered.Count; i++)
            EncapsulateRenderer(ref localBounds, filtered[i]);

        return true;
    }
}
