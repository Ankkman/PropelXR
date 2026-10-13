using UnityEngine;

public class RealisticVRHUD : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private Transform vrCamera;
    
    [Header("HUD Distance & Position")]
    [SerializeField] private float forwardDistance = 1.2f; // Distance from your eyes
    [SerializeField] private float downwardOffset = -0.15f; // Slightly below center line

    [Header("Realism Settings")]
    [SerializeField] private float followSpeed = 8.0f;     // Speed of the "lazy" catch-up (lower = lazier)
    [SerializeField] private float rotationSpeed = 8.0f;   // Speed of rotation alignment
    [SerializeField] private float subtleTiltFactor = 0.5f; // How much the HUD tilts when turning quickly

    private Vector3 currentVelocity;

    void Start()
    {
        // Automatically find the main VR camera if not assigned
        if (vrCamera == null && Camera.main != null)
        {
            vrCamera = Camera.main.transform;
        }

        if (vrCamera != null)
        {
            // Snap instantly to starting position on launch
            transform.position = CalculateTargetPosition();
            transform.rotation = vrCamera.rotation;
        }
    }

    void LateUpdate()
    {
        if (vrCamera == null) return;

        // 1. Smoothly interpolate position (Lazy HUD tracking)
        Vector3 targetPosition = CalculateTargetPosition();
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // 2. Smoothly interpolate rotation so it always faces your eyes
        Quaternion targetRotation = vrCamera.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    private Vector3 CalculateTargetPosition()
    {
        // Calculate the ideal spot directly in front of the player's face
        Vector3 forwardTarget = vrCamera.position + (vrCamera.forward * forwardDistance);
        Vector3 verticalTarget = vrCamera.up * downwardOffset;
        return forwardTarget + verticalTarget;
    }
}
