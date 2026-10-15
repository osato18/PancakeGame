using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32ControllerParser
{
    private ControllerSettings _conSetting;
    private byte isJump;
    private byte isButtonA;

    // 前回閾値を超えていたかを保持するフラグ
    private bool _wasOverJumpRange = false;

    // --- CSV出力用のデータ構造とリスト ---
    public struct SensorLogData
    {
        public float timestamp;       // 経過時間 (秒)
        public Vector3 accel;         // 加速度
        public Vector3 gyro;          // 角速度
        public bool isJumpTriggered;  // ジャンプ判定が有効か (1 or 0)
    }

    private List<SensorLogData> _sensorLogs = new List<SensorLogData>();

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

        InputInRange(packet);

        // --- 受信したデータとジャンプ判定を時刻（Time.time）とともに記録 ---
        _sensorLogs.Add(new SensorLogData
        {
            timestamp = Time.time,
            accel = packet.imuAccel,
            gyro = packet.imuGyro,
            isJumpTriggered = (isJump == 255)
        });

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

        if (isButtonA == 255)
        {
            // Debug.Log("ControllerJump");
        }
    }

    // --- CSVファイル出力メソッド ---
    public void ExportToCSV(string filePath)
    {
        StringBuilder sb = new StringBuilder();
        
        // CSVのヘッダー行
        sb.AppendLine("Timestamp,Accel_X,Accel_Y,Accel_Z,Gyro_X,Gyro_Y,Gyro_Z,IsJump");

        foreach (var log in _sensorLogs)
        {
            // 小数点以下4桁でフォーマット
            string line = $"{log.timestamp:F4}," +
                          $"{log.accel.x:F4},{log.accel.y:F4},{log.accel.z:F4}," +
                          $"{log.gyro.x:F4},{log.gyro.y:F4},{log.gyro.z:F4}," +
                          $"{(log.isJumpTriggered ? 1 : 0)}";
            sb.AppendLine(line);
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"<color=cyan>センサーログをCSVに出力しました: {filePath}</color>");
    }
}