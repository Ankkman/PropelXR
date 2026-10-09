
using TMPro;
using UnityEngine;

public class DroneStatusDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DroneInputReader inputReader;
    [SerializeField] private DroneFlightController flightController;
    [SerializeField] private Rigidbody droneRigidbody;
    [SerializeField] private TMP_Text statusText;

    private void Update()
    {
        if (inputReader == null ||
            flightController == null ||
            droneRigidbody == null ||
            statusText == null)
            return;

        Vector3 velocity = droneRigidbody.linearVelocity;
        float horizontalSpeed = new Vector2(
            velocity.x, velocity.z).magnitude;

        string state = flightController.IsArmed
            ? "ARMED"
            : "DISARMED";

        statusText.text =
            "DJI SPARK  |  FLIGHT STATUS\n" +
            "STATE: " + state + "\n" +
            "ALTITUDE: " +
                droneRigidbody.position.y.ToString("F2") + " m\n" +
            "HORIZONTAL SPEED: " +
                horizontalSpeed.ToString("F2") + " m/s\n" +
            "VERTICAL SPEED: " +
                velocity.y.ToString("F2") + " m/s\n\n" +
            "LEFT STICK  (YAW / ALTITUDE)\n" +
            "X: " + inputReader.LeftStick.x.ToString("F2") +
            "   Y: " + inputReader.LeftStick.y.ToString("F2") + "\n\n" +
            "RIGHT STICK  (ROLL / PITCH)\n" +
            "X: " + inputReader.RightStick.x.ToString("F2") +
            "   Y: " + inputReader.RightStick.y.ToString("F2");
    }
}
