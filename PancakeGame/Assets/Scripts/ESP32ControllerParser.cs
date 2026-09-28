using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32ControllerParser
{
    public void Parse(string data)
    {
        string[] values = data.Split(',');

        if (values.Length < 3)
            return;

        // BUTTON,A,1
        if (values[0] != "BUTTON")
            return;

        string buttonName = values[1];

        if (!int.TryParse(values[2], out int value))
            return;

        bool pressed = value == 1;

        if (buttonName == "A")
        {
            UpdateButtonA(pressed);
        }
    }

    private void UpdateButtonA(bool pressed)
    {
        if (ESP32Controller.current == null)
        {
            Debug.LogWarning("ESP32Controllerが存在しません");
            return;
        }

        ESP32ControllerState state = new ESP32ControllerState();

        state.buttonA = (byte)(pressed ? 1 : 0);

        InputSystem.QueueStateEvent(
            ESP32Controller.current,
            state
        );
    }
}