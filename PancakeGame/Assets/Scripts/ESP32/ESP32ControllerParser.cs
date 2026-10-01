using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32ControllerParser
{
    private ControllerSettings _conSetting;
    private byte isJump;
    private byte isButtonA;

    // 前回閾値を超えていたかを保持するフラグ
    private bool _wasOverJumpRange = false;

    public ESP32ControllerParser(ControllerSettings controllerSettings)
    {
        _conSetting = controllerSettings;
    }

    public void Parse(RelayPacket packet)
    {
        if (ESP32Controller.current == null)
        {
            Debug.LogWarning("ESP32Controllerが存在しません");
            return;
        }

        // 受信確認用のログを追加
        //Debug.Log($"[Received] Accel: {packet.imuAccel}, Button: {packet.buttonState}, ToF: {packet.tofSensor}");

        InputInRange(packet);

        // 受信データ（RelayPacket）を Input System 用の State 構造体に詰め替える
        ESP32ControllerState state = new ESP32ControllerState
        {
            shakeFlag = isJump,
            imuAccel = packet.imuAccel,
            imuGyro = packet.imuGyro,
            buttonA = isButtonA,
            tofSensor = packet.tofSensor
        };

        // QueueStateEvent を使って Input System にデータを登録・更新する
        InputSystem.QueueStateEvent(
            ESP32Controller.current,
            state
        );
    }

    private void InputInRange(RelayPacket packet)
    {
        // 閾値を超えている間は 255 (Pressed: 1.0)、下回っている間は 0 (Released: 0.0) にする
        bool isOver;
        if (packet.imuAccel.z >= 0 && Mathf.Abs(packet.imuAccel.z) >= _conSetting.jumpRangeMin)
        {
            isOver = true;
        }
        else
        {
            isOver = false;
        }

        isJump = isOver ? (byte)255 : (byte)0;
        isButtonA = isOver ? (byte)255 : (byte)0;

        if (isButtonA==255)
        {
            Debug.Log("ControllerJump");
        }
        // Debug.Log("isJump:" + isJump);
        // Debug.Log("isButtonA:" + isButtonA);
    }
}