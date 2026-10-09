
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class DroneFlightPrototype : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private TMP_Text statusText;

    [Header("Propellers - Visual Only")]
    [SerializeField] private Transform flPropeller;
    [SerializeField] private Transform frPropeller;
    [SerializeField] private Transform rlPropeller;
    [SerializeField] private Transform rrPropeller;
    [SerializeField] private float propellerRPM = 1800f;

    [Header("Stabilized Flight")]
    [SerializeField, Range(5f, 35f)] private float maxTiltAngle = 20f;
    [SerializeField] private float maxYawRate = 70f;
    [SerializeField] private float climbRate = 1.5f;
    [SerializeField] private float altitudeKp = 4f;
    [SerializeField] private float altitudeKd = 3f;
    [SerializeField] private float attitudeKp = 12f;
    [SerializeField] private float attitudeKd = 4.5f;
    [SerializeField] private float horizontalDamping = 0.8f;
    [SerializeField] private float stickDeadzone = 0.08f;

    private Rigidbody rb;
    private InputActionMap droneMap;
    private InputAction leftStick;
    private InputAction rightStick;
    private InputAction armAction;
    private InputAction disarmAction;
    private InputAction resetAction;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float targetAltitude;
    private float targetYaw;
    private float pitchTarget;
    private float rollTarget;
    private bool armed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (inputActions == null)
        {
            Debug.LogError("Assign DroneInputActions to DroneFlightPrototype.");
            enabled = false;
            return;
        }

        droneMap = inputActions.FindActionMap("Drone");

        if (droneMap == null)
        {
            Debug.LogError("The Drone action map was not found.");
            enabled = false;
            return;
        }

        leftStick = droneMap.FindAction("LeftStick");
        rightStick = droneMap.FindAction("RightStick");
        armAction = droneMap.FindAction("Arm");
        disarmAction = droneMap.FindAction("Disarm");
        resetAction = droneMap.FindAction("ResetDrone");

        if (leftStick == null || rightStick == null ||
            armAction == null || disarmAction == null ||
            resetAction == null)
        {
            Debug.LogError("One or more required Drone actions are missing.");
            enabled = false;
        }
    }

    private void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        targetAltitude = rb.position.y;
        targetYaw = rb.rotation.eulerAngles.y;

        // Safe testing: hold the drone still until explicitly armed.
        armed = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void OnEnable()
    {
        if (droneMap == null) return;

        armAction.performed += OnArm;
        disarmAction.performed += OnDisarm;
        resetAction.performed += OnReset;

        droneMap.Enable();
    }

    private void OnDisable()
    {
        if (droneMap == null) return;

        armAction.performed -= OnArm;
        disarmAction.performed -= OnDisarm;
        resetAction.performed -= OnReset;

        droneMap.Disable();
    }

    private void OnArm(InputAction.CallbackContext context)
    {
        if (armed) return;

        armed = true;
        targetAltitude = rb.position.y;
        targetYaw = rb.rotation.eulerAngles.y;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void OnDisarm(InputAction.CallbackContext context)
    {
        DisarmDrone();
    }

    private void DisarmDrone()
    {
        armed = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void OnReset(InputAction.CallbackContext context)
    {
        DisarmDrone();

        rb.position = spawnPosition;
        rb.rotation = spawnRotation;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        targetAltitude = spawnPosition.y;
        targetYaw = spawnRotation.eulerAngles.y;
        pitchTarget = 0f;
        rollTarget = 0f;
    }

    private static Vector2 ApplyDeadzone(Vector2 value, float deadzone)
    {
        if (value.magnitude < deadzone)
            return Vector2.zero;

        return Vector2.ClampMagnitude(value, 1f);
    }

    private void FixedUpdate()
    {
        if (!armed || rb.isKinematic) return;

        float dt = Time.fixedDeltaTime;

        Vector2 left = ApplyDeadzone(
            leftStick.ReadValue<Vector2>(), stickDeadzone);

        Vector2 right = ApplyDeadzone(
            rightStick.ReadValue<Vector2>(), stickDeadzone);

        // Left stick Y: change the altitude target.
        targetAltitude += left.y * climbRate * dt;

        // Left stick X: command a yaw rate.
        targetYaw += left.x * maxYawRate * dt;

        // Right stick: desired pitch and roll.
        pitchTarget = right.y * maxTiltAngle;
        rollTarget = -right.x * maxTiltAngle;

        Quaternion desiredRotation = Quaternion.Euler(
            pitchTarget, targetYaw, rollTarget);

        Quaternion errorRotation =
            desiredRotation * Quaternion.Inverse(rb.rotation);

        if (errorRotation.w < 0f)
        {
            errorRotation.x *= -1f;
            errorRotation.y *= -1f;
            errorRotation.z *= -1f;
            errorRotation.w *= -1f;
        }

        errorRotation.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
            angle -= 360f;

        Vector3 errorWorld = axis * angle * Mathf.Deg2Rad;
        Vector3 errorLocal = transform.InverseTransformDirection(errorWorld);
        Vector3 angularVelocityLocal =
            transform.InverseTransformDirection(rb.angularVelocity);

        Vector3 torque =
            errorLocal * attitudeKp -
            angularVelocityLocal * attitudeKd;

        rb.AddRelativeTorque(torque, ForceMode.Acceleration);

        // Altitude hold: compensate gravity and correct altitude error.

        float altitudeError = targetAltitude - rb.position.y;

        float desiredVerticalAcceleration =
            altitudeError * altitudeKp -
            rb.linearVelocity.y * altitudeKd;

        float upwardAlignment = Mathf.Max(
            Vector3.Dot(transform.up, Vector3.up),
            0.3f
        );

        float thrustAcceleration =
            (desiredVerticalAcceleration + Physics.gravity.magnitude)
            / upwardAlignment;

        rb.AddForce(
            transform.up * thrustAcceleration,
            ForceMode.Acceleration
        );

        Vector3 horizontalVelocity =
            new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(
            -horizontalVelocity * horizontalDamping,
            ForceMode.Acceleration
        );

    }

    private void Update()
    {
        UpdatePropellers();

        if (statusText == null || droneMap == null) return;

        Vector2 left = ApplyDeadzone(
            leftStick.ReadValue<Vector2>(), stickDeadzone);

        Vector2 right = ApplyDeadzone(
            rightStick.ReadValue<Vector2>(), stickDeadzone);

        statusText.text =
            $"SPARK FLIGHT TEST\n" +
            $"STATE: {(armed ? "ARMED" : "DISARMED")}\n" +
            $"LEFT  X: {left.x:F2}  Y: {left.y:F2}\n" +
            $"RIGHT X: {right.x:F2}  Y: {right.y:F2}\n" +
            $"ALTITUDE: {transform.position.y:F2} m\n" +
            $"X: YAW | Y: ALTITUDE\n" +
            $"RIGHT: PITCH / ROLL\n" +
            $"X (LEFT): ARM | A (RIGHT): DISARM\n" +
            $"Y (LEFT): RESET";
    }

    private void UpdatePropellers()
    {
        if (!armed) return;

        float angle = propellerRPM * 6f * Time.deltaTime;

        // Visual spin only; these do not generate physical lift.
        if (flPropeller != null)
            flPropeller.Rotate(Vector3.up, angle, Space.Self);

        if (rrPropeller != null)
            rrPropeller.Rotate(Vector3.up, angle, Space.Self);

        if (frPropeller != null)
            frPropeller.Rotate(Vector3.up, -angle, Space.Self);

        if (rlPropeller != null)
            rlPropeller.Rotate(Vector3.up, -angle, Space.Self);
    }
}
