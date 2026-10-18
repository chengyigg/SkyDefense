using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

public class SceneInspector : EditorWindow
{
    private Vector2 _scroll;
    private string _output = "";
    private bool _showEnemy = true;
    private bool _showTower = true;
    private bool _showTrail = true;
    private bool _showMaterial = false;

    [MenuItem("Tools/场景检查器")]
    public static void Open()
    {
        GetWindow<SceneInspector>("场景检查器");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("地毯式扫描场景中关键物体状态", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        _showEnemy = GUILayout.Toggle(_showEnemy, "敌人", "Button", GUILayout.Width(60));
        _showTower = GUILayout.Toggle(_showTower, "塔", "Button", GUILayout.Width(60));
        _showTrail = GUILayout.Toggle(_showTrail, "拖尾", "Button", GUILayout.Width(60));
        _showMaterial = GUILayout.Toggle(_showMaterial, "材质", "Button", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("扫描场景", GUILayout.Height(30))) Scan();

        if (GUILayout.Button("复制结果到剪贴板"))
            EditorGUIUtility.systemCopyBuffer = _output;

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        _output = EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void Scan()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("======== 场景检查 ========");
        sb.AppendLine("时间: " + System.DateTime.Now.ToString("HH:mm:ss"));
        sb.AppendLine();

        if (_showEnemy)
        {
            sb.AppendLine("---- 敌人 (Enemy.cs) ----");
            var enemies = FindObjectsOfType<Enemy>();
            sb.AppendLine($"数量: {enemies.Length}");
            foreach (var e in enemies)
            {
                sb.AppendLine($"  [{e.name}]");
                sb.AppendLine($"    Position: {e.transform.position}");
                sb.AppendLine($"    MoveSpeed: {e.moveSpeed}");
                var renderer = e.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    sb.AppendLine($"    Material: {renderer.sharedMaterial.name} (shader: {renderer.sharedMaterial.shader.name})");
                var col = e.GetComponentInChildren<Collider>();
                sb.AppendLine($"    Collider: {(col != null ? col.GetType().Name : "无")}");
                sb.AppendLine();
            }
        }

        if (_showTower)
        {
            sb.AppendLine("---- 塔 (Tower.cs) ----");
            var towers = FindObjectsOfType<Tower>();
            sb.AppendLine($"数量: {towers.Length}");
            foreach (var t in towers)
            {
                sb.AppendLine($"  [{t.name}]");
                sb.AppendLine($"    Position: {t.transform.position}");
                if (t.data != null)
                {
                    sb.AppendLine($"    Data: {t.data.name}");
                    sb.AppendLine($"    AttackType: {t.data.attackType}");
                    sb.AppendLine($"    CurrentLevel: {t.CurrentLevel + 1} / {t.data.MaxLevel}");

                    var stats = t.data.GetStats(t.CurrentLevel);
                    if (stats != null)
                    {
                        sb.AppendLine($"    Range: {stats.range}");
                        sb.AppendLine($"    Damage: {stats.damage}");
                        sb.AppendLine($"    AttackInterval: {stats.attackInterval}");
                        sb.AppendLine($"    SlowMul: {stats.slowMultiplier}, SlowDur: {stats.slowDuration}");
                        sb.AppendLine($"    NextUpgradeCost: {stats.upgradeCost}");
                    }
                    else
                    {
                        sb.AppendLine($"    Stats: 未配置。");
                    }
                }
                else
                {
                    sb.AppendLine($"    Data: 未设置!");
                }
                var ps = t.GetComponentInChildren<ParticleSystem>();
                sb.AppendLine($"    粒子系统: {(ps != null ? "有" : "无")}");
                sb.AppendLine();
            }
        }

        if (_showTrail)
        {
            sb.AppendLine("---- 拖尾 (TrailRenderer) ----");
            var all = FindObjectsOfType<TrailRenderer>();
            sb.AppendLine($"数量: {all.Length}");
            foreach (var tr in all)
            {
                sb.AppendLine($"  [{tr.name}] (parent: {tr.transform.parent?.name})");
                sb.AppendLine($"    Emitting: {tr.emitting}");
                sb.AppendLine($"    Time: {tr.time}");
                sb.AppendLine($"    WidthMultiplier: {tr.widthMultiplier}");
                if (tr.sharedMaterial != null)
                    sb.AppendLine($"    Material: {tr.sharedMaterial.name} (shader: {tr.sharedMaterial.shader.name})");
                else
                    sb.AppendLine($"    Material: 缺失!");
                sb.AppendLine();
            }
        }

        if (_showMaterial)
        {
            sb.AppendLine("---- 场景中所有材质 ----");
            var renderers = FindObjectsOfType<Renderer>();
            HashSet<string> seen = new HashSet<string>();
            foreach (var r in renderers)
            {
                var m = r.sharedMaterial;
                if (m == null || seen.Contains(m.name)) continue;
                seen.Add(m.name);
                sb.AppendLine($"  {m.name}: {m.shader.name}");
                if (m.HasProperty("_BaseColor"))
                    sb.AppendLine($"    _BaseColor: {m.GetColor("_BaseColor")}");
                if (m.HasProperty("_Color"))
                    sb.AppendLine($"    _Color: {m.GetColor("_Color")}");
            }
        }

        _output = sb.ToString();
        Debug.Log("场景扫描完成");
    }
}