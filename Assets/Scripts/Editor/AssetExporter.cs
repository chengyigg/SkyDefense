using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class AssetExporter : EditorWindow
{
    private string folderPath = "Assets";
    private List<string> results = new List<string>();
    private Vector2 scrollPos;

    private List<string> excludeFolders = new List<string>()
    {
        "Assets/TextMesh Pro",
        "Assets/TutorialInfo",
        "Assets/Settings",
    };
    private string newExcludeInput = "";

    [MenuItem("Tools/资源列表查看器")]
    public static void Open()
    {
        GetWindow<AssetExporter>("资源列表查看器");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("扫描整个 Assets，或者指定文件夹", MessageType.Info);

        var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, string.IsNullOrEmpty(folderPath) ? "拖文件夹到这里" : folderPath);
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
                    string p = AssetDatabase.GetAssetPath(obj);
                    if (Directory.Exists(p)) { folderPath = p; results.Clear(); }
                }
                evt.Use();
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("浏览选择"))
        {
            string p = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(p))
            {
                if (p.StartsWith(Application.dataPath))
                    folderPath = "Assets" + p.Substring(Application.dataPath.Length);
                else folderPath = p;
                results.Clear();
            }
        }
        if (GUILayout.Button("重置为 Assets")) { folderPath = "Assets"; results.Clear(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("忽略的文件夹（填相对路径）", EditorStyles.boldLabel);

        int removeIndex = -1;
        for (int i = 0; i < excludeFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            excludeFolders[i] = EditorGUILayout.TextField(excludeFolders[i]);
            if (GUILayout.Button("X", GUILayout.Width(24))) removeIndex = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0) excludeFolders.RemoveAt(removeIndex);

        EditorGUILayout.BeginHorizontal();
        newExcludeInput = EditorGUILayout.TextField("添加忽略路径", newExcludeInput);
        if (GUILayout.Button("+", GUILayout.Width(24)) && !string.IsNullOrEmpty(newExcludeInput))
        {
            excludeFolders.Add(newExcludeInput);
            newExcludeInput = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("扫描", GUILayout.Height(30))) Scan();

        EditorGUILayout.LabelField($"共 {results.Count} 个文件");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        var sb = new StringBuilder();
        foreach (var r in results) sb.AppendLine(r);
        EditorGUILayout.TextArea(sb.ToString(), GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("全选复制到剪贴板"))
        {
            EditorGUIUtility.systemCopyBuffer = string.Join("\n", results);
            Debug.Log("已复制 " + results.Count + " 个");
        }
    }

    private void Scan()
    {
        results.Clear();

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            results.Add("文件夹不存在：" + folderPath);
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        foreach (var f in files)
        {
            string ext = Path.GetExtension(f).ToLower();
            if (ext == ".meta") continue;

            string rel = f.Replace("\\", "/");
            if (rel.StartsWith(Application.dataPath))
                rel = "Assets" + rel.Substring(Application.dataPath.Length);

            bool skip = false;
            foreach (var ex in excludeFolders)
            {
                if (string.IsNullOrEmpty(ex)) continue;
                string exNorm = ex.Replace("\\", "/").TrimEnd('/');
                if (rel == exNorm || rel.StartsWith(exNorm + "/"))
                {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;

            results.Add(rel);
        }
        results.Sort();
        Debug.Log($"扫描完成，共 {results.Count} 个文件");
    }
}