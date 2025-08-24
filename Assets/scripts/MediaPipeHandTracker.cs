using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Computer Vision Hand Tracker with Camera Selection
/// Restored camera switching + fixed debug UI positioning
/// </summary>
public class MediaPipeHandTracker : MonoBehaviour
{
    [Header("Game Integration")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Camera mainCamera;
    
    [Header("Camera Selection")]
    [SerializeField] private int currentCameraIndex = 0;
    [SerializeField] private KeyCode switchCameraKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode nextCameraKey = KeyCode.Alpha2;
    [SerializeField] private bool showCameraList = true;
    
    [Header("WebCam Settings")]
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;
    
    [Header("Hand Detection")]
    [SerializeField] private bool enableHandDetection = true;
    [SerializeField] private float motionThreshold = 0.15f;
    [SerializeField] private float gestureHoldTime = 0.3f;
    [SerializeField] private int detectionSmoothing = 5;
    
    [Header("3D Positioning")]
    [SerializeField] private float depthFromCamera = 5f;
    [SerializeField] private Vector3 handOffset = Vector3.zero;
    [SerializeField] private float handScale = 1f;
    [SerializeField] private bool mirrorHorizontal = true;
    
    [Header("UI")]
    [SerializeField] private RawImage cameraPreview;
    [SerializeField] private Transform handVisualization;
    
    [Header("Debug UI")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Vector2 debugPanelPosition = new Vector2(10, 10);
    [SerializeField] private Vector2 cameraPanelPosition = new Vector2(10, 250);
    [SerializeField] private Vector2 gesturePanelPosition = new Vector2(10, 400);
    
    // Hand tracking state
    private string currentGesture = "open";
    private string lastGesture = "open";
    private Vector3 currentHandPosition = Vector3.zero;
    private bool hasValidHand = false;
    private float lastHandDetectionTime = 0f;
    private float gestureStartTime = 0f;
    
    // WebCam components
    private WebCamTexture webCamTexture;
    private bool isWebCamActive = false;
    private WebCamDevice[] availableCameras;
    private string[] cameraNames;
    
    // Computer vision data
    private Texture2D previousFrame;
    private Texture2D currentFrame;
    private Color[] previousPixels;
    private Color[] currentPixels;
    private Vector2 motionCenter = Vector2.zero;
    private float motionIntensity = 0f;
    
    // Detection smoothing
    private Queue<bool> handDetectionHistory = new Queue<bool>();
    private Queue<Vector2> motionCenterHistory = new Queue<Vector2>();
    private Queue<float> motionIntensityHistory = new Queue<float>();
    
    void Start()
    {
        // Initialize detection smoothing
        for (int i = 0; i < detectionSmoothing; i++)
        {
            handDetectionHistory.Enqueue(false);
            motionCenterHistory.Enqueue(Vector2.zero);
            motionIntensityHistory.Enqueue(0f);
        }
        
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        StartCoroutine(InitializeComputerVision());
    }
    
    void Update()
    {
        // Handle camera switching
        HandleCameraSwitching();
        
        if (isWebCamActive && enableHandDetection)
        {
            ProcessComputerVisionHandDetection();
        }
        
        UpdateHandVisualization();
        UpdateCameraPreview();
        
        // Check for hand tracking timeout
        if (hasValidHand && (Time.time - lastHandDetectionTime) > 1f)
        {
            hasValidHand = false;
            currentGesture = "open";
            HandleGestureChange();
        }
    }
    
    #region Camera Selection and Management
    
    void HandleCameraSwitching()
    {
        // Handle camera switching inputs
        if (Input.GetKeyDown(switchCameraKey))
        {
            SwitchToNextCamera();
        }
        else if (Input.GetKeyDown(nextCameraKey))
        {
            SwitchToNextCamera();
        }
        
        // Handle number keys for direct camera selection
        for (int i = 0; i < Mathf.Min(availableCameras.Length, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchToCamera(i);
            }
        }
    }
    
    public void SwitchToNextCamera()
    {
        if (availableCameras.Length <= 1) return;
        
        int nextIndex = (currentCameraIndex + 1) % availableCameras.Length;
        SwitchToCamera(nextIndex);
    }
    
    public void SwitchToCamera(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= availableCameras.Length) return;
        if (cameraIndex == currentCameraIndex && isWebCamActive) return;
        
        currentCameraIndex = cameraIndex;
        
        Debug.Log($"🔄 Switching to camera {currentCameraIndex}: {availableCameras[currentCameraIndex].name}");
        
        // Stop current camera
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
            isWebCamActive = false;
        }
        
        // Clean up computer vision textures
        CleanupComputerVisionTextures();
        
        // Start new camera
        StartCoroutine(StartSpecificCamera(currentCameraIndex));
    }
    
    #endregion
    
    #region Computer Vision Setup
    
    IEnumerator InitializeComputerVision()
    {
        Debug.Log("🎥 Starting Computer Vision Hand Tracker...");
        
        // Get available cameras
        availableCameras = WebCamTexture.devices;
        if (availableCameras.Length == 0)
        {
            Debug.LogError("❌ No cameras found!");
            yield break;
        }
        
        // Create camera names array for UI
        cameraNames = new string[availableCameras.Length];
        for (int i = 0; i < availableCameras.Length; i++)
        {
            cameraNames[i] = $"{i}: {availableCameras[i].name} {(availableCameras[i].isFrontFacing ? "(Front)" : "(Back)")}";
            Debug.Log($"📷 Camera {i}: {availableCameras[i].name} (Front: {availableCameras[i].isFrontFacing})");
        }
        
        // Clamp camera index
        currentCameraIndex = Mathf.Clamp(currentCameraIndex, 0, availableCameras.Length - 1);
        
        // Start initial camera
        yield return StartSpecificCamera(currentCameraIndex);
        
        Debug.Log("🚀 Computer Vision Hand Tracking ACTIVE!");
        Debug.Log($"🎮 Camera Controls: {switchCameraKey} or {nextCameraKey} to switch cameras, or press 1-{Mathf.Min(availableCameras.Length, 9)} for direct selection");
    }
    
    IEnumerator StartSpecificCamera(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= availableCameras.Length) yield break;
        
        Debug.Log($"📷 Starting camera {cameraIndex}: {availableCameras[cameraIndex].name}");
        
        // Create WebCamTexture for specific camera
        webCamTexture = new WebCamTexture(
            availableCameras[cameraIndex].name,
            requestedWidth,
            requestedHeight,
            requestedFPS
        );
        
        // Start camera
        webCamTexture.Play();
        
        // Wait for camera to start
        yield return new WaitForSeconds(2f);
        
        if (webCamTexture.isPlaying)
        {
            isWebCamActive = true;
            Debug.Log($"✅ Camera {cameraIndex} active: {webCamTexture.width}x{webCamTexture.height} @ {webCamTexture.requestedFPS}fps");
            
            // Initialize computer vision for this camera
            yield return InitializeMotionDetectionCoroutine();
        }
        else
        {
            Debug.LogError($"❌ Failed to start camera {cameraIndex}");
            isWebCamActive = false;
        }
    }
    
    IEnumerator InitializeMotionDetectionCoroutine()
    {
        if (!isWebCamActive) yield break;
        
        Debug.Log("🔍 Initializing motion detection...");
        
        // Wait for camera to stabilize
        yield return new WaitForSeconds(1f);
        
        // Clean up previous textures
        CleanupComputerVisionTextures();
        
        // Create textures for motion detection
        int width = Mathf.Min(webCamTexture.width, 320); // Reduce resolution for performance
        int height = Mathf.Min(webCamTexture.height, 240);
        
        previousFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
        currentFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
        
        // Initialize with first frame
        CopyWebCamToTexture(previousFrame);
        
        Debug.Log("✅ Motion detection ready");
    }
    
    void CleanupComputerVisionTextures()
    {
        if (previousFrame != null)
        {
            Destroy(previousFrame);
            previousFrame = null;
        }
        if (currentFrame != null)
        {
            Destroy(currentFrame);
            currentFrame = null;
        }
    }
    
    #endregion
    
    #region Computer Vision Hand Detection
    
    void ProcessComputerVisionHandDetection()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying || currentFrame == null) return;
        
        // Update current frame
        CopyWebCamToTexture(currentFrame);
        
        // Detect motion between frames
        DetectMotionBetweenFrames();
        
        // Analyze motion for hand detection
        AnalyzeMotionForHandDetection();
        
        // Swap frames for next detection
        SwapFrameTextures();
    }
    
    void CopyWebCamToTexture(Texture2D targetTexture)
    {
        if (webCamTexture == null || targetTexture == null) return;
        
        // Create a temporary RenderTexture to scale down the webcam feed
        RenderTexture tempRT = RenderTexture.GetTemporary(targetTexture.width, targetTexture.height);
        Graphics.Blit(webCamTexture, tempRT);
        
        // Read from RenderTexture to Texture2D
        RenderTexture.active = tempRT;
        targetTexture.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
        targetTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);
    }
    
    void DetectMotionBetweenFrames()
    {
        if (previousFrame == null || currentFrame == null) return;
        
        // Get pixels from both frames
        previousPixels = previousFrame.GetPixels();
        currentPixels = currentFrame.GetPixels();
        
        // Calculate motion
        float totalMotion = 0f;
        Vector2 weightedCenter = Vector2.zero;
        float totalWeight = 0f;
        
        int width = currentFrame.width;
        int height = currentFrame.height;
        
        for (int i = 0; i < previousPixels.Length; i++)
        {
            // Calculate pixel difference
            float diff = Mathf.Abs(previousPixels[i].r - currentPixels[i].r) +
                        Mathf.Abs(previousPixels[i].g - currentPixels[i].g) +
                        Mathf.Abs(previousPixels[i].b - currentPixels[i].b);
            
            diff /= 3f; // Average RGB difference
            
            // Apply motion threshold
            if (diff > motionThreshold)
            {
                totalMotion += diff;
                
                // Calculate pixel position
                int x = i % width;
                int y = i / width;
                
                // Weight center calculation
                weightedCenter += new Vector2(x, y) * diff;
                totalWeight += diff;
            }
        }
        
        // Calculate motion center and intensity
        if (totalWeight > 0)
        {
            motionCenter = weightedCenter / totalWeight;
            motionIntensity = totalMotion / previousPixels.Length;
        }
        else
        {
            motionCenter = Vector2.zero;
            motionIntensity = 0f;
        }
    }
    
    void AnalyzeMotionForHandDetection()
    {
        // Smooth motion data
        motionCenterHistory.Enqueue(motionCenter);
        motionIntensityHistory.Enqueue(motionIntensity);
        
        if (motionCenterHistory.Count > detectionSmoothing)
        {
            motionCenterHistory.Dequeue();
            motionIntensityHistory.Dequeue();
        }
        
        // Calculate smoothed values
        Vector2 avgCenter = Vector2.zero;
        float avgIntensity = 0f;
        
        foreach (var center in motionCenterHistory)
            avgCenter += center;
        foreach (var intensity in motionIntensityHistory)
            avgIntensity += intensity;
        
        avgCenter /= motionCenterHistory.Count;
        avgIntensity /= motionIntensityHistory.Count;
        
        // Determine if hand is detected
        bool handDetected = avgIntensity > motionThreshold * 2f;
        
        // Smooth hand detection
        handDetectionHistory.Enqueue(handDetected);
        if (handDetectionHistory.Count > detectionSmoothing)
            handDetectionHistory.Dequeue();
        
        // Count recent detections
        int detectionCount = 0;
        foreach (var detected in handDetectionHistory)
            if (detected) detectionCount++;
        
        // Update hand state
        bool handNowValid = detectionCount >= (detectionSmoothing / 2);
        
        if (handNowValid)
        {
            hasValidHand = true;
            lastHandDetectionTime = Time.time;
            
            // Convert motion center to 3D position
            currentHandPosition = ConvertMotionCenterTo3D(avgCenter);
            
            // Detect gesture from motion pattern
            string detectedGesture = DetectGestureFromMotion(avgIntensity);
            UpdateGesture(detectedGesture);
        }
        else if (hasValidHand)
        {
            hasValidHand = false;
            UpdateGesture("open");
        }
    }
    
    Vector3 ConvertMotionCenterTo3D(Vector2 motionCenter)
    {
        if (webCamTexture == null || currentFrame == null) return Vector3.zero;
        
        // Convert from texture coordinates to screen coordinates
        float screenX = (motionCenter.x / currentFrame.width) * UnityEngine.Screen.width;
        float screenY = (1f - (motionCenter.y / currentFrame.height)) * UnityEngine.Screen.height;
        
        // Apply horizontal mirroring
        if (mirrorHorizontal)
        {
            screenX = UnityEngine.Screen.width - screenX;
        }
        
        // Convert to world position
        Vector3 screenPos = new Vector3(screenX, screenY, depthFromCamera);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        
        // Apply scaling and offset
        return (worldPos * handScale) + handOffset;
    }
    
    string DetectGestureFromMotion(float motionIntensity)
    {
        // Simple gesture detection based on motion patterns
        if (motionIntensity > motionThreshold * 4f)
        {
            return "grab"; // High motion = grabbing/moving
        }
        else if (motionIntensity > motionThreshold * 2f)
        {
            return "select"; // Medium motion = selecting
        }
        else
        {
            return "open"; // Low motion = open hand
        }
    }
    
    void UpdateGesture(string newGesture)
    {
        if (currentGesture != newGesture)
        {
            if (newGesture != "open")
            {
                gestureStartTime = Time.time;
            }
            
            currentGesture = newGesture;
            HandleGestureChange();
        }
        else if (currentGesture != "open")
        {
            if (Time.time - gestureStartTime > gestureHoldTime)
            {
                HandleContinuousGesture();
            }
        }
    }
    
    void SwapFrameTextures()
    {
        var temp = previousFrame;
        previousFrame = currentFrame;
        currentFrame = temp;
    }
    
    #endregion
    
    #region Gesture Handling
    
    void HandleGestureChange()
    {
        // Gesture started
        if (lastGesture == "open" && currentGesture != "open")
        {
            Debug.Log($"🤏 Hand Gesture: {currentGesture} at {currentHandPosition}");
            gameManager?.HandleHandInput(currentHandPosition, currentGesture);
        }
        // Gesture ended
        else if (lastGesture != "open" && currentGesture == "open")
        {
            Debug.Log($"✋ Gesture Released: {lastGesture}");
            if (lastGesture == "grab")
            {
                gameManager?.HandleHandInput(currentHandPosition, "place");
            }
        }
        
        lastGesture = currentGesture;
    }
    
    void HandleContinuousGesture()
    {
        if (currentGesture == "grab")
        {
            gameManager?.HandleHandInput(currentHandPosition, "grab");
        }
    }
    
    #endregion
    
    #region UI and Visualization
    
    void UpdateCameraPreview()
    {
        if (cameraPreview != null && webCamTexture != null && webCamTexture.isPlaying)
        {
            cameraPreview.texture = webCamTexture;
            
            // Apply mirroring
            if (mirrorHorizontal)
            {
                var scale = cameraPreview.rectTransform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                cameraPreview.rectTransform.localScale = scale;
            }
        }
    }
    
    void UpdateHandVisualization()
    {
        if (handVisualization != null)
        {
            if (hasValidHand && currentGesture != "open")
            {
                handVisualization.position = currentHandPosition;
                handVisualization.gameObject.SetActive(true);
                
                // Color based on gesture
                var renderer = handVisualization.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color gestureColor = currentGesture == "select" ? Color.yellow : 
                                       currentGesture == "grab" ? Color.red : Color.green;
                    renderer.material.color = gestureColor;
                }
            }
            else
            {
                handVisualization.gameObject.SetActive(false);
            }
        }
    }
    
    #endregion
    
    #region Debug UI (Non-overlapping)
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        var boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 12 };
        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        
        // Main debug panel
        GUI.Box(new Rect(debugPanelPosition.x, debugPanelPosition.y, 450, 180), 
                "🎥 Computer Vision Hand Tracker", boxStyle);
        
        float y = debugPanelPosition.y + 25;
        
        GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                 $"Camera: {(isWebCamActive ? "Active" : "Inactive")} | Hand: {hasValidHand}", labelStyle);
        y += 20;
        
        if (isWebCamActive && webCamTexture != null)
        {
            GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                     $"Resolution: {webCamTexture.width}x{webCamTexture.height} | FPS: {webCamTexture.requestedFPS}", labelStyle);
            y += 20;
        }
        
        GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                 $"Gesture: {currentGesture} | Motion: {motionIntensity:F4}", labelStyle);
        y += 20;
        
        GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                 $"Hand Position: {currentHandPosition}", labelStyle);
        y += 20;
        
        GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                 $"Motion Center: {motionCenter} | Threshold: {motionThreshold:F3}", labelStyle);
        y += 20;
        
        GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                 $"Detection: {lastHandDetectionTime:F1}s | Smoothing: {detectionSmoothing}", labelStyle);
        y += 20;
        
        if (!hasValidHand && isWebCamActive)
        {
            GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                     "👋 Move your hand in front of the camera!", labelStyle);
        }
        else if (!isWebCamActive)
        {
            GUI.Label(new Rect(debugPanelPosition.x + 10, y, 430, 20), 
                     "📷 Camera starting... Grant permissions if prompted!", labelStyle);
        }
        
        // Camera selection panel (separate, non-overlapping)
        if (showCameraList && availableCameras != null && availableCameras.Length > 0)
        {
            float cameraBoxHeight = 30 + (availableCameras.Length * 20) + 40;
            GUI.Box(new Rect(cameraPanelPosition.x, cameraPanelPosition.y, 450, cameraBoxHeight), 
                    "📷 Camera Selection", boxStyle);
            
            float cameraY = cameraPanelPosition.y + 25;
            
            GUI.Label(new Rect(cameraPanelPosition.x + 10, cameraY, 430, 20), 
                     $"Current: {currentCameraIndex} - {availableCameras[currentCameraIndex].name}", labelStyle);
            cameraY += 25;
            
            for (int i = 0; i < availableCameras.Length; i++)
            {
                string marker = (i == currentCameraIndex) ? "► " : "   ";
                GUI.Label(new Rect(cameraPanelPosition.x + 10, cameraY, 430, 20), 
                         $"{marker}{cameraNames[i]}", labelStyle);
                cameraY += 20;
            }
            
            GUI.Label(new Rect(cameraPanelPosition.x + 10, cameraY, 430, 20), 
                     $"Press {switchCameraKey} or 1-{Mathf.Min(availableCameras.Length, 9)} to switch cameras", labelStyle);
        }
        
        // Gesture instructions panel (separate, non-overlapping)
        GUI.Box(new Rect(gesturePanelPosition.x, gesturePanelPosition.y, 450, 100), 
                "🎮 Gesture Detection", boxStyle);
        
        float gestureY = gesturePanelPosition.y + 25;
        
        GUI.Label(new Rect(gesturePanelPosition.x + 10, gestureY, 430, 20), 
                 "COMPUTER VISION GESTURES:", labelStyle);
        gestureY += 20;
        
        GUI.Label(new Rect(gesturePanelPosition.x + 10, gestureY, 430, 20), 
                 "🟡 Medium Motion = Select | 🔴 High Motion = Grab", labelStyle);
        gestureY += 20;
        
        GUI.Label(new Rect(gesturePanelPosition.x + 10, gestureY, 430, 20), 
                 "⚫ No/Low Motion = Open Hand", labelStyle);
    }
    
    #endregion
    
    #region Public Interface
    
    public bool HasValidHand() => hasValidHand;
    public string GetCurrentGesture() => currentGesture;
    public Vector3 GetHandPosition() => currentHandPosition;
    public bool IsActive() => isWebCamActive;
    public float GetMotionIntensity() => motionIntensity;
    public int GetCurrentCameraIndex() => currentCameraIndex;
    public int GetCameraCount() => availableCameras?.Length ?? 0;
    public string GetCurrentCameraName() => 
        (availableCameras != null && currentCameraIndex < availableCameras.Length) 
        ? availableCameras[currentCameraIndex].name : "Unknown";
    
    public void SetMotionThreshold(float threshold)
    {
        motionThreshold = Mathf.Clamp(threshold, 0.01f, 1f);
    }
    
    public void SetHandDetection(bool enable)
    {
        enableHandDetection = enable;
    }
    
    // Cleanup
    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
        }
        
        CleanupComputerVisionTextures();
    }
    
    #endregion
}
