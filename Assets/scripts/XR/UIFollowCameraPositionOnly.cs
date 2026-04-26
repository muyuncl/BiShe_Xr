using UnityEngine;

/// <summary>
/// 仅跟随相机位置变化，不跟随相机旋转。
/// 适用于：用户移动时 UI 跟着平移，原地转头时 UI 保持世界位置不变。
/// </summary>
public class UIFollowCameraPositionOnly : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("需要跟随的 UI 根节点。为空则使用当前物体。")]
    [SerializeField] private Transform uiRoot;

    [Header("轴向开关")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = true;

    [Header("世界坐标约束")]
    [Tooltip("启用后，UI 的世界 Z 坐标会被锁定为初始值。")]
    [SerializeField] private bool lockWorldZ = false;
    [Tooltip("启用后，UI 的世界旋转会被锁定为初始值，不跟随父级/相机旋转。")]
    [SerializeField] private bool lockWorldRotation = false;

    [Header("平滑与防抖")]
    [Tooltip("启动时自动脱离父节点，避免父级旋转导致抖动。")]
    [SerializeField] private bool detachFromParentOnStart = true;
    [Tooltip("相机微小抖动过滤阈值（米）。")]
    [SerializeField] private float movementDeadzone = 0.0025f;
    [Tooltip("位置平滑时间（秒）。越大越稳，越小越跟手。")]
    [SerializeField] private float smoothTime = 0.08f;

    private Vector3 lastCameraPosition;
    private bool initialized;
    private float fixedWorldZ;
    private Quaternion fixedWorldRotation;
    private Vector3 targetPosition;
    private Vector3 smoothVelocity;

    private void Awake()
    {
        if (uiRoot == null)
            uiRoot = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (detachFromParentOnStart && uiRoot != null && uiRoot.parent != null)
            uiRoot.SetParent(null, true);
    }

    private void OnEnable()
    {
        Reinitialize();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || uiRoot == null)
            return;

        if (!initialized)
        {
            lastCameraPosition = cameraTransform.position;
            targetPosition = uiRoot.position;
            initialized = true;
            return;
        }

        Vector3 delta = cameraTransform.position - lastCameraPosition;
        if (!followX) delta.x = 0f;
        if (!followY) delta.y = 0f;
        if (!followZ) delta.z = 0f;

        if (delta.magnitude >= movementDeadzone)
            targetPosition += delta;

        if (lockWorldZ)
            targetPosition.z = fixedWorldZ;

        uiRoot.position = Vector3.SmoothDamp(
            uiRoot.position,
            targetPosition,
            ref smoothVelocity,
            Mathf.Max(0.0001f, smoothTime));

        if (lockWorldRotation)
            uiRoot.rotation = fixedWorldRotation;

        lastCameraPosition = cameraTransform.position;
    }

    public void Reinitialize()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
            if (uiRoot != null)
            {
                fixedWorldZ = uiRoot.position.z;
                fixedWorldRotation = uiRoot.rotation;
                targetPosition = uiRoot.position;
                smoothVelocity = Vector3.zero;
            }
            initialized = true;
        }
        else
        {
            initialized = false;
        }
    }
}
