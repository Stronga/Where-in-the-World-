using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollSnapOnWheel : MonoBehaviour, IScrollHandler
{
    [Tooltip("The ScrollRect driving the carousel.")]
    public ScrollRect scrollRect;

    private int _itemCount;
    private float _step;

    void Start()
    {
        // Cache how many items and the normalized step between them
        _itemCount = scrollRect.content.childCount;
        if (_itemCount > 1)
            _step = 1f / (_itemCount - 1);
        else
            _step = 1f;
        
        // Disable inertia for crisp snapping
        scrollRect.inertia = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_itemCount < 2) return;

        // Determine current index
        float curr = scrollRect.horizontalNormalizedPosition;
        int idx = Mathf.RoundToInt(curr / _step);

        // Wheel delta: positive => scroll up/forward => next item
        // negative => scroll down/backward => previous item
        int dir = eventData.scrollDelta.y > 0 ? -1 : 1;
        idx = Mathf.Clamp(idx + dir, 0, _itemCount - 1);

        // Snap immediately
        scrollRect.horizontalNormalizedPosition = idx * _step;
    }
}
