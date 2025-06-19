using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CarouselNameUpdater : MonoBehaviour, IScrollHandler, IEndDragHandler
{
    [Tooltip("Automatic: finds ScrollRect on parent Scroll View")]
    public ScrollRect scrollRect;

    [Tooltip("Text field to display the centered landmark’s name")]
    public TextMeshProUGUI nameLabel;

    private RectTransform _content;
    private RectTransform _viewport;

    void Awake()
    {
        // Auto‑assign scrollRect if not set
        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>();

        if (scrollRect == null)
        {
            Debug.LogError("CarouselNameUpdater: No ScrollRect found on parent hierarchy.");
            enabled = false;
            return;
        }

        if (scrollRect.content == null)
        {
            Debug.LogError("CarouselNameUpdater: ScrollRect.content is unassigned in the inspector.");
            enabled = false;
            return;
        }

        _content  = scrollRect.content;
        _viewport = scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>();

        // Listen for value‑changed so drag/SnapScroll events update name
        scrollRect.onValueChanged.AddListener(_ => UpdateName());
        // Initial name set
        UpdateName();
    }

    public void OnScroll(PointerEventData eventData)
    {
        UpdateName();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UpdateName();
    }

    public void UpdateName()
    {
        int count = _content.childCount;
        if (count == 0)
        {
            nameLabel.text = "";
            return;
        }

        float bestDist = float.MaxValue;
        int   bestIdx  = 0;

        for (int i = 0; i < count; i++)
        {
            RectTransform item = (RectTransform)_content.GetChild(i);
            Vector3 worldCenter = item.TransformPoint(item.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, screenPoint, null, out Vector2 localPoint);

            float dist = Mathf.Abs(localPoint.x);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx  = i;
            }
        }

        var handler = _content
            .GetChild(bestIdx)
            .GetComponent<LandmarkDragHandler>();

        nameLabel.text = (handler != null && handler.landmark != null)
            ? handler.landmark.name
            : "";
    }
}
