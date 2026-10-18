using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI waveText;

    private bool _bound = false;

    void Start()
    {
        if (waveText != null) waveText.text = "波次: -/-";
        TryBind();
    }

    void Update()
    {
        if (_bound) return;
        TryBind();
    }

    private void TryBind()
    {
        if (GameManager.Instance == null) return;

        var gm = GameManager.Instance;

        Unbind(gm);

        gm.currentGold.OnValueChanged += OnGoldChanged;
        gm.currentHealth.OnValueChanged += OnHealthChanged;
        gm.currentWaveNV.OnValueChanged += OnWaveChangedInternal;
        gm.totalWavesNV.OnValueChanged += OnTotalWavesChanged;

        _bound = true;

        OnGoldChanged(0, gm.currentGold.Value);
        OnHealthChanged(0, gm.currentHealth.Value);
        RefreshWave();
    }

    private void Unbind(GameManager gm)
    {
        if (gm == null) return;
        gm.currentGold.OnValueChanged -= OnGoldChanged;
        gm.currentHealth.OnValueChanged -= OnHealthChanged;
        gm.currentWaveNV.OnValueChanged -= OnWaveChangedInternal;
        gm.totalWavesNV.OnValueChanged -= OnTotalWavesChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            Unbind(GameManager.Instance);
    }

    private void OnGoldChanged(int oldV, int newV)
    {
        if (goldText != null)
            goldText.text = "金币: " + newV;
    }

    private void OnHealthChanged(int oldV, int newV)
    {
        if (healthText != null)
            healthText.text = "血量: " + newV;
    }

    private void OnWaveChangedInternal(int oldV, int newV) => RefreshWave();
    private void OnTotalWavesChanged(int oldV, int newV) => RefreshWave();

    private void RefreshWave()
    {
        if (waveText == null || GameManager.Instance == null) return;

        int cw = GameManager.Instance.currentWaveNV.Value;
        int tw = GameManager.Instance.totalWavesNV.Value;
        if (tw > 0)
            waveText.text = $"波次: {cw}/{tw}";
        else
            waveText.text = "波次: -/-";
    }

    public void SetWaveText(int current, int total)
    {
    }
}