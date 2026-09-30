using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32ControllerParser
{
    public void Parse(RelayPacket packet)
    {
        if (ESP32Controller.current == null)
        {
            Debug.LogWarning("ESP32Controllerが存在しません");
            return;
        }

        ESP32ControllerState state = new ESP32ControllerState
        {
            shakeFlag = (byte)(packet.shakeFlag ? 1 : 0),
            imuAccel = packet.imuAccel,
            imuGyro = packet.imuGyro,
            buttonA = (byte)(packet.buttonState ? 1 : 0),
            tofSensor = packet.tofSensor
        };

        InputSystem.QueueStateEvent(
            ESP32Controller.current,
            state
        );
    }
}