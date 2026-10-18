using UnityEngine;
using Unity.Netcode;

public class Enemy : NetworkBehaviour
{
    [Header("属性")]
    public float moveSpeed = 5f;
    public int damage = 1;
    public int goldReward = 15;
    public int maxHealth = 200;

    [Header("网络血量")]
    public NetworkVariable<int> currentHealthNV = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("网络减速状态")]
    public NetworkVariable<bool> isSlowedNV = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("路径")]
    public Transform[] waypoints;

    private int _currentIndex = 0;
    private float _arriveThreshold = 0.2f;

    private float _speedMultiplier = 1f;
    private float _slowTimer = 0f;
    private float _stunTimer = 0f;

    private bool _isDead = false;
    public bool IsDead => _isDead;

    [Header("减速视觉")]
    public Color slowTint = new Color(0.4f, 0.7f, 1f);

    [Header("血条设置")]
    public float healthBarHeight = 6.0f;
    public float healthBarWidth = 3.5f;
    public float healthBarThickness = 0.4f;
    public Color healthBarFill = new Color(0.9f, 0.2f, 0.2f, 1f);

    private MeshRenderer[] _renderers;
    private Material[] _sharedMaterials;
    private string[] _colorPropertyNames;
    private Color[] _originalColors;
    private MaterialPropertyBlock _mpb;
    private TrailRenderer _trail;

    // 【性能优化】受击闪白状态
    private bool _isFlashing = false;
    private float _flashTimer = 0f;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealthNV.Value = maxHealth;

        currentHealthNV.OnValueChanged += HandleHealthChanged;
        isSlowedNV.OnValueChanged += HandleSlowChanged;

        _renderers = GetComponentsInChildren<MeshRenderer>(true);
        int n = _renderers.Length;
        _sharedMaterials = new Material[n];
        _colorPropertyNames = new string[n];
        _originalColors = new Color[n];

        for (int i = 0; i < n; i++)
        {
            _sharedMaterials[i] = _renderers[i].sharedMaterial;
            var mat = _sharedMaterials[i];

            if (mat == null)
            {
                _colorPropertyNames[i] = null;
                _originalColors[i] = Color.white;
                continue;
            }

            if (mat.HasProperty("baseColorFactor"))
            {
                _colorPropertyNames[i] = "baseColorFactor";
                _originalColors[i] = mat.GetColor("baseColorFactor");
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                _colorPropertyNames[i] = "_BaseColor";
                _originalColors[i] = mat.GetColor("_BaseColor");
            }
            else if (mat.HasProperty("_Color"))
            {
                _colorPropertyNames[i] = "_Color";
                _originalColors[i] = mat.GetColor("_Color");
            }
            else
            {
                _colorPropertyNames[i] = null;
                _originalColors[i] = Color.white;
            }
        }

        _trail = GetComponentInChildren<TrailRenderer>();
        if (_trail != null) _trail.emitting = false;

        ApplySlowVisual(isSlowedNV.Value);

        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.AddBar(this, healthBarWidth, healthBarThickness, healthBarFill);
            HealthBarManager.Instance.UpdateHealth(this, 1f);
        }

        if (EnemyManager.Instance != null) EnemyManager.Instance.Register(this);

        SoundManager.Instance?.Play("敌人生成");
    }

    public override void OnNetworkDespawn()
    {
        currentHealthNV.OnValueChanged -= HandleHealthChanged;
        isSlowedNV.OnValueChanged -= HandleSlowChanged;

        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.RemoveBar(this);

        if (EnemyManager.Instance != null) EnemyManager.Instance.Unregister(this);
    }

    private void HandleHealthChanged(int oldV, int newV)
    {
        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.UpdateHealth(this, Mathf.Clamp01((float)newV / maxHealth));

        if (newV < oldV)
        {
            // 【表现】受击闪白
            TriggerFlashWhite(0.08f);

            // 【表现】伤害飘字（稍后提供DamageTextManager后，这里会调用）
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.ShowDamage(transform.position + Vector3.up * healthBarHeight, oldV - newV);
            }

            if (newV <= 0)
                SoundManager.Instance?.Play("敌人死亡");
            else
                SoundManager.Instance?.Play("敌人受击");
        }
    }

    private void HandleSlowChanged(bool oldV, bool newV)
    {
        if (!_isFlashing)
            ApplySlowVisual(newV);
    }

    // 【表现】控制颜色（优先级：闪白 > 减速 > 原色）
    private void ApplySlowVisual(bool slowed)
    {
        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_colorPropertyNames[i] == null) continue;

            Color c = slowed ? slowTint : _originalColors[i];
            _mpb.SetColor(_colorPropertyNames[i], c);
            _renderers[i].SetPropertyBlock(_mpb);
        }

        if (_trail != null) _trail.emitting = slowed;
    }

    private void TriggerFlashWhite(float duration)
    {
        _isFlashing = true;
        _flashTimer = duration;

        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_colorPropertyNames[i] == null) continue;
            _mpb.SetColor(_colorPropertyNames[i], Color.white);
            _renderers[i].SetPropertyBlock(_mpb);
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        _speedMultiplier = Mathf.Min(_speedMultiplier, multiplier);
        _slowTimer = Mathf.Max(_slowTimer, duration);
        if (IsServer) isSlowedNV.Value = true;
    }

    public void ApplyStun(float duration)
    {
        _stunTimer = Mathf.Max(_stunTimer, duration);
    }

    void Update()
    {
        // 【表现】闪白倒计时（不需要区分服务器客户端，所有端都要表现）
        if (_isFlashing)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _isFlashing = false;
                ApplySlowVisual(isSlowedNV.Value); // 恢复为减速或原色
            }
        }

        if (!IsServer) return;
        if (_isDead) return;

        if (_slowTimer > 0f)
        {
            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0f)
            {
                _speedMultiplier = 1f;
                if (IsServer) isSlowedNV.Value = false;
            }
        }

        bool isStunned = false;
        if (_stunTimer > 0f)
        {
            _stunTimer -= Time.deltaTime;
            isStunned = true;
        }

        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[_currentIndex];
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.magnitude <= _arriveThreshold)
        {
            _currentIndex++;
            if (_currentIndex >= waypoints.Length)
            {
                ReachEnd();
                return;
            }
        }
        else
        {
            Vector3 moveDir = dir.normalized;
            if (!isStunned)
            {
                float actualSpeed = moveSpeed * _speedMultiplier;
                transform.position += moveDir * actualSpeed * Time.deltaTime;
            }
            if (moveDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(moveDir);
        }
    }

    private void ReachEnd()
    {
        if (!IsServer) return;
        if (_isDead) return;
        _isDead = true;

        if (GameManager.Instance != null)
            GameManager.Instance.TakeDamage(damage);

        PlaySoundClientRpc("敌人到终点");
        DespawnSelf();
    }

    public void SetPath(Transform[] path)
    {
        waypoints = path;
        _currentIndex = 0;
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;
        if (_isDead) return;

        currentHealthNV.Value -= amount;
        if (currentHealthNV.Value <= 0) Die();
    }

    private void Die()
    {
        if (!IsServer) return;
        if (_isDead) return;
        _isDead = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(goldReward);

        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    [ClientRpc]
    private void PlaySoundClientRpc(string soundName)
    {
        SoundManager.Instance?.Play(soundName);
    }
}