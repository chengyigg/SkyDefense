using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ScriptViewerWindow : EditorWindow
{
    private string folderPath = "Assets/Scripts";
    private string output = "";
    private Vector2 scrollPos;

    [MenuItem("Tools/脚本查看器")]
    public static void Open()
    {
        GetWindow<ScriptViewerWindow>("脚本查看器");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("把 Assets/Scripts 文件夹从 Project 窗口拖到这里，或手动输入路径", MessageType.Info);

        var dropRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "拖拽文件夹到这里");
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
                    if (Directory.Exists(p)) folderPath = p;
                }
                evt.Use();
            }
        }

        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField("文件夹", folderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string p = EditorUtility.OpenFolderPanel("选择脚本文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(p))
            {
                if (p.StartsWith(Application.dataPath))
                    folderPath = "Assets" + p.Substring(Application.dataPath.Length);
                else
                    folderPath = p;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("读取", GUILayout.Height(28)))
            ReadScripts();

        EditorGUILayout.LabelField($"内容长度：{output.Length} 字符");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        output = EditorGUILayout.TextArea(output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选并复制到剪贴板"))
        {
            EditorGUIUtility.systemCopyBuffer = output;
            Debug.Log("已复制！");
        }
        if (GUILayout.Button("清空"))
        {
            output = "";
        }
        EditorGUILayout.EndHorizontal();
    }

    void ReadScripts()
    {
        output = "";
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            output = "文件夹不存在：" + folderPath;
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        var sb = new StringBuilder();
        sb.AppendLine($"文件夹：{folderPath}");
        sb.AppendLine($"共 {files.Length} 个脚本\n");

        foreach (var f in files)
        {
            string rel = f.Replace("\\", "/");
            if (rel.StartsWith(Application.dataPath))
                rel = "Assets" + rel.Substring(Application.dataPath.Length);

            sb.AppendLine("=======================================");
            sb.AppendLine($"// {rel}");
            sb.AppendLine("=======================================");
            sb.AppendLine(File.ReadAllText(f));
            sb.AppendLine();
        }
        output = sb.ToString();
        Debug.Log($"读取完成，共 {files.Length} 个脚本");
    }
}