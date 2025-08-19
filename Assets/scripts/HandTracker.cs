using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple hand tracker that simulates hand gestures using keyboard input
/// Now supports continuous gesture detection for proper dragging
/// </summary>
public class HandTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RawImage previewImage; // Optional: For webcam preview (not used in simulation)
    
    [Header("Simulation Settings")]
    [SerializeField] private bool useKeyboardSimulation = true;
    [SerializeField] private bool showInstructions = true;
    
    [Header("Gesture Settings")]
    [SerializeField] private float projectionDepth = 5f; // Depth for hand position projection
    
    private string lastGesture = "open";
    private string currentGesture = "open";
    
    void Update()
    {
        if (useKeyboardSimulation && gameManager != null)
        {
            // Use mouse position as simulated hand position
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, projectionDepth)
            );
            
            // Detect current keyboard gesture
            currentGesture = DetectKeyboardGesture();
            
            // Handle gesture logic
            HandleGestureStates(mouseWorldPos);
        }
    }
    
    void HandleGestureStates(Vector3 mouseWorldPos)
    {
        // Handle gesture start (when gesture changes from open to something else)
        if (lastGesture == "open" && currentGesture != "open")
        {
            Debug.Log($"Started gesture: {currentGesture} at {mouseWorldPos}");
            gameManager.HandleHandInput(mouseWorldPos, currentGesture);
        }
        // Handle continuous gesture (same gesture, but position might change)
        else if (currentGesture != "open" && currentGesture == lastGesture)
        {
            // For grab gesture, continuously update position for dragging
            if (currentGesture == "grab")
            {
                gameManager.HandleHandInput(mouseWorldPos, "grab");
            }
        }
        // Handle gesture end (when gesture changes from something to open)
        else if (lastGesture != "open" && currentGesture == "open")
        {
            Debug.Log($"Ended gesture: {lastGesture}");
            // If we were grabbing, place the object
            if (lastGesture == "grab")
            {
                gameManager.HandleHandInput(mouseWorldPos, "place");
            }
        }
        
        lastGesture = currentGesture;
    }
    
    string DetectKeyboardGesture()
    {
        // Map keyboard keys to gestures
        if (Input.GetKey(KeyCode.Space)) // Hold space for pinch/select
        {
            return "select";
        }
        else if (Input.GetKey(KeyCode.G)) // Hold G for grab
        {
            return "grab";
        }
        else if (Input.GetKey(KeyCode.P)) // Hold P for place
        {
            return "place";
        }
        
        return "open"; // Default open hand
    }
    
    void OnGUI()
    {
        if (showInstructions && useKeyboardSimulation)
        {
            // Display instructions on screen
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 12;
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            
            GUI.Box(new UnityEngine.Rect(10, 250, 320, 140), "Hand Tracker - Keyboard Simulation", boxStyle);
            GUI.Label(new UnityEngine.Rect(20, 275, 300, 20), "SPACE (hold) = Point/Select landmark from UI", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 295, 300, 20), "G (hold + drag) = Grab and drag landmark", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 315, 300, 20), "Release G = Place landmark", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 335, 300, 20), "Mouse = Hand position", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 355, 300, 20), $"Current gesture: {currentGesture}", labelStyle);
            GUI.Label(new UnityEngine.Rect(20, 375, 300, 20), $"Last gesture: {lastGesture}", labelStyle);
        }
    }
    
    // Public methods for external control
    public void SetSimulationMode(bool enabled)
    {
        useKeyboardSimulation = enabled;
    }
    
    public void ShowInstructions(bool show)
    {
        showInstructions = show;
    }
    
    void OnDestroy()
    {
        // Clean up any resources if needed
        Debug.Log("HandTracker destroyed");
    }
}
