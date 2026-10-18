using UnityEngine;

public class TowerRangeIndicator : MonoBehaviour
{
    public static TowerRangeIndicator Instance;

    [Header("¹â»·")]
    public Color color = new Color(1f, 0.9f, 0.4f, 0.7f);
    public float lineWidth = 0.25f;
    public float ringHeight = 0.3f;

    private LineRenderer _lr;
    private const int SEGMENTS = 96;

    void Awake()
    {
        Instance = this;

        _lr = GetComponent<LineRenderer>();
        if (_lr == null) _lr = gameObject.AddComponent<LineRenderer>();

        _lr.useWorldSpace = true;
        _lr.loop = true;
        _lr.positionCount = SEGMENTS;
        _lr.widthMultiplier = lineWidth;
        _lr.numCapVertices = 2;

        Shader shader = Shader.Find("Sprites/Default");
        _lr.material = new Material(shader);

        _lr.startColor = color;
        _lr.endColor = color;
        _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows = false;

        _lr.enabled = false;
    }

    public void Show(Vector3 center, float radius)
    {
        _lr.enabled = true;

        Vector3 c = new Vector3(center.x, ringHeight, center.z);
        for (int i = 0; i < SEGMENTS; i++)
        {
            float angle = i * Mathf.PI * 2f / SEGMENTS;
            Vector3 p = c + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            _lr.SetPosition(i, p);
        }
    }

    public void Hide()
    {
        _lr.enabled = false;
    }
}