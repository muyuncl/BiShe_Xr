/// <summary>
/// 桌面锚点放置控制器通用接口。
/// 用于让锁定按钮兼容不同放置方案（单点放置/双角点放置等）。
/// </summary>
public interface IDeskAnchorPlacementController
{
    bool PlacementEnabled { get; }
    void SetPlacementEnabled(bool enabled);
    void TogglePlacementEnabled();
}
