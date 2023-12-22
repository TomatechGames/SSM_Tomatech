using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(110)]
public class PinToWorldTarget : MonoBehaviour
{
    [SerializeField]
    Transform target;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public Vector2 screenOffset;
    [SerializeField]
    bool clampToParent;
    [SerializeField]
    RectOffset clampedPadding;

    Canvas parentCanvas;
    RectTransform rectTransform;
    RectTransform GetRectParent() => (RectTransform)transform.parent;

    private void Start()
    {
        rectTransform = (RectTransform)transform;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void LateUpdate()
    {
        if (!target)
            return;
        transform.position = target.position;
        rectTransform.anchoredPosition += screenOffset;

        if(clampToParent)
        {
            var thisRect = rectTransform.rect;
            var parentRect = GetRectParent().rect;
            Vector2 offset = new();

            if (thisRect.xMin < parentRect.xMin)
                offset.x += parentRect.xMin - thisRect.xMin;
            if (thisRect.xMax > parentRect.xMax)
                offset.x += parentRect.xMax - thisRect.xMax;

            if (thisRect.yMin < parentRect.yMin)
                offset.y += parentRect.yMin - thisRect.yMin;
            if (thisRect.yMax > parentRect.yMax)
                offset.y += parentRect.yMax - thisRect.yMax;
            rectTransform.anchoredPosition += offset;
        }
    }
}
