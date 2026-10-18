using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public float speed = 40f;
    public int damage = 5;
    public float lifeTime = 3f;
    public float missLifeTime = 0.4f;

    private float _slowMultiplier = 1f;
    private float _slowDuration = 0f;
    private float _explosionRadius = 0f;
    private bool _visualOnly = false;

    private Transform _target;
    private Enemy _targetEnemy;
    private Vector3 _lastDirection = Vector3.forward;
    private float _missTimer = 0f;
    private bool _isMissing = false;

    private float _lifeTimer = 0f;

    private static Queue<Bullet> _pool = new Queue<Bullet>();
    private static Bullet _prefab;

    public static void SetPrefab(Bullet prefab)
    {
        if (_prefab == null) _prefab = prefab;
    }

    public static Bullet SpawnFromPool(Bullet prefab, Vector3 position)
    {
        SetPrefab(prefab);

        Bullet b;
        if (_pool.Count > 0)
        {
            b = _pool.Dequeue();
            b.transform.position = position;
            b.transform.rotation = Quaternion.identity;
            b.gameObject.SetActive(true);
        }
        else
        {
            b = Instantiate(prefab, position, Quaternion.identity);
        }
        return b;
    }

    public void SetTarget(Transform target, int dmg, float slowMul, float slowDur, float explosionRadius = 0f, bool visualOnly = false)
    {
        _target = target;
        _targetEnemy = target != null ? target.GetComponentInParent<Enemy>() : null;
        damage = dmg;
        _slowMultiplier = slowMul;
        _slowDuration = slowDur;
        _explosionRadius = explosionRadius;
        _visualOnly = visualOnly;

        _isMissing = false;
        _missTimer = 0f;
        _lifeTimer = lifeTime;
    }

    void Update()
    {
        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
        {
            Despawn();
            return;
        }

        if (_targetEnemy != null && _targetEnemy.IsDead)
        {
            _target = null;
            _targetEnemy = null;
        }

        if (_target == null && !_isMissing)
        {
            _isMissing = true;
            _missTimer = missLifeTime;
        }

        if (!_isMissing)
        {
            Vector3 dir = _target.position - transform.position;
            _lastDirection = dir.normalized;

            float step = speed * Time.deltaTime;
            if (dir.magnitude <= step)
            {
                OnHit();
                return;
            }

            transform.rotation = Quaternion.LookRotation(_lastDirection);
            transform.position += _lastDirection * step;
        }
        else
        {
            _missTimer -= Time.deltaTime;
            if (_missTimer <= 0f) { Despawn(); return; }
            if (_lastDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(_lastDirection);
            transform.position += _lastDirection * speed * Time.deltaTime;
        }
    }

    private void OnHit()
    {
        Vector3 hitPos = transform.position;

        if (!_visualOnly)
        {
            if (_explosionRadius > 0f)
            {
                Explode(hitPos);
            }
            else
            {
                if (_targetEnemy != null) ApplyDamage(_targetEnemy);
            }
        }
        else if (_explosionRadius > 0f)
        {
            ExplosionEffect.Spawn(hitPos, _explosionRadius);
        }

        Despawn();
    }

    private void Explode(Vector3 center)
    {
        if (EnemyManager.Instance == null) return;

        var all = EnemyManager.Instance.GetAllEnemies();
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null || e.IsDead) continue;
            float dist = Vector3.Distance(
                new Vector3(e.transform.position.x, 0, e.transform.position.z),
                new Vector3(center.x, 0, center.z));
            if (dist <= _explosionRadius) ApplyDamage(e);
        }
        ExplosionEffect.Spawn(center, _explosionRadius);
    }

    private void ApplyDamage(Enemy e)
    {
        e.TakeDamage(damage);
        if (_slowMultiplier < 1f && _slowDuration > 0f)
            e.ApplySlow(_slowMultiplier, _slowDuration);
    }

    private void Despawn()
    {
        gameObject.SetActive(false);
        _pool.Enqueue(this);
    }
}