using UnityEngine;
using System.Collections.Generic;

public class EnemyMaterialDiagnostic : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F9)) return;

        var enemies = FindObjectsOfType<Enemy>();
        Debug.Log($"[Diagnostic] Enemy Count: {enemies.Length}");

        var matIds = new Dictionary<int, int>();
        var matNames = new Dictionary<int, string>();
        int totalRenderers = 0;

        foreach (var e in enemies)
        {
            foreach (var r in e.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.sharedMaterial == null) continue;
                totalRenderers++;
                int id = r.sharedMaterial.GetInstanceID();
                matIds[id] = matIds.TryGetValue(id, out var cnt) ? cnt + 1 : 1;
                matNames[id] = r.sharedMaterial.name;
            }
        }

        Debug.Log($"[Diagnostic] Total Renderers: {totalRenderers}");
        Debug.Log($"[Diagnostic] Unique Materials: {matIds.Count}");

        foreach (var kv in matIds)
            Debug.Log($"  Material [{kv.Key}] {matNames[kv.Key]}: referenced by {kv.Value} renderers");

        if (matIds.Count <= 5)
            Debug.Log("[Diagnostic] PASS: Materials are SHARED. MPB will improve performance.");
        else if (matIds.Count >= enemies.Length)
            Debug.Log("[Diagnostic] FAIL: Materials are INSTANCED. Fix prefab material reference first.");
        else
            Debug.Log("[Diagnostic] WARNING: Partially shared.");
    }
}