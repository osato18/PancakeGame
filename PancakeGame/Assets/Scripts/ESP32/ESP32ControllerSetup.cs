using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32ControllerSetup : MonoBehaviour
{
    private void Awake()
    {
        InputSystem.RegisterLayout<ESP32Controller>();

        // 起動時や初期化時に 1 回実行されているか確認
        if (ESP32Controller.current == null)
        {
            InputSystem.AddDevice<ESP32Controller>();
        }
    }
}