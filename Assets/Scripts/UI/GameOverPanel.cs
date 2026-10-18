using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode;

public class GameOverPanel : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public Button restartButton;

    [Header("动画")]
    public float fadeDuration = 0.6f;
    public float delayBeforeShow = 0.8f;

    [Header("颜色")]
    public Color victoryColor = new Color(1f, 0.85f, 0.4f);
    public Color defeatColor = new Color(1f, 0.35f, 0.35f);

    void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnded += HandleGameEnded;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnded -= HandleGameEnded;
    }

    private void HandleGameEnded(bool isVictory)
    {
        StartCoroutine(ShowAfterDelay(isVictory));
    }

    private IEnumerator ShowAfterDelay(bool isVictory)
    {
        yield return new WaitForSeconds(delayBeforeShow);

        if (titleText != null)
        {
            titleText.text = isVictory ? "胜 利" : "失 败";
            titleText.color = isVictory ? victoryColor : defeatColor;
        }

        if (subtitleText != null)
            subtitleText.text = isVictory ? "你守住了防线！" : "敌人突破了防线";

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void OnRestartClicked()
    {
        if (restartButton != null) restartButton.interactable = false;

        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
            RestartServerRpc();
        else
            RequestRestartServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRestartServerRpc()
    {
        RestartServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartServerRpc()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        string sceneName = SceneManager.GetActiveScene().name;
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}