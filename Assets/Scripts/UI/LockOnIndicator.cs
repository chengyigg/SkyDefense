using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LockOnIndicator : MonoBehaviour
{
    [Header("¹â»·")]
    public Color color = new Color(1f, 0.2f, 0.2f, 0.9f);
    public float lineWidth = 0.18f;
    public float ringHeight = 0.3f;

    [Header("¶¯»­")]
    public float shrinkDuration = 0.4f;
    public float startScale = 4f;
    public float idleScale = 1.2f;
    public float holdPulseAmplitude = 0.08f;
    public float holdPulseSpeed = 5f;
    public float fadeOutDuration = 0.4f;

    private LineRenderer _lr;
    private const int SEGMENTS = 64;

    private Enemy _currentTarget;
    private float _stateTimer;
    private enum State { Idle, Shrinking, Holding, Fading }
    private State _state = State.Idle;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = false;
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

        for (int i = 0; i < SEGMENTS; i++)
        {
            float a = i * Mathf.PI * 2f / SEGMENTS;
            _lr.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
        }

        transform.localScale = new Vector3(idleScale, 1f, idleScale);
    }

    void LateUpdate()
    {
        Enemy locked = LockOnManager.Instance != null ? LockOnManager.Instance.LockedEnemy : null;

        if (locked != _currentTarget)
        {
            if (locked != null)
            {
                _currentTarget = locked;
                _state = State.Shrinking;
                _stateTimer = 0f;
                transform.localScale = new Vector3(startScale, 1f, startScale);
                _lr.enabled = true;
                SetAlpha(1f);
            }
            else
            {
                if (_currentTarget != null) { _state = State.Fading; _stateTimer = 0f; }
            }
        }

        if (_currentTarget == null && _state != State.Fading)
        {
            _lr.enabled = false;
            return;
        }

        if (_currentTarget != null)
            transform.position = new Vector3(
                _currentTarget.transform.position.x, ringHeight, _currentTarget.transform.position.z);

        _stateTimer += Time.deltaTime;

        switch (_state)
        {
            case State.Shrinking:
                {
                    float t = Mathf.Clamp01(_stateTimer / shrinkDuration);
                    float s = Mathf.Lerp(startScale, idleScale, t);
                    transform.localScale = new Vector3(s, 1f, s);
                    SetAlpha(1f);
                    if (t >= 1f) { _state = State.Holding; _stateTimer = 0f; }
                    break;
                }
            case State.Holding:
                {
                    float pulse = idleScale + Mathf.Sin(Time.time * holdPulseSpeed) * holdPulseAmplitude;
                    transform.localScale = new Vector3(pulse, 1f, pulse);
                    SetAlpha(1f);
                    break;
                }
            case State.Fading:
                {
                    float f = Mathf.Clamp01(_stateTimer / fadeOutDuration);
                    SetAlpha(1f - f);
                    if (f >= 1f)
                    {
                        _lr.enabled = false;
                        _currentTarget = null;
                        _state = State.Idle;
                    }
                    break;
                }
        }
    }

    private void SetAlpha(float a)
    {
        Color c = color; c.a = color.a * a;
        _lr.startColor = c;
        _lr.endColor = c;
    }
}