using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ExplosionEffect : MonoBehaviour
{
    public float lifeTime = 0.4f;
    public float mainWidth = 1.5f;
    public Color color = new Color(1f, 0.5f, 0.1f, 1f);   // 橘黄色

    private float _maxRadius;
    private float _currentRadius;
    private float _elapsed;
    private LineRenderer _lr;
    private const int SEGMENTS = 48;

    public static void Spawn(Vector3 pos, float radius)
    {
        GameObject obj = new GameObject("Explosion");
        obj.transform.position = new Vector3(pos.x, 0.4f, pos.z);
        ExplosionEffect fx = obj.AddComponent<ExplosionEffect>();
        fx.Init(radius);
    }

    public void Init(float radius)
    {
        _maxRadius = radius;
    }

    void Start()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true;
        _lr.loop = true;
        _lr.positionCount = SEGMENTS;
        _lr.widthMultiplier = mainWidth;
        _lr.numCapVertices = 2;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        _lr.material = new Material(shader);

        _lr.startColor = color;
        _lr.endColor = color;
        _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows = false;

        UpdateCircle(0.1f);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / lifeTime;
        if (t >= 1f) { Destroy(gameObject); return; }

        _currentRadius = Mathf.Lerp(0.1f, _maxRadius, t);

        // 前半段保持，后半段淡出
        float alpha = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);

        Color c = color; c.a = alpha;
        _lr.startColor = c; _lr.endColor = c;
        _lr.widthMultiplier = Mathf.Lerp(mainWidth, 0.2f, t);

        UpdateCircle(_currentRadius);
    }

    private void UpdateCircle(float r)
    {
        Vector3 center = transform.position;
        for (int i = 0; i < SEGMENTS; i++)
        {
            float angle = i * Mathf.PI * 2f / SEGMENTS;
            Vector3 p = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;
            _lr.SetPosition(i, p);
        }
    }
}