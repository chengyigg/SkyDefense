using UnityEngine;
using Unity.Netcode;

public class LockOnManager : NetworkBehaviour
{
    public static LockOnManager Instance;

    [Header("锁定参数")]
    public float lockDuration = 5f;

    public NetworkVariable<ulong> lockedTargetId = new NetworkVariable<ulong>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> lockStartTime = new NetworkVariable<float>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Enemy _cachedEnemy;

    public Enemy LockedEnemy
    {
        get
        {
            if (_cachedEnemy == null) return null;
            if (_cachedEnemy.IsDead) return null;
            if (Time.time - lockStartTime.Value > lockDuration) return null;
            return _cachedEnemy;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        lockedTargetId.OnValueChanged += OnLockIdChanged;
        RefreshCache();
    }

    public override void OnNetworkDespawn()
    {
        lockedTargetId.OnValueChanged -= OnLockIdChanged;
    }

    private void OnLockIdChanged(ulong oldV, ulong newV)
    {
        RefreshCache();
    }

    void RefreshCache()
    {
        _cachedEnemy = null;
        if (lockedTargetId.Value == 0) return;
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            lockedTargetId.Value, out var netObj))
        {
            _cachedEnemy = netObj.GetComponent<Enemy>();
        }
    }

    void Update()
    {
        if (!IsServer) return;

        if (lockedTargetId.Value != 0)
        {
            bool shouldClear = false;
            if (_cachedEnemy == null || _cachedEnemy.IsDead) shouldClear = true;
            if (Time.time - lockStartTime.Value > lockDuration) shouldClear = true;

            if (shouldClear)
            {
                lockedTargetId.Value = 0;
                _cachedEnemy = null;
            }
        }
    }

    public void RequestLock(Enemy target)
    {
        if (target == null) return;
        ulong id = target.NetworkObjectId;

        if (IsServer) DoLock(id);
        else RequestLockServerRpc(id);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLockServerRpc(ulong id)
    {
        DoLock(id);
    }

    private void DoLock(ulong id)
    {
        if (!IsServer) return;
        lockedTargetId.Value = id;
        lockStartTime.Value = Time.time;
        SoundManager.Instance?.Play("锁定敌人");
    }
}