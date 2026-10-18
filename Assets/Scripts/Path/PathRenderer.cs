using UnityEngine;
using System.Collections.Generic;

public class PathRenderer : MonoBehaviour
{
    [Header("?????")]
    public GameObject roadPrefab;
    public GameObject startPrefab;
    public GameObject endPrefab;

    [Header("???")]
    public float roadWidth = 5f;
    public float roadHeight = 0.1f;

    [Header("Waypoints?????????????????????")]
    public Transform[] waypoints;

    [ContextMenu("????¡¤??")]
    public void GenerateRoad()
    {
        // 1. ?????? Waypoints
        if (waypoints == null || waypoints.Length < 2)
        {
            var list = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Waypoint")) list.Add(child);
            }
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            waypoints = list.ToArray();
        }

        if (waypoints.Length < 2)
        {
            Debug.LogError("Waypoint ??????????????? 2 ????");
            return;
        }

        // 2. ?????????????¡¤
        Transform old = transform.Find("GeneratedRoad");
        if (old != null) DestroyImmediate(old.gameObject);

        // 3. ?????????
        GameObject root = new GameObject("GeneratedRoad");
        root.transform.SetParent(transform);
        root.transform.position = Vector3.zero;

        // 4. ???
        if (startPrefab != null)
        {
            GameObject start = Instantiate(startPrefab, waypoints[0].position, Quaternion.identity, root.transform);
            start.name = "Start";
        }

        // 5. ?§Þ?¡¤?¹×????????
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 a = waypoints[i].position;
            Vector3 b = waypoints[i + 1].position;
            Vector3 dir = (b - a).normalized;
            float dist = Vector3.Distance(a, b);

            // ?? roadWidth ??????????????
            int stepCount = Mathf.RoundToInt(dist / roadWidth);
            for (int s = 0; s < stepCount; s++)
            {
                Vector3 pos = a + dir * (roadWidth * s); // ?? a ?????ÿ???????
                Quaternion rot = Quaternion.LookRotation(dir);

                GameObject road = Instantiate(roadPrefab, pos, rot, root.transform);
                road.name = "Road_" + i + "_" + s;
                // ?????????????§³?????????????
            }
        }

        // 6. ???
        if (endPrefab != null)
        {
            GameObject end = Instantiate(endPrefab, waypoints[waypoints.Length - 1].position, Quaternion.identity, root.transform);
            end.name = "End";
        }

        Debug.Log("¡¤???????????? " + (waypoints.Length - 1) + " ?¦²?????????");
    }
}