using UnityEngine;
using System.Collections.Generic;

public class MortarShell : MonoBehaviour
{
    [Header("抛物线参数")]
    public float flightTime = 1.4f;
    public float arcHeight = 14f;

    private Vector3 _start;
    private Vector3 _end;
    private float _elapsed;
    private bool _launched = false;

    private int _damage = 5;
    private float _explosionRadius = 4f;
    private float _slowMultiplier = 1f;
    private float _slowDuration = 0f;
    private float _stunDuration = 0f;
    private bool _visualOnly = false;

    private static Queue<MortarShell> _pool = new Queue<MortarShell>();

    public static MortarShell SpawnFromPool(MortarShell prefab, Vector3 position)
    {
        MortarShell m;
        if (_pool.Count > 0)
        {
            m = _pool.Dequeue();
            m.transform.position = position;
            m.transform.rotation = Quaternion.identity;
            m.gameObject.SetActive(true);
        }
        else
        {
            m = Instantiate(prefab, position, Quaternion.identity);
        }
        return m;
    }

    public void Launch(Vector3 start, Vector3 end, int damage, float radius,
                        float slowMul, float slowDur, float stunDur, bool visualOnly = false)
    {
        _start = start;
        _end = new Vector3(end.x, 0.3f, end.z);
        _damage = damage;
        _explosionRadius = radius;
        _slowMultiplier = slowMul;
        _slowDuration = slowDur;
        _stunDuration = stunDur;
        _visualOnly = visualOnly;
        _elapsed = 0f;
        _launched = true;
        transform.position = start;
    }

    void Update()
    {
        if (!_launched) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / flightTime);

        Vector3 pos = Vector3.Lerp(_start, _end, t);
        pos.y += arcHeight * 4f * t * (1f - t);
        transform.position = pos;

        Vector3 tangent;
        tangent.x = _end.x - _start.x;
        tangent.z = _end.z - _start.z;
        tangent.y = (_end.y - _start.y) + arcHeight * 4f * (1f - 2f * t);

        if (tangent.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(tangent.normalized);

        if (t >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        SoundManager.Instance?.Play("爆炸");

        if (!_visualOnly)
        {
            var all = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllEnemies() : null;
            if (all != null)
            {
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    var e = all[i];
                    if (e == null || e.IsDead) continue;
                    float dist = Vector3.Distance(
                        new Vector3(e.transform.position.x, 0, e.transform.position.z),
                        new Vector3(transform.position.x, 0, transform.position.z));
                    if (dist <= _explosionRadius)
                    {
                        e.TakeDamage(_damage);
                        if (_slowMultiplier < 1f && _slowDuration > 0f) e.ApplySlow(_slowMultiplier, _slowDuration);
                        if (_stunDuration > 0f) e.ApplyStun(_stunDuration);
                    }
                }
            }
        }

        ExplosionEffect.Spawn(transform.position, _explosionRadius);
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.6f, 0.2f);

        Despawn();
    }

    private void Despawn()
    {
        _launched = false;
        gameObject.SetActive(false);
        _pool.Enqueue(this);
    }
}