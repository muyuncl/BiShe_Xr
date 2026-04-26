using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 双锚点桌面构建控制器：
/// - 以左右两个锚点“底部端点”作为桌面对角点
/// - 约束锚点不旋转、两锚点同高度
/// - 可选：启动时将两锚点初始化到 OpenXRLeftHand/OpenXRRightHand
/// - 输出 DeskAnchor 位姿，并可把目标 UI 根节点对齐到 DeskAnchor（不改父级）
/// </summary>
public class DualAnchorDeskPlacementController : MonoBehaviour, IDeskAnchorPlacementController
{
    [Header("双锚点对象")]
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform rightAnchor;

    [Header("底部端点（可选）")]
    [Tooltip("左锚点底部端点（推荐：锚点模型最下方的子节点）。为空则自动从锚点包围盒推算。")]
    [SerializeField] private Transform leftBottomPoint;
    [Tooltip("右锚点底部端点（推荐：锚点模型最下方的子节点）。为空则自动从锚点包围盒推算。")]
    [SerializeField] private Transform rightBottomPoint;

    [Header("桌面输出")]
    [Tooltip("桌面输出锚点。为空会自动创建 DeskAnchor。")]
    [SerializeField] private Transform deskAnchor;
    [SerializeField] private float deskHeightOffset = 0.0f;

    [Header("页面/内容对齐（不改父级）")]
    [SerializeField] private List<Transform> rootsToAlign = new List<Transform>();

    [Header("初始化（可选）")]
    [SerializeField] private bool initializeFromHandsOnStart = true;
    [SerializeField] private Transform openXRLeftHand;
    [SerializeField] private Transform openXRRightHand;
    [SerializeField] private string leftHandName = "OpenXRLeftHand";
    [SerializeField] private string rightHandName = "OpenXRRightHand";

    [Header("约束")]
    [SerializeField] private bool lockAnchorRotation = true;
    [SerializeField] private bool forceSameHeight = true;
    [SerializeField] private bool verboseLog = false;

    [Header("运行开关")]
    [SerializeField] private bool placementEnabled = true;

    private Quaternion leftInitialRotation;
    private Quaternion rightInitialRotation;
    private bool initialized;

    public bool PlacementEnabled => placementEnabled;

    private void Awake()
    {
        EnsureDeskAnchor();
        CaptureInitialRotations();
    }

    private void Start()
    {
        if (initializeFromHandsOnStart)
            InitializeAnchorsFromHands();

        initialized = true;
        RefreshDeskAnchor();
    }

    private void LateUpdate()
    {
        if (!initialized || !placementEnabled)
            return;

        ApplyAnchorConstraints();
        RefreshDeskAnchor();
    }

    public void SetPlacementEnabled(bool enabled)
    {
        placementEnabled = enabled;
    }

    public void TogglePlacementEnabled()
    {
        placementEnabled = !placementEnabled;
    }

    [ContextMenu("Initialize Anchors From Hands")]
    public void InitializeAnchorsFromHands()
    {
        ResolveHandRefsIfNeeded();
        if (leftAnchor == null || rightAnchor == null)
            return;

        if (openXRLeftHand != null)
            leftAnchor.position = openXRLeftHand.position;
        if (openXRRightHand != null)
            rightAnchor.position = openXRRightHand.position;

        if (verboseLog)
            Debug.Log("[DualAnchorDeskPlacementController] Anchors initialized from hand transforms.");
    }

    [ContextMenu("Refresh Desk Anchor")]
    public void RefreshDeskAnchor()
    {
        if (leftAnchor == null || rightAnchor == null || deskAnchor == null)
            return;

        Vector3 pL = GetBottomWorldPoint(leftAnchor, leftBottomPoint);
        Vector3 pR = GetBottomWorldPoint(rightAnchor, rightBottomPoint);

        if (forceSameHeight)
        {
            float y = 0.5f * (pL.y + pR.y);
            pL.y = y;
            pR.y = y;
        }

        Vector3 center = 0.5f * (pL + pR);
        center.y += deskHeightOffset;

        // 以对角线方向作为 X 轴，构建水平桌面朝向
        Vector3 right = (pR - pL);
        right.y = 0f;
        if (right.sqrMagnitude < 1e-6f)
            right = Vector3.right;
        right.Normalize();

        Vector3 up = Vector3.up;
        Vector3 forward = Vector3.Cross(right, up).normalized;
        if (forward.sqrMagnitude < 1e-6f)
            forward = Vector3.forward;

        deskAnchor.SetPositionAndRotation(center, Quaternion.LookRotation(forward, up));
        AlignRootsToDeskAnchor();
    }

    private void ApplyAnchorConstraints()
    {
        if (leftAnchor == null || rightAnchor == null)
            return;

        if (forceSameHeight)
        {
            float y = 0.5f * (leftAnchor.position.y + rightAnchor.position.y);
            Vector3 lp = leftAnchor.position;
            Vector3 rp = rightAnchor.position;
            lp.y = y;
            rp.y = y;
            leftAnchor.position = lp;
            rightAnchor.position = rp;
        }

        if (lockAnchorRotation)
        {
            leftAnchor.rotation = leftInitialRotation;
            rightAnchor.rotation = rightInitialRotation;
        }
    }

    private void AlignRootsToDeskAnchor()
    {
        for (int i = 0; i < rootsToAlign.Count; i++)
        {
            Transform t = rootsToAlign[i];
            if (t == null) continue;
            // 只对齐世界位置：不改 UI 的旋转与缩放
            t.position = deskAnchor.position;
        }
    }

    private Vector3 GetBottomWorldPoint(Transform anchor, Transform explicitBottom)
    {
        if (explicitBottom != null)
            return explicitBottom.position;

        var renderers = anchor.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return anchor.position;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        Vector3 p = anchor.position;
        p.y = b.min.y;
        return p;
    }

    private void EnsureDeskAnchor()
    {
        if (deskAnchor != null) return;
        var go = GameObject.Find("DeskAnchor");
        if (go == null) go = new GameObject("DeskAnchor");
        deskAnchor = go.transform;
    }

    private void CaptureInitialRotations()
    {
        leftInitialRotation = leftAnchor != null ? leftAnchor.rotation : Quaternion.identity;
        rightInitialRotation = rightAnchor != null ? rightAnchor.rotation : Quaternion.identity;
    }

    private void ResolveHandRefsIfNeeded()
    {
        if (openXRLeftHand == null || openXRRightHand == null)
        {
            Transform[] all = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (openXRLeftHand == null && all[i].name == leftHandName)
                    openXRLeftHand = all[i];
                if (openXRRightHand == null && all[i].name == rightHandName)
                    openXRRightHand = all[i];
            }
        }
    }
}

