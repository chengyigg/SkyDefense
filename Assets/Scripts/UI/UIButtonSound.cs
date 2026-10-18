using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
{
    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => SoundManager.Instance?.Play("UIµã»÷"));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.Instance?.Play("UIÐüÍ£", 0.5f);
    }
}