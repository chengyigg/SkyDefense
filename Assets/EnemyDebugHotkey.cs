using UnityEngine;
using System.Text;

public class EnemyDebugHotkey : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var all = FindObjectsOfType<Enemy>();
            var sb = new StringBuilder();
            sb.AppendLine($"======= ?????? {all.Length} ?????? =======");

            foreach (var e in all)
            {
                if (e == null) continue;

                var r = e.GetComponentInChildren<Renderer>();
                string rStr = (r == null) ? "??Renderer"
                    : $"R.enabled={r.enabled}, visible={r.isVisible}, mat={r.sharedMaterial?.name}";

                sb.AppendLine($"[{e.name}] HP={e.maxHealth}, Dead={e.IsDead}, " +
                              $"Pos={e.transform.position}, Active={e.gameObject.activeInHierarchy}, {rStr}");
            }

            Debug.Log(sb.ToString());
        }
    }
}