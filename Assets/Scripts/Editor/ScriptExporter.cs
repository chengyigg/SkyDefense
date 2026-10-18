using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class ScriptExporter
{
    [MenuItem("Tools/导出/所有脚本")]
    public static void ExportScripts()
    {
        string[] guids = AssetDatabase.FindAssets("t:MonoScript");
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"共 {guids.Length} 个脚本\n");

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) continue;

            sb.AppendLine("=========================================");
            sb.AppendLine($"// 文件: {path}");
            sb.AppendLine("=========================================");
            sb.AppendLine(File.ReadAllText(fullPath));
            sb.AppendLine();
        }

        string outPath = Path.Combine(Application.dataPath, "../Export/AllScripts.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log($"脚本已导出: {outPath}");
        EditorUtility.RevealInFinder(outPath);
    }
}