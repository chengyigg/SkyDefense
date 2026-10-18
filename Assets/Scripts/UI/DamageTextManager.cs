using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;

    [Header("挂载与预制体")]
    public GameObject damageTextPrefab;
    public Transform textRoot; // 【新增】指定飘字存放的父节点

    private Queue<GameObject> _pool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;

        // 如果没在面板里指定，就找一下场景里的 Canvas，临时建一个 Root
        if (textRoot == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject rootObj = new GameObject("DamageTextRoot");
                rootObj.transform.SetParent(canvas.transform, false);
                textRoot = rootObj.transform;
            }
            else
            {
                textRoot = transform;
            }
        }
    }

    public void ShowDamage(Vector3 worldPos, int amount)
    {
        if (damageTextPrefab == null || textRoot == null) return;

        GameObject go;
        if (_pool.Count > 0)
        {
            go = _pool.Dequeue();
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(damageTextPrefab, textRoot); // 【修改】强制作为 Root 的子节点
            if (go.GetComponent<DamageTextItem>() == null)
                go.AddComponent<DamageTextItem>();
        }

        DamageTextItem item = go.GetComponent<DamageTextItem>();
        item.Setup(worldPos, amount, this);
    }

    public void ReturnToPool(GameObject go)
    {
        go.SetActive(false);
        _pool.Enqueue(go);
    }
}