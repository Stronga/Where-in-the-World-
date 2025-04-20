using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels & Controls")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private Button startButton;
    [SerializeField] private RectTransform scorePanel;
    [SerializeField] private RectTransform landmarkPanel;
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Camera / Globe")]
    [SerializeField] private DragRotate dragRotate;
    [SerializeField] private Transform globe;
    [SerializeField] private float globeRadius = 1f;
    [SerializeField] private ParticleSystem explosionEffect;
    [SerializeField] private ParticleSystem successEffect; // New ParticleSystem for correct placement

    [SerializeField] private AudioClip successSound;
    private AudioSource audioSource;

    [Header("Landmarks")]
    [SerializeField] private Transform landmarkContainer;
    [SerializeField] private GameObject landmarkButtonPrefab;
    [SerializeField] private Transform landmarkButtonContainer;
    [SerializeField] private List<Landmark> landmarks;

    [Header("Managers")]
    [SerializeField] private ScoreManager scoreManager;

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

        // ... (existing Start code)
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (scoreManager == null)
            Debug.LogError("GameManager: ScoreManager reference is missing.");

        // Cache and hide panels
        scorePanelStartPos = scorePanel.anchoredPosition;
        landmarkPanelStartPos = landmarkPanel.anchoredPosition;
        scorePanel.anchoredPosition = new Vector2(-500, scorePanelStartPos.y);
        landmarkPanel.anchoredPosition = new Vector2(500, landmarkPanelStartPos.y);

        // Ensure CanvasGroups
        hintCanvasGroup = hintButton.GetComponent<CanvasGroup>() ?? hintButton.gameObject.AddComponent<CanvasGroup>();
        hintCanvasGroup.alpha = 0;
        landmarkCanvasGroup = landmarkPanel.GetComponent<CanvasGroup>() ?? landmarkPanel.gameObject.AddComponent<CanvasGroup>();
        landmarkCanvasGroup.blocksRaycasts = true;

        // Wire UI
        startButton.onClick.AddListener(OnStartClicked);
        hintButton.onClick.AddListener(ShowHint);

        PopulateLandmarkPanel();
        dragRotate.ZoomToDefault();

        // Start the pulsing animation for the Start button
        UIAnimator.Instance.Pulse(startButton.gameObject);
    }

    private void OnStartClicked()
    {
        // Stop the pulsing animation
        UIAnimator.Instance.Stop(startButton.gameObject);

        // Fade out intro
        UIAnimator.Instance.Fade(introPanel, 0, 1f, Ease.Linear);
        introPanel.GetComponent<CanvasGroup>().DOFade(0, 1f).OnComplete(() => introPanel.SetActive(false));

        // Show game UI
        gamePanel.SetActive(true);
        UIAnimator.Instance.Bounce(scorePanel, scorePanelStartPos, 1f);
        UIAnimator.Instance.Bounce(landmarkPanel, landmarkPanelStartPos, 1f);
        UIAnimator.Instance.Fade(hintButton.gameObject, 1, 1f);
        UIAnimator.Instance.PopIn(hintButton.gameObject);

        state = GameState.Idle;
    }

private void PopulateLandmarkPanel()
{
    if (landmarks == null || landmarks.Count == 0)
    {
        Debug.LogWarning("GameManager: No landmarks assigned.");
        return;
    }

    int index = 0;
    foreach (var lm in landmarks)
    {
        if (lm.prefab == null)
        {
            Debug.LogError($"Landmark '{lm.name}' missing prefab.");
            continue;
        }

        var btnObj = Instantiate(landmarkButtonPrefab, landmarkButtonContainer);
        var img = btnObj.GetComponent<Image>();
        if (img != null && lm.icon != null)
        {
            img.sprite = lm.icon;
            RectTransform imgRect = img.GetComponent<RectTransform>();
            imgRect.sizeDelta = lm.iconSize;
            img.preserveAspect = true;

            // Set the button's RectTransform to match the icon size
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = lm.iconSize;

            // Center the button in the layout
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
        }

        var handler = btnObj.AddComponent<LandmarkDragHandler>();
        handler.gameManager = this;
        handler.landmark = lm;

        UIAnimator.Instance.PopIn(btnObj, 0.5f, Ease.OutBack);
        btnObj.GetComponent<RectTransform>().DOScale(1f, 0.5f).SetDelay(index * 0.1f);

        index++;
    }

    LayoutRebuilder.ForceRebuildLayoutImmediate(landmarkButtonContainer.GetComponent<RectTransform>());
}

   public void BeginDrag(Landmark lm, GameObject button)
{
    if (state != GameState.Idle) return;

    currentLandmarkData = lm;
    currentLandmarkButton = button;
    currentLandmark = Instantiate(lm.prefab, landmarkContainer);
    currentLandmark.transform.position = Vector3.one * 1000f;

    dragStartPos = Input.mousePosition;
    hasMovedEnough = false;

    dragRotate.SetRotationEnabled(false);
    landmarkCanvasGroup.blocksRaycasts = false;
    hintText.text = string.Empty;

    // Add selection effect on the landmark button
    UIAnimator.Instance.Pulse(currentLandmarkButton, 1.5f, 0.5f, Ease.InOutSine);

    state = GameState.Dragging;
}

    public void UpdateDrag(Vector2 screenPos)
    {
        if (state != GameState.Dragging || currentLandmark == null) return;

        if (!hasMovedEnough && Vector2.Distance(screenPos, dragStartPos) > dragThreshold)
            hasMovedEnough = true;

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

        if (!hasMovedEnough)
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
        state = scoreManager.GetScore() >= landmarks.Count ? GameState.Complete : GameState.Idle;
    }

private void HandleCorrectPlacement()
{
    scoreManager.AddScore(1);

    if (currentLandmarkData.correctPosition != null && currentLandmark != null)
    {
        Debug.Log($"Snapping landmark to correctPosition: {currentLandmarkData.correctPosition.position}, Current position: {currentLandmark.transform.position}");

        Vector3 targetPos = currentLandmarkData.correctPosition.position;
        Vector3 normalDir = (targetPos - globe.position).normalized;

        GameObject landmarkToSnap = currentLandmark;

        landmarkToSnap.transform.DOMove(targetPos, 0.5f).SetEase(Ease.InOutQuad);
        landmarkToSnap.transform.DORotateQuaternion(Quaternion.FromToRotation(Vector3.up, normalDir), 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            Debug.Log($"OnComplete callback executed for HandleCorrectPlacement. landmarkToSnap: {(landmarkToSnap != null ? "Valid" : "Null")}");
            if (landmarkToSnap.TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (landmarkToSnap.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

            if (successEffect != null)
            {
                var fx = Instantiate(successEffect, targetPos, Quaternion.identity);
                fx.transform.localScale = Vector3.one * 0.04f;
                fx.transform.rotation = Quaternion.FromToRotation(Vector3.up, normalDir);
                Debug.Log($"Instantiated successEffect at position: {fx.transform.position}, scale: {fx.transform.localScale}, parented to: None");
                fx.transform.SetParent(landmarkToSnap.transform, true);
                fx.transform.localPosition = Vector3.zero;
                fx.Play();
                Destroy(fx.gameObject, fx.main.duration);

                if (successSound != null && audioSource != null)
                {
                    Debug.Log("Playing success sound");
                    audioSource.PlayOneShot(successSound);
                }
                else
                {
                    Debug.LogWarning("Success sound or audioSource is missing");
                }
            }
            else
            {
                Debug.LogWarning("SuccessEffect is not assigned in GameManager");
            }
        });
    }
    else
    {
        Debug.LogWarning($"HandleCorrectPlacement: correctPosition or currentLandmark not set for '{currentLandmarkData?.name}'");
    }

    if (currentLandmarkButton != null)
    {
        UIAnimator.Instance.Stop(currentLandmarkButton);
        Destroy(currentLandmarkButton);
    }
}

    private void HandleWrongPlacement()
    {
        var pos = currentLandmark.transform.position;
        Destroy(currentLandmark);
        if (explosionEffect != null)
        {
            var fx = Instantiate(explosionEffect, pos, Quaternion.identity);
            fx.Play();
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

            // Ensure CanvasGroup on hintText
            CanvasGroup hintTextCanvasGroup = hintText.GetComponent<CanvasGroup>();
            if (hintTextCanvasGroup == null)
                hintTextCanvasGroup = hintText.gameObject.AddComponent<CanvasGroup>();

            // Animate the hint text with fade and slide
            UIAnimator.Instance.Fade(hintText.gameObject, 1, 0.5f);
            UIAnimator.Instance.Slide(hintText.GetComponent<RectTransform>(), Vector2.zero, 0.5f);
        }
    }
}