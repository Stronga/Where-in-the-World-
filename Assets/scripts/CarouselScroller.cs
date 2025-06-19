using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class CarouselScroller : MonoBehaviour
{
    [Tooltip("The ScrollRect that scrolls this content")]
    public ScrollRect scrollRect;

    [Tooltip("How far (in pixels) an item fades out / shrinks")]
    public float fadeDistance = 300f;

    [Tooltip("Min scale when far from center"), Range(0.1f, 1f)]
    public float minScale = 0.5f;

    [Tooltip("Max scale at center")]
    public float maxScale = 1f;

    RectTransform _content;
    RectTransform _viewport;

    void Awake()
    {
        _content = (RectTransform)transform;
        _viewport = scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>();
    }

    void Update()
    {
        // For each child in Content
        for (int i = 0; i < _content.childCount; i++)
        {
            RectTransform item = (RectTransform)_content.GetChild(i);

            // Compute item center in viewport space
            Vector3 worldPos = item.TransformPoint(item.rect.center);
            Vector2 viewportLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, 
                RectTransformUtility.WorldToScreenPoint(null, worldPos), 
                null, 
                out viewportLocal);

            // Distance from viewport center (x axis)
            float dist = Mathf.Abs(viewportLocal.x);
            float t = Mathf.Clamp01(dist / fadeDistance);

            // Fade out & shrink with distance
            float scale = Mathf.Lerp(maxScale, minScale, t);
            item.localScale = Vector3.one * scale;

            var cg = item.GetComponent<CanvasGroup>();
            if (cg == null) cg = item.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = Mathf.Lerp(1f, 0f, t);
        }
    }
}
