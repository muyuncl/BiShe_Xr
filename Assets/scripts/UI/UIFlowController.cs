using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 三页面总控：负责页面切换与导航按钮行为。
/// </summary>
public class UIFlowController : MonoBehaviour
{
    [Header("页面根节点（按顺序：页面1/页面2/页面3）")]
    [SerializeField] private List<GameObject> pageRoots = new List<GameObject>();

    [Header("导航按钮（可选）")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button exitButton;

    [Header("初始化")]
    [SerializeField] private int startPageIndex = 0;

    [Header("流程门禁（可选）")]
    [SerializeField] private TextMeshProUGUI nextBlockHintText;
    [SerializeField] private float navigationDebounceSeconds = 0.2f;

    private int currentPageIndex;
    private float lastNavigateTime = -999f;

    public int CurrentPageIndex => currentPageIndex;
    public int PageCount => pageRoots.Count;

    private void Awake()
    {
        BindButtons();
        int safeIndex = pageRoots.Count == 0 ? 0 : Mathf.Clamp(startPageIndex, 0, pageRoots.Count - 1);
        ShowPage(safeIndex);
    }

    private void Update()
    {
        // 页内状态（例如“锚点已锁定”）可能实时变化，这里持续刷新按钮可用性。
        UpdateNavigationState();
    }

    /// <summary>
    /// 显示指定页面（0-based）。
    /// </summary>
    public void ShowPage(int pageIndex)
    {
        if (pageRoots == null || pageRoots.Count == 0)
        {
            currentPageIndex = 0;
            UpdateNavigationState();
            return;
        }

        currentPageIndex = Mathf.Clamp(pageIndex, 0, pageRoots.Count - 1);

        for (int i = 0; i < pageRoots.Count; i++)
        {
            if (pageRoots[i] != null)
                pageRoots[i].SetActive(i == currentPageIndex);
        }

        UpdateNavigationState();
    }

    /// <summary>
    /// 下一步：跳转到下一个页面。
    /// </summary>
    public void GoNext()
    {
        if (Time.unscaledTime - lastNavigateTime < Mathf.Max(0f, navigationDebounceSeconds))
            return;
        lastNavigateTime = Time.unscaledTime;
        ShowPage(currentPageIndex + 1);
    }

    /// <summary>
    /// 上一步：返回上一个页面。
    /// </summary>
    public void GoBack()
    {
        if (Time.unscaledTime - lastNavigateTime < Mathf.Max(0f, navigationDebounceSeconds))
            return;
        lastNavigateTime = Time.unscaledTime;
        ShowPage(currentPageIndex - 1);
    }

    /// <summary>
    /// 退出应用。
    /// </summary>
    public void ExitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(GoNext);
            nextButton.onClick.AddListener(GoNext);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBack);
            backButton.onClick.AddListener(GoBack);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitApp);
            exitButton.onClick.AddListener(ExitApp);
        }
    }

    private void UpdateNavigationState()
    {
        bool hasPages = pageRoots != null && pageRoots.Count > 0;
        bool hasPrev = hasPages && currentPageIndex > 0;
        bool hasNextPage = hasPages && currentPageIndex < pageRoots.Count - 1;
        string blockReason = string.Empty;
        bool canProceed = hasNextPage && CanProceedCurrentPage(out blockReason);

        if (backButton != null)
            backButton.interactable = hasPrev;

        if (nextButton != null)
            nextButton.interactable = canProceed;

        if (nextBlockHintText != null)
        {
            bool showHint = hasNextPage && !canProceed && !string.IsNullOrEmpty(blockReason);
            nextBlockHintText.gameObject.SetActive(showHint);
            if (showHint)
                nextBlockHintText.text = blockReason;
        }
    }

    private bool CanProceedCurrentPage(out string blockReason)
    {
        blockReason = string.Empty;
        if (pageRoots == null || pageRoots.Count == 0 || currentPageIndex < 0 || currentPageIndex >= pageRoots.Count)
            return false;

        var currentPage = pageRoots[currentPageIndex];
        if (currentPage == null) return false;

        var gates = currentPage.GetComponentsInChildren<IUIFlowPageGate>(true);
        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] == null) continue;
            if (!gates[i].CanProceed())
            {
                blockReason = gates[i].GetBlockReason();
                return false;
            }
        }
        return true;
    }
}
