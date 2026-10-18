using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("默认参数")]
    public float defaultIntensity = 0.3f;
    public float defaultDuration = 0.15f;

    [Header("衰减曲线")]
    public AnimationCurve falloff = new AnimationCurve(
        new Keyframe(0, 1),
        new Keyframe(1, 0)
    );

    private Vector3 _basePosition;
    private float _shakeTimer = 0f;
    private float _currentIntensity = 0f;
    private float _currentDuration = 0f;
    private bool _isShaking = false;

    void Awake()
    {
        Instance = this;
        _basePosition = transform.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        if (intensity > _currentIntensity || _shakeTimer <= 0f)
        {
            _currentIntensity = intensity;
            _currentDuration = duration;
            _shakeTimer = duration;
            _isShaking = true;
        }
    }

    public void Shake() => Shake(defaultIntensity, defaultDuration);

    void LateUpdate()
    {
        if (!_isShaking) return;

        _shakeTimer -= Time.deltaTime;

        if (_shakeTimer <= 0f)
        {
            transform.localPosition = _basePosition;
            _currentIntensity = 0f;
            _isShaking = false;
            return;
        }

        float t = 1f - (_shakeTimer / _currentDuration);
        float falloffValue = falloff.Evaluate(t);
        float amp = _currentIntensity * falloffValue;

        Vector3 offset = Random.insideUnitSphere * amp;
        transform.localPosition = _basePosition + offset;
    }
}