using System.Collections.Generic;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;
using UnityEngine.UI;

public class HandTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HandLandmarkerRunner handLandmarkerRunner;
    [SerializeField] private RawImage previewImage;

    [Header("Settings")]
    [SerializeField] private float projectionDepth = 5f;
    [SerializeField] private float pinchThreshold = 0.05f;
    [SerializeField] private float fistThreshold = 0.2f;
    [SerializeField] private bool useKeyboardSimulation = true;
    [SerializeField] private bool showInstructions = true;

    [Header("Gesture Stability")]
    [SerializeField] private float pinchEnterThreshold = 0.035f;
    [SerializeField] private float pinchExitThreshold = 0.075f;
    [SerializeField] private float pinchHoldTime = 0.18f;
    [SerializeField] private float releaseHoldTime = 0.22f;
    [SerializeField] private float trackingLossGraceTime = 0.35f;
    [SerializeField] private bool useFistForGrab = false;

    [Header("Hand Preference")]
    [SerializeField] private bool restrictGrabToRightHand = true;
    [SerializeField] private bool showHandDebugInfo = false;
    [SerializeField] private bool useSingleHandAsRightHand = false;
    [SerializeField] private bool swapHandRoles = false;

    [Header("Coordinate Mapping")]
    [SerializeField] private bool mirrorInputX = false;
    [SerializeField] private bool useIndexTipForPointer = true;

    [Header("Dual Hand Control")]
    [SerializeField] private DragRotate dragRotate;

    private const string OpenGesture = "open";
    private const string LeftPinchGesture = "pinch";
    private const string HandSelectGesture = "handSelect";
    private const string HandPlaceGesture = "handPlace";
    private const string KeyboardSelectGesture = "select";
    private const string KeyboardGrabGesture = "grab";
    private const string KeyboardPlaceGesture = "place";

    private readonly StableHoldState leftPinchState = new StableHoldState();
    private readonly StableHoldState rightHoldState = new StableHoldState();

    private string leftHandGesture = OpenGesture;
    private string rightHandGesture = OpenGesture;
    private Vector3 leftHandPosition = Vector3.zero;
    private Vector3 rightHandPosition = Vector3.zero;

    private WebCamTexture webCamTexture;

    private sealed class StableHoldState
    {
        public bool RawHeld;
        public bool Active;
        public float HoldStartedAt = -1f;
        public float ReleaseStartedAt = -1f;
        public float MissingStartedAt = -1f;
    }

    private void Start()
    {
        if (previewImage != null)
        {
            webCamTexture = new WebCamTexture();
            previewImage.texture = webCamTexture;
            webCamTexture.Play();
            LogDebug("Optional webcam preview initialized.");
        }

        if (handLandmarkerRunner == null)
        {
            Debug.LogError("HandLandmarkerRunner reference missing on HandTracker.");
        }

        if (gameManager == null)
        {
            Debug.LogError("GameManager reference missing on HandTracker.");
        }
    }

    private void Update()
    {
        bool leftHandFound = false;
        bool rightHandFound = false;

        if (handLandmarkerRunner != null)
        {
            HandLandmarkerResult result = handLandmarkerRunner.LatestResult;
            int handCount = result.handLandmarks?.Count ?? 0;

            for (int i = 0; i < handCount; i++)
            {
                var landmarks = result.handLandmarks[i].landmarks;
                if (landmarks == null || landmarks.Count < 21)
                {
                    continue;
                }

                string label = GetHandLabel(result, i);
                string role = ResolveHandRole(label, handCount, leftHandFound, rightHandFound, i);
                Vector3 worldPos = LandmarkToWorld(GetPointerLandmark(landmarks));

                if (role == "Left")
                {
                    leftHandFound = true;
                    leftHandPosition = worldPos;
                    UpdateLeftHand(landmarks, label);
                }
                else
                {
                    rightHandFound = true;
                    rightHandPosition = worldPos;
                    UpdateRightHand(landmarks, label);
                }
            }
        }

        if (!leftHandFound)
        {
            UpdateLeftHandMissing();
        }

        if (!rightHandFound)
        {
            UpdateRightHandMissing();
        }

        if (useKeyboardSimulation && !leftHandFound && !rightHandFound && !leftPinchState.Active && !rightHoldState.Active)
        {
            UpdateKeyboardSimulation();
        }
    }

    private void UpdateLeftHand(List<NormalizedLandmark> landmarks, string label)
    {
        bool wasActive = leftPinchState.Active;
        bool rawPinch = UpdatePinchHysteresis(leftPinchState, GetDistance(landmarks[4], landmarks[8]));
        bool stablePinch = UpdateStableHold(leftPinchState, rawPinch, out bool started, out bool ended);

        if (started)
        {
            leftHandGesture = LeftPinchGesture;
            StartLeftHandPinch();
            LogDebug($"Left hand pinch started ({label}).");
        }
        else if (stablePinch)
        {
            leftHandGesture = LeftPinchGesture;
            UpdateLeftHandPinch();
        }
        else if (ended || wasActive)
        {
            leftHandGesture = OpenGesture;
            EndLeftHandPinch();
            LogDebug($"Left hand pinch ended ({label}).");
        }
        else
        {
            leftHandGesture = OpenGesture;
        }
    }

    private void UpdateRightHand(List<NormalizedLandmark> landmarks, string label)
    {
        bool wasActive = rightHoldState.Active;
        bool rawHold = UpdatePinchHysteresis(rightHoldState, GetDistance(landmarks[4], landmarks[8]));

        if (useFistForGrab && (!restrictGrabToRightHand || label == "Right" || useSingleHandAsRightHand))
        {
            rawHold = rawHold || CalculateOpenness(landmarks) < fistThreshold;
        }

        bool stableHold = UpdateStableHold(rightHoldState, rawHold, out bool started, out bool ended);

        if (started)
        {
            rightHandGesture = HandSelectGesture;
            SendRightHandInput(HandSelectGesture);
            LogDebug($"Right hand hold started ({label}).");
        }
        else if (stableHold)
        {
            rightHandGesture = HandSelectGesture;
            SendRightHandInput(HandSelectGesture);
        }
        else if (ended || wasActive)
        {
            rightHandGesture = OpenGesture;
            SendRightHandInput(HandPlaceGesture);
            LogDebug($"Right hand hold ended ({label}).");
        }
        else
        {
            rightHandGesture = OpenGesture;
        }
    }

    private void UpdateLeftHandMissing()
    {
        bool stillActive = UpdateMissingHold(leftPinchState, out bool ended);
        if (stillActive)
        {
            return;
        }

        leftHandGesture = OpenGesture;
        if (ended)
        {
            EndLeftHandPinch();
        }
    }

    private void UpdateRightHandMissing()
    {
        bool stillActive = UpdateMissingHold(rightHoldState, out bool ended);
        if (stillActive)
        {
            return;
        }

        rightHandGesture = OpenGesture;
        if (ended)
        {
            SendRightHandInput(HandPlaceGesture);
        }
    }

    private bool UpdatePinchHysteresis(StableHoldState state, float pinchDistance)
    {
        float enterThreshold = GetPinchEnterThreshold();
        float exitThreshold = Mathf.Max(GetPinchExitThreshold(), enterThreshold + 0.001f);

        state.RawHeld = state.RawHeld
            ? pinchDistance < exitThreshold
            : pinchDistance < enterThreshold;

        return state.RawHeld;
    }

    private bool UpdateStableHold(StableHoldState state, bool rawHeld, out bool started, out bool ended)
    {
        started = false;
        ended = false;
        state.MissingStartedAt = -1f;

        float now = Time.time;

        if (state.Active)
        {
            state.HoldStartedAt = -1f;

            if (rawHeld)
            {
                state.ReleaseStartedAt = -1f;
                return true;
            }

            if (state.ReleaseStartedAt < 0f)
            {
                state.ReleaseStartedAt = now;
            }

            if (now - state.ReleaseStartedAt >= releaseHoldTime)
            {
                ResetHoldState(state);
                ended = true;
                return false;
            }

            return true;
        }

        state.ReleaseStartedAt = -1f;

        if (!rawHeld)
        {
            state.HoldStartedAt = -1f;
            return false;
        }

        if (state.HoldStartedAt < 0f)
        {
            state.HoldStartedAt = now;
        }

        if (now - state.HoldStartedAt < pinchHoldTime)
        {
            return false;
        }

        state.Active = true;
        state.HoldStartedAt = -1f;
        started = true;
        return true;
    }

    private bool UpdateMissingHold(StableHoldState state, out bool ended)
    {
        ended = false;
        state.RawHeld = false;
        state.HoldStartedAt = -1f;
        state.ReleaseStartedAt = -1f;

        if (!state.Active)
        {
            state.MissingStartedAt = -1f;
            return false;
        }

        float now = Time.time;
        if (state.MissingStartedAt < 0f)
        {
            state.MissingStartedAt = now;
        }

        if (now - state.MissingStartedAt < trackingLossGraceTime)
        {
            return true;
        }

        ResetHoldState(state);
        ended = true;
        return false;
    }

    private void ResetHoldState(StableHoldState state)
    {
        state.RawHeld = false;
        state.Active = false;
        state.HoldStartedAt = -1f;
        state.ReleaseStartedAt = -1f;
        state.MissingStartedAt = -1f;
    }

    private float GetPinchEnterThreshold()
    {
        return pinchEnterThreshold > 0f ? pinchEnterThreshold : pinchThreshold;
    }

    private float GetPinchExitThreshold()
    {
        return pinchExitThreshold > 0f ? pinchExitThreshold : pinchThreshold * 1.5f;
    }

    private string GetHandLabel(HandLandmarkerResult result, int index)
    {
        if (result.handedness == null || index >= result.handedness.Count)
        {
            return string.Empty;
        }

        var handedness = result.handedness[index];
        if (handedness.categories == null || handedness.categories.Count == 0)
        {
            return string.Empty;
        }

        return handedness.categories[0].categoryName;
    }

    private string ResolveHandRole(string label, int handCount, bool leftHandFound, bool rightHandFound, int index)
    {
        if (useSingleHandAsRightHand && handCount == 1)
        {
            return "Right";
        }

        if (swapHandRoles)
        {
            if (label == "Left")
            {
                label = "Right";
            }
            else if (label == "Right")
            {
                label = "Left";
            }
        }

        if (label == "Left" && !leftHandFound)
        {
            return "Left";
        }

        if (label == "Right" && !rightHandFound)
        {
            return "Right";
        }

        if (!rightHandFound)
        {
            return "Right";
        }

        if (!leftHandFound)
        {
            return "Left";
        }

        return index == 0 ? "Right" : "Left";
    }

    private NormalizedLandmark GetPointerLandmark(List<NormalizedLandmark> landmarks)
    {
        return useIndexTipForPointer && landmarks.Count > 8 ? landmarks[8] : landmarks[0];
    }

    private Vector3 LandmarkToWorld(NormalizedLandmark landmark)
    {
        if (Camera.main == null)
        {
            return Vector3.zero;
        }

        float normalizedX = mirrorInputX ? 1f - landmark.x : landmark.x;
        Vector3 screenPoint = new Vector3(
            normalizedX * Screen.width,
            (1f - landmark.y) * Screen.height,
            projectionDepth
        );

        return Camera.main.ScreenToWorldPoint(screenPoint);
    }

    private float GetDistance(NormalizedLandmark a, NormalizedLandmark b)
    {
        return Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
    }

    private float CalculateOpenness(List<NormalizedLandmark> landmarks)
    {
        Vector2 wrist = new Vector2(landmarks[0].x, landmarks[0].y);
        float distance = 0f;
        distance += Vector2.Distance(wrist, new Vector2(landmarks[4].x, landmarks[4].y));
        distance += Vector2.Distance(wrist, new Vector2(landmarks[8].x, landmarks[8].y));
        distance += Vector2.Distance(wrist, new Vector2(landmarks[12].x, landmarks[12].y));
        distance += Vector2.Distance(wrist, new Vector2(landmarks[16].x, landmarks[16].y));
        distance += Vector2.Distance(wrist, new Vector2(landmarks[20].x, landmarks[20].y));
        return distance / 5f;
    }

    private void SendRightHandInput(string gesture)
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.HandleHandInput(rightHandPosition, gesture);
    }

    private void UpdateKeyboardSimulation()
    {
        rightHandPosition = Camera.main != null
            ? Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, projectionDepth))
            : Vector3.zero;

        string keyboardGesture = DetectKeyboardGesture();
        if (keyboardGesture != OpenGesture && gameManager != null)
        {
            gameManager.HandleHandInput(rightHandPosition, keyboardGesture);
        }
    }

    private string DetectKeyboardGesture()
    {
        if (Input.GetKey(KeyCode.Space)) return KeyboardSelectGesture;
        if (Input.GetKey(KeyCode.G)) return KeyboardGrabGesture;
        if (Input.GetKey(KeyCode.P)) return KeyboardPlaceGesture;
        return OpenGesture;
    }

    private void StartLeftHandPinch()
    {
        if (dragRotate == null)
        {
            return;
        }

        dragRotate.StartHandRotation(leftHandPosition);
    }

    private void UpdateLeftHandPinch()
    {
        if (dragRotate != null)
        {
            dragRotate.UpdateHandRotation(leftHandPosition);
        }
    }

    private void EndLeftHandPinch()
    {
        if (dragRotate != null)
        {
            dragRotate.EndHandRotation();
        }
    }

    private void OnGUI()
    {
        if (!showInstructions)
        {
            return;
        }

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 12 };
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };

        GUI.Box(new UnityEngine.Rect(10, 250, 320, 160), "Hand Tracker - Gestures", boxStyle);
        GUI.Label(new UnityEngine.Rect(20, 275, 300, 20), "Hold pinch = select and drag", labelStyle);
        GUI.Label(new UnityEngine.Rect(20, 295, 300, 20), "Open hand = place after short delay", labelStyle);
        GUI.Label(new UnityEngine.Rect(20, 315, 300, 20), "Left pinch = rotate globe", labelStyle);
        GUI.Label(new UnityEngine.Rect(20, 335, 300, 20), $"Right gesture: {rightHandGesture}", labelStyle);
        GUI.Label(new UnityEngine.Rect(20, 355, 300, 20), $"Left gesture: {leftHandGesture}", labelStyle);
        if (useKeyboardSimulation)
        {
            GUI.Label(new UnityEngine.Rect(20, 375, 300, 20), "Fallback: SPACE/G/P keys", labelStyle);
        }
    }

    private void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }

    private void LogDebug(string message)
    {
        if (showHandDebugInfo)
        {
            Debug.Log(message);
        }
    }
}
