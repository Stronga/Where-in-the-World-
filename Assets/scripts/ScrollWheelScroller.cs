using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollWheelScroller : MonoBehaviour, IScrollHandler
{
    [Tooltip("How much to move per scroll tick (0-1 normalized).")]
    public float wheelSpeed = 0.1f;

    private ScrollRect _scrollRect;

    void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        _scrollRect.inertia = false; // optional: crisp stops
    }

    public void OnScroll(PointerEventData data)
    {
        // data.scrollDelta.y is positive when wheel scrolled up
        float delta = data.scrollDelta.y * wheelSpeed;
        _scrollRect.horizontalNormalizedPosition = 
            Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + delta);
    }
}
