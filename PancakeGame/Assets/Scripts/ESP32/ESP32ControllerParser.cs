using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32ControllerParser
{
    private ControllerSettings _conSetting;
    private byte isJump;
    private byte isButtonA;
    public ESP32ControllerParser(ControllerSettings controllerSettings)
    {
        _conSetting=controllerSettings;
    }
    public void Parse(RelayPacket packet)
    {
        if (ESP32Controller.current == null)
        {
            Debug.LogWarning("ESP32Controllerが存在しません");
            return;
        }

        // 受信確認用のログを追加
        Debug.Log($"[Received] Accel: {packet.imuAccel}, Button: {packet.buttonState}, ToF: {packet.tofSensor}");
        
        InputInRange(packet);

        // 受信データ（RelayPacket）を Input System 用の State 構造体に詰め替える
        ESP32ControllerState state = new ESP32ControllerState
        {
            //shakeFlag = (byte)(packet.shakeFlag ? 1 : 0),
            shakeFlag=isJump,
            imuAccel = packet.imuAccel,
            imuGyro = packet.imuGyro,
            //buttonA = (byte)(packet.buttonState ? 1 : 0),
            buttonA=isButtonA,
            tofSensor = packet.tofSensor
        };
        
        //QueueStateEvent を使って Input System にデータを登録・更新する
        InputSystem.QueueStateEvent(
            ESP32Controller.current,
            state
        );
    }

    private void InputInRange(RelayPacket packet)
    {
        if (packet.imuAccel.z < _conSetting.jumpRangeMin)
        {
            isJump=(byte)0;
            isButtonA=(byte)0;
        }
        else
        {
            isJump=(byte)1;
            isButtonA=(byte)1;
        }
    }
}