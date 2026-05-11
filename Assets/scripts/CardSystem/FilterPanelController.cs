using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 折叠按钮的显隐只应由本脚本的 <see cref="TogglePanel"/> / <see cref="RefreshPanels"/> 控制。
/// 请勿在 Inspector 里给「赠送对象 / 政治目标」折叠按钮再绑 GameObject.SetActive，否则会与代码监听冲突（例如政治下拉被强制设为 false）。
/// </summary>
public class FilterPanelController : MonoBehaviour
{
    [Header("折叠按钮")]
    [SerializeField] private Button recipientFoldButton;
    [SerializeField] private Button politicalFoldButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;

    [Header("下拉面板")]
    [SerializeField] private GameObject recipientDropdown;
    [SerializeField] private GameObject politicalDropdown;

    [Header("依赖")]
    [SerializeField] private RecipientRepository recipientRepository;
    [SerializeField] private RecipientSelectorController recipientSelector;
    [SerializeField] private PoliticalSelectorController politicalSelector;
    [SerializeField] private SelectedConditionsController selectedConditions;
    [SerializeField] private CardManager cardManager;

    private string openedPanel = string.Empty;
    private bool _foldListenersAdded;
    private bool _actionListenersAdded;
    private bool _initialized;
    private int _lastToggleFrame = -1;
    private string _lastTogglePanel = string.Empty;
    private float _lastToggleTime = -10f;

    private void Awake()
    {
        AutoBindIfMissing();
        EnsureFoldButtonListeners();
        EnsureActionButtonListeners();
    }

    private void OnEnable()
    {
        // 防止运行中对象重建/切页导致监听丢失
        AutoBindIfMissing();
        EnsureFoldButtonListeners();
        EnsureActionButtonListeners();
    }

    private void Start()
    {
        StartCoroutine(TryInitializeWhenReady());
    }

    /// <summary>
    /// 折叠按钮必须在 Awake 就挂上：原先只在 Initialize 里挂，而 Initialize 要等 CardManager 成功加载 JSON 才会调，
    /// 若未绑定 CardManager、JSON 失败或时序不对，两个按钮会一直点不出下拉。
    /// </summary>
    private void EnsureFoldButtonListeners()
    {
        if (recipientFoldButton != null)
        {
            recipientFoldButton.onClick.RemoveListener(OnRecipientFoldClicked);
            recipientFoldButton.onClick.AddListener(OnRecipientFoldClicked);
        }
        if (politicalFoldButton != null)
        {
            politicalFoldButton.onClick.RemoveListener(OnPoliticalFoldClicked);
            politicalFoldButton.onClick.AddListener(OnPoliticalFoldClicked);
        }

        _foldListenersAdded = recipientFoldButton != null || politicalFoldButton != null;
    }

    private void EnsureActionButtonListeners()
    {
        if (_actionListenersAdded)
            return;

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetFilter);
            resetButton.onClick.AddListener(ResetFilter);
        }

        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplyFilter);
            applyButton.onClick.AddListener(ApplyFilter);
        }

        _actionListenersAdded = true;
    }

    private void AutoBindIfMissing()
    {
        // 在场景里按常用命名自动找，减少手动拖引用导致的“点了没反应”。
        if (recipientFoldButton == null)
            recipientFoldButton = transform.Find("RecipientFoldButton")?.GetComponent<Button>();
        if (politicalFoldButton == null)
            politicalFoldButton = transform.Find("PoliticalFoldButton")?.GetComponent<Button>();
        if (resetButton == null)
            resetButton = transform.Find("ResetButton")?.GetComponent<Button>();
        if (applyButton == null)
            applyButton = transform.Find("ApplyButton")?.GetComponent<Button>();

        if (recipientDropdown == null)
            recipientDropdown = transform.Find("RecipientDropdown")?.gameObject;
        if (politicalDropdown == null)
            politicalDropdown = transform.Find("PoliticalDropdown")?.gameObject;

        if (recipientRepository == null)
            recipientRepository = GetComponentInChildren<RecipientRepository>(true);
        if (recipientSelector == null)
            recipientSelector = GetComponentInChildren<RecipientSelectorController>(true);
        if (politicalSelector == null)
            politicalSelector = GetComponentInChildren<PoliticalSelectorController>(true);
        if (selectedConditions == null)
            selectedConditions = GetComponentInChildren<SelectedConditionsController>(true);
        if (cardManager == null)
            cardManager = CardManager.Instance != null ? CardManager.Instance : FindFirstObjectByType<CardManager>();
    }

    private System.Collections.IEnumerator TryInitializeWhenReady()
    {
        if (_initialized)
            yield break;

        // 给 CardManager.LoadDatabase 一个时机，避免与 Start 时序竞争。
        for (int i = 0; i < 120 && !_initialized; i++)
        {
            AutoBindIfMissing();

            List<string> goals = new List<string>();
            if (cardManager != null && cardManager.GetDatabase() != null)
                goals = new CardFilter().GetAllPoliticalGoals(cardManager.GetDatabase());

            Initialize(goals);
            _initialized = true;
            yield break;
        }
    }

    public void Initialize(List<string> politicalGoals)
    {
        EnsureFoldButtonListeners();
        EnsureActionButtonListeners();

        if (recipientRepository != null)
            recipientRepository.Load();

        if (recipientSelector != null && recipientRepository != null)
            recipientSelector.Initialize(recipientRepository);

        if (politicalSelector != null)
            politicalSelector.Initialize(politicalGoals);

        if (selectedConditions != null)
            selectedConditions.Initialize(recipientSelector, politicalSelector);

        RefreshPanels();
    }

    public void TogglePanel(string panelName)
    {
        // XR 下同一次点击可能触发两次 onClick（按下/抬起链路），这里做轻量防抖避免“开了又关”。
        if (_lastTogglePanel == panelName && (_lastToggleFrame == Time.frameCount || Time.unscaledTime - _lastToggleTime < 0.12f))
        {
            Debug.Log($"[FilterPanelController] 忽略重复 TogglePanel: {panelName}");
            return;
        }

        _lastTogglePanel = panelName;
        _lastToggleFrame = Time.frameCount;
        _lastToggleTime = Time.unscaledTime;

        openedPanel = openedPanel == panelName ? string.Empty : panelName;
        Debug.Log($"[FilterPanelController] TogglePanel => {panelName}, openedPanel={openedPanel}");
        RefreshPanels();
    }

    public void OnRecipientFoldClicked() => TogglePanel("recipient");
    public void OnPoliticalFoldClicked() => TogglePanel("political");

    public void ResetFilter()
    {
        openedPanel = string.Empty;
        recipientSelector?.ClearSelection();
        politicalSelector?.ClearSelection();
        selectedConditions?.Refresh();
        cardManager?.ClearDisplayedResult();
        RefreshPanels();
    }

    public void ApplyFilter()
    {
        if (cardManager == null || recipientSelector == null || politicalSelector == null || recipientRepository == null)
            return;

        var selectedNodeIds = recipientSelector.GetSelectedNodeIds();
        var expandedRecipients = recipientRepository.ExpandRecipients(selectedNodeIds);
        var selectedGoals = politicalSelector.GetSelectedGoals();
        cardManager.ApplyFilter(expandedRecipients, selectedGoals);
    }

    private void RefreshPanels()
    {
        if (recipientDropdown != null)
            recipientDropdown.SetActive(openedPanel == "recipient");
        if (politicalDropdown != null)
            politicalDropdown.SetActive(openedPanel == "political");
    }
}
