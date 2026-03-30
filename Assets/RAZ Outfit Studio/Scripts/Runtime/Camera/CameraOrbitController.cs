using UnityEngine;

[AddComponentMenu("RAZ Outfit Studio/Camera Orbit Controller")]
public class CameraOrbitController : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float rotateSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    [Header("Current Values")]
    public Vector3 targetPosition = Vector3.zero;
    public float currentDistance = 5f;
    public float currentRotation = 0f;
    public float currentHeight = 1.5f;

    private bool isDragging = false;
    private Vector3 lastMousePosition;

    private void Start()
    {
        UpdateCameraPosition();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            currentRotation += delta.x * rotateSpeed * 0.1f;
            currentHeight += delta.y * rotateSpeed * 0.05f;
            currentHeight = Mathf.Clamp(currentHeight, -2f, 3f);

            UpdateCameraPosition();
            lastMousePosition = Input.mousePosition;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            UpdateCameraPosition();
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 position = new Vector3(
            Mathf.Sin(currentRotation * Mathf.Deg2Rad) * currentDistance,
            currentHeight,
            Mathf.Cos(currentRotation * Mathf.Deg2Rad) * currentDistance
        );

        transform.position = position + targetPosition;
        transform.LookAt(targetPosition);
    }

    public void SetCameraPosition(float distance, float rotation, float height)
    {
        currentDistance = distance;
        currentRotation = rotation;
        currentHeight = height;
        UpdateCameraPosition();
    }
}
