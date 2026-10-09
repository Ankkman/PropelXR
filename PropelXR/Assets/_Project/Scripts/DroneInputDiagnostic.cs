
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneInputDiagnostic : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private InputActionMap droneMap;
    private InputAction leftStick;
    private InputAction rightStick;
    private InputAction arm;
    private InputAction disarm;
    private InputAction resetDrone;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("Assign DroneInputActions in the Inspector.");
            enabled = false;
            return;
        }

        droneMap = inputActions.FindActionMap("Drone");

        if (droneMap == null)
        {
            Debug.LogError("Action map 'Drone' not found.");
            enabled = false;
            return;
        }

        leftStick = droneMap.FindAction("LeftStick");
        rightStick = droneMap.FindAction("RightStick");
        arm = droneMap.FindAction("Arm");
        disarm = droneMap.FindAction("Disarm");
        resetDrone = droneMap.FindAction("ResetDrone");

        if (leftStick == null || rightStick == null ||
            arm == null || disarm == null || resetDrone == null)
        {
            Debug.LogError("One or more Drone actions are missing.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (droneMap == null) return;

        leftStick.performed += OnLeftStick;
        rightStick.performed += OnRightStick;

        leftStick.canceled += OnLeftStickCentered;
        rightStick.canceled += OnRightStickCentered;

        arm.performed += OnArm;
        disarm.performed += OnDisarm;
        resetDrone.performed += OnResetDrone;

        droneMap.Enable();
    }

    private void OnDisable()
    {
        if (droneMap == null) return;

        leftStick.performed -= OnLeftStick;
        rightStick.performed -= OnRightStick;

        leftStick.canceled -= OnLeftStickCentered;
        rightStick.canceled -= OnRightStickCentered;

        arm.performed -= OnArm;
        disarm.performed -= OnDisarm;
        resetDrone.performed -= OnResetDrone;

        droneMap.Disable();
    }

    private void OnLeftStick(InputAction.CallbackContext ctx)
    {
        Debug.Log($"LEFT STICK: {ctx.ReadValue<Vector2>()}");
    }

    private void OnRightStick(InputAction.CallbackContext ctx)
    {
        Debug.Log($"RIGHT STICK: {ctx.ReadValue<Vector2>()}");
    }

    private void OnLeftStickCentered(InputAction.CallbackContext ctx)
    {
        Debug.Log("LEFT STICK: CENTER");
    }

    private void OnRightStickCentered(InputAction.CallbackContext ctx)
    {
        Debug.Log("RIGHT STICK: CENTER");
    }

    private void OnArm(InputAction.CallbackContext ctx)
    {
        Debug.Log("ARM BUTTON PRESSED");
    }

    private void OnDisarm(InputAction.CallbackContext ctx)
    {
        Debug.Log("DISARM BUTTON PRESSED");
    }

    private void OnResetDrone(InputAction.CallbackContext ctx)
    {
        Debug.Log("RESET BUTTON PRESSED");
    }
}
