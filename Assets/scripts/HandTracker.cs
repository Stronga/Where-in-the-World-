using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;  // Add this for NormalizedLandmark
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System.Collections.Generic;
using System.Linq;  // Add this if missing

public class HandTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HandLandmarkerRunner handLandmarkerRunner;  // Reference to HandLandmarkerRunner
    [SerializeField] private RawImage previewImage;  // Optional: Webcam preview

    [Header("Settings")]
    [SerializeField] private float projectionDepth = 5f;  // Depth for projecting hand pos
    [SerializeField] private float pinchThreshold = 0.05f;  // Normalized dist for pinch ("select")
    [SerializeField] private float fistThreshold = 0.2f;  // Avg normalized dist for fist ("grab")
    [SerializeField] private bool useKeyboardSimulation = true;  // Fallback if no hands detected
    [SerializeField] private bool showInstructions = true;

    [Header("Hand Preference")]
    [SerializeField] private bool restrictGrabToRightHand = true;  // Toggle for right-hand restriction
    [SerializeField] private bool showHandDebugInfo = true;       // Show hand detection info

    [Header("Dual Hand Control")]
    [SerializeField] private DragRotate dragRotate; // Reference to DragRotate component

    private string leftHandGesture = "open";
    private string rightHandGesture = "open";
    private string lastLeftHandGesture = "open";
    private string lastRightHandGesture = "open";
    private Vector3 leftHandPosition = Vector3.zero;
    private Vector3 rightHandPosition = Vector3.zero;
    private bool leftHandPinching = false;
    private string currentHandType = "unknown";  // Track which hand is being used

    private string lastGesture = "open";
    private string currentGesture = "open";
    private WebCamTexture webCamTexture;

    void Start()
    {
        // Optional: Start webcam for preview
        if (previewImage != null)
        {
            webCamTexture = new WebCamTexture();
            if (webCamTexture != null)
            {
                previewImage.texture = webCamTexture;
                webCamTexture.Play();
                Debug.Log("Webcam texture initialized and playing.");
            }
            else
            {
                Debug.LogError("Failed to create WebCamTexture.");
            }
        }

        if (handLandmarkerRunner == null)
        {
            Debug.LogError("HandLandmarkerRunner reference missing! Drag from HandDetectionSystem in Hierarchy.");
        }
        else
        {
            Debug.Log("HandLandmarkerRunner reference found.");
        }
    }

    void Update()
    {
        bool handsDetected = false;

        // Reset hand states
        bool leftHandFound = false;
        bool rightHandFound = false;

        // Check MediaPipe hand detection
        if (handLandmarkerRunner != null)
        {
            var result = GetHandLandmarkerResult();
            Debug.Log($"HandLandmarkerResult detected: {result.handLandmarks?.Count ?? 0} hands");

            if (result.handLandmarks != null && result.handLandmarks.Count > 0)
            {
                // Process each detected hand
                if (result.handedness != null && result.handedness.Count > 0)
                {
                    for (int i = 0; i < result.handedness.Count && i < result.handLandmarks.Count; i++)
                    {
                        var handedness = result.handedness[i];
                       if (handedness.categories != null && handedness.categories.Count > 0)
{
    var classification = handedness.categories[0];
                            string handLabel = classification.categoryName;

                            Debug.Log($"Hand {i}: {handLabel} (confidence: {classification.score:F2})");

                            var handLandmarks = result.handLandmarks[i];
                            var landmarks = handLandmarks.landmarks;

                            // Calculate hand position
                            var wrist = landmarks[0];
                            float ux = wrist.x * Screen.width;
                            float uy = Screen.height * (1f - wrist.y);  // Flip Y for Unity
                            Vector3 worldPos = Camera.main != null
                                ? Camera.main.ScreenToWorldPoint(new Vector3(ux, uy, projectionDepth))
                                : Vector3.zero;

                            // Process based on hand type
                            if (handLabel == "Left")
                            {
                                leftHandFound = true;
                                leftHandPosition = worldPos;
                                leftHandGesture = DetectGestureForHand(landmarks, "Left");
                                HandleLeftHandGestures();
                            }
                            else if (handLabel == "Right")
                            {
                                rightHandFound = true;
                                rightHandPosition = worldPos;
                                rightHandGesture = DetectGestureForHand(landmarks, "Right");
                                HandleRightHandGestures();
                            }
                        }
                    }
                    handsDetected = leftHandFound || rightHandFound;
                }
            }
            else
            {
                Debug.Log("No hand landmark lists detected.");
            }
        }

        // Reset hand states if hands not found
        if (!leftHandFound)
        {
            if (leftHandPinching)
            {
                EndLeftHandPinch();
            }
            leftHandGesture = "open";
            currentHandType = rightHandFound ? "Right" : "unknown";
        }

        if (!rightHandFound)
        {
            rightHandGesture = "open";
            currentHandType = leftHandFound ? "Left" : "unknown";
        }

        // Fallback to keyboard simulation
        if (useKeyboardSimulation && !handsDetected)
        {
            Vector3 mouseWorldPos = Camera.main != null
                ? Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, projectionDepth))
                : Vector3.zero;

            rightHandGesture = DetectKeyboardGesture();
            rightHandPosition = mouseWorldPos;
            HandleRightHandGestures();
        }
    }


    private HandLandmarkerResult GetHandLandmarkerResult()
    {
        if (handLandmarkerRunner != null)
        {
            return handLandmarkerRunner.LatestResult;
        }
        Debug.LogError("HandLandmarkerRunner is null, cannot get result.");
        return new HandLandmarkerResult(); // Return default struct
    }

    private string DetectGesture(List<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> landmarks, bool allowGrab)
    {
        if (landmarks == null || landmarks.Count < 21) // Ensure all 21 landmarks are present
        {
            Debug.LogWarning("Insufficient landmarks for gesture detection.");
            return "open";
        }

        // Pinch: Distance between thumb tip (4) and index tip (8) - Always allowed
        float pinchDist = GetDistance(landmarks[4], landmarks[8]);
        Debug.Log($"Pinch distance: {pinchDist}");
        if (pinchDist < pinchThreshold) return "select";

        // Fist: Only allowed if using right hand (or restriction is disabled)
        if (allowGrab)
        {
            float openness = CalculateOpenness(landmarks);
            Debug.Log($"Openness: {openness}");
            if (openness < fistThreshold) return "grab";
        }
        else
        {
            Debug.Log("Grab gesture ignored - not using right hand");
        }

        return "open";
    }


    private float GetDistance(NormalizedLandmark a, NormalizedLandmark b)
    {
        Vector2 posA = new Vector2(a.x, a.y);
        Vector2 posB = new Vector2(b.x, b.y);
        return Vector2.Distance(posA, posB);
    }
private float CalculateOpenness(List<NormalizedLandmark> landmarks)
{
    Vector2 wrist = new Vector2(landmarks[0].x, landmarks[0].y);
    float dist = 0;
    dist += Vector2.Distance(wrist, new Vector2(landmarks[4].x, landmarks[4].y));  // Thumb
    dist += Vector2.Distance(wrist, new Vector2(landmarks[8].x, landmarks[8].y));  // Index
    dist += Vector2.Distance(wrist, new Vector2(landmarks[12].x, landmarks[12].y)); // Middle
    dist += Vector2.Distance(wrist, new Vector2(landmarks[16].x, landmarks[16].y)); // Ring
    dist += Vector2.Distance(wrist, new Vector2(landmarks[20].x, landmarks[20].y)); // Pinky
    return dist / 5f;
}

    private void HandleGestureStates(Vector3 handPos, bool allowGrab)
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null, cannot handle gesture.");
            return;
        }

        if (lastGesture == "open" && currentGesture != "open")
        {
            Debug.Log($"Started gesture: {currentGesture} at {handPos} (Hand: {currentHandType})");
            gameManager.HandleHandInput(handPos, currentGesture);
        }
        else if (currentGesture != "open" && currentGesture == lastGesture)
        {
            if (currentGesture == "grab" && allowGrab)
            {
                gameManager.HandleHandInput(handPos, "grab");
            }
        }
        else if (lastGesture != "open" && currentGesture == "open")
        {
            Debug.Log($"Ended gesture: {lastGesture} (Hand: {currentHandType})");
            if (lastGesture == "grab" && allowGrab)
            {
                gameManager.HandleHandInput(handPos, "place");
            }
        }

        lastGesture = currentGesture;
    }


    private string DetectKeyboardGesture()
    {
        if (Input.GetKey(KeyCode.Space)) return "select";
        else if (Input.GetKey(KeyCode.G)) return "grab";
        else if (Input.GetKey(KeyCode.P)) return "place";
        return "open";
    }

    void OnGUI()
    {
        if (showInstructions)
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 12 };
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };

            GUI.Box(new UnityEngine.Rect(10, 250, 320, 160), "Hand Tracker - Gestures", boxStyle);
            GUI.Label(new UnityEngine.Rect(20, 275, 300, 20), "Pinch (thumb+index close) = Select from UI", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 295, 300, 20), "Fist = Grab and drag landmark", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 315, 300, 20), "Open hand = Place landmark", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 335, 300, 20), $"Current gesture: {currentGesture}", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 355, 300, 20), $"Last gesture: {lastGesture}", labelStyle);
            if (useKeyboardSimulation) GUI.Label(new UnityEngine.Rect(20, 375, 300, 20), "Fallback: SPACE/G/P keys", labelStyle);
        }
    }

    void OnDestroy()
    {
        if (webCamTexture != null) webCamTexture.Stop();
    }
    
    private string DetectGestureForHand(List<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> landmarks, string handType)
{
    if (landmarks == null || landmarks.Count < 21)
    {
        Debug.LogWarning($"Insufficient landmarks for {handType} hand gesture detection.");
        return "open";
    }

    // Pinch: Distance between thumb tip (4) and index tip (8)
    float pinchDist = GetDistance(landmarks[4], landmarks[8]);
    
    if (handType == "Left")
    {
        // Left hand: Only detect pinch for rotation
        Debug.Log($"Left hand pinch distance: {pinchDist}");
        if (pinchDist < pinchThreshold) return "pinch";
    }
    else if (handType == "Right")
    {
        // Right hand: Detect pinch for selection and fist for grab
        Debug.Log($"Right hand pinch distance: {pinchDist}");
        if (pinchDist < pinchThreshold) return "select";
        
        float openness = CalculateOpenness(landmarks);
        Debug.Log($"Right hand openness: {openness}");
        if (openness < fistThreshold) return "grab";
    }

    return "open";
}

private void HandleLeftHandGestures()
{
    // Left hand pinch for globe rotation
    if (lastLeftHandGesture == "open" && leftHandGesture == "pinch")
    {
        StartLeftHandPinch();
    }
    else if (leftHandGesture == "pinch" && leftHandPinching)
    {
        UpdateLeftHandPinch();
    }
    else if (lastLeftHandGesture == "pinch" && leftHandGesture == "open")
    {
        EndLeftHandPinch();
    }
    
    lastLeftHandGesture = leftHandGesture;
}

private void HandleRightHandGestures()
{
    // Right hand gestures for landmark placement (existing logic)
    if (lastRightHandGesture == "open" && rightHandGesture != "open")
    {
        Debug.Log($"Started right hand gesture: {rightHandGesture} at {rightHandPosition}");
        if (gameManager != null)
        {
            gameManager.HandleHandInput(rightHandPosition, rightHandGesture);
        }
    }
    else if (rightHandGesture != "open" && rightHandGesture == lastRightHandGesture)
    {
        if (rightHandGesture == "grab" && gameManager != null)
        {
            gameManager.HandleHandInput(rightHandPosition, "grab");
        }
    }
    else if (lastRightHandGesture != "open" && rightHandGesture == "open")
    {
        Debug.Log($"Ended right hand gesture: {lastRightHandGesture}");
        if (lastRightHandGesture == "grab" && gameManager != null)
        {
            gameManager.HandleHandInput(rightHandPosition, "place");
        }
    }
    
    lastRightHandGesture = rightHandGesture;
}

private void StartLeftHandPinch()
{
    if (dragRotate != null)
    {
        leftHandPinching = true;
        dragRotate.StartHandRotation(leftHandPosition);
        Debug.Log("Started left hand pinch rotation");
    }
}

private void UpdateLeftHandPinch()
{
    if (dragRotate != null && leftHandPinching)
    {
        dragRotate.UpdateHandRotation(leftHandPosition);
    }
}

private void EndLeftHandPinch()
{
    if (dragRotate != null)
    {
        leftHandPinching = false;
        dragRotate.EndHandRotation();
        Debug.Log("Ended left hand pinch rotation");
    }
}

}