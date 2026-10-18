using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class SlowPulse : MonoBehaviour
{
    [Header("圆环参数")]
    public float mainWidth = 1.4f;
    public float innerWidthScale = 0.45f;

    [Header("圆环高度")]
    public float ringHeight = 0.4f;

    [Header("持续时间")]
    public float lifeTime = 1.8f;

    [Header("淡出曲线")]
    [Range(0f, 1f)] public float holdRatio = 0.6f;

    private float _maxRadius = 8f;
    private int _damage = 2;
    private float _slowMul = 0.5f;
    private float _slowDur = 2f;
    private Color _pulseColor = new Color(0.3f, 0.8f, 1f, 1f);
    private float _stunDuration = 0f;
    private bool _isVisualOnly = false;

    private float _expandSpeed;
    private float _currentRadius = 0f;
    private bool _playedHitSound = false;

    private LineRenderer _lr;
    private LineRenderer _lrInner;

    private HashSet<Enemy> _hit = new HashSet<Enemy>();
    private const int SEGMENTS = 64;

    private Color _innerColorCache;
    private Material _pulseMat;

    public void Init(float range, int damage, float slowMul, float slowDur, Color color, float stunDur)
    {
        _maxRadius = range;
        _damage = damage;
        _slowMul = slowMul;
        _slowDur = slowDur;
        _pulseColor = color;
        _stunDuration = stunDur;
        _expandSpeed = range / lifeTime;
        _isVisualOnly = damage <= 0;
        _innerColorCache = Color.Lerp(_pulseColor, Color.white, 0.35f);
    }

    void Start()
    {
        _pulseMat = CreatePulseMaterial();

        _lr = GetComponent<LineRenderer>();
        SetupLineRenderer(_lr, mainWidth, _pulseColor);

        GameObject innerObj = new GameObject("InnerRing");
        innerObj.transform.SetParent(transform);
        innerObj.transform.localPosition = Vector3.zero;
        _lrInner = innerObj.AddComponent<LineRenderer>();
        SetupLineRenderer(_lrInner, mainWidth * innerWidthScale, _innerColorCache);

        UpdateCircle();
    }

    private Material CreatePulseMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_Color", Color.white);

        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 2f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }

    private void SetupLineRenderer(LineRenderer lr, float width, Color color)
    {
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = SEGMENTS;
        lr.widthMultiplier = width;
        lr.numCapVertices = 2;
        lr.material = _pulseMat;
        lr.startColor = color;
        lr.endColor = color;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    void Update()
    {
        _currentRadius += _expandSpeed * Time.deltaTime;

        HitCheck();

        if (_currentRadius >= _maxRadius)
        {
            Destroy(gameObject);
            return;
        }

        UpdateCircle();

        float t = _currentRadius / _maxRadius;
        float alpha;
        if (t < holdRatio) alpha = 1f;
        else alpha = Mathf.Lerp(1f, 0f, (t - holdRatio) / (1f - holdRatio));

        Color c = _pulseColor; c.a = alpha;
        _lr.startColor = c; _lr.endColor = c;
        _lr.widthMultiplier = Mathf.Lerp(mainWidth, mainWidth * 0.5f, t);

        Color ic = _innerColorCache; ic.a = alpha * 0.7f;
        _lrInner.startColor = ic; _lrInner.endColor = ic;
        _lrInner.widthMultiplier = Mathf.Lerp(mainWidth * innerWidthScale, mainWidth * innerWidthScale * 0.4f, t);
    }

    private void UpdateCircle()
    {
        Vector3 center = transform.position;
        center.y = ringHeight;

        for (int i = 0; i < SEGMENTS; i++)
        {
            float angle = i * Mathf.PI * 2f / SEGMENTS;
            Vector3 p = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _currentRadius;
            _lr.SetPosition(i, p);
        }

        float innerRadius = _currentRadius * 0.75f;
        for (int i = 0; i < SEGMENTS; i++)
        {
            float angle = i * Mathf.PI * 2f / SEGMENTS;
            Vector3 p = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * innerRadius;
            _lrInner.SetPosition(i, p);
        }
    }

    private void HitCheck()
    {
        if (EnemyManager.Instance == null) return;

        var all = EnemyManager.Instance.GetAllEnemies();

        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null || _hit.Contains(e)) continue;

            float dist = Vector3.Distance(
                new Vector3(e.transform.position.x, 0, e.transform.position.z),
                new Vector3(transform.position.x, 0, transform.position.z)
            );

            if (dist <= _currentRadius)
            {
                _hit.Add(e);

                if (!_playedHitSound)
                {
                    SoundManager.Instance?.Play("减速脉冲命中");
                    _playedHitSound = true;
                }

                if (!_isVisualOnly)
                {
                    e.TakeDamage(_damage);
                    e.ApplySlow(_slowMul, _slowDur);

                    if (_stunDuration > 0f)
                        e.ApplyStun(_stunDuration);
                }
            }
        }
    }
}