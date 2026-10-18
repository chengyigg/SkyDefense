using UnityEngine;
using System.Collections;

public enum PanelAnimationMode { Slide, Fade }

[RequireComponent(typeof(RectTransform))]
public class UIPanelAnimator : MonoBehaviour
{
    [Header("模式")]
    public PanelAnimationMode mode = PanelAnimationMode.Slide;

    [Header("通用")]
    public float duration = 0.3f;
    public AnimationCurve easeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    [Header("Slide 模式参数")]
    public float offsetX = 300f;

    [Header("Fade 模式参数")]
    [Range(0.5f, 1f)] public float fadeFromScale = 0.9f;
    public float fadeFromOffsetY = 40f;

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalPos;
    private Vector3 _originalScale;
    private Coroutine _currentAnim;
    private bool _initialized = false;
    private bool _isVisible = false;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originalPos = _rect.anchoredPosition;
        _originalScale = _rect.localScale;

        if (mode == PanelAnimationMode.Fade)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
        }

        if (mode == PanelAnimationMode.Slide)
        {
            _rect.anchoredPosition = _originalPos + new Vector2(offsetX, 0);
        }
        else
        {
            _rect.anchoredPosition = _originalPos + new Vector2(0, -fadeFromOffsetY);
            _rect.localScale = _originalScale * fadeFromScale;
        }

        gameObject.SetActive(false);
        _isVisible = false;
        _initialized = true;
    }

    public void Show()
    {
        if (!_initialized) return;
        if (_isVisible) return;

        _isVisible = true;
        gameObject.SetActive(true);
        if (_currentAnim != null) StopCoroutine(_currentAnim);

        if (mode == PanelAnimationMode.Slide)
            _currentAnim = StartCoroutine(AnimateSlide(_originalPos + new Vector2(offsetX, 0), _originalPos));
        else
            _currentAnim = StartCoroutine(AnimateFadeShow());
    }

    public void Hide()
    {
        if (!_initialized) return;
        if (!_isVisible) return;

        _isVisible = false;
        if (_currentAnim != null) StopCoroutine(_currentAnim);

        if (mode == PanelAnimationMode.Slide)
            _currentAnim = StartCoroutine(AnimateSlide(_originalPos, _originalPos + new Vector2(offsetX, 0), () => gameObject.SetActive(false)));
        else
            _currentAnim = StartCoroutine(AnimateFadeHide(() => gameObject.SetActive(false)));
    }

    private IEnumerator AnimateSlide(Vector2 start, Vector2 end, System.Action onComplete = null)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float curveValue = easeCurve.Evaluate(t);
            _rect.anchoredPosition = Vector2.LerpUnclamped(start, end, curveValue);
            yield return null;
        }
        _rect.anchoredPosition = end;
        onComplete?.Invoke();
    }

    private IEnumerator AnimateFadeShow()
    {
        Vector2 startPos = _originalPos + new Vector2(0, -fadeFromOffsetY);
        Vector3 startScale = _originalScale * fadeFromScale;

        _canvasGroup.alpha = 0f;
        _rect.anchoredPosition = startPos;
        _rect.localScale = startScale;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float curveValue = easeCurve.Evaluate(t);

            _canvasGroup.alpha = Mathf.Clamp01(curveValue);
            _rect.anchoredPosition = Vector2.LerpUnclamped(startPos, _originalPos, curveValue);
            _rect.localScale = Vector3.LerpUnclamped(startScale, _originalScale, curveValue);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
        _rect.anchoredPosition = _originalPos;
        _rect.localScale = _originalScale;
    }

    private IEnumerator AnimateFadeHide(System.Action onComplete)
    {
        Vector2 startPos = _rect.anchoredPosition;
        Vector3 startScale = _rect.localScale;
        float startAlpha = _canvasGroup.alpha;

        Vector2 endPos = _originalPos + new Vector2(0, fadeFromOffsetY * 0.3f);
        Vector3 endScale = _originalScale * 0.95f;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float curveValue = easeCurve.Evaluate(t);

            _canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 0f, curveValue);
            _rect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curveValue);
            _rect.localScale = Vector3.LerpUnclamped(startScale, endScale, curveValue);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        onComplete?.Invoke();
    }
}