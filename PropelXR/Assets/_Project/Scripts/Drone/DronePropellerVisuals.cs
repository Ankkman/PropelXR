
using UnityEngine;

public class DronePropellerVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DroneFlightController flightController;

    [SerializeField] private Transform flPropeller;
    [SerializeField] private Transform frPropeller;
    [SerializeField] private Transform rlPropeller;
    [SerializeField] private Transform rrPropeller;

    [Header("Visual Rotation")]
    [SerializeField, Min(0f)] private float idleRPM = 0f;
    [SerializeField, Min(0f)] private float flightRPM = 1800f;
    [SerializeField, Min(0f)] private float spinUpSpeed = 1200f;

    private float currentRPM;

    private void Update()
    {
        if (flightController == null)
            return;

        float targetRPM = flightController.IsArmed
            ? flightRPM
            : idleRPM;

        currentRPM = Mathf.MoveTowards(
            currentRPM,
            targetRPM,
            spinUpSpeed * Time.deltaTime
        );

        float angle = currentRPM * 6f * Time.deltaTime;

        // Visual rotation only. These rotations do not
        // generate physical thrust or torque.
        RotatePropeller(flPropeller, angle);
        RotatePropeller(rrPropeller, angle);
        RotatePropeller(frPropeller, -angle);
        RotatePropeller(rlPropeller, -angle);
    }

    private static void RotatePropeller(
        Transform propeller,
        float angle)
    {
        if (propeller != null)
            propeller.Rotate(Vector3.up, angle, Space.Self);
    }
}
