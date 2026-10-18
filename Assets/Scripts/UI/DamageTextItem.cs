using UnityEngine;
using TMPro;

public class DamageTextItem : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private Vector3 _worldPos;
    private float _lifeTime = 0.8f;
    private float _timer = 0f;
    private DamageTextManager _manager;
    private Vector3 _randomOffset;

    private Color _originalColor; // 【新增】用于记录预制体里设置的颜色

    void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();

        // 【新增】把预制体里你调好的颜色存起来
        if (_text != null)
        {
            _originalColor = _text.color;
        }
    }

    public void Setup(Vector3 worldPos, int amount, DamageTextManager manager)
    {
        _manager = manager;
        _worldPos = worldPos;
        _timer = 0f;

        _randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), 0f);

        if (_text != null)
        {
            _text.text = $"-{amount}";
            // 不用在这里设颜色了，直接用预制体里的
            _text.color = _originalColor;
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;

        _worldPos += Vector3.up * 2f * Time.deltaTime;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(_worldPos + _randomOffset);
        transform.position = screenPos;

        float alpha = Mathf.Lerp(1f, 0f, _timer / _lifeTime);
        if (_text != null)
        {
            // 只改变透明度，保持你在预制体里设置的颜色
            Color c = _originalColor;
            c.a = alpha;
            _text.color = c;
        }

        if (_timer >= _lifeTime && _manager != null)
        {
            _manager.ReturnToPool(gameObject);
        }
    }
}