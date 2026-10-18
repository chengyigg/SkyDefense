using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Tower : NetworkBehaviour
{
    public TowerData data;

    [Header("网络数据索引")]
    public NetworkVariable<int> dataIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("当前等级")]
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("炮塔朝向（Server 广播给客户端）")]
    public NetworkVariable<float> turretYawNV = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("发射点")]
    public Transform firePoint;

    private float buildTimer = 0f;
    private bool isBuilt = false;
    private Slider progressSlider;
    private Vector3 _originalScale;

    private Enemy _currentTarget;
    private float _cooldown = 0f;

    private bool _isAiming = false;
    private float _aimTimer = 0f;
    private LineRenderer _aimLine;

    private float _lastSentYaw = 0f;

    private static Material _aimLineSharedMaterial;

    public int CurrentLevel => currentLevel.Value;
    public bool IsBuilt => isBuilt;
    public TowerData Data => data;
    private TowerLevelStats Stats => data != null ? data.GetStats(CurrentLevel) : null;

    public float GetCurrentRange()
    {
        var stats = Stats;
        return stats != null ? stats.range : 0f;
    }

    public override void OnNetworkSpawn()
    {
        dataIndex.OnValueChanged += HandleDataIndexChanged;
        turretYawNV.OnValueChanged += HandleYawChanged;

        TryResolveDataFromIndex(dataIndex.Value);

        if (BuildManager.Instance != null)
        {
            Vector2Int key = BuildManager.Instance.WorldToGrid(transform.position);
            BuildManager.Instance.RegisterTower(key, this);
        }
    }

    public override void OnNetworkDespawn()
    {
        dataIndex.OnValueChanged -= HandleDataIndexChanged;
        turretYawNV.OnValueChanged -= HandleYawChanged;
    }

    private void HandleDataIndexChanged(int oldV, int newV)
    {
        TryResolveDataFromIndex(newV);
    }

    private void TryResolveDataFromIndex(int idx)
    {
        if (idx < 0) return;
        if (BuildManager.Instance == null) return;
        var datas = BuildManager.Instance.towerDatas;
        if (datas == null || idx >= datas.Length)
        {
            Debug.LogWarning($"[Tower] dataIndex {idx} 越界（数组长度 {(datas == null ? 0 : datas.Length)}），已忽略");
            return;
        }
        data = datas[idx];
    }

    private void HandleYawChanged(float oldV, float newV)
    {
        if (!IsServer)
            transform.rotation = Quaternion.Euler(0, newV, 0);
    }

    void Start()
    {
        progressSlider = GetComponentInChildren<Slider>();
        if (progressSlider != null) progressSlider.value = 0f;

        _originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private Vector3 GetFirePosition()
    {
        if (firePoint != null) return firePoint.position;
        return transform.position + Vector3.up * 1.5f;
    }

    void Update()
    {
        if (!isBuilt)
        {
            buildTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(buildTimer / data.buildTime);
            transform.localScale = Vector3.Lerp(Vector3.zero, _originalScale, progress);
            if (progressSlider != null) progressSlider.value = progress;
            if (progress >= 1f) FinishBuild();
            return;
        }

        if (IsSpawned && !IsServer) return;

        UpdateAttack();
    }

    private void FinishBuild()
    {
        isBuilt = true;
        transform.localScale = _originalScale;
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        BuildDustParticles.Spawn(transform.position);
        if (BuildManager.Instance != null) BuildManager.Instance.OnTowerBuilt();
        SoundManager.Instance?.Play("建造完成");
    }

    private void UpdateAttack()
    {
        var stats = Stats;
        if (stats == null) return;

        if (LockOnManager.Instance != null)
        {
            var locked = LockOnManager.Instance.LockedEnemy;
            if (locked != null && locked != _currentTarget && IsInRange(locked))
            {
                _currentTarget = locked;
                _cooldown = 0f;
                if (_isAiming) CancelAim();
            }
        }

        if (_isAiming)
        {
            if (_currentTarget == null)
            {
                CancelAim();
                _cooldown = 0f;
                return;
            }

            RotateTurretTo(_currentTarget);
            _aimTimer -= Time.deltaTime;
            UpdateAimLine();

            if (_aimTimer <= 0f)
            {
                Fire();
                _cooldown = Mathf.Max(0.05f, stats.attackInterval - data.aimLineDuration);
                EndAim();
            }
            return;
        }

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        if (data.attackType == TowerAttackType.Pulse)
        {
            Fire();
            _cooldown = stats.attackInterval;
            return;
        }

        if (_currentTarget == null || !IsInRange(_currentTarget))
            _currentTarget = FindTarget(stats.range);

        if (_currentTarget == null) return;

        RotateTurretTo(_currentTarget);

        if (data.useAimLine && data.aimLineDuration > 0f)
            StartAim();
        else
        {
            Fire();
            _cooldown = stats.attackInterval;
        }
    }

    public int GetTotalInvested()
    {
        if (data == null) return 0;
        int total = data.cost;
        for (int i = 1; i <= CurrentLevel; i++)
        {
            var stats = data.GetStats(i);
            if (stats != null) total += stats.upgradeCost;
        }
        return total;
    }

    public void RequestSell()
    {
        if (IsServer) DoSell();
        else SellServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SellServerRpc() { DoSell(); }

    private void DoSell()
    {
        if (!IsServer) return;

        int total = GetTotalInvested();
        int refund = Mathf.RoundToInt(total * 0.7f);

        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(refund);

        if (BuildManager.Instance != null)
        {
            Vector2Int key = BuildManager.Instance.WorldToGrid(transform.position);
            BuildManager.Instance.UnregisterTower(key);
        }

        // 【新增】删塔时播放建造时的扬尘特效
        BuildDustParticles.Spawn(transform.position);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);

        PlaySoundClientRpc("卖塔");
    }

    private void RotateTurretTo(Enemy target)
    {
        if (target == null) return;
        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = targetRot;

        float yaw = targetRot.eulerAngles.y;
        if (Mathf.Abs(Mathf.DeltaAngle(_lastSentYaw, yaw)) > 2f)
        {
            _lastSentYaw = yaw;
            if (IsServer) turretYawNV.Value = yaw;
        }
    }

    private Enemy FindTarget(float range)
    {
        if (LockOnManager.Instance != null)
        {
            var locked = LockOnManager.Instance.LockedEnemy;
            if (locked != null && !locked.IsDead && IsInRange(locked))
                return locked;
        }

        if (EnemyManager.Instance == null) return null;
        return data.targetPriority == TargetPriority.Farthest
            ? EnemyManager.Instance.FindFarthestInRange(transform.position, range)
            : EnemyManager.Instance.FindNearestInRange(transform.position, range);
    }

    private bool IsInRange(Enemy e)
    {
        if (e == null) return false;
        var stats = Stats;
        if (stats == null) return false;
        return (e.transform.position - transform.position).sqrMagnitude <= stats.range * stats.range;
    }

    private void Fire()
    {
        if (data.attackType == TowerAttackType.Projectile) FireProjectile();
        else if (data.attackType == TowerAttackType.Pulse) FirePulse();
    }

    private void FireProjectile()
    {
        if (data.bulletPrefab == null) return;
        var stats = Stats;
        if (stats == null) return;
        if (_currentTarget == null) return;

        Vector3 spawnPos = GetFirePosition();
        Vector3 targetPos = _currentTarget.transform.position;

        if (data.projectileMode == ProjectileMode.Mortar)
        {
            SoundManager.Instance?.Play("群伤塔发射");
            SpawnMortarLocal(spawnPos, targetPos, stats, true);
            SpawnMortarClientRpc(spawnPos, targetPos, dataIndex.Value, CurrentLevel);
        }
        else
        {
            if (data.useAimLine)
                SoundManager.Instance?.Play("狙击塔开火");
            else
                SoundManager.Instance?.Play("机枪塔开火");

            SpawnBulletLocal(spawnPos, _currentTarget.transform, stats, true);
            SpawnBulletClientRpc(spawnPos, _currentTarget.NetworkObjectId, dataIndex.Value, CurrentLevel);

            if (data.useAimLine && CameraShake.Instance != null)
                CameraShake.Instance.Shake(0.15f, 0.1f);

            // 【新增】狙击塔专属：客户端顿帧（Hit Stop），极大增强打击感
            if (data.useAimLine)
            {
                StartCoroutine(HitStop(0.05f, 0.08f));
            }
        }
    }

    // 【新增】顿帧协程
    private System.Collections.IEnumerator HitStop(float timeScale, float duration)
    {
        // 只在客户端或者Host上生效表现，不改服务器逻辑
        if (Time.timeScale < 1f) yield break;

        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private void SpawnBulletLocal(Vector3 spawnPos, Transform target, TowerLevelStats stats, bool dealDamage)
    {
        Bullet bulletPrefabComp = data.bulletPrefab.GetComponent<Bullet>();
        Bullet bullet = Bullet.SpawnFromPool(bulletPrefabComp, spawnPos);

        if (bullet != null)
            bullet.SetTarget(target, stats.damage, stats.slowMultiplier, stats.slowDuration, stats.explosionRadius, !dealDamage);
    }

    private void SpawnMortarLocal(Vector3 spawnPos, Vector3 targetPos, TowerLevelStats stats, bool dealDamage)
    {
        MortarShell mortarPrefabComp = data.bulletPrefab.GetComponent<MortarShell>();
        MortarShell shell = MortarShell.SpawnFromPool(mortarPrefabComp, spawnPos);

        if (shell != null)
            shell.Launch(spawnPos, targetPos, stats.damage, stats.explosionRadius,
                stats.slowMultiplier, stats.slowDuration, stats.stunDuration, !dealDamage);
    }

    [ClientRpc]
    private void SpawnBulletClientRpc(Vector3 spawnPos, ulong targetNetObjId, int idx, int level)
    {
        if (IsServer) return;
        if (BuildManager.Instance == null) return;
        var datas = BuildManager.Instance.towerDatas;
        if (datas == null || idx < 0 || idx >= datas.Length) return;
        var towerData = datas[idx];
        if (towerData == null) return;

        var stats = towerData.GetStats(level);
        if (stats == null) return;

        if (towerData.useAimLine)
            SoundManager.Instance?.Play("狙击塔开火");
        else
            SoundManager.Instance?.Play("机枪塔开火");

        Transform target = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var netObj))
            target = netObj.transform;

        if (target == null) return;

        Bullet bulletPrefabComp = towerData.bulletPrefab.GetComponent<Bullet>();
        Bullet bullet = Bullet.SpawnFromPool(bulletPrefabComp, spawnPos);

        if (bullet != null)
            bullet.SetTarget(target, stats.damage, stats.slowMultiplier, stats.slowDuration, stats.explosionRadius, true);
    }

    [ClientRpc]
    private void SpawnMortarClientRpc(Vector3 spawnPos, Vector3 targetPos, int idx, int level)
    {
        if (IsServer) return;
        if (BuildManager.Instance == null) return;
        var datas = BuildManager.Instance.towerDatas;
        if (datas == null || idx < 0 || idx >= datas.Length) return;
        var towerData = datas[idx];
        if (towerData == null) return;

        var stats = towerData.GetStats(level);
        if (stats == null) return;

        SoundManager.Instance?.Play("群伤塔发射");

        MortarShell mortarPrefabComp = towerData.bulletPrefab.GetComponent<MortarShell>();
        MortarShell shell = MortarShell.SpawnFromPool(mortarPrefabComp, spawnPos);

        if (shell != null)
            shell.Launch(spawnPos, targetPos, stats.damage, stats.explosionRadius,
                stats.slowMultiplier, stats.slowDuration, stats.stunDuration, true);
    }

    private void FirePulse()
    {
        var stats = Stats;
        if (stats == null) return;

        SoundManager.Instance?.Play("减速塔开火");

        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Play();

        SpawnPulseLocal(stats, false);
        SpawnPulseClientRpc(dataIndex.Value, CurrentLevel);
    }

    private void SpawnPulseLocal(TowerLevelStats stats, bool isVisualOnly)
    {
        GameObject obj = new GameObject("SlowPulse");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        SlowPulse pulse = obj.AddComponent<SlowPulse>();
        pulse.Init(
            stats.range, isVisualOnly ? 0 : stats.damage,
            stats.slowMultiplier, stats.slowDuration,
            stats.pulseColor, stats.stunDuration
        );
    }

    [ClientRpc]
    private void SpawnPulseClientRpc(int idx, int level)
    {
        if (IsServer) return;
        if (BuildManager.Instance == null) return;
        var datas = BuildManager.Instance.towerDatas;
        if (datas == null || idx < 0 || idx >= datas.Length) return;
        var towerData = datas[idx];
        if (towerData == null) return;

        var stats = towerData.GetStats(level);
        if (stats == null) return;

        SoundManager.Instance?.Play("减速塔开火");

        SpawnPulseLocal(stats, true);

        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Play();
    }

    [ClientRpc]
    private void PlaySoundClientRpc(string soundName)
    {
        SoundManager.Instance?.Play(soundName);
    }

    private void EnsureAimLine()
    {
        if (_aimLine != null) return;

        GameObject obj = new GameObject("AimLine");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        _aimLine = obj.AddComponent<LineRenderer>();
        _aimLine.useWorldSpace = true;
        _aimLine.positionCount = 2;
        _aimLine.widthMultiplier = data.aimLineWidth;
        _aimLine.numCapVertices = 2;
        _aimLine.material = GetSharedAimLineMaterial();
        _aimLine.startColor = data.aimLineColor;
        _aimLine.endColor = data.aimLineColor;
        _aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _aimLine.receiveShadows = false;
        _aimLine.enabled = false;
    }

    private static Material GetSharedAimLineMaterial()
    {
        if (_aimLineSharedMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _aimLineSharedMaterial = new Material(shader);
        }
        return _aimLineSharedMaterial;
    }

    private void StartAim()
    {
        _isAiming = true;
        _aimTimer = data.aimLineDuration;
        EnsureAimLine();
        _aimLine.widthMultiplier = data.aimLineWidth;
        _aimLine.startColor = data.aimLineColor;
        _aimLine.endColor = data.aimLineColor;
        _aimLine.enabled = true;
        UpdateAimLine();
        SoundManager.Instance?.Play("狙击瞄准");
    }

    private void UpdateAimLine()
    {
        if (_aimLine == null) return;
        if (_currentTarget == null) return;
        Vector3 start = GetFirePosition();
        Vector3 end = _currentTarget.transform.position + Vector3.up * 1f;
        _aimLine.SetPosition(0, start);
        _aimLine.SetPosition(1, end);
    }

    private void EndAim() { _isAiming = false; if (_aimLine != null) _aimLine.enabled = false; }
    private void CancelAim() { _isAiming = false; if (_aimLine != null) _aimLine.enabled = false; }

    public void SetData(TowerData towerData) { data = towerData; }

    public bool TryUpgrade()
    {
        if (data == null) return false;
        int nextLevel = CurrentLevel + 1;
        if (nextLevel >= data.MaxLevel)
        {
            Debug.Log("已是最高等级");
            return false;
        }

        if (!IsServer) { UpgradeServerRpc(); return true; }
        return DoUpgrade();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpgradeServerRpc() { DoUpgrade(); }

    private bool DoUpgrade()
    {
        int nextLevel = CurrentLevel + 1;
        if (nextLevel >= data.MaxLevel) return false;

        var nextStats = data.GetStats(nextLevel);
        if (nextStats == null) return false;
        if (GameManager.Instance == null) return false;

        if (!GameManager.Instance.SpendGold(nextStats.upgradeCost))
        {
            Debug.Log("金币不足，无法升级");
            return false;
        }

        currentLevel.Value = nextLevel;
        Debug.Log($"{data.towerName} 升级到 Lv{nextLevel + 1}");
        PlaySoundClientRpc("塔升级");
        return true;
    }
}