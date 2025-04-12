using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private Button startButton;
    [SerializeField] private RectTransform scorePanel;
    [SerializeField] private RectTransform landmarkPanel;
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private DragRotate dragRotate;
    [SerializeField] private Transform globe;
    [SerializeField] private float globeRadius = 1f;
    [SerializeField] private ParticleSystem explosionEffect;
    [SerializeField] private Transform landmarkContainer;
    [SerializeField] private GameObject landmarkButtonPrefab;
    [SerializeField] private Transform landmarkButtonContainer;
    [SerializeField] private List<Landmark> landmarks;

    private int score = 0;
    private Vector2 scorePanelStartPos;
    private Vector2 landmarkPanelStartPos;
    private GameObject currentLandmark;
    private Landmark currentLandmarkData;
    private bool isDragging = false;
    private bool hasStartedDragging = false;
    private Vector2 dragStartPosition;
    private bool hasMovedEnough = false;
    private float dragThreshold = 10f;

    void Start()
    {
        introPanel.SetActive(true);
        gamePanel.SetActive(false);

        scorePanelStartPos = scorePanel.anchoredPosition;
        landmarkPanelStartPos = landmarkPanel.anchoredPosition;

        scorePanel.anchoredPosition = new Vector2(-500, scorePanelStartPos.y);
        landmarkPanel.anchoredPosition = new Vector2(500, landmarkPanelStartPos.y);

        CanvasGroup hintCanvasGroup = hintButton.GetComponent<CanvasGroup>();
        if (hintCanvasGroup == null)
        {
            hintCanvasGroup = hintButton.gameObject.AddComponent<CanvasGroup>();
        }
        hintCanvasGroup.alpha = 0;

        startButton.onClick.AddListener(StartGame);
        hintButton.onClick.AddListener(ShowHint);

        UpdateScoreText();
        hintText.text = "";

        PopulateLandmarkPanel();
    }

    void Update()
    {
        if (isDragging && currentLandmark != null)
        {
            Debug.Log("Dragging landmark: " + currentLandmark.name);

            Vector2 currentMousePos = Input.mousePosition;
            float distanceMoved = Vector2.Distance(currentMousePos, dragStartPosition);
            if (distanceMoved > dragThreshold && !hasStartedDragging)
            {
                hasStartedDragging = true;
                hasMovedEnough = true;
                Debug.Log("Dragging started, mouse moved enough: " + distanceMoved);
            }

            if (hasStartedDragging)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    Debug.Log("Raycast hit: " + hit.transform.name + " at position: " + hit.point);
                    if (hit.transform == globe || hit.transform.IsChildOf(globe))
                    {
                        Vector3 hitPoint = hit.point;
                        Vector3 direction = (hitPoint - globe.position).normalized;
                        currentLandmark.transform.position = globe.position + direction * (globeRadius + 0.1f);
                        currentLandmark.transform.up = direction;
                        Debug.Log("Landmark position updated to: " + currentLandmark.transform.position + ", Scale: " + currentLandmark.transform.localScale);
                    }
                    else
                    {
                        Debug.LogWarning("Raycast hit something other than the globe: " + hit.transform.name);
                    }
                }
                else
                {
                    Debug.LogWarning("Raycast did not hit anything! Mouse position: " + Input.mousePosition);
                }
            }

            if (Input.GetMouseButtonUp(0) && hasMovedEnough)
            {
                Debug.Log("Mouse button released, placing landmark");
                PlaceLandmark();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Debug.Log("Mouse button released, but not moved enough, canceling drag");
                CancelDrag();
            }
        }
    }

    void StartGame()
    {
        CanvasGroup introCanvasGroup = introPanel.GetComponent<CanvasGroup>();
        if (introCanvasGroup == null) introCanvasGroup = introPanel.AddComponent<CanvasGroup>();
        introCanvasGroup.DOFade(0, 1f).OnComplete(() => introPanel.SetActive(false));

        gamePanel.SetActive(true);
        scorePanel.DOAnchorPos(scorePanelStartPos, 1f).SetEase(Ease.OutQuad);
        landmarkPanel.DOAnchorPos(landmarkPanelStartPos, 1f).SetEase(Ease.OutQuad);
        hintButton.GetComponent<CanvasGroup>().DOFade(1, 1f);
        dragRotate.ZoomToDefault();
    }

    void PopulateLandmarkPanel()
    {
        Debug.Log("Populating landmark panel with " + landmarks.Count + " landmarks");
        foreach (Landmark landmark in landmarks)
        {
            if (landmark.prefab == null)
            {
                Debug.LogError("Landmark prefab is null for: " + landmark.name);
                continue;
            }

            GameObject buttonObj = Instantiate(landmarkButtonPrefab, landmarkButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            if (landmark.icon != null)
            {
                button.GetComponent<Image>().sprite = landmark.icon;
            }
            else
            {
                Debug.LogWarning("Icon not assigned for landmark: " + landmark.name);
            }
            button.GetComponentInChildren<TextMeshProUGUI>().text = landmark.name;
            button.onClick.AddListener(() => StartDraggingLandmark(landmark));
            Debug.Log("Created button for landmark: " + landmark.name);
        }
    }

    void StartDraggingLandmark(Landmark landmark)
    {
        if (currentLandmark != null)
        {
            Debug.LogWarning("Already dragging a landmark, ignoring new drag request");
            return;
        }

        Debug.Log("Starting to drag landmark: " + landmark.name);
        currentLandmarkData = landmark;
        currentLandmark = Instantiate(landmark.prefab, landmarkContainer);
        if (currentLandmark == null)
        {
            Debug.LogError("Failed to instantiate landmark prefab for: " + landmark.name);
            return;
        }
        Debug.Log("Landmark instantiated at position: " + currentLandmark.transform.position + ", Scale: " + currentLandmark.transform.localScale);
        currentLandmark.transform.position = new Vector3(1000, 1000, 1000);
        Debug.Log("Landmark moved to initial position: " + currentLandmark.transform.position);
        isDragging = true;
        dragStartPosition = Input.mousePosition;
        hasStartedDragging = false;
        hasMovedEnough = false;
        dragRotate.SetRotationEnabled(false);
        CanvasGroup panelCanvasGroup = landmarkPanel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null) panelCanvasGroup = landmarkPanel.gameObject.AddComponent<CanvasGroup>();
        panelCanvasGroup.blocksRaycasts = false;
        hintText.text = "";
    }

    void PlaceLandmark()
    {
        Debug.Log("Placing landmark: " + currentLandmarkData.name);
        isDragging = false;
        dragRotate.SetRotationEnabled(true);
        landmarkPanel.GetComponent<CanvasGroup>().blocksRaycasts = true;

        if (!hasStartedDragging)
        {
            Debug.LogWarning("Landmark was never dragged, canceling placement");
            CancelDrag();
            return;
        }

        float distance = Vector3.Distance(currentLandmark.transform.position, currentLandmarkData.correctPosition.position);
        Debug.Log($"Distance to correct position: {distance}, Tolerance: {currentLandmarkData.tolerance}");
        if (distance <= currentLandmarkData.tolerance)
        {
            Debug.Log("Placement successful!");
            AddScore(1);
            currentLandmark = null;
            currentLandmarkData = null;
        }
        else
        {
            Debug.Log("Placement failed!");
            Vector3 explosionPos = currentLandmark.transform.position;
            Destroy(currentLandmark);
            ParticleSystem explosion = Instantiate(explosionEffect, explosionPos, Quaternion.identity);
            explosion.Play();
            Destroy(explosion.gameObject, explosion.main.duration);
            currentLandmark = null;
            currentLandmarkData = null;
        }
    }

    void CancelDrag()
    {
        Debug.Log("Canceling drag");
        isDragging = false;
        dragRotate.SetRotationEnabled(true);
        landmarkPanel.GetComponent<CanvasGroup>().blocksRaycasts = true;
        Destroy(currentLandmark);
        currentLandmark = null;
        currentLandmarkData = null;
    }

    void ShowHint()
    {
        if (currentLandmarkData != null)
        {
            hintText.text = currentLandmarkData.hint;
        }
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        scoreText.text = $"Score: {score}";
    }
}