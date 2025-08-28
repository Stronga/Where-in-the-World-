using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

public class MyGestureController : MonoBehaviour
{
    [Tooltip("Drag the GameManager GameObject here.")]
    public GameManager gameManager;

    [Tooltip("Assign a 3D object (like a small sphere) to act as a visual hand cursor.")]
    public Transform cursor;

    [Tooltip("How far the cursor should appear in front of the camera.")]
    [SerializeField] private float cursorDepth = 10f;

    private const float GestureCooldown = 0.5f;
    private float lastGestureTime;
    private string lastSentGesture = "none";

    public void OnHandLandmarksOutput(HandLandmarkerResult result)
    {
        if (gameManager == null || result.handLandmarks.Count == 0)
        {
            if (cursor != null) cursor.gameObject.SetActive(false);
            return;
        }

        var landmarks = result.handLandmarks[0];
        if (cursor != null)
        {
            cursor.gameObject.SetActive(true);
            UpdateCursorPosition(landmarks.landmarks);
        }
        
        string currentGesture = RecognizeGesture(landmarks.landmarks);
        
        if (currentGesture != "none" && Time.time > lastGestureTime + GestureCooldown)
        {
            if (currentGesture != lastSentGesture || currentGesture == "grab")
            {
                Debug.Log($"<color=cyan>Gesture Recognized: {currentGesture}</color>");
                Vector3 handWorldPosition = (cursor != null) ? cursor.position : Vector3.zero;
                gameManager.HandleHandInput(handWorldPosition, currentGesture);
                lastGestureTime = Time.time;
                lastSentGesture = currentGesture;
            }
        } 
        else if (currentGesture == "none")
        {
            lastSentGesture = "none";
        }
    }

    private void UpdateCursorPosition(IReadOnlyList<NormalizedLandmark> landmarks)
    {
        var wristLandmark = landmarks[0];
        Vector3 screenPoint = new Vector3(
            wristLandmark.x * UnityEngine.Screen.width,
            (1 - wristLandmark.y) * UnityEngine.Screen.height,
            cursorDepth 
        );
        cursor.position = Camera.main.ScreenToWorldPoint(screenPoint);
    }

    private string RecognizeGesture(IReadOnlyList<NormalizedLandmark> landmarks)
    {
        var wristPos = landmarks[0];

        bool IsFingerExtended(int tipIndex, int pipIndex)
        {
            var tipPoint = new Vector3(landmarks[tipIndex].x, landmarks[tipIndex].y, landmarks[tipIndex].z);
            var pipPoint = new Vector3(landmarks[pipIndex].x, landmarks[pipIndex].y, landmarks[pipIndex].z);
            var wristPoint = new Vector3(wristPos.x, wristPos.y, wristPos.z);

            return Vector3.Distance(tipPoint, wristPoint) > Vector3.Distance(pipPoint, wristPoint);
        }

        bool isIndexExtended  = IsFingerExtended(8, 6);
        bool isMiddleExtended = IsFingerExtended(12, 10);
        bool isRingExtended   = IsFingerExtended(16, 14);
        bool isPinkyExtended  = IsFingerExtended(20, 18);

        if (isIndexExtended && !isMiddleExtended && !isRingExtended && !isPinkyExtended) return "select";
        if (isIndexExtended && isMiddleExtended && isRingExtended && isPinkyExtended) return "place";
        if (!isIndexExtended && !isMiddleExtended && !isRingExtended && !isPinkyExtended) return "grab";

        return "none";
    }
}