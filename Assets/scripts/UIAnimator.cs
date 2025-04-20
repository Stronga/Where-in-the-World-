using UnityEngine;
using DG.Tweening;

public class UIAnimator : MonoBehaviour
{
    // Singleton instance for easy access (optional)
    public static UIAnimator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Pulse animation: Scales the object up and down repeatedly
    public void Pulse(GameObject target, float scaleAmount = 1.1f, float duration = 0.8f, Ease easeType = Ease.InOutSine)
    {
        if (target == null) return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.DOKill(); // Stop any existing animations
            rect.DOScale(scaleAmount, duration)
                .SetEase(easeType)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            Transform trans = target.transform;
            trans.DOKill();
            trans.DOScale(scaleAmount, duration)
                .SetEase(easeType)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    // Fade animation: Fades the object in or out
    public void Fade(GameObject target, float targetAlpha, float duration = 0.5f, Ease easeType = Ease.Linear)
    {
        if (target == null) return;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();

        canvasGroup.DOKill();
        canvasGroup.DOFade(targetAlpha, duration)
            .SetEase(easeType);
    }

    // Slide animation: Slides the object to a target position (for UI elements)
    public void Slide(RectTransform target, Vector2 targetPos, float duration = 0.5f, Ease easeType = Ease.OutQuad)
    {
        if (target == null) return;

        target.DOKill();
        target.DOAnchorPos(targetPos, duration)
            .SetEase(easeType);
    }

    // Bounce animation: Slides with a bounce effect
    public void Bounce(RectTransform target, Vector2 targetPos, float duration = 0.5f, Ease easeType = Ease.OutBounce)
    {
        if (target == null) return;

        target.DOKill();
        target.DOAnchorPos(targetPos, duration)
            .SetEase(easeType);
    }

    // Pop-in animation: Scales from 0 to 1 with an overshoot
    public void PopIn(GameObject target, float duration = 0.5f, Ease easeType = Ease.OutBack)
    {
        if (target == null) return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.DOKill();
            rect.localScale = Vector3.zero;
            rect.DOScale(1f, duration)
                .SetEase(easeType);
        }
        else
        {
            Transform trans = target.transform;
            trans.DOKill();
            trans.localScale = Vector3.zero;
            trans.DOScale(1f, duration)
                .SetEase(easeType);
        }
    }

    // Stop all animations on a target
    public void Stop(GameObject target)
    {
        if (target == null) return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.DOKill();
        }
        else
        {
            target.transform.DOKill();
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
    }
}