using UnityEngine;

public enum TowerAttackType { Projectile, Pulse }
public enum ProjectileMode { Straight, Mortar }
public enum TargetPriority { Nearest, Farthest }

[System.Serializable]
public class TowerLevelStats
{
    public float range = 10f;
    public float attackInterval = 0.5f;
    public int damage = 5;

    [Range(0.1f, 1f)] public float slowMultiplier = 1f;
    public float slowDuration = 0f;

    [Header("减速塔 Lv3 特效")]
    public Color pulseColor = new Color(0.3f, 0.8f, 1f, 1f);
    public float stunDuration = 0f;

    [Header("群伤（0 = 单体，> 0 = 范围伤害）")]
    public float explosionRadius = 0f;

    public int upgradeCost = 0;
}

[CreateAssetMenu(fileName = "NewTowerData", menuName = "TowerDefense/TowerData")]
public class TowerData : ScriptableObject
{
    public string towerName;
    public int cost;
    public float buildTime;
    public GameObject towerPrefab;

    [Header("攻击配置")]
    public TowerAttackType attackType = TowerAttackType.Projectile;
    public ProjectileMode projectileMode = ProjectileMode.Straight;
    public GameObject bulletPrefab;

    [Header("目标选择策略")]
    public TargetPriority targetPriority = TargetPriority.Nearest;

    [Header("瞄准线特效（狙击塔专用）")]
    public bool useAimLine = false;
    public float aimLineDuration = 0.3f;
    public Color aimLineColor = new Color(1f, 0.15f, 0.15f, 0.9f);
    public float aimLineWidth = 0.08f;

    [Header("每一级属性（0=Lv1, 1=Lv2, 2=Lv3）")]
    public TowerLevelStats[] levels;

    public TowerLevelStats GetStats(int level)
    {
        if (levels == null || levels.Length == 0) return null;
        return levels[Mathf.Clamp(level, 0, levels.Length - 1)];
    }

    public int MaxLevel => levels != null ? levels.Length : 1;
}