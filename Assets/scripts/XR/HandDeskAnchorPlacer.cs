using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

/// <summary>
/// 使用右手捏合将 UI 锚定到真实桌面（或任意可射线命中的表面）。
/// 依赖 XR Hands 子系统。
/// </summary>
public class HandDeskAnchorPlacer : MonoBehaviour
{
    [Header("锚点与UI")]
    [Tooltip("桌面锚点。未设置时会自动创建 DeskAnchor。")]
    [SerializeField] private Transform deskAnchor;
    [Tooltip("需要绑定到桌面锚点下的 UI 根节点。")]
    [SerializeField] private List<Transform> uiRootsToAttach = new List<Transform>();
    [Tooltip("命中后 UI 相对桌面的高度偏移（米）。")]
    [SerializeField] private float uiHeightOffset = 0.02f;

    [Header("射线与目标")]
    [Tooltip("可被锚定的层（桌面/场景网格）。")]
    [SerializeField] private LayerMask anchorLayerMask = ~0;
    [SerializeField] private float rayDistance = 3f;
    [Tooltip("参考相机（用于决定锚点朝向）。")]
    [SerializeField] private Transform viewForwardSource;
    [Tooltip("可选：预览点。命中时会移动到命中位置。")]
    [SerializeField] private Transform previewMarker;

    [Header("捏合判定")]
    [Tooltip("进入捏合阈值（米）。")]
    [SerializeField] private float pinchStartDistance = 0.025f;
    [Tooltip("退出捏合阈值（米）。")]
    [SerializeField] private float pinchEndDistance = 0.04f;
    [Tooltip("捏合保持时是否持续更新锚点位置。")]
    [SerializeField] private bool continuousUpdateWhilePinching = true;

    [Header("调试")]
    [SerializeField] private bool verboseLog = false;
    [SerializeField] private bool placementEnabled = true;

    private XRHandSubsystem handSubsystem;
    private bool isPinching;
    private bool warnedNoHandsSubsystem;

    public bool PlacementEnabled => placementEnabled;

    private void Awake()
    {
        EnsureAnchor();
        AttachUIRoots();

        if (viewForwardSource == null && Camera.main != null)
            viewForwardSource = Camera.main.transform;
    }

    private void Update()
    {
        if (!placementEnabled)
        {
            SetPreviewActive(false);
            isPinching = false;
            return;
        }

        EnsureHandSubsystem();
        if (handSubsystem == null)
            return;

        if (!TryGetPinchData(out Vector3 thumbTip, out Vector3 indexTip, out Vector3 wrist, out float pinchDistance))
        {
            SetPreviewActive(false);
            isPinching = false;
            return;
        }

        bool pinchNow = isPinching
            ? pinchDistance <= pinchEndDistance
            : pinchDistance <= pinchStartDistance;

        Vector3 rayOrigin = indexTip;
        Vector3 rayDirection = (indexTip - wrist).sqrMagnitude > 1e-6f
            ? (indexTip - wrist).normalized
            : transform.forward;

        bool hasHit = Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayDistance, anchorLayerMask, QueryTriggerInteraction.Ignore);
        UpdatePreview(hasHit, hit);

        if (!hasHit)
        {
            isPinching = pinchNow;
            return;
        }

        bool pinchStarted = pinchNow && !isPinching;
        bool pinchHolding = pinchNow && isPinching;

        if (pinchStarted || (continuousUpdateWhilePinching && pinchHolding))
            PlaceAnchor(hit.point, hit.normal);

        isPinching = pinchNow;
    }

    private void EnsureAnchor()
    {
        if (deskAnchor != null)
            return;

        GameObject anchor = new GameObject("DeskAnchor");
        deskAnchor = anchor.transform;
    }

    private void AttachUIRoots()
    {
        if (deskAnchor == null) return;
        for (int i = 0; i < uiRootsToAttach.Count; i++)
        {
            var root = uiRootsToAttach[i];
            if (root == null) continue;
            root.SetParent(deskAnchor, true);
        }
    }

    private void EnsureHandSubsystem()
    {
        if (handSubsystem != null) return;

        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        handSubsystem = loader?.GetLoadedSubsystem<XRHandSubsystem>();

        if (handSubsystem == null && !warnedNoHandsSubsystem)
        {
            warnedNoHandsSubsystem = true;
            Debug.LogWarning("[HandDeskAnchorPlacer] 未找到 XRHandSubsystem。请确认已启用 OpenXR Hand Tracking。");
        }
    }

    private bool TryGetPinchData(out Vector3 thumbTip, out Vector3 indexTip, out Vector3 wrist, out float pinchDistance)
    {
        thumbTip = indexTip = wrist = Vector3.zero;
        pinchDistance = float.MaxValue;

        XRHand rightHand = handSubsystem.rightHand;
        if (!rightHand.isTracked) return false;

        var thumbTipJoint = rightHand.GetJoint(XRHandJointID.ThumbTip);
        var indexTipJoint = rightHand.GetJoint(XRHandJointID.IndexTip);
        var wristJoint = rightHand.GetJoint(XRHandJointID.Wrist);

        if (!thumbTipJoint.TryGetPose(out Pose thumbPose)) return false;
        if (!indexTipJoint.TryGetPose(out Pose indexPose)) return false;
        if (!wristJoint.TryGetPose(out Pose wristPose)) return false;

        thumbTip = thumbPose.position;
        indexTip = indexPose.position;
        wrist = wristPose.position;
        pinchDistance = Vector3.Distance(thumbTip, indexTip);

        if (verboseLog)
            Debug.Log($"[HandDeskAnchorPlacer] pinchDistance={pinchDistance:F4}");

        return true;
    }

    private void PlaceAnchor(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (deskAnchor == null) return;

        Vector3 position = hitPoint + hitNormal.normalized * uiHeightOffset;
        Vector3 cameraForward = viewForwardSource != null ? viewForwardSource.forward : Vector3.forward;
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(cameraForward, hitNormal).normalized;
        if (forwardOnPlane.sqrMagnitude < 1e-6f)
            forwardOnPlane = Vector3.ProjectOnPlane(Vector3.forward, hitNormal).normalized;

        Quaternion rotation = Quaternion.LookRotation(forwardOnPlane, hitNormal);
        deskAnchor.SetPositionAndRotation(position, rotation);
    }

    private void UpdatePreview(bool hasHit, RaycastHit hit)
    {
        if (previewMarker == null) return;
        previewMarker.gameObject.SetActive(hasHit);
        if (hasHit)
            previewMarker.position = hit.point;
    }

    private void SetPreviewActive(bool active)
    {
        if (previewMarker != null)
            previewMarker.gameObject.SetActive(active);
    }

    public void SetPlacementEnabled(bool enabled)
    {
        placementEnabled = enabled;
        if (!placementEnabled)
        {
            isPinching = false;
            SetPreviewActive(false);
        }
    }

    public void TogglePlacementEnabled()
    {
        SetPlacementEnabled(!placementEnabled);
    }
}
