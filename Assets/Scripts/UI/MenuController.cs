using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class MenuController : MonoBehaviour
{
    [Header("Canvas 组")]
    public GameObject mainMenuCanvas;
    public GameObject gameCanvas;

    [Header("页面")]
    public GameObject mainPage;
    public GameObject multiplayerPage;
    public GameObject waitingPage;
    public GameObject joinPage;

    [Header("主页按钮")]
    public Button btnSingle;
    public Button btnMulti;
    public Button btnQuitMain;

    [Header("联机页按钮")]
    public Button btnHost;
    public Button btnJoin;
    public Button btnMultiBack;

    [Header("等待页")]
    public TextMeshProUGUI waitingText;
    public TextMeshProUGUI waitingStatusText;
    public Button btnWaitCancel;

    [Header("加入页")]
    public InputField inputFieldIP;
    public TextMeshProUGUI joinStatusText;
    public Button btnJoinConnect;
    public Button btnJoinBack;

    private bool _isWaitingForStart = false;

    void Start()
    {
        btnSingle.onClick.AddListener(OnClickSingle);
        btnMulti.onClick.AddListener(OnClickMulti);
        btnQuitMain.onClick.AddListener(OnClickQuit);

        btnHost.onClick.AddListener(OnClickHost);
        btnJoin.onClick.AddListener(OnClickJoin);
        btnMultiBack.onClick.AddListener(OnClickMultiBack);

        btnWaitCancel.onClick.AddListener(OnClickWaitCancel);

        btnJoinConnect.onClick.AddListener(OnClickJoinConnect);
        btnJoinBack.onClick.AddListener(OnClickJoinBack);

        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStartedChanged += OnGameStarted;

        ShowMainPage();
        if (gameCanvas != null) gameCanvas.SetActive(false);
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStartedChanged -= OnGameStarted;
    }

    void ShowPage(GameObject page)
    {
        if (mainPage != null) mainPage.SetActive(false);
        if (multiplayerPage != null) multiplayerPage.SetActive(false);
        if (waitingPage != null) waitingPage.SetActive(false);
        if (joinPage != null) joinPage.SetActive(false);
        if (page != null) page.SetActive(true);
    }

    void ShowMainPage() => ShowPage(mainPage);
    void ShowMultiplayerPage() => ShowPage(multiplayerPage);
    void ShowWaitingPage() => ShowPage(waitingPage);
    void ShowJoinPage() => ShowPage(joinPage);

    void OnClickSingle()
    {
        NetworkManager.Singleton.StartHost();
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
    }

    void OnClickMulti() { ShowMultiplayerPage(); }

    void OnClickQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnClickHost()
    {
        _isWaitingForStart = true;
        NetworkManager.Singleton.StartHost();
        ShowWaitingPage();
        if (waitingText != null) waitingText.text = "等待玩家加入……";
        if (waitingStatusText != null) waitingStatusText.text = "已加入：1 人";
    }

    void OnClickJoin()
    {
        ShowJoinPage();
        if (joinStatusText != null) joinStatusText.text = "";
    }

    void OnClickMultiBack() { ShowMainPage(); }

    void OnClickWaitCancel()
    {
        _isWaitingForStart = false;
        NetworkManager.Singleton.Shutdown();
        ShowMainPage();
    }

    void OnClickJoinConnect()
    {
        string ip = (inputFieldIP != null && !string.IsNullOrEmpty(inputFieldIP.text))
            ? inputFieldIP.text.Trim() : "127.0.0.1";

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, 7777);

        if (joinStatusText != null) joinStatusText.text = $"正在连接 {ip}...";
        NetworkManager.Singleton.StartClient();
    }

    void OnClickJoinBack()
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsConnectedClient)
            NetworkManager.Singleton.Shutdown();
        ShowMultiplayerPage();
    }

    void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && clientId != 0 && _isWaitingForStart)
        {
            if (waitingStatusText != null)
                waitingStatusText.text = "已加入：2 人，游戏开始！";

            if (GameManager.Instance != null) GameManager.Instance.StartGame();
            _isWaitingForStart = false;
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsConnectedClient)
        {
            if (joinStatusText != null)
                joinStatusText.text = "连接失败，请检查 IP 或主机状态";
        }
    }

    void OnGameStarted()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        if (gameCanvas != null) gameCanvas.SetActive(true);
    }
}