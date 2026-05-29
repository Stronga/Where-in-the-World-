using UnityEngine.EventSystems;
using UnityEngine;
using DG.Tweening;

public class DragRotate : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotationSensitivity = 4f;
    [SerializeField] private float zoomSensitivity = 5f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float introDistance = 25f;
    
    [Header("Auto Rotation")]
    [SerializeField] private float autoRotationSpeed = 15f;
    
    [Header("Hand Gesture Control")]
    [SerializeField] private bool handGestureEnabled = true;
    [SerializeField] private float handRotationSensitivity = 15f;
    [SerializeField] private float handRotationSmoothTime = 0.08f;
    [SerializeField] private float handReleaseEaseDuration = 0.2f;

    private Vector3 lastMousePosition;
    private Vector3 lastHandPosition;
    private Quaternion targetRotation;
    private float currentDistance;
    private float targetDistance;
    private float distanceVelocity = 0f;
    private float defaultDistance;
    private bool rotationEnabled = true;
    private bool mouseInputEnabled = true;
    private bool autoRotationEnabled = false;
    private bool hasUserInteracted = false;
    private bool isHandRotating = false;
    private Vector3 smoothedHandDelta = Vector3.zero;
    private Vector3 handDeltaVelocity = Vector3.zero;
    private Vector3 releaseHandDelta = Vector3.zero;
    private float releaseHandEaseRemaining = 0f;


    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Please assign a target to the DragRotate script!");
            return;
        }

        defaultDistance = (minDistance + maxDistance) / 2f;
        currentDistance = targetDistance = introDistance;
        transform.position = target.position - (Vector3.forward * currentDistance);
        transform.LookAt(target);
        targetRotation = target.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool overUI = EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject();

        // Check for user interaction
        if (mouseInputEnabled && !hasUserInteracted && !overUI)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) ||
                Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                hasUserInteracted = true;
                autoRotationEnabled = false;
            }
        }

        // Auto rotation when enabled
        if (autoRotationEnabled && !hasUserInteracted)
        {
            targetRotation *= Quaternion.AngleAxis(autoRotationSpeed * Time.deltaTime, Vector3.up);
        }

        if (!isHandRotating && releaseHandEaseRemaining > 0f && rotationEnabled && handGestureEnabled)
        {
            float duration = Mathf.Max(handReleaseEaseDuration, 0.001f);
            float ease = releaseHandEaseRemaining / duration;
            ApplyHandRotation(releaseHandDelta * ease * (Time.deltaTime / duration));
            releaseHandEaseRemaining = Mathf.Max(0f, releaseHandEaseRemaining - Time.deltaTime);
        }

        // Manual rotation
        if (rotationEnabled && mouseInputEnabled && !overUI && hasUserInteracted)
        {
            if (Input.GetMouseButtonDown(0))
                lastMousePosition = Input.mousePosition;
            else if (Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                float rotX = -delta.y * rotationSensitivity * Time.deltaTime;
                float rotY = -delta.x * rotationSensitivity * Time.deltaTime;
                targetRotation *= Quaternion.AngleAxis(rotY, Vector3.up);
                targetRotation *= Quaternion.AngleAxis(rotX, Vector3.right);
                lastMousePosition = Input.mousePosition;
            }
        }

        target.rotation = Quaternion.Slerp(
            target.rotation,
            targetRotation,
            Time.deltaTime / smoothTime
        );

        // Zoom functionality
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (rotationEnabled && mouseInputEnabled && scroll != 0 && !overUI)
        {
            targetDistance = Mathf.Clamp(
                targetDistance - scroll * zoomSensitivity,
                minDistance, maxDistance
            );
        }

        // Camera positioning
        currentDistance = Mathf.SmoothDamp(
            currentDistance, targetDistance, ref distanceVelocity, smoothTime
        );
        Vector3 dir = (transform.position - target.position).normalized;
        transform.position = target.position + dir * currentDistance;
        transform.LookAt(target);
    }

    public void ZoomToDefault()
    {
        targetDistance = defaultDistance;
    }

    public void ZoomToDefaultAnimated(float duration = 2f)
    {
        DOTween.To(() => targetDistance, x => targetDistance = x, defaultDistance, duration)
               .SetEase(Ease.OutQuad);
    }

    public void SetRotationEnabled(bool enabled)
    {
        rotationEnabled = enabled;
        if (!enabled && isHandRotating)
        {
            EndHandRotation();
        }
    }

    public void SetMouseInputEnabled(bool enabled)
    {
        mouseInputEnabled = enabled;
        if (!enabled)
        {
            lastMousePosition = Input.mousePosition;
        }
    }

    public void StartAutoRotation()
    {
        autoRotationEnabled = true;
        hasUserInteracted = false;
    }

    public void StopAutoRotation()
    {
        autoRotationEnabled = false;
    }

    public void ResetInteractionState()
    {
        hasUserInteracted = false;
        autoRotationEnabled = false;
    }
    
public void StartHandRotation(Vector3 handPosition)
{
    if (!handGestureEnabled || !rotationEnabled) return;
    
    isHandRotating = true;
    lastHandPosition = handPosition;
    smoothedHandDelta = Vector3.zero;
    handDeltaVelocity = Vector3.zero;
    releaseHandDelta = Vector3.zero;
    releaseHandEaseRemaining = 0f;
    hasUserInteracted = true;
    autoRotationEnabled = false;
    
    Debug.Log($"Started hand rotation at {handPosition}");
}

public void UpdateHandRotation(Vector3 handPosition)
{
    if (!isHandRotating || !handGestureEnabled || !rotationEnabled) return;
    
    Vector3 rawDelta = handPosition - lastHandPosition;
    smoothedHandDelta = handRotationSmoothTime > 0f
        ? Vector3.SmoothDamp(smoothedHandDelta, rawDelta, ref handDeltaVelocity, handRotationSmoothTime)
        : rawDelta;

    ApplyHandRotation(smoothedHandDelta);
    releaseHandDelta = smoothedHandDelta;
    releaseHandEaseRemaining = handReleaseEaseDuration;
    lastHandPosition = handPosition;
}

public void EndHandRotation()
{
    if (!isHandRotating) return;
    
    isHandRotating = false;
    Debug.Log("Ended hand rotation");
}

private void ApplyHandRotation(Vector3 delta)
{
    float rotX = -delta.y * handRotationSensitivity;
    float rotY = -delta.x * handRotationSensitivity;

    targetRotation *= Quaternion.AngleAxis(rotY, Vector3.up);
    targetRotation *= Quaternion.AngleAxis(rotX, Vector3.right);
}

public bool IsHandRotating()
{
    return isHandRotating;
}

public void SetHandGestureEnabled(bool enabled)
{
    handGestureEnabled = enabled;
    if (!enabled && isHandRotating)
    {
        EndHandRotation();
    }
}

}
