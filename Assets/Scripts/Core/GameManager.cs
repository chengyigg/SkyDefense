using UnityEngine;
using Unity.Netcode;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("金币")]
    public int startGold = 500;
    public NetworkVariable<int> currentGold = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int CurrentGold => currentGold.Value;

    [Header("玩家血量")]
    public int startHealth = 20;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int CurrentHealth => currentHealth.Value;

    [Header("波次")]
    public NetworkVariable<int> currentWaveNV = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> totalWavesNV = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("游戏状态")]
    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isGameOverNV = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isVictoryNV = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<bool> OnGameEnded;
    public event Action<int, int> OnWaveChanged;
    public event Action OnGameStartedChanged;

    public bool IsGameOver => isGameOverNV.Value;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        isGameOverNV.OnValueChanged += HandleGameOverChanged;
        isVictoryNV.OnValueChanged += HandleVictoryChanged;
        currentWaveNV.OnValueChanged += HandleWaveChanged;
        gameStarted.OnValueChanged += HandleGameStartedChanged;

        if (IsServer)
        {
            currentGold.Value = startGold;
            currentHealth.Value = startHealth;

            var spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null && spawner.waves != null)
                totalWavesNV.Value = spawner.waves.Length;
        }

        if (gameStarted.Value) OnGameStartedChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        isGameOverNV.OnValueChanged -= HandleGameOverChanged;
        isVictoryNV.OnValueChanged -= HandleVictoryChanged;
        currentWaveNV.OnValueChanged -= HandleWaveChanged;
        gameStarted.OnValueChanged -= HandleGameStartedChanged;
    }

    private void HandleGameOverChanged(bool oldV, bool newV)
    {
        if (newV) OnGameEnded?.Invoke(false);
    }

    private void HandleVictoryChanged(bool oldV, bool newV)
    {
        if (newV) OnGameEnded?.Invoke(true);
    }

    private void HandleWaveChanged(int oldV, int newV)
    {
        OnWaveChanged?.Invoke(currentWaveNV.Value, totalWavesNV.Value);
    }

    private void HandleGameStartedChanged(bool oldV, bool newV)
    {
        if (newV) OnGameStartedChanged?.Invoke();
    }

    public void StartGame()
    {
        if (!IsServer) return;

        gameStarted.Value = true;

        var spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.StartWaves();
    }

    public void SetWaveInfo(int current, int total)
    {
        if (!IsServer) return;
        totalWavesNV.Value = total;
        currentWaveNV.Value = current;
    }

    public void PlaySoundAll(string soundName)
    {
        if (!IsServer) return;
        PlaySoundAllClientRpc(soundName);
    }

    [ClientRpc]
    private void PlaySoundAllClientRpc(string soundName)
    {
        SoundManager.Instance?.Play(soundName);
    }

    public bool SpendGold(int amount)
    {
        if (IsGameOver) return false;
        if (!IsServer) return false;
        if (currentGold.Value >= amount)
        {
            currentGold.Value -= amount;
            Debug.Log("花钱成功，剩余：" + currentGold.Value);
            return true;
        }
        SoundManager.Instance?.Play("金币不足");
        return false;
    }

    public void AddGold(int amount)
    {
        if (IsGameOver) return;
        if (!IsServer) return;
        currentGold.Value += amount;
        SoundManager.Instance?.Play("获得金币");
    }

    public void TakeDamage(int amount)
    {
        if (IsGameOver) return;
        if (!IsServer) return;
        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            GameOver();
        }
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.5f, 0.3f);
    }

    private void GameOver()
    {
        if (IsServer) isGameOverNV.Value = true;
    }

    public void Victory()
    {
        if (IsServer) isVictoryNV.Value = true;
    }
}