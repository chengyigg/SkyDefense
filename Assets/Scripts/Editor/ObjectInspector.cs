using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

public class ObjectInspector : EditorWindow
{
    private Object _target;
    private Vector2 _scroll;
    private string _output = "";
    private bool _includePrivate = true;

    [MenuItem("Tools/对象检查器")]
    public static void Open()
    {
        GetWindow<ObjectInspector>("对象检查器");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("从 Hierarchy 拖物体，或从 Project 拖预制体/材质等", MessageType.Info);

        var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, _target == null ? "拖对象到这里" : _target.name);
        var evt = Event.current;
        if (dropRect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    _target = DragAndDrop.objectReferences[0];
                    Dump();
                }
                evt.Use();
            }
        }

        _includePrivate = EditorGUILayout.Toggle("包含私有字段", _includePrivate);

        if (GUILayout.Button("导出属性", GUILayout.Height(24))) Dump();

        if (GUILayout.Button("复制导出内容"))
            EditorGUIUtility.systemCopyBuffer = _output;

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        _output = EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void Dump()
    {
        if (_target == null) { _output = "没有对象"; return; }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("======== 对象信息 ========");

        GameObject go = _target as GameObject;
        if (go == null)
        {
            Component comp = _target as Component;
            if (comp != null) go = comp.gameObject;
        }

        if (go != null)
        {
            sb.AppendLine($"名字: {go.name}");
            sb.AppendLine($"Layer: {LayerMask.LayerToName(go.layer)}");
            sb.AppendLine($"Tag: {go.tag}");
            sb.AppendLine($"Active: {go.activeSelf}");
            sb.AppendLine($"Prefab: {(PrefabUtility.IsPartOfPrefabAsset(go) ? "是" : "否")}");
            sb.AppendLine();

            sb.AppendLine("---- 层级结构 ----");
            DumpHierarchy(go.transform, 0, sb);
            sb.AppendLine();

            sb.AppendLine("---- 组件列表 ----");
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) { sb.AppendLine("  [Missing Script]"); continue; }
                sb.AppendLine($"  {c.GetType().Name}");
            }
            sb.AppendLine();

            sb.AppendLine("---- 组件详情 ----");
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                DumpComponent(c, sb);
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine($"资源: {_target.name}");
            sb.AppendLine($"类型: {_target.GetType().Name}");
            sb.AppendLine();
            DumpObject(_target, sb, 0);
        }

        _output = sb.ToString();
        Debug.Log("导出完成: " + _target.name);
    }

    void DumpHierarchy(Transform t, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}- {t.name}  Pos={t.localPosition}  Scale={t.localScale}");
        foreach (Transform child in t)
            DumpHierarchy(child, depth + 1, sb);
    }

    void DumpComponent(Component c, StringBuilder sb)
    {
        sb.AppendLine($"  [{c.GetType().Name}]");

        if (c is Renderer r)
        {
            sb.AppendLine($"    Material: {(r.sharedMaterial != null ? r.sharedMaterial.name : "无")}");
            if (r.sharedMaterial != null)
                sb.AppendLine($"    Shader: {r.sharedMaterial.shader.name}");
        }
        else if (c is TrailRenderer tr)
        {
            sb.AppendLine($"    Emitting: {tr.emitting}");
            sb.AppendLine($"    Time: {tr.time}");
            sb.AppendLine($"    WidthMultiplier: {tr.widthMultiplier}");
            sb.AppendLine($"    Material: {(tr.sharedMaterial != null ? tr.sharedMaterial.name : "无")}");
        }
        else if (c is ParticleSystem ps)
        {
            sb.AppendLine($"    PlayOnAwake: {ps.main.playOnAwake}");
            sb.AppendLine($"    Looping: {ps.main.loop}");
            sb.AppendLine($"    Duration: {ps.main.duration}");
            sb.AppendLine($"    StartLifetime: {ps.main.startLifetime.constant}");
            sb.AppendLine($"    StartSpeed: {ps.main.startSpeed.constant}");
            sb.AppendLine($"    StartSize: {ps.main.startSize.constant}");
            sb.AppendLine($"    Emission Rate: {ps.emission.rateOverTime.constant}");
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                sb.AppendLine($"    RenderMode: {psr.renderMode}");
                sb.AppendLine($"    Mesh: {(psr.mesh != null ? psr.mesh.name : "无")}");
                sb.AppendLine($"    Material: {(psr.sharedMaterial != null ? psr.sharedMaterial.name : "无")}");
                if (psr.sharedMaterial != null)
                    sb.AppendLine($"    Shader: {psr.sharedMaterial.shader.name}");
            }
        }
        else if (c is LineRenderer lr)
        {
            sb.AppendLine($"    PositionCount: {lr.positionCount}");
            sb.AppendLine($"    WidthMultiplier: {lr.widthMultiplier}");
            sb.AppendLine($"    StartColor: {lr.startColor}");
            sb.AppendLine($"    EndColor: {lr.endColor}");
            sb.AppendLine($"    Material: {(lr.sharedMaterial != null ? lr.sharedMaterial.name : "无")}");
            if (lr.sharedMaterial != null)
                sb.AppendLine($"    Shader: {lr.sharedMaterial.shader.name}");
        }

        var fields = c.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var f in fields)
        {
            if (!_includePrivate && (f.IsPrivate || f.IsFamily)) continue;

            object val = f.GetValue(c);
            string valStr = FormatValue(val);
            sb.AppendLine($"    {f.Name}: {valStr}");
        }
    }

    void DumpObject(Object obj, StringBuilder sb, int depth)
    {
        var fields = obj.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var f in fields)
        {
            if (!_includePrivate && (f.IsPrivate || f.IsFamily)) continue;
            object val = f.GetValue(obj);
            sb.AppendLine($"  {f.Name}: {FormatValue(val)}");
        }
    }

    string FormatValue(object val)
    {
        if (val == null) return "null";
        if (val is Object o) return $"{o.name} ({o.GetType().Name})";
        if (val is Vector3 v) return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        if (val is Color c) return $"({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
        return val.ToString();
    }
}