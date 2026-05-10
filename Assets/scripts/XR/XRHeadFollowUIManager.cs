using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 头显前方 World Space UI 的集中管理：由单一 Manager 驱动多个 UI 根节点，无需在每个 UI 上挂跟随脚本。
/// 请将 <see cref="headAnchor"/> 设为场景中的 CenterEyeAnchor（或等价头显参考 Transform）。
/// </summary>
public class XRHeadFollowUIManager : MonoBehaviour
{
    public enum OrientationMode
    {
        /// <summary>与头旋转一致，再叠加 localEulerOffset。</summary>
        AlignWithHead,
        /// <summary>UI 朝向头位置（面向用户），便于阅读。</summary>
        FaceHead,
        /// <summary>仅继承头的水平朝向（绕 Y），再叠加 localEulerOffset。</summary>
        YawOnlyAlignWithHead
    }

    [Serializable]
    public class FollowEntry
    {
        [Tooltip("要跟头的 UI 根物体（一般是 World Space Canvas 的根 Transform）。")]
        public Transform target;

        [Tooltip(
            "运行时：进入 Play 并经过 Startup Delay 后的第一帧，用「当时」Target 与头的相对关系算位移，只存在内存里，不会把数字写回下面 Local Position Offset。\n" +
            "若要在编辑阶段就把数字写进 Inspector，请在本组件标题栏右键选「编辑态捕获…」。")]
        public bool autoCaptureLocalPositionOffset = true;

        [Tooltip("相对 Head Anchor 的局部位置（米）：X 右、Y 上、Z 前。关闭「Auto Capture」时由这里驱动；也可用右键菜单从当前场景写入。")]
        public Vector3 localPositionOffset = new Vector3(0f, -0.25f, 1.2f);

        [Tooltip("额外欧拉角（度）。Align With Head / Yaw Only 时叠在头朝向上；Face Head 时叠在「朝向头」的旋转上。")]
        public Vector3 localEulerOffset = Vector3.zero;

        [Tooltip("Align With Head：UI 与头同向。Face Head：UI 面朝向头（适合读字）。Yaw Only：只跟头的水平转向。若勾选下方「锁定世界旋转」则本项无效。")]
        public OrientationMode orientationMode = OrientationMode.FaceHead;

        [Tooltip("勾选后只更新位置，世界旋转固定为「开始跟随时」Target 上的朝向；不跟头晃。Orientation / Local Euler / Rotation Smoothing 在此模式下无效。")]
        public bool lockWorldRotation;

        [Tooltip("关闭则这一条完全不跟随（相当于暂停）。")]
        public bool enabled = true;

        [Tooltip("位置跟到目标点的平滑时间（秒），越大越稳。0 会按极小值处理以免除零。")]
        [Min(0f)]
        public float positionSmoothTime = 0.06f;

        [Tooltip("位置死区（米）：与目标差小于此则直接对齐，减轻微抖。")]
        [Min(0f)]
        public float positionDeadzone = 0.0015f;

        [Tooltip("旋转贴近目标的快慢，越大转得越快。0 表示几乎不转。")]
        [Min(0f)]
        public float rotationSmoothing = 12f;
    }

    [Header("头显锚点")]
    [SerializeField]
    [Tooltip("一般为 OVRCameraRig / XR Origin 下的 CenterEyeAnchor。")]
    private Transform headAnchor;

    [Header("全局")]
    [SerializeField]
    [Tooltip("跳过开头若干帧，等 XR 追踪稳定后再跟随。")]
    private int startupDelayFrames = 2;

    [SerializeField]
    private List<FollowEntry> entries = new List<FollowEntry>();

    private readonly List<FollowEntry> _runtimeEntries = new List<FollowEntry>();

    private class EntryState
    {
        public Vector3 PositionVelocity;
        public Quaternion SmoothedRotation = Quaternion.identity;
        public bool Initialized;
        public Vector3 EffectiveLocalPositionOffset;
        public bool HasEffectiveLocalPositionOffset;
        public Quaternion LockedWorldRotation;
    }

    private readonly Dictionary<int, EntryState> _states = new Dictionary<int, EntryState>();
    private int _frameCounter;

    private void Awake()
    {
        TryAutoAssignHeadAnchor();
    }

    private void LateUpdate()
    {
        if (headAnchor == null)
            return;

        if (_frameCounter < startupDelayFrames)
        {
            _frameCounter++;
            return;
        }

        ApplyAll(entries);
        ApplyAll(_runtimeEntries);
    }

    private void ApplyAll(List<FollowEntry> list)
    {
        if (list == null)
            return;

        for (int i = 0; i < list.Count; i++)
        {
            FollowEntry e = list[i];
            if (e == null || !e.enabled || e.target == null)
                continue;

            int id = e.target.GetInstanceID();
            if (!_states.TryGetValue(id, out EntryState st))
            {
                st = new EntryState();
                _states[id] = st;
            }

            if (!st.HasEffectiveLocalPositionOffset)
            {
                if (e.autoCaptureLocalPositionOffset)
                    st.EffectiveLocalPositionOffset =
                        Quaternion.Inverse(headAnchor.rotation) * (e.target.position - headAnchor.position);
                else
                    st.EffectiveLocalPositionOffset = e.localPositionOffset;
                st.HasEffectiveLocalPositionOffset = true;
            }

            Vector3 desiredPosition = headAnchor.position + headAnchor.rotation * st.EffectiveLocalPositionOffset;
            Quaternion desiredRotation = e.lockWorldRotation
                ? e.target.rotation
                : ComputeDesiredRotation(e, desiredPosition);

            if (!st.Initialized)
            {
                if (e.lockWorldRotation)
                    st.LockedWorldRotation = e.target.rotation;

                Quaternion useRotation = e.lockWorldRotation ? st.LockedWorldRotation : desiredRotation;
                e.target.SetPositionAndRotation(desiredPosition, useRotation);
                st.SmoothedRotation = useRotation;
                st.PositionVelocity = Vector3.zero;
                st.Initialized = true;
                continue;
            }

            Vector3 smoothedPos = Vector3.SmoothDamp(
                e.target.position,
                desiredPosition,
                ref st.PositionVelocity,
                Mathf.Max(1e-4f, e.positionSmoothTime));

            if (e.positionDeadzone > 0f && (desiredPosition - smoothedPos).sqrMagnitude < e.positionDeadzone * e.positionDeadzone)
                smoothedPos = desiredPosition;

            Quaternion smoothedRot;
            if (e.lockWorldRotation)
                smoothedRot = st.LockedWorldRotation;
            else
            {
                float rotT = 1f - Mathf.Exp(-e.rotationSmoothing * Time.deltaTime);
                smoothedRot = Quaternion.Slerp(st.SmoothedRotation, desiredRotation, Mathf.Clamp01(rotT));
                st.SmoothedRotation = smoothedRot;
            }

            e.target.SetPositionAndRotation(smoothedPos, smoothedRot);
        }
    }

    private Quaternion ComputeDesiredRotation(FollowEntry e, Vector3 uiWorldPosition)
    {
        Quaternion headRot = headAnchor.rotation;
        Quaternion offset = Quaternion.Euler(e.localEulerOffset);

        switch (e.orientationMode)
        {
            case OrientationMode.AlignWithHead:
                return headRot * offset;

            case OrientationMode.YawOnlyAlignWithHead:
            {
                Vector3 f = headAnchor.forward;
                f.y = 0f;
                if (f.sqrMagnitude < 1e-6f)
                    f = Vector3.forward;
                else
                    f.Normalize();
                Quaternion yaw = Quaternion.LookRotation(f, Vector3.up);
                return yaw * offset;
            }

            case OrientationMode.FaceHead:
            default:
            {
                Vector3 toHead = headAnchor.position - uiWorldPosition;
                if (toHead.sqrMagnitude < 1e-8f)
                    return headRot * offset;
                return Quaternion.LookRotation(toHead.normalized, Vector3.up) * offset;
            }
        }
    }

    /// <summary>运行时注册一条跟随配置（与 Inspector 列表并存）。</summary>
    public void Register(FollowEntry entry)
    {
        if (entry == null || entry.target == null)
            return;
        _runtimeEntries.Add(entry);
    }

    /// <summary>按 Transform 移除运行时注册项；返回是否移除成功。</summary>
    public bool Unregister(Transform target)
    {
        if (target == null)
            return false;
        for (int i = _runtimeEntries.Count - 1; i >= 0; i--)
        {
            if (_runtimeEntries[i] != null && _runtimeEntries[i].target == target)
            {
                _runtimeEntries.RemoveAt(i);
                _states.Remove(target.GetInstanceID());
                return true;
            }
        }
        return false;
    }

    /// <summary>清除内部平滑状态，下一帧会重新对齐到当前头显（例如 recenter 后手动调用）。</summary>
    public void ResetSmoothingState()
    {
        _states.Clear();
        _frameCounter = 0;
    }

    private void TryAutoAssignHeadAnchor()
    {
        if (headAnchor != null)
            return;

        GameObject found = GameObject.Find("CenterEyeAnchor");
        if (found != null)
            headAnchor = found.transform;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        startupDelayFrames = Mathf.Max(0, startupDelayFrames);
    }

    /// <summary>
    /// 在编辑模式下根据当前场景中 Target 与 Head Anchor 的位姿，把局部位移写入 <see cref="FollowEntry.localPositionOffset"/>，
    /// 并关闭各条目的 Auto Capture，便于在 Inspector 里直接看到数字。
    /// </summary>
    [ContextMenu("编辑态：捕获局部位移到 Inspector（相对 Head Anchor）")]
    private void EditorCaptureOffsetsToInspector()
    {
        if (!Application.isPlaying)
            TryAutoAssignHeadAnchor();

        if (headAnchor == null)
        {
            Debug.LogWarning("[XRHeadFollowUIManager] 请先指定 Head Anchor（例如 CenterEyeAnchor），或保证场景里有该名称物体。");
            return;
        }

        Undo.RecordObject(this, "XR Head Follow capture offsets");
        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            FollowEntry e = entries[i];
            if (e == null || e.target == null)
                continue;

            e.localPositionOffset =
                Quaternion.Inverse(headAnchor.rotation) * (e.target.position - headAnchor.position);
            e.autoCaptureLocalPositionOffset = false;
            count++;
        }

        EditorUtility.SetDirty(this);
        if (count == 0)
            Debug.LogWarning("[XRHeadFollowUIManager] Entries 里没有有效的 Target，未写入任何位移。");
        else
            Debug.Log($"[XRHeadFollowUIManager] 已写入 {count} 条 Local Position Offset，并已关闭对应 Auto Capture。请勾选每条「Enabled」并保存场景。");
    }
#endif
}
