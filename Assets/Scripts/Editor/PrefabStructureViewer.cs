using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Text;

public class PrefabStructureViewer : EditorWindow
{
    private GameObject _target;
    private Vector2 _scroll;
    private string _output = "";

    [MenuItem("Tools/预制体结构查看器")]
    public static void Open()
    {
        GetWindow<PrefabStructureViewer>("预制体结构查看器");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("把 Hierarchy 或 Project 里的物体拖到下面的框", MessageType.Info);

        var dropRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
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
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    var go = obj as GameObject;
                    if (go != null) { _target = go; Dump(); break; }
                }
                evt.Use();
            }
        }

        EditorGUILayout.BeginHorizontal();
        _target = (GameObject)EditorGUILayout.ObjectField("目标", _target, typeof(GameObject), true);
        if (GUILayout.Button("刷新", GUILayout.Width(60))) Dump();
        if (GUILayout.Button("复制", GUILayout.Width(60)))
        {
            EditorGUIUtility.systemCopyBuffer = _output;
            Debug.Log("已复制到剪贴板");
        }
        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        _output = EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void Dump()
    {
        if (_target == null) { _output = "没有目标"; return; }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== {_target.name} ===");
        sb.AppendLine($"Active: {_target.activeSelf}");
        sb.AppendLine();
        DumpTransform(_target.transform, 0, sb);
        _output = sb.ToString();
    }

    void DumpTransform(Transform t, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);

        sb.AppendLine($"{indent}[{t.name}]");
        sb.AppendLine($"{indent}  Active:      {t.gameObject.activeSelf}");
        sb.AppendLine($"{indent}  Local Pos:   ({t.localPosition.x:F2}, {t.localPosition.y:F2}, {t.localPosition.z:F2})");
        sb.AppendLine($"{indent}  Local Scale: ({t.localScale.x:F2}, {t.localScale.y:F2}, {t.localScale.z:F2})");

        var rt = t as RectTransform;
        if (rt != null)
        {
            sb.AppendLine($"{indent}  Rect Size:   ({rt.sizeDelta.x:F1}, {rt.sizeDelta.y:F1})");
            sb.AppendLine($"{indent}  Anchors:     Min({rt.anchorMin.x:F1}, {rt.anchorMin.y:F1}) Max({rt.anchorMax.x:F1}, {rt.anchorMax.y:F1})");
        }

        var comps = t.GetComponents<Component>();
        string compNames = "";
        foreach (var c in comps)
        {
            if (c == null) { compNames += "[Missing] "; continue; }
            compNames += c.GetType().Name + " ";
        }
        sb.AppendLine($"{indent}  Components:  {compNames}");

        var img = t.GetComponent<Image>();
        if (img != null)
        {
            string spriteName = img.sprite == null ? "❌ NULL! (没选图片)" : img.sprite.name;
            sb.AppendLine($"{indent}  [Image 检查]");
            sb.AppendLine($"{indent}    Sprite:      {spriteName}");
            sb.AppendLine($"{indent}    Color:       RGBA({img.color.r:F1}, {img.color.g:F1}, {img.color.b:F1}, {img.color.a:F1})");
            sb.AppendLine($"{indent}    Type:        {img.type}");
            if (img.type == Image.Type.Filled)
            {
                sb.AppendLine($"{indent}    FillMethod:  {img.fillMethod}");
                sb.AppendLine($"{indent}    FillOrigin:  {img.fillOrigin}");
                sb.AppendLine($"{indent}    FillAmount:  {img.fillAmount:F2}");
            }
        }

        sb.AppendLine();

        foreach (Transform child in t)
            DumpTransform(child, depth + 1, sb);
    }
}