using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class WaveConfig
    {
        public int enemyCount = 5;
        public float spawnInterval = 1f;
    }

    [Header("预制体")]
    public GameObject enemyPrefab;

    [Header("路径")]
    public Transform[] waypoints;

    [Header("波次配置")]
    public WaveConfig[] waves;
    public float timeBetweenWaves = 5f;
    public float startDelay = 3f;

    [Header("运行状态")]
    public int currentWaveIndex = -1;
    public bool allWavesDone = false;

    private bool _hasStarted = false;

    private GameManager GM => GameManager.Instance;

    void Start() { }

    public void StartWaves()
    {
        if (_hasStarted) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        _hasStarted = true;
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(startDelay);

        for (int w = 0; w < waves.Length; w++)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;

            currentWaveIndex = w;
            UpdateWaveUI();

            for (int i = 0; i < waves[w].enemyCount; i++)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;

                Spawn();
                yield return new WaitForSeconds(waves[w].spawnInterval);
            }

            while (EnemyManager.Instance != null && EnemyManager.Instance.AliveCount > 0)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
                yield return null;
            }

            if (w < waves.Length - 1)
            {
                if (GM != null) GM.PlaySoundAll("波次完成");
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        allWavesDone = true;

        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            GameManager.Instance.Victory();
    }

    private void Spawn()
    {
        if (enemyPrefab == null || waypoints == null || waypoints.Length < 2) return;

        Vector3 spawnPos = waypoints[0].position;
        GameObject obj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null) enemy.SetPath(waypoints);

        NetworkObject no = obj.GetComponent<NetworkObject>();
        if (no != null) no.Spawn();
    }

    private void UpdateWaveUI()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetWaveInfo(currentWaveIndex + 1, waves.Length);

        if (GM != null) GM.PlaySoundAll("敌人来袭");
    }
}