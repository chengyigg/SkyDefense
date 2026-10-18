using UnityEngine;
using Unity.Netcode;

public class Wingman : NetworkBehaviour
{
    [Header("运行参数")]
    public ulong targetNetworkId;
    public int orbitIndex = 0;
    public int totalCount = 3;
    public GameObject bulletPrefab;
    public float attackInterval = 0.3f;
    public int damage = 3;
    public float orbitRadius = 4f;
    public float orbitSpeed = 180f;
    public float lifeTime = 10f;

    private float _elapsed = 0f;
    private float _angle = 0f;
    private float _cooldown = 0f;
    private Enemy _target;

    public override void OnNetworkSpawn()
    {
        _angle = orbitIndex * (360f / totalCount);
        _elapsed = 0f;

        if (IsServer) FindTarget();
    }

    void FindTarget()
    {
        _target = null;

        if (targetNetworkId != 0 &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out var netObj))
        {
            _target = netObj.GetComponent<Enemy>();
        }

        if (_target == null || _target.IsDead)
        {
            if (LockOnManager.Instance != null && LockOnManager.Instance.LockedEnemy != null)
                _target = LockOnManager.Instance.LockedEnemy;
            else if (EnemyManager.Instance != null)
                _target = EnemyManager.Instance.FindNearestInRange(transform.position, 9999f);
        }
    }

    void Update()
    {
        if (!IsServer) return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= lifeTime)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        if (_target == null || _target.IsDead) FindTarget();
        if (_target == null) return;

        _angle += orbitSpeed * Time.deltaTime;
        float rad = _angle * Mathf.Deg2Rad;
        Vector3 center = _target.transform.position;
        Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitRadius;
        Vector3 desired = center + offset;
        desired.y = center.y + 2f;

        transform.position = Vector3.Lerp(transform.position, desired, 5f * Time.deltaTime);

        Vector3 lookDir = _target.transform.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir);

        _cooldown -= Time.deltaTime;
        if (_cooldown <= 0f)
        {
            Fire();
            _cooldown = attackInterval;
        }
    }

    void Fire()
    {
        if (_target == null || bulletPrefab == null) return;

        SoundManager.Instance?.Play("僚机开火");

        Vector3 spawnPos = transform.position + transform.forward * 1f;
        GameObject obj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet bullet = obj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.SetTarget(_target.transform, damage, 1f, 0f, 0f, false);

        FireClientRpc(spawnPos, _target.NetworkObjectId);
    }

    [ClientRpc]
    void FireClientRpc(Vector3 spawnPos, ulong targetId)
    {
        if (IsServer) return;

        SoundManager.Instance?.Play("僚机开火");

        Enemy target = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var netObj))
            target = netObj.GetComponent<Enemy>();
        if (target == null) return;

        GameObject obj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet bullet = obj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.SetTarget(target.transform, damage, 1f, 0f, 0f, true);
    }
}