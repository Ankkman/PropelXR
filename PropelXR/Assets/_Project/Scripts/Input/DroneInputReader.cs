
// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class DroneInputReader : MonoBehaviour
// {
//     [Header("Input Actions")]
//     [SerializeField] private InputActionAsset inputActions;

//     [Header("Stick Filtering")]
//     [SerializeField, Range(0f, 0.25f)]
//     private float deadzone = 0.08f;

//     [SerializeField, Range(0f, 20f)]
//     private float smoothing = 8f;

//     [SerializeField, Range(0.1f, 2f)]
//     private float sensitivity = 1f;

//     [Header("Optional Axis Calibration")]
//     [SerializeField]
//     private Vector2 leftStickOffset = Vector2.zero;

//     [SerializeField]
//     private Vector2 rightStickOffset = Vector2.zero;

//     public Vector2 LeftStick { get; private set; }
//     public Vector2 RightStick { get; private set; }

//     public bool IsArmed { get; private set; }

//     public event Action ArmPressed;
//     public event Action DisarmPressed;
//     public event Action ResetPressed;

//     private InputActionMap droneMap;
//     private InputAction leftStickAction;
//     private InputAction rightStickAction;
//     private InputAction armAction;
//     private InputAction disarmAction;
//     private InputAction resetAction;

//     private Vector2 leftTarget;
//     private Vector2 rightTarget;

//     private void Awake()
//     {
//         if (inputActions == null)
//         {
//             Debug.LogError(
//                 "DroneInputReader: Assign DroneInputActions.",
//                 this);
//             enabled = false;
//             return;
//         }

//         droneMap = inputActions.FindActionMap("Drone");

//         if (droneMap == null)
//         {
//             Debug.LogError(
//                 "DroneInputReader: Action map 'Drone' not found.",
//                 this);
//             enabled = false;
//             return;
//         }

//         leftStickAction = droneMap.FindAction("LeftStick");
//         rightStickAction = droneMap.FindAction("RightStick");
//         armAction = droneMap.FindAction("Arm");
//         disarmAction = droneMap.FindAction("Disarm");
//         resetAction = droneMap.FindAction("ResetDrone");

//         if (leftStickAction == null ||
//             rightStickAction == null ||
//             armAction == null ||
//             disarmAction == null ||
//             resetAction == null)
//         {
//             Debug.LogError(
//                 "DroneInputReader: One or more Drone actions are missing.",
//                 this);
//             enabled = false;
//         }
//     }

//     private void OnEnable()
//     {
//         if (droneMap == null)
//             return;

//         leftStickAction.performed += OnLeftStickChanged;
//         leftStickAction.canceled += OnLeftStickChanged;

//         rightStickAction.performed += OnRightStickChanged;
//         rightStickAction.canceled += OnRightStickChanged;

//         armAction.performed += OnArm;
//         disarmAction.performed += OnDisarm;
//         resetAction.performed += OnReset;

//         droneMap.Enable();
//     }

//     private void OnDisable()
//     {
//         if (droneMap == null)
//             return;

//         leftStickAction.performed -= OnLeftStickChanged;
//         leftStickAction.canceled -= OnLeftStickChanged;

//         rightStickAction.performed -= OnRightStickChanged;
//         rightStickAction.canceled -= OnRightStickChanged;

//         armAction.performed -= OnArm;
//         disarmAction.performed -= OnDisarm;
//         resetAction.performed -= OnReset;

//         droneMap.Disable();

//         LeftStick = Vector2.zero;
//         RightStick = Vector2.zero;
//         leftTarget = Vector2.zero;
//         rightTarget = Vector2.zero;
//         IsArmed = false;
//     }

//     private void OnLeftStickChanged(InputAction.CallbackContext context)
//     {
//         leftTarget = FilterStick(
//             context.ReadValue<Vector2>() - leftStickOffset);
//     }

//     private void OnRightStickChanged(InputAction.CallbackContext context)
//     {
//         rightTarget = FilterStick(
//             context.ReadValue<Vector2>() - rightStickOffset);
//     }

//     private Vector2 FilterStick(Vector2 value)
//     {
//         value = Vector2.ClampMagnitude(value * sensitivity, 1f);

//         // Apply a radial dead zone and rescale the remaining range.
//         float magnitude = value.magnitude;

//         if (magnitude <= deadzone)
//             return Vector2.zero;

//         float scaledMagnitude =
//             Mathf.Clamp01((magnitude - deadzone) / (1f - deadzone));

//         return value.normalized * scaledMagnitude;
//     }

//     private void Update()
//     {
//         float blend = smoothing <= 0f
//             ? 1f
//             : 1f - Mathf.Exp(-smoothing * Time.deltaTime);

//         LeftStick = Vector2.Lerp(LeftStick, leftTarget, blend);
//         RightStick = Vector2.Lerp(RightStick, rightTarget, blend);
//     }

//     private void OnArm(InputAction.CallbackContext context)
//     {
//         if (IsArmed)
//             return;

//         IsArmed = true;
//         ArmPressed?.Invoke();
//     }

//     private void OnDisarm(InputAction.CallbackContext context)
//     {
//         if (!IsArmed)
//             return;

//         IsArmed = false;
//         DisarmPressed?.Invoke();
//     }


//     private void OnReset(InputAction.CallbackContext context)
//     {
//         IsArmed = false;
//         LeftStick = Vector2.zero;
//         RightStick = Vector2.zero;
//         leftTarget = Vector2.zero;
//         rightTarget = Vector2.zero;

//         ResetPressed?.Invoke();
//     }


//     public void SetCalibrationOffsets(
//         Vector2 leftOffset,
//         Vector2 rightOffset)
//     {
//         leftStickOffset = leftOffset;
//         rightStickOffset = rightOffset;
//     }

//     public void ResetCalibration()
//     {
//         leftStickOffset = Vector2.zero;
//         rightStickOffset = Vector2.zero;
//     }
// }













// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class DroneInputReader : MonoBehaviour
// {
//     [Header("Input Actions")]
//     [SerializeField] private InputActionAsset inputActions;

//     [Header("Stick Filtering")]
//     [SerializeField, Range(0f, 0.25f)]
//     private float deadzone = 0.08f;

//     [SerializeField, Range(0f, 20f)]
//     private float smoothing = 8f;

//     [SerializeField, Range(0.1f, 2f)]
//     private float sensitivity = 1f;

//     [Header("Optional Axis Calibration")]
//     [SerializeField]
//     private Vector2 leftStickOffset = Vector2.zero;

//     [SerializeField]
//     private Vector2 rightStickOffset = Vector2.zero;

//     public Vector2 LeftStick { get; private set; }
//     public Vector2 RightStick { get; private set; }

//     public bool IsArmed { get; private set; }

//     public event Action ArmPressed;
//     public event Action DisarmPressed;
//     public event Action ResetPressed;

//     private InputActionMap droneMap;
//     private InputAction leftStickAction;
//     private InputAction rightStickAction;
//     private InputAction armAction;
//     private InputAction disarmAction;
//     private InputAction resetAction;

//     private Vector2 leftTarget;
//     private Vector2 rightTarget;

//     private void Awake()
//     {
//         // Ensure clean start state
//         leftTarget = Vector2.zero;
//         rightTarget = Vector2.zero;
//         LeftStick = Vector2.zero;
//         RightStick = Vector2.zero;

//         if (inputActions == null)
//         {
//             Debug.LogError("DroneInputReader: Assign DroneInputActions.", this);
//             enabled = false;
//             return;
//         }

//         droneMap = inputActions.FindActionMap("Drone");

//         if (droneMap == null)
//         {
//             Debug.LogError("DroneInputReader: Action map 'Drone' not found.", this);
//             enabled = false;
//             return;
//         }

//         leftStickAction = droneMap.FindAction("LeftStick");
//         rightStickAction = droneMap.FindAction("RightStick");
//         armAction = droneMap.FindAction("Arm");
//         disarmAction = droneMap.FindAction("Disarm");
//         resetAction = droneMap.FindAction("ResetDrone");

//         if (leftStickAction == null || rightStickAction == null || 
//             armAction == null || disarmAction == null || resetAction == null)
//         {
//             Debug.LogError("DroneInputReader: One or more Drone actions are missing.", this);
//             enabled = false;
//         }
//     }

//     private void OnEnable()
//     {
//         if (droneMap == null)
//             return;

//         leftStickAction.performed += OnLeftStickChanged;
//         leftStickAction.canceled += OnLeftStickChanged;

//         rightStickAction.performed += OnRightStickChanged;
//         rightStickAction.canceled += OnRightStickChanged;

//         armAction.performed += OnArm;
//         disarmAction.performed += OnDisarm;
//         resetAction.performed += OnReset;

//         droneMap.Enable();
//     }

//     private void OnDisable()
//     {
//         if (droneMap == null)
//             return;

//         leftStickAction.performed -= OnLeftStickChanged;
//         leftStickAction.canceled -= OnLeftStickChanged;

//         rightStickAction.performed -= OnRightStickChanged;
//         rightStickAction.canceled -= OnRightStickChanged;

//         armAction.performed -= OnArm;
//         disarmAction.performed -= OnDisarm;
//         resetAction.performed -= OnReset;

//         droneMap.Disable();

//         LeftStick = Vector2.zero;
//         RightStick = Vector2.zero;
//         leftTarget = Vector2.zero;
//         rightTarget = Vector2.zero;
//         IsArmed = false;
//     }

//     private void OnLeftStickChanged(InputAction.CallbackContext context)
//     {
//         leftTarget = FilterStick(context.ReadValue<Vector2>() - leftStickOffset);
//     }

//     private void OnRightStickChanged(InputAction.CallbackContext context)
//     {
//         rightTarget = FilterStick(context.ReadValue<Vector2>() - rightStickOffset);
//     }

//     private Vector2 FilterStick(Vector2 value)
//     {
//         value = Vector2.ClampMagnitude(value * sensitivity, 1f);

//         // Apply a radial dead zone and rescale the remaining range.
//         float magnitude = value.magnitude;

//         if (magnitude <= deadzone)
//             return Vector2.zero;

//         float scaledMagnitude = Mathf.Clamp01((magnitude - deadzone) / (1f - deadzone));

//         return value.normalized * scaledMagnitude;
//     }

//     private void Update()
//     {
//         float blend = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-smoothing * Time.deltaTime);

//         LeftStick = Vector2.Lerp(LeftStick, leftTarget, blend);
//         RightStick = Vector2.Lerp(RightStick, rightTarget, blend);
//     }

//     private void OnArm(InputAction.CallbackContext context)
//     {
//         if (IsArmed)
//             return;

//         IsArmed = true;
//         ArmPressed?.Invoke();
//     }

//     private void OnDisarm(InputAction.CallbackContext context)
//     {
//         if (!IsArmed)
//             return;

//         IsArmed = false;
//         DisarmPressed?.Invoke();
//     }

//     private void OnReset(InputAction.CallbackContext context)
//     {
//         IsArmed = false;
//         LeftStick = Vector2.zero;
//         RightStick = Vector2.zero;
//         leftTarget = Vector2.zero;
//         rightTarget = Vector2.zero;

//         ResetPressed?.Invoke();
//     }

//     public void SetCalibrationOffsets(Vector2 leftOffset, Vector2 rightOffset)
//     {
//         leftStickOffset = leftOffset;
//         rightStickOffset = rightOffset;
//     }

//     public void ResetCalibration()
//     {
//         leftStickOffset = Vector2.zero;
//         rightStickOffset = Vector2.zero;
//     }
// }










using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Stick Filtering")]
    [SerializeField, Range(0f, 0.25f)]
    private float deadzone = 0.08f;

    [SerializeField, Range(0f, 20f)]
    private float smoothing = 8f;

    [SerializeField, Range(0.1f, 2f)]
    private float sensitivity = 1f;

    [Header("Optional Axis Calibration")]
    [SerializeField] private Vector2 leftStickOffset = Vector2.zero;
    [SerializeField] private Vector2 rightStickOffset = Vector2.zero;

    public Vector2 LeftStick { get; private set; }
    public Vector2 RightStick { get; private set; }

    public bool IsArmed { get; private set; }

    public event Action ArmPressed;
    public event Action DisarmPressed;
    public event Action ResetPressed;

    private InputActionMap droneMap;
    private InputAction leftStickAction;
    private InputAction rightStickAction;
    private InputAction armAction;
    private InputAction disarmAction;
    private InputAction resetAction;

    private Vector2 leftTarget;
    private Vector2 rightTarget;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError(
                "DroneInputReader: Assign DroneInputActions.",
                this);

            enabled = false;
            return;
        }

        droneMap = inputActions.FindActionMap("Drone");

        if (droneMap == null)
        {
            Debug.LogError(
                "DroneInputReader: Action map 'Drone' not found.",
                this);

            enabled = false;
            return;
        }

        leftStickAction = droneMap.FindAction("LeftStick");
        rightStickAction = droneMap.FindAction("RightStick");
        armAction = droneMap.FindAction("Arm");
        disarmAction = droneMap.FindAction("Disarm");
        resetAction = droneMap.FindAction("ResetDrone");

        if (leftStickAction == null ||
            rightStickAction == null ||
            armAction == null ||
            disarmAction == null ||
            resetAction == null)
        {
            Debug.LogError(
                "DroneInputReader: One or more Drone actions are missing.",
                this);

            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (droneMap == null ||
            leftStickAction == null ||
            rightStickAction == null ||
            armAction == null ||
            disarmAction == null ||
            resetAction == null)
        {
            return;
        }

        leftStickAction.performed += OnLeftStickChanged;
        leftStickAction.canceled += OnLeftStickChanged;

        rightStickAction.performed += OnRightStickChanged;
        rightStickAction.canceled += OnRightStickChanged;

        armAction.performed += OnArm;
        disarmAction.performed += OnDisarm;
        resetAction.performed += OnReset;

        droneMap.Enable();

        leftTarget = FilterStick(
            leftStickAction.ReadValue<Vector2>() -
            leftStickOffset);

        rightTarget = FilterStick(
            rightStickAction.ReadValue<Vector2>() -
            rightStickOffset);
    }

    private void OnDisable()
    {
        if (droneMap == null)
            return;

        if (leftStickAction != null)
        {
            leftStickAction.performed -= OnLeftStickChanged;
            leftStickAction.canceled -= OnLeftStickChanged;
        }

        if (rightStickAction != null)
        {
            rightStickAction.performed -= OnRightStickChanged;
            rightStickAction.canceled -= OnRightStickChanged;
        }

        if (armAction != null)
            armAction.performed -= OnArm;

        if (disarmAction != null)
            disarmAction.performed -= OnDisarm;

        if (resetAction != null)
            resetAction.performed -= OnReset;

        droneMap.Disable();

        LeftStick = Vector2.zero;
        RightStick = Vector2.zero;
        leftTarget = Vector2.zero;
        rightTarget = Vector2.zero;
        IsArmed = false;
    }

    private void Update()
    {
        float blend = smoothing <= 0f
            ? 1f
            : 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        LeftStick = Vector2.Lerp(
            LeftStick,
            leftTarget,
            blend);

        RightStick = Vector2.Lerp(
            RightStick,
            rightTarget,
            blend);
    }

    private void OnLeftStickChanged(
        InputAction.CallbackContext context)
    {
        leftTarget = FilterStick(
            context.ReadValue<Vector2>() -
            leftStickOffset);
    }

    private void OnRightStickChanged(
        InputAction.CallbackContext context)
    {
        rightTarget = FilterStick(
            context.ReadValue<Vector2>() -
            rightStickOffset);
    }

    private Vector2 FilterStick(Vector2 value)
    {
        value = Vector2.ClampMagnitude(
            value * sensitivity,
            1f);

        float magnitude = value.magnitude;

        if (magnitude <= deadzone)
            return Vector2.zero;

        float scaledMagnitude = Mathf.Clamp01(
            (magnitude - deadzone) /
            (1f - deadzone));

        return value.normalized * scaledMagnitude;
    }

    private void OnArm(InputAction.CallbackContext context)
    {
        if (IsArmed)
            return;

        IsArmed = true;
        ArmPressed?.Invoke();
    }

    private void OnDisarm(InputAction.CallbackContext context)
    {
        if (!IsArmed)
            return;

        IsArmed = false;
        DisarmPressed?.Invoke();
    }

    private void OnReset(InputAction.CallbackContext context)
    {
        IsArmed = false;

        LeftStick = Vector2.zero;
        RightStick = Vector2.zero;

        leftTarget = Vector2.zero;
        rightTarget = Vector2.zero;

        ResetPressed?.Invoke();
    }

    public void SetArmedState(bool value)
    {
        IsArmed = value;
    }

    public void SetCalibrationOffsets(
        Vector2 leftOffset,
        Vector2 rightOffset)
    {
        leftStickOffset = leftOffset;
        rightStickOffset = rightOffset;
    }

    public void ResetCalibration()
    {
        leftStickOffset = Vector2.zero;
        rightStickOffset = Vector2.zero;
    }
}