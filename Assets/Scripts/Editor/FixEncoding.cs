using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class RestoreEncoding
{
    [MenuItem("Tools/一键反向修复乱码 (救命用)")]
    public static void FixAll()
    {
        string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        int count = 0;
        foreach (var f in files)
        {
            // 注意：这次反过来。按 UTF-8 读，按 GBK 写
            string content = File.ReadAllText(f, Encoding.UTF8);
            File.WriteAllText(f, content, Encoding.GetEncoding("GBK"));
            count++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>反向修复了 {count} 个脚本！</color>");
    }
}