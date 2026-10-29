// using UnityEngine;

// [RequireComponent(typeof(Rigidbody))]
// public class DroneFlightController : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private DroneInputReader inputReader;

//     [Header("Flight Mode")]
//     [SerializeField] private bool stabilizedMode = true;

//     [Header("Altitude Control")]
//     [Tooltip("Altitude target movement speed while the climb stick is held.")]
//     [SerializeField] private float climbRate = 1.5f;

//     [Tooltip(
//         "Lowest normal altitude target in world Y. " +
//         "Holding descend after reaching this value enters landing mode.")]
//     [SerializeField] private float minAltitude = 0.5f;

//     [SerializeField] private float maxAltitude = 10f;

//     [Tooltip("Position gain for altitude hold.")]
//     [SerializeField] private float altitudeKp = 3.5f;

//     [Tooltip("Vertical velocity tracking/damping gain.")]
//     [SerializeField] private float altitudeKd = 3.5f;

//     [Tooltip("Maximum commanded vertical acceleration in m/s².")]
//     [SerializeField] private float maxVerticalAcceleration = 5f;

//     [Tooltip("Maximum upward thrust acceleration before tilt compensation.")]
//     [SerializeField] private float maxThrustAcceleration = 20f;

//     [Tooltip("Maximum vertical speed in m/s.")]
//     [SerializeField] private float maxVerticalSpeed = 2f;


//     [Header("Landing")]
//     [Tooltip("Vertical input magnitude required to count as an intentional climb or descent command.")]
//     [SerializeField, Range(0.01f, 0.3f)]
//     private float landingInputThreshold = 0.05f;

//     [Tooltip("Controlled descent speed after landing mode begins.")]
//     [SerializeField] private float landingDescentRate = 0.7f;

//     [Tooltip("How quickly the drone matches the controlled landing descent speed.")]
//     [SerializeField] private float landingVelocityResponse = 4f;

//     [Tooltip("How close the altitude target must be to the current altitude before ground contact can enter landing mode.")]
//     [SerializeField] private float landingAltitudeTolerance = 0.1f;

//     [Tooltip("How long a valid ground contact remains valid after a physics step.")]
//     [SerializeField] private float groundedGraceTime = 0.12f;

//     [Tooltip("Minimum upward normal required for a collision to count as ground.")]
//     [SerializeField, Range(0.1f, 1f)]
//     private float groundNormalMinDot = 0.55f;

//     [Tooltip("Extra damping used only while resting on the ground in landing mode.")]
//     [SerializeField] private float groundedHorizontalDamping = 5f;

//     [Tooltip("Lowest target altitude the pilot can command. Small, so the drone " +
//          "can descend smoothly all the way down. Ground contact is handled " +
//          "by the collider, not this value.")]
//     [SerializeField] private float landingFloor = 0.05f;

//     [Tooltip("When the drone's ACTUAL altitude drops below this value while the " +
//             "pilot is still commanding descend, the landing controller engages.")]
//     [SerializeField] private float landingEngageAltitude = 0.25f;


//     [Header("Attitude Control")]
//     [SerializeField, Range(5f, 35f)]
//     private float maxTiltAngle = 15f;

//     [SerializeField] private float maxYawRate = 50f;

//     [Tooltip("Rotation position gain.")]
//     [SerializeField] private float attitudeKp = 8f;

//     [Tooltip("Rotation velocity gain.")]
//     [SerializeField] private float attitudeKd = 5.5f;

//     [Tooltip("Safety limit for requested angular acceleration.")]
//     [SerializeField] private float maxAttitudeAcceleration = 20f;

//     [Tooltip("Maximum rate at which pitch and roll targets change.")]
//     [SerializeField] private float attitudeResponse = 45f;

//     [Header("Horizontal Flight")]
//     [Tooltip("Maximum requested horizontal speed in m/s.")]
//     [SerializeField] private float maxHorizontalSpeed = 3f;

//     [Tooltip("How strongly horizontal velocity error requests acceleration.")]
//     [SerializeField] private float velocityResponse = 1.5f;

//     [Tooltip("Maximum acceleration when building speed, in m/s².")]
//     [SerializeField] private float horizontalAcceleration = 2f;

//     [Tooltip("Maximum deceleration when braking, in m/s².")]
//     [SerializeField] private float horizontalBraking = 3f;

//     [Tooltip("Passive aerodynamic-style horizontal drag.")]
//     [SerializeField] private float horizontalDamping = 0.2f;

//     [Header("Safety")]
//     [SerializeField] private bool startDisarmed = true;

//     private Rigidbody rb;

//     private Vector3 spawnPosition;
//     private Quaternion spawnRotation;

//     private float targetAltitude;
//     private float targetYaw;

//     private float currentTargetPitch;
//     private float currentTargetRoll;

//     private Quaternion previousDesiredRotation;
//     private bool hasPreviousDesiredRotation;

//     private float lastGroundContactTime = float.NegativeInfinity;

//     private bool armed;
//     private bool initialized;
//     private bool landingRequested;

//     public bool IsArmed => armed;
//     public bool IsLanding => landingRequested;
//     public bool IsGrounded =>
//         Time.fixedTime - lastGroundContactTime <= groundedGraceTime;

//     public float TargetAltitude => targetAltitude;
//     public float CurrentAltitude =>
//         rb != null ? rb.position.y : transform.position.y;

//     private void Awake()
//     {
//         rb = GetComponent<Rigidbody>();

//         if (inputReader == null)
//         {
//             Debug.LogError(
//                 "DroneFlightController: Assign DroneInputReader.",
//                 this);

//             enabled = false;
//             return;
//         }

//         rb.interpolation = RigidbodyInterpolation.Interpolate;
//         rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
//     }

//     private void Start()
//     {
//         spawnPosition = rb.position;
//         spawnRotation = rb.rotation;

//         targetAltitude = Mathf.Clamp(
//         Mathf.Max(rb.position.y, minAltitude),
//         landingFloor,
//         Mathf.Max(landingFloor, maxAltitude));
//         targetYaw = rb.rotation.eulerAngles.y;

//         currentTargetPitch = 0f;
//         currentTargetRoll = 0f;

//         armed = !startDisarmed;
//         landingRequested = false;

//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         rb.isKinematic = startDisarmed;
//         rb.useGravity = true;

//         ResetDesiredRotationHistory();

//         inputReader.SetArmedState(armed);

//         initialized = true;
//     }

//     private void OnEnable()
//     {
//         if (inputReader == null)
//             return;

//         inputReader.ArmPressed += ArmDrone;
//         inputReader.DisarmPressed += DisarmDrone;
//         inputReader.ResetPressed += ResetDrone;
//     }

//     private void OnDisable()
//     {
//         if (inputReader == null)
//             return;

//         inputReader.ArmPressed -= ArmDrone;
//         inputReader.DisarmPressed -= DisarmDrone;
//         inputReader.ResetPressed -= ResetDrone;
//     }

//     private void ArmDrone()
//     {
//         if (!initialized || armed)
//             return;

//         armed = true;

//         targetAltitude = Mathf.Clamp(
//         Mathf.Max(rb.position.y, minAltitude),
//         landingFloor,
//         Mathf.Max(landingFloor, maxAltitude));
//         targetYaw = rb.rotation.eulerAngles.y;

//         currentTargetPitch = 0f;
//         currentTargetRoll = 0f;

//         landingRequested = IsGrounded;

//         rb.isKinematic = false;
//         rb.useGravity = true;
//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         ResetDesiredRotationHistory();

//         inputReader.SetArmedState(true);
//     }

//     private void DisarmDrone()
//     {
//         if (!initialized || !armed)
//             return;

//         armed = false;
//         landingRequested = false;

//         // Motors stop. The Rigidbody remains physical and gravity remains active.
//         rb.isKinematic = false;
//         rb.useGravity = true;

//         inputReader.SetArmedState(false);
//     }

//     private void ResetDrone()
//     {
//         if (!initialized)
//             return;

//         armed = false;
//         landingRequested = false;
//         lastGroundContactTime = float.NegativeInfinity;

//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         rb.isKinematic = true;
//         rb.useGravity = true;

//         rb.position = spawnPosition;
//         rb.rotation = spawnRotation;

//         targetAltitude = Mathf.Clamp(
//         Mathf.Max(spawnPosition.y, minAltitude),
//         landingFloor,
//         Mathf.Max(landingFloor, maxAltitude));
//         targetYaw = spawnRotation.eulerAngles.y;

//         currentTargetPitch = 0f;
//         currentTargetRoll = 0f;

//         ResetDesiredRotationHistory();

//         inputReader.SetArmedState(false);
//     }

//     private void FixedUpdate()
//     {
//         if (!initialized || !armed || rb.isKinematic)
//             return;

//         float dt = Time.fixedDeltaTime;

//         Vector2 leftStick = inputReader.LeftStick;
//         Vector2 rightStick = inputReader.RightStick;

//         bool grounded = IsGrounded;

//         float targetVerticalVelocity =
//             UpdateAltitudeTarget(leftStick.y, grounded, dt);

//         targetYaw = Mathf.Repeat(
//             targetYaw + leftStick.x * maxYawRate * dt,
//             360f);

//         bool restingOnGround = landingRequested && grounded;

//         Vector3 currentHorizontalVelocity = new Vector3(
//             rb.linearVelocity.x,
//             0f,
//             rb.linearVelocity.z);

//         Quaternion yawRotation =
//             Quaternion.AngleAxis(targetYaw, Vector3.up);

//         Vector3 desiredHorizontalVelocity = Vector3.zero;

//         if (!restingOnGround)
//         {
//             Vector3 stickDirection = Vector3.ClampMagnitude(
//                 new Vector3(rightStick.x, 0f, rightStick.y),
//                 1f);

//             desiredHorizontalVelocity =
//                 yawRotation * stickDirection * maxHorizontalSpeed;
//         }

//         Vector3 velocityError =
//             desiredHorizontalVelocity - currentHorizontalVelocity;

//         Vector3 desiredNetAcceleration =
//             velocityError * velocityResponse;

//         bool braking =
//             currentHorizontalVelocity.sqrMagnitude > 0.0001f &&
//             Vector3.Dot(
//                 velocityError,
//                 currentHorizontalVelocity) < 0f;

//         float accelerationLimit = braking
//             ? horizontalBraking
//             : horizontalAcceleration;

//         desiredNetAcceleration = Vector3.ClampMagnitude(
//             desiredNetAcceleration,
//             accelerationLimit);

//         // Compensate passive drag so velocity control remains predictable.
//         Vector3 requestedTiltAcceleration =
//             desiredNetAcceleration +
//             currentHorizontalVelocity * horizontalDamping;

//         float gravity = Mathf.Max(Physics.gravity.magnitude, 0.01f);

//         float maxTiltAcceleration =
//             gravity * Mathf.Tan(maxTiltAngle * Mathf.Deg2Rad);

//         requestedTiltAcceleration = Vector3.ClampMagnitude(
//             requestedTiltAcceleration,
//             maxTiltAcceleration);

//         Vector3 yawForward = yawRotation * Vector3.forward;
//         Vector3 yawRight = yawRotation * Vector3.right;

//         float forwardAcceleration = Vector3.Dot(
//             requestedTiltAcceleration,
//             yawForward);

//         float rightAcceleration = Vector3.Dot(
//             requestedTiltAcceleration,
//             yawRight);

//         float requestedPitch = Mathf.Atan2(
//             forwardAcceleration,
//             gravity) * Mathf.Rad2Deg;

//         float requestedRoll = -Mathf.Atan2(
//             rightAcceleration,
//             gravity) * Mathf.Rad2Deg;

//         if (!stabilizedMode || restingOnGround)
//         {
//             requestedPitch = 0f;
//             requestedRoll = 0f;
//         }

//         currentTargetPitch = Mathf.MoveTowards(
//             currentTargetPitch,
//             requestedPitch,
//             attitudeResponse * dt);

//         currentTargetRoll = Mathf.MoveTowards(
//             currentTargetRoll,
//             requestedRoll,
//             attitudeResponse * dt);

//         Quaternion desiredRotation =
//             Quaternion.AngleAxis(targetYaw, Vector3.up) *
//             Quaternion.Euler(
//                 currentTargetPitch,
//                 0f,
//                 currentTargetRoll);

//         ApplyAttitudeControl(desiredRotation, dt);

//         if (restingOnGround)
//         {
//             rb.AddForce(
//                 -currentHorizontalVelocity * groundedHorizontalDamping,
//                 ForceMode.Acceleration);

//             LimitVelocity();
//             return;
//         }

//         ApplyAltitudeControl(targetVerticalVelocity);

//         rb.AddForce(
//             -currentHorizontalVelocity * horizontalDamping,
//             ForceMode.Acceleration);

//         LimitVelocity();
//     }

//     private float UpdateAltitudeTarget(
//         float verticalInput,
//         bool grounded,
//         float dt)
//     {
//         float targetBeforeInput = targetAltitude;

//         bool climbing =
//             verticalInput > landingInputThreshold;

//         bool descending =
//             verticalInput < -landingInputThreshold;

//         if (climbing)
//         {
//             if (landingRequested)
//             {
//                 landingRequested = false;

//                 targetAltitude = ClampAltitude(
//                     Mathf.Max(targetAltitude, rb.position.y));

//                 targetBeforeInput = targetAltitude;
//             }

//             targetAltitude += verticalInput * climbRate * dt;
//         }
//         else if (descending)
//         {
//             targetAltitude += verticalInput * climbRate * dt;
//         }

//         // The target is now free to travel between landingFloor and maxAltitude.
//         // minAltitude is no longer a hard floor — it is only the take-off/rest
//         // altitude the drone returns to after arming.
//         targetAltitude = Mathf.Clamp(
//             targetAltitude,
//             landingFloor,
//             Mathf.Max(landingFloor, maxAltitude));

//         // Engage landing only when the drone's ACTUAL altitude is near the ground
//         // and the pilot is still asking to descend. This is what removes the
//         // "single tap at rest altitude -> full landing" behaviour.
//         if (descending && rb.position.y <= landingEngageAltitude)
//         {
//             landingRequested = true;
//         }

//         // Ground-contact fallback, unchanged in intent.
//         if (grounded &&
//             (landingRequested || descending) &&
//             targetAltitude <= rb.position.y + landingAltitudeTolerance)
//         {
//             landingRequested = true;
//         }

//         return (targetAltitude - targetBeforeInput) /
//                Mathf.Max(dt, 0.0001f);
//     }

//     private void ApplyAltitudeControl(float targetVerticalVelocity)
//     {
//         float gravity = Mathf.Max(Physics.gravity.magnitude, 0.01f);

//         float desiredVerticalAcceleration;

//         if (landingRequested)
//         {
//             // Landing mode intentionally bypasses normal minimum-altitude
//             // hover behavior and continues a controlled descent until contact.
//             float landingVelocity = -landingDescentRate;

//             desiredVerticalAcceleration =
//                 (landingVelocity - rb.linearVelocity.y) *
//                 landingVelocityResponse;
//         }
//         else
//         {
//             float altitudeError =
//                 targetAltitude - rb.position.y;

//             desiredVerticalAcceleration =
//                 altitudeError * altitudeKp +
//                 (targetVerticalVelocity - rb.linearVelocity.y) *
//                 altitudeKd;
//         }

//         desiredVerticalAcceleration = Mathf.Clamp(
//             desiredVerticalAcceleration,
//             -maxVerticalAcceleration,
//             maxVerticalAcceleration);

//         float upwardAlignment = Mathf.Max(
//             Vector3.Dot(transform.up, Vector3.up),
//             0.35f);

//         float thrustAcceleration =
//             (gravity + desiredVerticalAcceleration) /
//             upwardAlignment;

//         thrustAcceleration = Mathf.Clamp(
//             thrustAcceleration,
//             0f,
//             maxThrustAcceleration);

//         rb.AddForce(
//             transform.up * thrustAcceleration,
//             ForceMode.Acceleration);
//     }

//     private void ApplyAttitudeControl(
//         Quaternion desiredRotation,
//         float dt)
//     {
//         if (!hasPreviousDesiredRotation)
//         {
//             previousDesiredRotation = desiredRotation;
//             hasPreviousDesiredRotation = true;
//         }

//         Vector3 desiredAngularVelocity =
//             CalculateAngularVelocity(
//                 previousDesiredRotation,
//                 desiredRotation,
//                 dt);

//         previousDesiredRotation = desiredRotation;

//         Vector3 rotationError =
//             GetRotationError(
//                 desiredRotation,
//                 rb.rotation);

//         Vector3 angularVelocityError =
//             desiredAngularVelocity - rb.angularVelocity;

//         Vector3 torque =
//             rotationError * attitudeKp +
//             angularVelocityError * attitudeKd;

//         torque = Vector3.ClampMagnitude(
//             torque,
//             maxAttitudeAcceleration);

//         rb.AddTorque(torque, ForceMode.Acceleration);
//     }

//     private static Vector3 GetRotationError(
//         Quaternion desired,
//         Quaternion current)
//     {
//         Quaternion error =
//             desired * Quaternion.Inverse(current);

//         if (error.w < 0f)
//         {
//             error.x = -error.x;
//             error.y = -error.y;
//             error.z = -error.z;
//             error.w = -error.w;
//         }

//         error.ToAngleAxis(
//             out float angle,
//             out Vector3 axis);

//         if (angle > 180f)
//             angle -= 360f;

//         if (axis.sqrMagnitude < 0.000001f ||
//             Mathf.Abs(angle) < 0.001f)
//         {
//             return Vector3.zero;
//         }

//         return axis.normalized *
//                (angle * Mathf.Deg2Rad);
//     }

//     private static Vector3 CalculateAngularVelocity(
//         Quaternion from,
//         Quaternion to,
//         float dt)
//     {
//         Quaternion delta =
//             to * Quaternion.Inverse(from);

//         if (delta.w < 0f)
//         {
//             delta.x = -delta.x;
//             delta.y = -delta.y;
//             delta.z = -delta.z;
//             delta.w = -delta.w;
//         }

//         delta.ToAngleAxis(
//             out float angle,
//             out Vector3 axis);

//         if (angle > 180f)
//             angle -= 360f;

//         if (axis.sqrMagnitude < 0.000001f ||
//             Mathf.Abs(angle) < 0.001f)
//         {
//             return Vector3.zero;
//         }

//         return axis.normalized *
//                (angle * Mathf.Deg2Rad /
//                 Mathf.Max(dt, 0.0001f));
//     }

//     private void LimitVelocity()
//     {
//         Vector3 velocity = rb.linearVelocity;

//         Vector3 horizontalVelocity = new Vector3(
//             velocity.x,
//             0f,
//             velocity.z);

//         if (horizontalVelocity.magnitude > maxHorizontalSpeed)
//         {
//             horizontalVelocity =
//                 horizontalVelocity.normalized *
//                 maxHorizontalSpeed;

//             velocity.x = horizontalVelocity.x;
//             velocity.z = horizontalVelocity.z;
//         }

//         velocity.y = Mathf.Clamp(
//             velocity.y,
//             -maxVerticalSpeed,
//             maxVerticalSpeed);

//         rb.linearVelocity = velocity;
//     }

//     private void ResetDesiredRotationHistory()
//     {
//         previousDesiredRotation =
//             Quaternion.AngleAxis(targetYaw, Vector3.up) *
//             Quaternion.Euler(
//                 currentTargetPitch,
//                 0f,
//                 currentTargetRoll);

//         hasPreviousDesiredRotation = true;
//     }

//     private float ClampAltitude(float altitude)
//     {
//         return Mathf.Clamp(
//             altitude,
//             landingFloor,
//             Mathf.Max(landingFloor, maxAltitude));
//     }

//     private void OnCollisionEnter(Collision collision)
//     {
//         RegisterGroundContact(collision);
//     }

//     private void OnCollisionStay(Collision collision)
//     {
//         RegisterGroundContact(collision);
//     }

//     private void RegisterGroundContact(Collision collision)
//     {
//         for (int i = 0; i < collision.contactCount; i++)
//         {
//             ContactPoint contact = collision.GetContact(i);

//             if (Vector3.Dot(
//                     contact.normal,
//                     Vector3.up) >= groundNormalMinDot)
//             {
//                 lastGroundContactTime = Time.fixedTime;
//                 return;
//             }
//         }
//     }

//     private void OnValidate()
//     {
//         maxAltitude = Mathf.Max(maxAltitude, minAltitude);

//         climbRate = Mathf.Max(0f, climbRate);
//         maxVerticalSpeed = Mathf.Max(0.01f, maxVerticalSpeed);
//         maxVerticalAcceleration = Mathf.Max(0.01f, maxVerticalAcceleration);
//         maxThrustAcceleration = Mathf.Max(0.01f, maxThrustAcceleration);

//         landingDescentRate = Mathf.Clamp(
//             landingDescentRate,
//             0.01f,
//             maxVerticalSpeed);

//         landingVelocityResponse =
//             Mathf.Max(0.01f, landingVelocityResponse);

//         horizontalAcceleration =
//             Mathf.Max(0f, horizontalAcceleration);

//         horizontalBraking =
//             Mathf.Max(0f, horizontalBraking);

//         horizontalDamping =
//             Mathf.Max(0f, horizontalDamping);

//         maxHorizontalSpeed =
//             Mathf.Max(0f, maxHorizontalSpeed);

//         attitudeResponse =
//             Mathf.Max(0f, attitudeResponse);

//         maxAttitudeAcceleration =
//             Mathf.Max(0.01f, maxAttitudeAcceleration);

//         landingFloor = Mathf.Clamp(landingFloor, 0.01f, 1f);
//         landingEngageAltitude = Mathf.Max(landingEngageAltitude, landingFloor);
//         maxAltitude = Mathf.Max(maxAltitude, minAltitude);
//     }
// }


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

    [Header("Collision Recovery")]
    [Tooltip("Wall contact plus this much tilt from upright triggers an unpowered crash.")]
    [SerializeField, Range(45f, 90f)] private float wallCrashTiltAngle = 75f;

    [Tooltip("How long a severe wall tilt must persist before motors stop.")]
    [SerializeField, Min(0f)] private float wallCrashHoldTime = 0.12f;

    [Tooltip("Short grace period for ceiling contact. During this time vertical thrust is reduced, but attitude stabilization remains active.")]
    [SerializeField, Min(0f)] private float ceilingMotorPause = 0.12f;

    [Tooltip("Lower the altitude target after ceiling contact to avoid immediately climbing back into it.")]
    [SerializeField, Min(0f)] private float ceilingAltitudeStepDown = 0.35f;

    
    [Header("Crash Ground Friction")]
    [SerializeField, Min(0f)]
    private float crashGroundDeceleration = 3.5f;

    [SerializeField, Min(0f)]
    private float crashAngularDeceleration = 4f;

    [SerializeField, Min(0f)]
    private float crashStopSpeed = 0.08f;


    
    [Header("Crash Settling")]
    [SerializeField, Min(0f)]
    private float crashSettleDelay = 0.8f;

    private float crashGroundedDuration = 0f;



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
    private bool crashed;
    private float lastWallContactTime = float.NegativeInfinity;
    private float severeWallTiltDuration;
    private float lastCeilingContactTime = float.NegativeInfinity;

    public bool IsArmed => armed;
    public bool IsCrashed => crashed;
    public bool IsLanding => landingRequested;
    public bool IsGrounded =>
        Time.fixedTime - lastGroundContactTime <= groundedGraceTime;

    public float TargetAltitude => targetAltitude;
    public float CurrentAltitude =>
        rb != null ? rb.position.y : transform.position.y;




    
    [Header("Corner Recovery")]
    [SerializeField, Min(0.1f)]
    private float cornerCrashDelay = 0.35f;

    [SerializeField, Min(0f)]
    private float cornerReleaseSpeed = 0.7f;

    [SerializeField, Min(0.1f)]
    private float cornerStuckDelay = 0.45f;

    [SerializeField, Min(0f)]
    private float cornerStillSpeed = 0.15f;

    [Header("Crash Re-Arm")]
    [SerializeField, Range(5f, 45f)]
    private float maxRearmTilt = 25f;

    [SerializeField, Min(0f)]
    private float maxRearmSpeed = 0.15f;

    [SerializeField, Min(0f)]
    private float maxRearmAngularSpeed = 0.3f;

    [SerializeField, Min(0f)]
    private float rearmGroundedTime = 0.4f;

    private float lastCornerContactTime = float.NegativeInfinity;
    private float cornerContactDuration = 0f;
    private float cornerStuckDuration = 0f;
    private float groundedSettledDuration = 0f;
    private bool cornerReleased = false;
    private Vector3 cornerWallNormal = Vector3.zero;


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
        ResetCollisionState();

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

        if (crashed)
        {
            float uprightAngle = Vector3.Angle(
                transform.up,
                Vector3.up
            );

            bool upright = uprightAngle <= maxRearmTilt;

            bool safelySettled =
                IsGrounded &&
                groundedSettledDuration >= rearmGroundedTime;

            // Require a reset if the drone is upside down,
            // severely tilted, or still moving.
            if (!upright || !safelySettled)
            {
                inputReader.SetArmedState(false);

                Debug.Log(
                    "DroneFlightController: Cannot re-arm. " +
                    "Drone must be upright and settled. " +
                    "Press Reset if inverted.",
                    this
                );

                return;
            }

            // Successful recovery at the current location.
            // Do NOT teleport back to the spawn point.
            ResetCollisionState();

            Debug.Log(
                "DroneFlightController: recovered at " +
                "current landing position.",
                this
            );
        }

        armed = true;

        targetAltitude = Mathf.Clamp(
            Mathf.Max(rb.position.y, minAltitude),
            landingFloor,
            Mathf.Max(landingFloor, maxAltitude)
        );

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
        ResetCollisionState();

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
        if (!initialized)
            return;

        UpdateRecoveryState();

        if (crashed)
        {
            ApplyCrashGroundFriction();
            return;
        }

        if (!armed || rb.isKinematic)
            return;

        float dt = Time.fixedDeltaTime;

        // A near-vertical drone held against a wall is considered crashed.
        // Gentle wall contacts do not disarm the drone.
        bool touchingWall =
            Time.fixedTime - lastWallContactTime <= dt * 1.5f;
        bool severelyTilted =
            Vector3.Angle(transform.up, Vector3.up) >= wallCrashTiltAngle;

        severeWallTiltDuration = touchingWall && severelyTilted
            ? severeWallTiltDuration + dt
            : 0f;

        if (touchingWall && severelyTilted &&
            severeWallTiltDuration >= wallCrashHoldTime)
        {
            EnterCrashState();
            return;
        }

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

        // Preserve attitude and directional control on ceiling contact.
        // Only reduce vertical thrust enough for gravity to separate the drone.
        if (Time.fixedTime - lastCeilingContactTime <= ceilingMotorPause)
            ApplyCeilingSeparationThrust();
        else
            ApplyAltitudeControl(targetVerticalVelocity);

        rb.AddForce(
            -currentHorizontalVelocity * horizontalDamping,
            ForceMode.Acceleration);

        LimitVelocity();
    }


    
    private void UpdateRecoveryState()
    {
        float dt = Time.fixedDeltaTime;

        // Track whether a crashed drone is safely
        // grounded and nearly motionless.
        bool settled =
            crashed &&
            IsGrounded &&
            rb.linearVelocity.magnitude <= maxRearmSpeed &&
            rb.angularVelocity.magnitude <= maxRearmAngularSpeed;

        groundedSettledDuration = settled
            ? groundedSettledDuration + dt
            : 0f;

        // Recent wall and ceiling contacts together
        // indicate a possible corner trap.
        bool wallTouching =
            Time.fixedTime - lastWallContactTime <= 0.08f;

        bool ceilingTouching =
            Time.fixedTime - lastCeilingContactTime <= 0.08f;

        bool inCorner = wallTouching && ceilingTouching;

        if (!inCorner)
        {
            cornerContactDuration = 0f;
            cornerStuckDuration = 0f;
            cornerReleased = false;
            return;
        }

        cornerContactDuration += dt;

        // If both surfaces keep the powered drone
        // trapped, stop the motors rather than
        // continuing to push against the corner.
        if (armed && !crashed &&
            cornerContactDuration >= cornerCrashDelay)
        {
            EnterCrashState();
        }

        if (!crashed || IsGrounded || cornerReleased)
            return;

        if (rb.linearVelocity.magnitude > cornerStillSpeed)
        {
            cornerStuckDuration = 0f;
            return;
        }

        cornerStuckDuration += dt;

        if (cornerStuckDuration >= cornerStuckDelay)
        {
            ReleaseFromCorner();
        }
    }

    private void ReleaseFromCorner()
    {
        if (rb.isKinematic ||
            cornerWallNormal.sqrMagnitude < 0.01f)
            return;

        // One small push away from the wall,
        // plus a slight downward component.
        Vector3 releaseVelocity =
            cornerWallNormal.normalized * cornerReleaseSpeed +
            Vector3.down * 0.15f;

        rb.AddForce(
            releaseVelocity,
            ForceMode.VelocityChange
        );

        cornerReleased = true;

        Debug.Log(
            "DroneFlightController: corner release.",
            this
        );
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
        RegisterContacts(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        RegisterContacts(collision);
    }

    private void RegisterContacts(Collision collision) 
    { 
        for (int i = 0; i < collision.contactCount; i++) 
        { 
            ContactPoint contact = collision.GetContact(i); 
            float upDot = Vector3.Dot(contact.normal, Vector3.up); 
            
            if (upDot >= groundNormalMinDot) 
            { 
                lastGroundContactTime = Time.fixedTime; 
            } 
            else if (upDot <= -0.65f) 
            { 
                // Continue recording ceiling contact
                // even after a crash.
                float previousCeilingContact = lastCeilingContactTime;
                lastCeilingContactTime = Time.fixedTime;

                // Only powered flight changes altitude target.
                if (!initialized || !armed || crashed) continue;

                if (Time.fixedTime - previousCeilingContact > 0.2f) 
                {
                    targetAltitude = ClampAltitude(
                        Mathf.Min(
                            targetAltitude, 
                            rb.position.y - ceilingAltitudeStepDown
                        )
                    );
                    Debug.Log("DroneFlightController: ceiling contact.", this);
                }
            } 
            else if (Mathf.Abs(upDot) <= 0.45f) 
            { 
                lastWallContactTime = Time.fixedTime; 
                // Record the direction away from the wall. 
                Vector3 horizontalNormal = Vector3.ProjectOnPlane(contact.normal, Vector3.up); 
                if (horizontalNormal.sqrMagnitude > 0.001f) 
                { 
                    cornerWallNormal = horizontalNormal.normalized; 
                } 
            } 
        } 
    }

    private void ApplyCeilingSeparationThrust()
    {
        // Gentle downward separation from the ceiling.
        // Keep attitude stabilization active.
        float upwardAlignment = Mathf.Max(
            Vector3.Dot(transform.up, Vector3.up),
            0.35f
        );

        float thrustAcceleration = Mathf.Min(
            Physics.gravity.magnitude * 0.88f / upwardAlignment,
            maxThrustAcceleration
        );

        rb.AddForce(
            transform.up * thrustAcceleration,
            ForceMode.Acceleration
        );
    }


    private void EnterCrashState()
    {
        if (crashed)
            return;

        crashed = true;
        armed = false;
        landingRequested = false;

        // Do not disable this MonoBehaviour: it must receive ResetPressed.
        // Do not make the Rigidbody kinematic or clear its velocities.
        rb.isKinematic = false;
        rb.useGravity = true;
        inputReader.SetArmedState(false);
        Debug.Log("DroneFlightController: wall crash. Press Reset to recover.", this);
    }

    

    private void ApplyCrashGroundFriction()
    {
        if (rb.isKinematic)
            return;

        if (!IsGrounded)
        {
            crashGroundedDuration = 0f;
            return;
        }

        float dt = Time.fixedDeltaTime;
        crashGroundedDuration += dt;

        // Allow natural falling and tumbling at initial contact.
        if (crashGroundedDuration < crashSettleDelay)
            return;

        // Gradually reduce floor sliding.
        Vector3 velocity = rb.linearVelocity;

        Vector3 horizontalVelocity = new Vector3(
            velocity.x, 0f, velocity.z
        );

        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            Vector3.zero,
            crashGroundDeceleration * dt
        );

        velocity.x = newHorizontalVelocity.x;
        velocity.z = newHorizontalVelocity.z;

        if (newHorizontalVelocity.magnitude < crashStopSpeed)
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        rb.linearVelocity = velocity;

        // Only damp rotation after the initial settling period.
        rb.angularVelocity = Vector3.MoveTowards(
            rb.angularVelocity,
            Vector3.zero,
            crashAngularDeceleration * dt
        );
    }



    private void ResetCollisionState()
    {
        crashed = false;
        lastWallContactTime = float.NegativeInfinity;
        severeWallTiltDuration = 0f;
        lastCeilingContactTime = float.NegativeInfinity;

        
        crashGroundedDuration = 0f;

        
        cornerContactDuration = 0f;
        cornerStuckDuration = 0f;
        groundedSettledDuration = 0f;
        cornerReleased = false;
        cornerWallNormal = Vector3.zero;
        lastCornerContactTime = float.NegativeInfinity;


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
        wallCrashHoldTime = Mathf.Max(0f, wallCrashHoldTime);
        ceilingMotorPause = Mathf.Max(0f, ceilingMotorPause);
        ceilingAltitudeStepDown = Mathf.Max(0f, ceilingAltitudeStepDown);
    }
}
