using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance;

    [Header("????????")]
    public GameObject healthBarPrefab;

    private Dictionary<Enemy, Image> _healthBars = new Dictionary<Enemy, Image>();
    private Camera _mainCamera;

    void Awake()
    {
        Instance = this;
        _mainCamera = Camera.main;
    }

    public void AddBar(Enemy enemy, float width, float height, Color fillColor)
    {
        if (healthBarPrefab == null || _healthBars.ContainsKey(enemy)) return;

        GameObject go = Instantiate(healthBarPrefab, transform);
        RectTransform rt = go.GetComponent<RectTransform>();

        rt.sizeDelta = new Vector2(width * 100f, height * 100f);

        Transform fillTf = go.transform.Find("Fill");
        Image fillImg = null;
        if (fillTf != null)
        {
            fillImg = fillTf.GetComponent<Image>();
            if (fillImg != null)
            {
                fillImg.color = fillColor;
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImg.fillAmount = 1f;
            }

            // ??????????????????—¨??????????????
            fillTf.SetAsLastSibling();

            RectTransform fillRt = fillTf.GetComponent<RectTransform>();
            if (fillRt != null)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
                // ?????????????????????Z??????????? World Space ?????
                fillRt.localPosition = new Vector3(fillRt.localPosition.x, fillRt.localPosition.y, -0.1f);
            }
        }

        _healthBars[enemy] = fillImg;
    }

    public void RemoveBar(Enemy enemy)
    {
        if (_healthBars.TryGetValue(enemy, out var img))
        {
            if (img != null) Destroy(img.transform.parent.gameObject);
            _healthBars.Remove(enemy);
        }
    }

    public void UpdateHealth(Enemy enemy, float healthPct)
    {
        if (_healthBars.TryGetValue(enemy, out var img) && img != null)
        {
            img.fillAmount = healthPct;
        }
    }

    void LateUpdate()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        transform.rotation = _mainCamera.transform.rotation;

        foreach (var kvp in _healthBars)
        {
            var enemy = kvp.Key;
            var fillImg = kvp.Value;

            if (enemy == null)
            {
                if (fillImg != null) Destroy(fillImg.transform.parent.gameObject);
                continue;
            }

            if (fillImg == null) continue;

            Vector3 worldPos = enemy.transform.position + Vector3.up * enemy.healthBarHeight;
            Transform barTf = fillImg.transform.parent;
            barTf.position = worldPos;

            barTf.gameObject.SetActive(Vector3.Dot(_mainCamera.transform.forward, worldPos - _mainCamera.transform.position) > 0);
        }
    }
}