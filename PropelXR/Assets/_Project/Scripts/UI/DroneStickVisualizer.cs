
using UnityEngine;
using UnityEngine.UI;

public class DroneStickVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DroneInputReader inputReader;

    [Header("Left Stick")]
    [SerializeField] private RectTransform leftStickArea;
    [SerializeField] private RectTransform leftStickDot;

    [Header("Right Stick")]
    [SerializeField] private RectTransform rightStickArea;
    [SerializeField] private RectTransform rightStickDot;

    [Header("Display")]
    [SerializeField, Range(0f, 1f)]
    private float movementRange = 0.4f;

    private void Update()
    {
        if (inputReader == null)
            return;

        UpdateStick(
            inputReader.LeftStick,
            leftStickArea,
            leftStickDot);

        UpdateStick(
            inputReader.RightStick,
            rightStickArea,
            rightStickDot);
    }

    private void UpdateStick(
        Vector2 input,
        RectTransform area,
        RectTransform dot)
    {
        if (area == null || dot == null)
            return;

        Vector2 halfSize = area.rect.size * 0.5f;

        Vector2 offset = new Vector2(
            input.x * halfSize.x * movementRange * 2f,
            input.y * halfSize.y * movementRange * 2f);

        dot.anchoredPosition = offset;
    }
}
