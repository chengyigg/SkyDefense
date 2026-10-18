using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private List<Enemy> _enemies = new List<Enemy>();

    // ================= 【C-2 空间网格优化】 =================
    private const float CellSize = 10f;
    private const int GridThreshold = 20;      // 超过 20 个敌人才启用网格
    private const float RebuildInterval = 0.15f; // 网格重建间隔，防止每帧重建

    private Dictionary<Vector2Int, List<Enemy>> _grid = new Dictionary<Vector2Int, List<Enemy>>();
    private float _lastRebuildTime = -1f;
    // =======================================================

    void Awake()
    {
        Instance = this;
    }

    public void Register(Enemy e)
    {
        if (!_enemies.Contains(e)) _enemies.Add(e);
    }

    public void Unregister(Enemy e)
    {
        _enemies.Remove(e);
    }

    public List<Enemy> GetAllEnemies() => _enemies;

    public int AliveCount => _enemies.Count;

    private void RebuildGridIfNecessary()
    {
        if (_enemies.Count <= GridThreshold) return;

        if (Time.time - _lastRebuildTime < RebuildInterval) return;
        _lastRebuildTime = Time.time;

        foreach (var list in _grid.Values)
            list.Clear();

        foreach (var e in _enemies)
        {
            if (e == null) continue;

            Vector2Int key = new Vector2Int(
                Mathf.FloorToInt(e.transform.position.x / CellSize),
                Mathf.FloorToInt(e.transform.position.z / CellSize)
            );

            if (!_grid.TryGetValue(key, out var list))
            {
                list = new List<Enemy>();
                _grid[key] = list;
            }
            list.Add(e);
        }
    }

    public Enemy FindFarthestInRange(Vector3 pos, float range)
    {
        Enemy farthest = null;
        float bestSqr = 0f;

        if (_enemies.Count <= GridThreshold)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                float sqr = (e.transform.position - pos).sqrMagnitude;
                if (sqr <= range * range && sqr > bestSqr)
                {
                    bestSqr = sqr;
                    farthest = e;
                }
            }
            return farthest;
        }

        RebuildGridIfNecessary();

        int minX = Mathf.FloorToInt((pos.x - range) / CellSize);
        int maxX = Mathf.FloorToInt((pos.x + range) / CellSize);
        int minZ = Mathf.FloorToInt((pos.z - range) / CellSize);
        int maxZ = Mathf.FloorToInt((pos.z + range) / CellSize);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (_grid.TryGetValue(new Vector2Int(x, z), out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        if (e == null) continue;
                        float sqr = (e.transform.position - pos).sqrMagnitude;
                        if (sqr <= range * range && sqr > bestSqr)
                        {
                            bestSqr = sqr;
                            farthest = e;
                        }
                    }
                }
            }
        }
        return farthest;
    }

    public Enemy FindNearestInRange(Vector3 pos, float range)
    {
        Enemy nearest = null;
        float bestSqr = range * range;

        if (_enemies.Count <= GridThreshold)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                float sqr = (e.transform.position - pos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    nearest = e;
                }
            }
            return nearest;
        }

        RebuildGridIfNecessary();

        int minX = Mathf.FloorToInt((pos.x - range) / CellSize);
        int maxX = Mathf.FloorToInt((pos.x + range) / CellSize);
        int minZ = Mathf.FloorToInt((pos.z - range) / CellSize);
        int maxZ = Mathf.FloorToInt((pos.z + range) / CellSize);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (_grid.TryGetValue(new Vector2Int(x, z), out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        if (e == null) continue;
                        float sqr = (e.transform.position - pos).sqrMagnitude;
                        if (sqr <= bestSqr)
                        {
                            bestSqr = sqr;
                            nearest = e;
                        }
                    }
                }
            }
        }
        return nearest;
    }
}