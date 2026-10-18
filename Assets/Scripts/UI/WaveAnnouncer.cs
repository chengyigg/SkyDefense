using UnityEngine;
using TMPro;
using System.Collections;

public class WaveAnnouncer : MonoBehaviour
{
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.3f;
    public float holdDuration = 1.7f;
    public float fadeOutDuration = 0.5f;

    private Coroutine _current;

    void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (text != null) text.text = "";
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveChanged += HandleWaveChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveChanged -= HandleWaveChanged;
    }

    private void HandleWaveChanged(int current, int total)
    {
        Show($"µÚ {current} ²¨À´Ï®£¡({current}/{total})");
    }

    public void Show(string message)
    {
        if (text == null || canvasGroup == null) return;
        text.text = message;
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        text.text = "";
        _current = null;
    }
}