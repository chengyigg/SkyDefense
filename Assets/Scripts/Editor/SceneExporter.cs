using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

public static class SceneExporter
{
    [MenuItem("Tools/导出/当前场景结构")]
    public static void ExportScene()
    {
        string path = Path.Combine(Application.dataPath, "../Export/Scene_Export.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        StringBuilder sb = new StringBuilder();
        var scene = EditorSceneManager.GetActiveScene();
        sb.AppendLine($"场景: {scene.name}");
        sb.AppendLine($"根物体数量: {scene.rootCount}");
        sb.AppendLine("=========================================");

        foreach (var go in scene.GetRootGameObjects())
            ExportGameObject(go, 0, sb);

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"场景已导出: {path}");
        EditorUtility.RevealInFinder(path);
    }

    static void ExportGameObject(GameObject go, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}- {go.name}  [Layer: {LayerMask.LayerToName(go.layer)}]");
        sb.AppendLine($"{indent}  Pos: {go.transform.position}  Scale: {go.transform.localScale}");

        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp == null) continue;
            sb.AppendLine($"{indent}  Comp: {comp.GetType().Name}");
        }

        foreach (Transform child in go.transform)
            ExportGameObject(child.gameObject, depth + 1, sb);
    }
}