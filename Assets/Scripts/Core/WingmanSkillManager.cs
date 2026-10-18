using UnityEngine;
using Unity.Netcode;

public class WingmanSkillManager : NetworkBehaviour
{
    public static WingmanSkillManager Instance;

    [Header("参数")]
    public GameObject wingmanPrefab;
    public GameObject bulletPrefab;

    public int cost = 30;
    public float cooldown = 15f;
    public int wingmanCount = 3;
    public float lifeTime = 10f;
    public float attackInterval = 0.3f;
    public int damage = 3;
    public float orbitRadius = 4f;
    public float orbitSpeed = 180f;

    [Header("冷却共享（客户端读取）")]
    public NetworkVariable<float> cooldownEndTime = new NetworkVariable<float>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.Q))
            RequestSummonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSummonServerRpc(ServerRpcParams p = default)
    {
        DoSummon();
    }

    private void DoSummon()
    {
        if (!IsServer) return;

        if (Time.time < cooldownEndTime.Value)
        {
            Debug.Log("技能冷却中");
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        Enemy target = null;
        if (LockOnManager.Instance != null)
            target = LockOnManager.Instance.LockedEnemy;

        if (target == null && EnemyManager.Instance != null)
            target = EnemyManager.Instance.FindNearestInRange(transform.position, 9999f);

        if (target == null)
        {
            Debug.Log("没有可攻击的目标");
            return;
        }

        if (!GameManager.Instance.SpendGold(cost))
        {
            Debug.Log("金币不足");
            return;
        }

        cooldownEndTime.Value = Time.time + cooldown;

        for (int i = 0; i < wingmanCount; i++)
        {
            float angle = i * (360f / wingmanCount);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 spawnPos = target.transform.position + new Vector3(
                Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitRadius;
            spawnPos.y = target.transform.position.y + 2f;

            GameObject obj = Instantiate(wingmanPrefab, spawnPos, Quaternion.identity);
            Wingman w = obj.GetComponent<Wingman>();
            if (w != null)
            {
                w.targetNetworkId = target.NetworkObjectId;
                w.orbitIndex = i;
                w.totalCount = wingmanCount;
                w.bulletPrefab = bulletPrefab;
                w.attackInterval = attackInterval;
                w.damage = damage;
                w.orbitRadius = orbitRadius;
                w.orbitSpeed = orbitSpeed;
                w.lifeTime = lifeTime;
            }

            NetworkObject no = obj.GetComponent<NetworkObject>();
            if (no != null) no.Spawn();
        }
        SoundManager.Instance?.Play("召唤僚机");
        Debug.Log($"召唤僚机，目标 {target.name}");
    }
}