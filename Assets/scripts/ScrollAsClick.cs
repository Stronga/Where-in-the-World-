using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Invoke different buttons depending on scroll direction:
/// scroll up invokes scrollUpButton, scroll down invokes scrollDownButton.
/// </summary>
public class ScrollAsClick : MonoBehaviour, IScrollHandler
{
    [Tooltip("Button to invoke when the user scrolls up")]
    public Button scrollUpButton;

    [Tooltip("Button to invoke when the user scrolls down")]
    public Button scrollDownButton;

    public void OnScroll(PointerEventData eventData)
    {
        // Positive delta.y = wheel up; negative = wheel down
        if (eventData.scrollDelta.y > 0)
        {
            scrollUpButton?.onClick.Invoke();
        }
        else if (eventData.scrollDelta.y < 0)
        {
            scrollDownButton?.onClick.Invoke();
        }
    }
}
