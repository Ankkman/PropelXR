using UnityEngine;

public class PropellerSpinner : MonoBehaviour
{
    [Header("Propeller Settings")]
    [Tooltip("The speed at which the propellers rotate.")]
    public float rotationSpeed = 1500f;

    [Header("Propeller GameObjects")]
    public Transform frontLeftProp;
    public Transform frontRightProp;
    public Transform rearLeftProp;
    public Transform rearRightProp;

    void Update()
    {
        // Calculate rotation step based on frame rate
        float rotationStep = rotationSpeed * Time.deltaTime;

        // Spin Front Left and Rear Right Counter-Clockwise (CCW)
        if (frontLeftProp != null)  frontLeftProp.Rotate(Vector3.up, rotationStep);
        if (rearRightProp != null)  rearRightProp.Rotate(Vector3.up, rotationStep);

        // Spin Front Right and Rear Left Clockwise (CW)
        if (frontRightProp != null) frontRightProp.Rotate(Vector3.up, -rotationStep);
        if (rearLeftProp != null)   rearLeftProp.Rotate(Vector3.up, -rotationStep);
    }
}
