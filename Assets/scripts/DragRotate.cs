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
    [SerializeField] private float introDistance = 15f;
    
    private Vector3 lastMousePosition;
    private Quaternion targetRotation;
    private float currentDistance;
    private float targetDistance;
    private float distanceVelocity = 0f;
    private float defaultDistance;
    private bool rotationEnabled = true;

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

    // Process rotation input only if rotation is enabled.
    if (rotationEnabled)
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            
            float rotX = -delta.y * rotationSensitivity * Time.deltaTime;
            float rotY = -delta.x * rotationSensitivity * Time.deltaTime;

            targetRotation *= Quaternion.AngleAxis(rotY, Vector3.up);
            targetRotation *= Quaternion.AngleAxis(rotX, Vector3.right);
            
            lastMousePosition = Input.mousePosition;
        }
        // Only update the target rotation if we're allowed to rotate.
        target.rotation = Quaternion.Slerp(target.rotation, targetRotation, Time.deltaTime / smoothTime);
    }

    // Handle zoom input (mouse scroll)
    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if (scroll != 0)
    {
        targetDistance -= scroll * zoomSensitivity;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }

    // Always update camera distance
    currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, smoothTime);
    Vector3 direction = (transform.position - target.position).normalized;
    transform.position = target.position + direction * currentDistance;
}

    public void ZoomToDefault()
    {
        targetDistance = defaultDistance;
    }

    public void SetRotationEnabled(bool enabled)
    {
        rotationEnabled = enabled;
    }
}