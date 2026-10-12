using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneFlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DroneInputReader inputReader;

    [Header("Flight Mode")]
    [SerializeField] private bool stabilizedMode = true;

    [Header("Altitude Control")]
    [Tooltip("Altitude target movement speed while the climb stick is held.")]
    [SerializeField] private float climbRate = 1.5f;

    [Tooltip(
        "Lowest normal altitude target in world Y. " +
        "Holding descend after reaching this value enters landing mode.")]
    [SerializeField] private float minAltitude = 0.5f;

    [SerializeField] private float maxAltitude = 10f;

    [Tooltip("Position gain for altitude hold.")]
    [SerializeField] private float altitudeKp = 3.5f;

    [Tooltip("Vertical velocity tracking/damping gain.")]
    [SerializeField] private float altitudeKd = 3.5f;

    [Tooltip("Maximum commanded vertical acceleration in m/s².")]
    [SerializeField] private float maxVerticalAcceleration = 5f;

    [Tooltip("Maximum upward thrust acceleration before tilt compensation.")]
    [SerializeField] private float maxThrustAcceleration = 20f;

    [Tooltip("Maximum vertical speed in m/s.")]
    [SerializeField] private float maxVerticalSpeed = 2f;


    [Header("Landing")]
    [Tooltip("Vertical input magnitude required to count as an intentional climb or descent command.")]
    [SerializeField, Range(0.01f, 0.3f)]
    private float landingInputThreshold = 0.05f;

    [Tooltip("Controlled descent speed after landing mode begins.")]
    [SerializeField] private float landingDescentRate = 0.7f;

    [Tooltip("How quickly the drone matches the controlled landing descent speed.")]
    [SerializeField] private float landingVelocityResponse = 4f;

    [Tooltip("How close the altitude target must be to the current altitude before ground contact can enter landing mode.")]
    [SerializeField] private float landingAltitudeTolerance = 0.1f;

    [Tooltip("How long a valid ground contact remains valid after a physics step.")]
    [SerializeField] private float groundedGraceTime = 0.12f;

    [Tooltip("Minimum upward normal required for a collision to count as ground.")]
    [SerializeField, Range(0.1f, 1f)]
    private float groundNormalMinDot = 0.55f;

    [Tooltip("Extra damping used only while resting on the ground in landing mode.")]
    [SerializeField] private float groundedHorizontalDamping = 5f;

    [Tooltip("Lowest target altitude the pilot can command. Small, so the drone " +
         "can descend smoothly all the way down. Ground contact is handled " +
         "by the collider, not this value.")]
    [SerializeField] private float landingFloor = 0.05f;

    [Tooltip("When the drone's ACTUAL altitude drops below this value while the " +
            "pilot is still commanding descend, the landing controller engages.")]
    [SerializeField] private float landingEngageAltitude = 0.25f;


    [Header("Attitude Control")]
    [SerializeField, Range(5f, 35f)]
    private float maxTiltAngle = 15f;

    [SerializeField] private float maxYawRate = 50f;

    [Tooltip("Rotation position gain.")]
    [SerializeField] private float attitudeKp = 8f;

    [Tooltip("Rotation velocity gain.")]
    [SerializeField] private float attitudeKd = 5.5f;

    [Tooltip("Safety limit for requested angular acceleration.")]
    [SerializeField] private float maxAttitudeAcceleration = 20f;

    [Tooltip("Maximum rate at which pitch and roll targets change.")]
    [SerializeField] private float attitudeResponse = 45f;

    [Header("Horizontal Flight")]
    [Tooltip("Maximum requested horizontal speed in m/s.")]
    [SerializeField] private float maxHorizontalSpeed = 3f;

    [Tooltip("How strongly horizontal velocity error requests acceleration.")]
    [SerializeField] private float velocityResponse = 1.5f;

    [Tooltip("Maximum acceleration when building speed, in m/s².")]
    [SerializeField] private float horizontalAcceleration = 2f;

    [Tooltip("Maximum deceleration when braking, in m/s².")]
    [SerializeField] private float horizontalBraking = 3f;

    [Tooltip("Passive aerodynamic-style horizontal drag.")]
    [SerializeField] private float horizontalDamping = 0.2f;

    [Header("Safety")]
    [SerializeField] private bool startDisarmed = true;

    private Rigidbody rb;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float targetAltitude;
    private float targetYaw;

    private float currentTargetPitch;
    private float currentTargetRoll;

    private Quaternion previousDesiredRotation;
    private bool hasPreviousDesiredRotation;

    private float lastGroundContactTime = float.NegativeInfinity;

    private bool armed;
    private bool initialized;
    private bool landingRequested;

    public bool IsArmed => armed;
    public bool IsLanding => landingRequested;
    public bool IsGrounded =>
        Time.fixedTime - lastGroundContactTime <= groundedGraceTime;

    public float TargetAltitude => targetAltitude;
    public float CurrentAltitude =>
        rb != null ? rb.position.y : transform.position.y;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (inputReader == null)
        {
            Debug.LogError(
                "DroneFlightController: Assign DroneInputReader.",
                this);

            enabled = false;
            return;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        spawnPosition = rb.position;
        spawnRotation = rb.rotation;

        targetAltitude = Mathf.Clamp(
        Mathf.Max(rb.position.y, minAltitude),
        landingFloor,
        Mathf.Max(landingFloor, maxAltitude));
        targetYaw = rb.rotation.eulerAngles.y;

        currentTargetPitch = 0f;
        currentTargetRoll = 0f;

        armed = !startDisarmed;
        landingRequested = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = startDisarmed;
        rb.useGravity = true;

        ResetDesiredRotationHistory();

        inputReader.SetArmedState(armed);

        initialized = true;
    }

    private void OnEnable()
    {
        if (inputReader == null)
            return;

        inputReader.ArmPressed += ArmDrone;
        inputReader.DisarmPressed += DisarmDrone;
        inputReader.ResetPressed += ResetDrone;
    }

    private void OnDisable()
    {
        if (inputReader == null)
            return;

        inputReader.ArmPressed -= ArmDrone;
        inputReader.DisarmPressed -= DisarmDrone;
        inputReader.ResetPressed -= ResetDrone;
    }

    private void ArmDrone()
    {
        if (!initialized || armed)
            return;

        armed = true;

        targetAltitude = Mathf.Clamp(
        Mathf.Max(rb.position.y, minAltitude),
        landingFloor,
        Mathf.Max(landingFloor, maxAltitude));
        targetYaw = rb.rotation.eulerAngles.y;

        currentTargetPitch = 0f;
        currentTargetRoll = 0f;

        landingRequested = IsGrounded;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        ResetDesiredRotationHistory();

        inputReader.SetArmedState(true);
    }

    private void DisarmDrone()
    {
        if (!initialized || !armed)
            return;

        armed = false;
        landingRequested = false;

        // Motors stop. The Rigidbody remains physical and gravity remains active.
        rb.isKinematic = false;
        rb.useGravity = true;

        inputReader.SetArmedState(false);
    }

    private void ResetDrone()
    {
        if (!initialized)
            return;

        armed = false;
        landingRequested = false;
        lastGroundContactTime = float.NegativeInfinity;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
        rb.useGravity = true;

        rb.position = spawnPosition;
        rb.rotation = spawnRotation;

        targetAltitude = Mathf.Clamp(
        Mathf.Max(spawnPosition.y, minAltitude),
        landingFloor,
        Mathf.Max(landingFloor, maxAltitude));
        targetYaw = spawnRotation.eulerAngles.y;

        currentTargetPitch = 0f;
        currentTargetRoll = 0f;

        ResetDesiredRotationHistory();

        inputReader.SetArmedState(false);
    }

    private void FixedUpdate()
    {
        if (!initialized || !armed || rb.isKinematic)
            return;

        float dt = Time.fixedDeltaTime;

        Vector2 leftStick = inputReader.LeftStick;
        Vector2 rightStick = inputReader.RightStick;

        bool grounded = IsGrounded;

        float targetVerticalVelocity =
            UpdateAltitudeTarget(leftStick.y, grounded, dt);

        targetYaw = Mathf.Repeat(
            targetYaw + leftStick.x * maxYawRate * dt,
            360f);

        bool restingOnGround = landingRequested && grounded;

        Vector3 currentHorizontalVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z);

        Quaternion yawRotation =
            Quaternion.AngleAxis(targetYaw, Vector3.up);

        Vector3 desiredHorizontalVelocity = Vector3.zero;

        if (!restingOnGround)
        {
            Vector3 stickDirection = Vector3.ClampMagnitude(
                new Vector3(rightStick.x, 0f, rightStick.y),
                1f);

            desiredHorizontalVelocity =
                yawRotation * stickDirection * maxHorizontalSpeed;
        }

        Vector3 velocityError =
            desiredHorizontalVelocity - currentHorizontalVelocity;

        Vector3 desiredNetAcceleration =
            velocityError * velocityResponse;

        bool braking =
            currentHorizontalVelocity.sqrMagnitude > 0.0001f &&
            Vector3.Dot(
                velocityError,
                currentHorizontalVelocity) < 0f;

        float accelerationLimit = braking
            ? horizontalBraking
            : horizontalAcceleration;

        desiredNetAcceleration = Vector3.ClampMagnitude(
            desiredNetAcceleration,
            accelerationLimit);

        // Compensate passive drag so velocity control remains predictable.
        Vector3 requestedTiltAcceleration =
            desiredNetAcceleration +
            currentHorizontalVelocity * horizontalDamping;

        float gravity = Mathf.Max(Physics.gravity.magnitude, 0.01f);

        float maxTiltAcceleration =
            gravity * Mathf.Tan(maxTiltAngle * Mathf.Deg2Rad);

        requestedTiltAcceleration = Vector3.ClampMagnitude(
            requestedTiltAcceleration,
            maxTiltAcceleration);

        Vector3 yawForward = yawRotation * Vector3.forward;
        Vector3 yawRight = yawRotation * Vector3.right;

        float forwardAcceleration = Vector3.Dot(
            requestedTiltAcceleration,
            yawForward);

        float rightAcceleration = Vector3.Dot(
            requestedTiltAcceleration,
            yawRight);

        float requestedPitch = Mathf.Atan2(
            forwardAcceleration,
            gravity) * Mathf.Rad2Deg;

        float requestedRoll = -Mathf.Atan2(
            rightAcceleration,
            gravity) * Mathf.Rad2Deg;

        if (!stabilizedMode || restingOnGround)
        {
            requestedPitch = 0f;
            requestedRoll = 0f;
        }

        currentTargetPitch = Mathf.MoveTowards(
            currentTargetPitch,
            requestedPitch,
            attitudeResponse * dt);

        currentTargetRoll = Mathf.MoveTowards(
            currentTargetRoll,
            requestedRoll,
            attitudeResponse * dt);

        Quaternion desiredRotation =
            Quaternion.AngleAxis(targetYaw, Vector3.up) *
            Quaternion.Euler(
                currentTargetPitch,
                0f,
                currentTargetRoll);

        ApplyAttitudeControl(desiredRotation, dt);

        if (restingOnGround)
        {
            rb.AddForce(
                -currentHorizontalVelocity * groundedHorizontalDamping,
                ForceMode.Acceleration);

            LimitVelocity();
            return;
        }

        ApplyAltitudeControl(targetVerticalVelocity);

        rb.AddForce(
            -currentHorizontalVelocity * horizontalDamping,
            ForceMode.Acceleration);

        LimitVelocity();
    }

    private float UpdateAltitudeTarget(
        float verticalInput,
        bool grounded,
        float dt)
    {
        float targetBeforeInput = targetAltitude;

        bool climbing =
            verticalInput > landingInputThreshold;

        bool descending =
            verticalInput < -landingInputThreshold;

        if (climbing)
        {
            if (landingRequested)
            {
                landingRequested = false;

                targetAltitude = ClampAltitude(
                    Mathf.Max(targetAltitude, rb.position.y));

                targetBeforeInput = targetAltitude;
            }

            targetAltitude += verticalInput * climbRate * dt;
        }
        else if (descending)
        {
            targetAltitude += verticalInput * climbRate * dt;
        }

        // The target is now free to travel between landingFloor and maxAltitude.
        // minAltitude is no longer a hard floor — it is only the take-off/rest
        // altitude the drone returns to after arming.
        targetAltitude = Mathf.Clamp(
            targetAltitude,
            landingFloor,
            Mathf.Max(landingFloor, maxAltitude));

        // Engage landing only when the drone's ACTUAL altitude is near the ground
        // and the pilot is still asking to descend. This is what removes the
        // "single tap at rest altitude -> full landing" behaviour.
        if (descending && rb.position.y <= landingEngageAltitude)
        {
            landingRequested = true;
        }

        // Ground-contact fallback, unchanged in intent.
        if (grounded &&
            (landingRequested || descending) &&
            targetAltitude <= rb.position.y + landingAltitudeTolerance)
        {
            landingRequested = true;
        }

        return (targetAltitude - targetBeforeInput) /
               Mathf.Max(dt, 0.0001f);
    }

    private void ApplyAltitudeControl(float targetVerticalVelocity)
    {
        float gravity = Mathf.Max(Physics.gravity.magnitude, 0.01f);

        float desiredVerticalAcceleration;

        if (landingRequested)
        {
            // Landing mode intentionally bypasses normal minimum-altitude
            // hover behavior and continues a controlled descent until contact.
            float landingVelocity = -landingDescentRate;

            desiredVerticalAcceleration =
                (landingVelocity - rb.linearVelocity.y) *
                landingVelocityResponse;
        }
        else
        {
            float altitudeError =
                targetAltitude - rb.position.y;

            desiredVerticalAcceleration =
                altitudeError * altitudeKp +
                (targetVerticalVelocity - rb.linearVelocity.y) *
                altitudeKd;
        }

        desiredVerticalAcceleration = Mathf.Clamp(
            desiredVerticalAcceleration,
            -maxVerticalAcceleration,
            maxVerticalAcceleration);

        float upwardAlignment = Mathf.Max(
            Vector3.Dot(transform.up, Vector3.up),
            0.35f);

        float thrustAcceleration =
            (gravity + desiredVerticalAcceleration) /
            upwardAlignment;

        thrustAcceleration = Mathf.Clamp(
            thrustAcceleration,
            0f,
            maxThrustAcceleration);

        rb.AddForce(
            transform.up * thrustAcceleration,
            ForceMode.Acceleration);
    }

    private void ApplyAttitudeControl(
        Quaternion desiredRotation,
        float dt)
    {
        if (!hasPreviousDesiredRotation)
        {
            previousDesiredRotation = desiredRotation;
            hasPreviousDesiredRotation = true;
        }

        Vector3 desiredAngularVelocity =
            CalculateAngularVelocity(
                previousDesiredRotation,
                desiredRotation,
                dt);

        previousDesiredRotation = desiredRotation;

        Vector3 rotationError =
            GetRotationError(
                desiredRotation,
                rb.rotation);

        Vector3 angularVelocityError =
            desiredAngularVelocity - rb.angularVelocity;

        Vector3 torque =
            rotationError * attitudeKp +
            angularVelocityError * attitudeKd;

        torque = Vector3.ClampMagnitude(
            torque,
            maxAttitudeAcceleration);

        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    private static Vector3 GetRotationError(
        Quaternion desired,
        Quaternion current)
    {
        Quaternion error =
            desired * Quaternion.Inverse(current);

        if (error.w < 0f)
        {
            error.x = -error.x;
            error.y = -error.y;
            error.z = -error.z;
            error.w = -error.w;
        }

        error.ToAngleAxis(
            out float angle,
            out Vector3 axis);

        if (angle > 180f)
            angle -= 360f;

        if (axis.sqrMagnitude < 0.000001f ||
            Mathf.Abs(angle) < 0.001f)
        {
            return Vector3.zero;
        }

        return axis.normalized *
               (angle * Mathf.Deg2Rad);
    }

    private static Vector3 CalculateAngularVelocity(
        Quaternion from,
        Quaternion to,
        float dt)
    {
        Quaternion delta =
            to * Quaternion.Inverse(from);

        if (delta.w < 0f)
        {
            delta.x = -delta.x;
            delta.y = -delta.y;
            delta.z = -delta.z;
            delta.w = -delta.w;
        }

        delta.ToAngleAxis(
            out float angle,
            out Vector3 axis);

        if (angle > 180f)
            angle -= 360f;

        if (axis.sqrMagnitude < 0.000001f ||
            Mathf.Abs(angle) < 0.001f)
        {
            return Vector3.zero;
        }

        return axis.normalized *
               (angle * Mathf.Deg2Rad /
                Mathf.Max(dt, 0.0001f));
    }

    private void LimitVelocity()
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 horizontalVelocity = new Vector3(
            velocity.x,
            0f,
            velocity.z);

        if (horizontalVelocity.magnitude > maxHorizontalSpeed)
        {
            horizontalVelocity =
                horizontalVelocity.normalized *
                maxHorizontalSpeed;

            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }

        velocity.y = Mathf.Clamp(
            velocity.y,
            -maxVerticalSpeed,
            maxVerticalSpeed);

        rb.linearVelocity = velocity;
    }

    private void ResetDesiredRotationHistory()
    {
        previousDesiredRotation =
            Quaternion.AngleAxis(targetYaw, Vector3.up) *
            Quaternion.Euler(
                currentTargetPitch,
                0f,
                currentTargetRoll);

        hasPreviousDesiredRotation = true;
    }

    private float ClampAltitude(float altitude)
    {
        return Mathf.Clamp(
            altitude,
            landingFloor,
            Mathf.Max(landingFloor, maxAltitude));
    }

    private void OnCollisionEnter(Collision collision)
    {
        RegisterGroundContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        RegisterGroundContact(collision);
    }

    private void RegisterGroundContact(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            if (Vector3.Dot(
                    contact.normal,
                    Vector3.up) >= groundNormalMinDot)
            {
                lastGroundContactTime = Time.fixedTime;
                return;
            }
        }
    }

    private void OnValidate()
    {
        maxAltitude = Mathf.Max(maxAltitude, minAltitude);

        climbRate = Mathf.Max(0f, climbRate);
        maxVerticalSpeed = Mathf.Max(0.01f, maxVerticalSpeed);
        maxVerticalAcceleration = Mathf.Max(0.01f, maxVerticalAcceleration);
        maxThrustAcceleration = Mathf.Max(0.01f, maxThrustAcceleration);

        landingDescentRate = Mathf.Clamp(
            landingDescentRate,
            0.01f,
            maxVerticalSpeed);

        landingVelocityResponse =
            Mathf.Max(0.01f, landingVelocityResponse);

        horizontalAcceleration =
            Mathf.Max(0f, horizontalAcceleration);

        horizontalBraking =
            Mathf.Max(0f, horizontalBraking);

        horizontalDamping =
            Mathf.Max(0f, horizontalDamping);

        maxHorizontalSpeed =
            Mathf.Max(0f, maxHorizontalSpeed);

        attitudeResponse =
            Mathf.Max(0f, attitudeResponse);

        maxAttitudeAcceleration =
            Mathf.Max(0.01f, maxAttitudeAcceleration);

        landingFloor = Mathf.Clamp(landingFloor, 0.01f, 1f);
        landingEngageAltitude = Mathf.Max(landingEngageAltitude, landingFloor);
        maxAltitude = Mathf.Max(maxAltitude, minAltitude);
    }
}



