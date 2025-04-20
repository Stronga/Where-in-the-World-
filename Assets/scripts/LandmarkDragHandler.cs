using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles UI drag events on landmark buttons and forwards them to the GameManager.
/// Passes the button GameObject so the GameManager can hide/destroy it on placement.
/// </summary>
public class LandmarkDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Landmark landmark;
    [HideInInspector] public GameManager gameManager;

    /// <summary>
    /// Called when drag begins; informs GameManager to start drag for this landmark and button.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        gameManager.BeginDrag(landmark, gameObject);
    }

    /// <summary>
    /// Called every frame during drag; passes screen position to GameManager.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        gameManager.UpdateDrag(eventData.position);
    }

    /// <summary>
    /// Called when drag ends; informs GameManager to finalize placement.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        gameManager.EndDrag();
    }
}
