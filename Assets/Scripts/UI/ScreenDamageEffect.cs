using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenDamageEffect : MonoBehaviour
{
    [Header("音量")]
    public Volume globalVolume;

    [Header("基础强度（正常血量时的 Vignette 强度）")]
    public float baseIntensity = 0.35f;

    [Header("极低血量时的强度")]
    public float maxIntensity = 0.85f;

    [Header("从多少血量比例开始泛红（0.5 = 50%）")]
    [Range(0.1f, 1f)] public float lowHealthThreshold = 0.5f;

    [Header("受击闪烁")]
    public float flashDuration = 0.4f;
    public float flashStrength = 0.4f;

    [Header("危险颜色")]
    public Color normalColor = Color.black;
    public Color dangerColor = new Color(0.6f, 0f, 0f);

    private Vignette _vignette;
    private float _flashTimer = 0f;
    private int _lastHealth = -1;

    void Start()
    {
        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogWarning("ScreenDamageEffect: 没有指定 Global Volume");
            enabled = false;
            return;
        }

        if (!globalVolume.profile.TryGet<Vignette>(out _vignette))
        {
            Debug.LogWarning("ScreenDamageEffect: Global Volume 里没有 Vignette，去加一个");
            enabled = false;
            return;
        }

        _vignette.active = true;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        int health = GameManager.Instance.CurrentHealth;
        int maxHealth = Mathf.Max(1, GameManager.Instance.startHealth);

        if (_lastHealth >= 0 && health < _lastHealth)
        {
            _flashTimer = flashDuration;
        }
        _lastHealth = health;

        if (_flashTimer > 0f) _flashTimer -= Time.deltaTime;
        float flash = _flashTimer > 0f ? flashStrength * (_flashTimer / flashDuration) : 0f;

        float healthRatio = (float)health / maxHealth;
        float danger = 1f - Mathf.Clamp01(healthRatio / lowHealthThreshold);

        float targetIntensity = baseIntensity + danger * (maxIntensity - baseIntensity);
        _vignette.intensity.value = Mathf.Clamp01(targetIntensity + flash);

        _vignette.color.value = Color.Lerp(normalColor, dangerColor, danger);
    }
}