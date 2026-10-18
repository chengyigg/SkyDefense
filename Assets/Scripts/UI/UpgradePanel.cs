using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradePanel : MonoBehaviour
{
    [Header("ÎÄ±¾")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI nextStatsText;
    public TextMeshProUGUI costText;

    [Header("°´Å¥")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    public Button closeButton;

    [Header("Ãæ°å¶¯»­")]
    public UIPanelAnimator animator;

    private Tower _currentTower;
    public Button sellButton;
    public TextMeshProUGUI sellButtonText;

    void Start()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
        if (animator != null) animator.Hide();
    }

    public void Show(Tower tower)
    {
        UnsubscribeCurrent();

        _currentTower = tower;
        Refresh();
        if (animator != null) animator.Show();

        if (_currentTower != null && _currentTower.currentLevel != null)
            _currentTower.currentLevel.OnValueChanged += HandleLevelChanged;
    }

    private void OnSellClicked()
    {
        if (_currentTower == null) return;
        _currentTower.RequestSell();
        Hide();
    }

    public void Hide()
    {
        UnsubscribeCurrent();
        if (animator != null) animator.Hide();
        _currentTower = null;
    }

    private void UnsubscribeCurrent()
    {
        if (_currentTower != null && _currentTower.currentLevel != null)
            _currentTower.currentLevel.OnValueChanged -= HandleLevelChanged;
    }

    private void HandleLevelChanged(int oldValue, int newValue)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_currentTower == null || _currentTower.Data == null) return;

        var data = _currentTower.Data;
        int lv = _currentTower.CurrentLevel;
        var curStats = data.GetStats(lv);
        bool isMax = lv >= data.MaxLevel - 1;

        if (titleText != null)
            titleText.text = $"{data.towerName}  Lv{lv + 1}";
        if (sellButtonText != null && _currentTower != null)
        {
            int refund = Mathf.RoundToInt(_currentTower.GetTotalInvested() * 0.7f);
            sellButtonText.text = $"ÂôËþ (+{refund})";
        }
        if (statsText != null && curStats != null)
        {
            statsText.text =
                $"·¶Î§: {curStats.range}\n" +
                $"¹¥»÷¼ä¸ô: {curStats.attackInterval}s\n" +
                $"ÉËº¦: {curStats.damage}";
        }

        if (nextStatsText != null)
        {
            if (isMax) nextStatsText.text = "ÒÑÊÇÂú¼¶";
            else
            {
                var next = data.GetStats(lv + 1);
                nextStatsText.text =
                    $"¡ú ·¶Î§: {next.range}\n" +
                    $"¡ú ¹¥»÷¼ä¸ô: {next.attackInterval}s\n" +
                    $"¡ú ÉËº¦: {next.damage}";
            }
        }

        if (isMax)
        {
            if (costText != null) costText.text = "";
            if (upgradeButton != null) upgradeButton.interactable = false;
            if (upgradeButtonText != null) upgradeButtonText.text = "Âú¼¶";
        }
        else
        {
            var next = data.GetStats(lv + 1);
            if (costText != null) costText.text = $"Éý¼¶ÏûºÄ: {next.upgradeCost}";
            if (upgradeButton != null)
                upgradeButton.interactable =
                    GameManager.Instance != null &&
                    GameManager.Instance.CurrentGold >= next.upgradeCost;
            if (upgradeButtonText != null) upgradeButtonText.text = "Éý¼¶";
        }
    }

    private void OnUpgradeClicked()
    {
        if (_currentTower == null) return;
        _currentTower.TryUpgrade();
    }

    private void OnCloseClicked()
    {
        Hide();
        if (BuildManager.Instance != null && BuildManager.Instance.player != null)
            BuildManager.Instance.player.canShowPreview = true;
    }
}