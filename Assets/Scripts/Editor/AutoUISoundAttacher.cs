using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class AutoUISoundAttacher
{
    [MenuItem("Tools/自动挂载 UI 音效")]
    public static void AttachToAllButtons()
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        int count = 0;

        foreach (var b in buttons)
        {
            if (b.GetComponent<UIButtonSound>() == null)
            {
                Undo.AddComponent<UIButtonSound>(b.gameObject);
                count++;
            }
        }

        Debug.Log($"[AutoUISound] 给 {count} 个按钮挂了 UIButtonSound");
    }
}