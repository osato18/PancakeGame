using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32ControllerSetup : MonoBehaviour
{
    private void Awake()
    {
        InputSystem.RegisterLayout<ESP32Controller>();

        InputSystem.AddDevice<ESP32Controller>();
    }
}