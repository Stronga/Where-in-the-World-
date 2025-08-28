using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels & Controls")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button startOverButton;
    [SerializeField] private RectTransform scorePanel;
    [SerializeField] private RectTransform landmarkPanel;
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Camera / Globe")]
    [SerializeField] private DragRotate dragRotate;
    [SerializeField] private Transform globe;
    [SerializeField] private float globeRadius = 1f;
    [SerializeField] private ParticleSystem explosionEffect;
    [SerializeField] private ParticleSystem successEffect; 

    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip backgroundMusic;
    private AudioSource audioSource;

    [Header("Landmarks")]
    [SerializeField] private Transform landmarkContainer;
    [SerializeField] private GameObject landmarkButtonPrefab;
    [SerializeField] private Transform landmarkButtonContainer;
    [SerializeField] private List<Landmark> landmarks;
    private List<Landmark> initialLandmarks;

    [Header("Managers")]
    [SerializeField] private ScoreManager scoreManager;

    // NEW: Reference to the scroll view's content panel
    [SerializeField] private RectTransform landmarkContent; 


    // CanvasGroups for UI toggles
    private CanvasGroup hintCanvasGroup;
    private CanvasGroup landmarkCanvasGroup;

    private enum GameState { Intro, Idle, Dragging, Feedback, Complete }
    private GameState state = GameState.Intro;

    // Drag tracking
    private GameObject currentLandmark;
    private Landmark currentLandmarkData;
    private GameObject currentLandmarkButton;
    private Vector2 dragStartPos;
    private bool hasMovedEnough;
    private float dragThreshold = 10f;

    // UI panel positions
    private Vector2 scorePanelStartPos;
    private Vector2 landmarkPanelStartPos;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Background music clip is not assigned in GameManager!");
        }

        if (scoreManager == null)
            Debug.LogError("GameManager: ScoreManager reference is missing.");

        scorePanelStartPos = scorePanel.anchoredPosition;
        landmarkPanelStartPos = landmarkPanel.anchoredPosition;
        scorePanel.anchoredPosition = new Vector2(-500, scorePanelStartPos.y);
        landmarkPanel.anchoredPosition = new Vector2(500, landmarkPanelStartPos.y);

        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Success panel is not assigned in GameManager!");
        }

        hintCanvasGroup = hintButton.GetComponent<CanvasGroup>() ?? hintButton.gameObject.AddComponent<CanvasGroup>();
        hintCanvasGroup.alpha = 0;
        landmarkCanvasGroup = landmarkPanel.GetComponent<CanvasGroup>() ?? landmarkPanel.gameObject.AddComponent<CanvasGroup>();
        landmarkCanvasGroup.blocksRaycasts = true;

        startButton.onClick.AddListener(OnStartClicked);
        hintButton.onClick.AddListener(ShowHint);

        if (startOverButton != null)
        {
            startOverButton.onClick.AddListener(StartOver);
        }
        else
        {
            Debug.LogWarning("Start Over button is not assigned in GameManager!");
        }

        if (landmarks != null)
        {
            initialLandmarks = new List<Landmark>(landmarks);
        }
        else
        {
            initialLandmarks = new List<Landmark>();
            Debug.LogWarning("Landmarks list is null in GameManager! Initial landmarks list will be empty.");
        }

        PopulateLandmarkPanel();
        dragRotate.ZoomToDefault();

        UIAnimator.Instance.Pulse(startButton.gameObject);
    }

    // ##################################################################
    // ## NEW: HAND GESTURE INTEGRATION
    // ##################################################################
    
    /// <summary>
    /// Public entry point for hand gesture input from a MediaPipe controller.
    /// </summary>
    /// <param name="handPosition">The world position of the hand.</param>
    /// <param name="gesture">A string representing the recognized gesture (e.g., "select", "grab", "place").</param>
    public void HandleHandInput(Vector3 handPosition, string gesture)
    {
        Debug.Log($"Gesture: {gesture} at position: {handPosition}");
        
        switch (gesture)
        {
            case "select":
                // Handles selecting the centered landmark and starting the drag.
                HandleHandSelection();
                break;
                
            case "grab":
                // If we aren't dragging, "grab" initiates a drag.
                // If we are already dragging, it updates the landmark position.
                HandleHandGrab(handPosition);
                break;
                
            case "place":
                // If a landmark is being dragged, this places it.
                HandleHandPlace();
                break;
        }
    }

    /// <summary>
    /// Selects the landmark currently centered in the scroll view and begins dragging.
    /// </summary>
  private void HandleHandSelection()
    {
        if (state != GameState.Idle) return;

        // Find the MzUGUIScrollCtrl component which manages the carousel
        MzTool.MzUGUIScrollCtrl scrollCtrl = FindObjectOfType<MzTool.MzUGUIScrollCtrl>();
        if (scrollCtrl == null)
        {
            Debug.LogError("MzUGUIScrollCtrl not found! Cannot determine which landmark is selected.");
            return;
        }

        // --- REPLACEMENT LOGIC ---
        // Instead of GetCurrentItem, we find the item closest to the carousel's center.
        GameObject centeredItem = null;
        float smallestDistance = float.MaxValue;
        Vector3 centerPosition = scrollCtrl.transform.position; // The center of the carousel UI element

        var scrollItems = scrollCtrl.GetItems(); // Assuming this method returns all item GameObjects
        if (scrollItems == null || scrollItems.Count == 0)
        {
             Debug.LogWarning("No items found in the scroll view.");
             return;
        }

        foreach (var item in scrollItems)
        {
            float distance = Vector3.Distance(item.transform.position, centerPosition);
            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                centeredItem = item.gameObject;
            }
        }
        // --- END REPLACEMENT LOGIC ---

        if (centeredItem == null)
        {
            Debug.LogWarning("Could not determine the centered item.");
            return;
        }
    
        LandmarkDragHandler handler = centeredItem.GetComponent<LandmarkDragHandler>();
        if (handler != null)
        {
            Debug.Log($"Hand selected landmark: {handler.landmark.name}");
            // Use the existing BeginDrag logic, passing the button we found.
            BeginDrag(handler.landmark, centeredItem.gameObject);
        }
    }
    
    /// <summary>
    /// Updates the drag position based on hand location, or initiates a drag if not already dragging.
    /// </summary>
    private void HandleHandGrab(Vector3 handPos)
    {
        if (state == GameState.Idle)
        {
            // If not dragging, a "grab" acts like a "select" to start the process.
            HandleHandSelection();
        }
        else if (state == GameState.Dragging)
        {
            // If already dragging, convert the 3D hand position to a 2D screen position
            // and update the landmark's location on the globe.
            Vector3 screenPos = Camera.main.WorldToScreenPoint(handPos);
            UpdateDrag(screenPos);
        }
    }
    
    /// <summary>
    /// Finalizes the placement of a landmark.
    /// </summary>
    private void HandleHandPlace()
    {
        if (state == GameState.Dragging)
        {
            Debug.Log("Placing landmark with hand gesture.");
            EndDrag();
        }
    }

    // ##################################################################
    // ## END: HAND GESTURE INTEGRATION
    // ##################################################################

    private void OnStartClicked()
    {
        UIAnimator.Instance.Stop(startButton.gameObject);
        introPanel.GetComponent<CanvasGroup>().DOFade(0, 1f).OnComplete(() => introPanel.SetActive(false));
        gamePanel.SetActive(true);
        UIAnimator.Instance.Bounce(scorePanel, scorePanelStartPos, 1f);
        UIAnimator.Instance.Bounce(landmarkPanel, landmarkPanelStartPos, 1f);
        UIAnimator.Instance.Fade(hintButton.gameObject, 1, 1f);
        UIAnimator.Instance.PopIn(hintButton.gameObject);
        StartGlobeIntroSequence();
        state = GameState.Idle;
    }

    private void StartGlobeIntroSequence()
    {
        dragRotate.SetRotationEnabled(false);
        dragRotate.ZoomToDefaultAnimated(1f);
        DOVirtual.DelayedCall(2.5f, () =>
        {
            dragRotate.SetRotationEnabled(true);
            dragRotate.StartAutoRotation();
        });
    }

    private void PopulateLandmarkPanel()
    {
        if (landmarks == null || landmarks.Count == 0)
        {
            Debug.LogWarning("GameManager: No landmarks assigned.");
            return;
        }

        foreach (Transform child in landmarkContent)
            Destroy(child.gameObject);

        int index = 0;
        foreach (var lm in landmarks)
        {
            if (lm.prefab == null)
            {
                Debug.LogError($"Landmark '{lm.name}' missing prefab.");
                continue;
            }

            var btnObj = Instantiate(landmarkButtonPrefab, landmarkContent);
            var img = btnObj.GetComponent<Image>();
            if (img != null && lm.icon != null)
            {
                img.sprite = lm.icon;
                img.preserveAspect = true;
                img.rectTransform.sizeDelta = lm.iconSize;
                var btnRect = btnObj.GetComponent<RectTransform>();
                btnRect.sizeDelta = lm.iconSize;
                btnRect.anchorMin = btnRect.anchorMax = btnRect.pivot = new Vector2(0.5f, 0.5f);
            }
            var handler = btnObj.AddComponent<LandmarkDragHandler>();
            handler.gameManager = this;
            handler.landmark = lm;
            UIAnimator.Instance.PopIn(btnObj, 0.5f, Ease.OutBack);
            btnObj.GetComponent<RectTransform>().DOScale(1f, 0.5f).SetDelay(index * 0.1f);
            index++;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(landmarkContent);
    }

    public void BeginDrag(Landmark lm, GameObject button)
    {
        if (state != GameState.Idle) return;

        state = GameState.Dragging;
        currentLandmarkData = lm;
        currentLandmarkButton = button; // This will be the button GameObject from the UI
        currentLandmark = Instantiate(lm.prefab, landmarkContainer);
        currentLandmark.transform.position = Vector3.one * 1000f; // Hide it initially
        
        hasMovedEnough = false;
        // For hand gestures, Input.mousePosition is irrelevant, but we set it for consistency.
        dragStartPos = Input.mousePosition; 

        dragRotate.SetRotationEnabled(false);
        landmarkCanvasGroup.blocksRaycasts = false;
        hintText.text = string.Empty;
    }

    public void UpdateDrag(Vector2 screenPos)
    {
        if (state != GameState.Dragging || currentLandmark == null) return;

        // With hand gestures, we can consider the drag to have "moved enough" immediately.
        if (!hasMovedEnough) hasMovedEnough = true; 

        if (hasMovedEnough)
        {
            var ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit) && (hit.transform == globe || hit.transform.IsChildOf(globe)))
            {
                var dir = (hit.point - globe.position).normalized;
                currentLandmark.transform.position = globe.position + dir * (globeRadius + 0.1f);
                currentLandmark.transform.up = dir;
            }
        }
    }

    public void EndDrag()
    {
        if (state != GameState.Dragging) return;
        state = GameState.Feedback;

        // If using hand gestures, we assume movement has occurred.
        // The check for hasMovedEnough is more for mouse to prevent accidental clicks.
        if (!hasMovedEnough && currentLandmarkButton != null) // Only check for mouse
        {
            ResetDrag();
            return;
        }

        float dist = Vector3.Distance(
            currentLandmark.transform.position,
            currentLandmarkData.correctPosition.position
        );
        bool success = dist <= currentLandmarkData.tolerance;

        if (success)
            HandleCorrectPlacement();
        else
            HandleWrongPlacement();

        ResetDrag();
        
        // MODIFIED: Check against the original landmark count, not the dynamic list
        if (scoreManager.GetScore() == initialLandmarks.Count)
        {
            state = GameState.Complete;
            ShowGameSuccess();
        }
        else
        {
            state = GameState.Idle;
        }
    }

    private void ShowGameSuccess()
    {
        if (successPanel != null)
        {
            successPanel.SetActive(true);
            UIAnimator.Instance.Fade(successPanel, 1, 1f, Ease.Linear);
            UIAnimator.Instance.PopIn(successPanel, 1f, Ease.OutBack);
            dragRotate.SetRotationEnabled(false);
            landmarkCanvasGroup.blocksRaycasts = false;
            hintCanvasGroup.blocksRaycasts = false;
            hintCanvasGroup.alpha = 0;
            hintText.text = string.Empty;
            if (startOverButton != null) startOverButton.interactable = true;
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        }
    }

    private void HandleCorrectPlacement()
    {
        scoreManager.AddScore(1);

        if (currentLandmarkData.correctPosition != null && currentLandmark != null)
        {
            Vector3 targetPos = currentLandmarkData.correctPosition.position;
            Vector3 normalDir = (targetPos - globe.position).normalized;
            currentLandmark.transform.DOMove(targetPos, 0.5f).SetEase(Ease.InOutQuad);
            currentLandmark.transform.DORotateQuaternion(Quaternion.FromToRotation(Vector3.up, normalDir), 0.5f).SetEase(Ease.InOutQuad);
            
            if (successEffect != null)
            {
                var fx = Instantiate(successEffect, targetPos, Quaternion.LookRotation(normalDir));
                Destroy(fx.gameObject, fx.main.duration);
            }
            if (successSound != null) audioSource.PlayOneShot(successSound);
        }

        // MODIFIED: This section is now robust enough for both mouse and hand gestures.
        GameObject buttonToDestroy = null;
        if (currentLandmarkButton != null)
        {
            // Case 1: Drag was initiated by mouse/touch, so we have a direct reference.
            buttonToDestroy = currentLandmarkButton;
        }
        else if (currentLandmarkData != null)
        {
            // Case 2: Drag was initiated by hand gesture, so we need to find the button.
            foreach (Transform child in landmarkContent)
            {
                var handler = child.GetComponent<LandmarkDragHandler>();
                if (handler != null && handler.landmark == currentLandmarkData)
                {
                    buttonToDestroy = child.gameObject;
                    break;
                }
            }
        }

        if (buttonToDestroy != null)
        {
            landmarks.Remove(currentLandmarkData);
            Destroy(buttonToDestroy);
            StartCoroutine(RefreshMzToolsScrollAfterDestroy());
        }
    }

    // NEW: Coroutine to refresh the MzTools carousel after a button is destroyed.
    private IEnumerator RefreshMzToolsScrollAfterDestroy()
    {
        // Wait one frame for the Destroy operation to complete.
        yield return null;
        
        MzTool.MzUGUIScrollCtrl scrollCtrl = FindObjectOfType<MzTool.MzUGUIScrollCtrl>();
        if (scrollCtrl != null)
        {
            // Tell the MzTools controller to reload its items from the hierarchy.
            scrollCtrl.LoadItems();
            Debug.Log("Refreshed MzTools scroll items.");
        }
    }

    private void HandleWrongPlacement()
    {
        var pos = currentLandmark.transform.position;
        Destroy(currentLandmark);
        if (explosionEffect != null)
        {
            var fx = Instantiate(explosionEffect, pos, Quaternion.identity);
            Destroy(fx.gameObject, fx.main.duration);
        }
    }

    private void ResetDrag()
    {
        dragRotate.SetRotationEnabled(true);
        landmarkCanvasGroup.blocksRaycasts = true;
        currentLandmark = null;
        currentLandmarkData = null;
        currentLandmarkButton = null;
    }

    private void ShowHint()
    {
        if (state == GameState.Dragging && currentLandmarkData != null)
        {
            hintText.text = currentLandmarkData.hint;
            CanvasGroup hintTextCanvasGroup = hintText.GetComponent<CanvasGroup>() ?? hintText.gameObject.AddComponent<CanvasGroup>();
            UIAnimator.Instance.Fade(hintText.gameObject, 1, 0.5f);
            UIAnimator.Instance.Slide(hintText.GetComponent<RectTransform>(), Vector2.zero, 0.5f);
        }
    }

    private void StartOver()
    {
        if (successPanel != null)
        {
            successPanel.GetComponent<CanvasGroup>().DOFade(0, 0.5f).OnComplete(() => successPanel.SetActive(false));
        }

        state = GameState.Idle;
        scoreManager.ResetScore();
        landmarks.Clear();
        landmarks.AddRange(initialLandmarks);

        foreach (Transform child in landmarkContainer) Destroy(child.gameObject);
        foreach (Transform child in landmarkButtonContainer) Destroy(child.gameObject);
        
        PopulateLandmarkPanel();

        dragRotate.SetRotationEnabled(true);
        landmarkCanvasGroup.blocksRaycasts = true;
        hintCanvasGroup.blocksRaycasts = true;
        hintCanvasGroup.alpha = 1;
        hintText.text = string.Empty;

        if (backgroundMusic != null && audioSource != null)
        {
            audioSource.Play();
        }
        dragRotate.ZoomToDefault();
    }
}