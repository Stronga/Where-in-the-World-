using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class SnapScrollRect : MonoBehaviour, IEndDragHandler
{
    [Tooltip("The Content RectTransform containing one button per landmark.")]
    public RectTransform content;

    [Tooltip("How fast to lerp into place.")]
    public float snapSpeed = 10f;

    private ScrollRect _scrollRect;
    private int _itemCount;
    private float[] _snapPositions;

    void Start()
    {
        _scrollRect = GetComponent<ScrollRect>();
        _itemCount  = content.childCount;

        if (_itemCount == 0)
        {
            Debug.LogWarning("SnapScrollRect: content has no children to snap to.");
            return;
        }

        // Build our normalized snap positions array
        _snapPositions = new float[_itemCount];

        if (_itemCount == 1)
        {
            // Only one item — always at start (0)
            _snapPositions[0] = 0f;
        }
        else
        {
            // Distribute evenly from 0 to 1
            float step = 1f / (_itemCount - 1);
            for (int i = 0; i < _itemCount; i++)
                _snapPositions[i] = step * i;
        }

        // Optional: disable inertia for crisp snapping
        _scrollRect.inertia = false;
    }

    public void OnEndDrag(PointerEventData data)
    {
        // Only start snapping if we have something to snap to
        if (_itemCount > 0)
            StartCoroutine(SnapToClosest());
    }

    private IEnumerator SnapToClosest()
    {
        // Find nearest snap position
        float curr = _scrollRect.horizontalNormalizedPosition;
        float nearest = _snapPositions[0];
        float bestDist = Mathf.Abs(curr - nearest);

        for (int i = 1; i < _snapPositions.Length; i++)
        {
            float d = Mathf.Abs(curr - _snapPositions[i]);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = _snapPositions[i];
            }
        }

        // Lerp until close enough
        while (Mathf.Abs(_scrollRect.horizontalNormalizedPosition - nearest) > 0.001f)
        {
            _scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(_scrollRect.horizontalNormalizedPosition, nearest, Time.deltaTime * snapSpeed);
            yield return null;
        }

        // Snap exactly
        _scrollRect.horizontalNormalizedPosition = nearest;
    }
}
