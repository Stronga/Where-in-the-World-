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

        // Check MediaPipe hand detection
        if (handLandmarkerRunner != null)
        {
            var result = GetHandLandmarkerResult();
            Debug.Log($"HandLandmarkerResult detected: {result.handLandmarks?.Count ?? 0} hands");
            if (result.handLandmarks != null && result.handLandmarks.Count > 0)
            {
                handsDetected = true;
                var handLandmarks = result.handLandmarks[0];  // First detected hand
                var landmarks = handLandmarks.landmarks; // List<NormalizedLandmark> - corrected casing

                // Wrist position in screen space
                var wrist = landmarks[0];
                float ux = wrist.x * Screen.width;
                float uy = Screen.height * (1f - wrist.y);  // Flip Y for Unity
                Vector3 screenPos = new Vector3(ux, uy, 0);
                if (Camera.main != null)
                {
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(ux, uy, projectionDepth));
                    Debug.Log($"Wrist position in world space: {worldPos}");

                    // Detect gesture
                    currentGesture = DetectGesture(landmarks);

                    // Handle gesture
                    HandleGestureStates(worldPos);
                }
                else
                {
                    Debug.LogError("No Main Camera found in scene!");
                }
            }
            else
            {
                Debug.Log("No hand landmarks detected.");
            }
        }
        else
        {
            Debug.LogError("HandLandmarkerRunner is null during Update.");
        }

        // Fallback to keyboard simulation
        if (useKeyboardSimulation && !handsDetected)
        {
            Vector3 mouseWorldPos = Camera.main != null
                ? Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, projectionDepth))
                : Vector3.zero;
            if (Camera.main == null) Debug.LogError("No Main Camera for keyboard fallback!");
            currentGesture = DetectKeyboardGesture();
            HandleGestureStates(mouseWorldPos);
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

    private string DetectGesture(List<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> landmarks)
    {
        if (landmarks == null || landmarks.Count < 21) // Ensure all 21 landmarks are present
        {
            Debug.LogWarning("Insufficient landmarks for gesture detection.");
            return "open";
        }

        // Pinch: Distance between thumb tip (4) and index tip (8)
        float pinchDist = GetDistance(landmarks[4], landmarks[8]);
        Debug.Log($"Pinch distance: {pinchDist}");
        if (pinchDist < pinchThreshold) return "select";

        // Fist: Average distance from wrist (0) to finger tips (4,8,12,16,20)
        float openness = CalculateOpenness(landmarks);
        Debug.Log($"Openness: {openness}");
        if (openness < fistThreshold) return "grab";

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

    private void HandleGestureStates(Vector3 handPos)
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null, cannot handle gesture.");
            return;
        }

        if (lastGesture == "open" && currentGesture != "open")
        {
            Debug.Log($"Started gesture: {currentGesture} at {handPos}");
            gameManager.HandleHandInput(handPos, currentGesture);
        }
        else if (currentGesture != "open" && currentGesture == lastGesture)
        {
            if (currentGesture == "grab")
            {
                gameManager.HandleHandInput(handPos, "grab");
            }
        }
        else if (lastGesture != "open" && currentGesture == "open")
        {
            Debug.Log($"Ended gesture: {lastGesture}");
            if (lastGesture == "grab")
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
}