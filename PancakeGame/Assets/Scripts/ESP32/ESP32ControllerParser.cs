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

    // 移動およびToFセンサー用のメンバ変数
    private int _moveInt;
    private bool _onMoveCalled;
    private bool _tof0;
    private bool _tof1;
    private bool _tof2;

    // --- CSV出力用のデータ構造とリスト ---
    public struct SensorLogData
    {
        public float timestamp;       // 経過時間 (秒)
        public Vector3 accel;         // 加速度
        public Vector3 gyro;          // 角速度
        public bool isJumpTriggered;  // ジャンプ判定が有効か
        public bool tof0;             // tofDetected[0]
        public bool tof1;             // tofDetected[1]
        public bool tof2;             // tofDetected[2]
        public int moveInput;     // OnMoveの入力値(int)
        public bool onMoveCalled;     // OnMoveが呼ばれた（入力が有効になった）か
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

        // 各種入力・センサーのパース処理を実行
        InputJumpInRange(packet);
        InputMoveParse(packet);

        // --- 受信したデータと各判定結果を時刻（Time.time）とともに記録 ---
        _sensorLogs.Add(new SensorLogData
        {
            timestamp = Time.time,
            accel = packet.imuAccel,
            gyro = packet.imuGyro,
            isJumpTriggered = (isJump == 255),
            tof0 = _tof0,
            tof1 = _tof1,
            tof2 = _tof2,
            moveInput = _moveInt,
            onMoveCalled = _onMoveCalled
        });

        // 受信データ（RelayPacket）を Input System 用の State 構造体に詰め替える
        ESP32ControllerState state = new ESP32ControllerState
        {
            shakeFlag = isJump,
            imuAccel = packet.imuAccel,
            imuGyro = packet.imuGyro,
            buttonA = isButtonA,
            move = _moveInt
        };

        // QueueStateEvent を使って Input System にデータを登録・更新する
        InputSystem.QueueStateEvent(
            ESP32Controller.current,
            state
        );
    }

    private void InputJumpInRange(RelayPacket packet)
    {
        bool isOver;
        if (Mathf.Abs(packet.imuAccel.z) >= _conSetting.jumpRangeMin)
        {
            isOver = true;
        }
        else
        {
            isOver = false;
        }

        isJump = isOver ? (byte)255 : (byte)0;
        isButtonA = isOver ? (byte)255 : (byte)0;
    }

    private void InputMoveParse(RelayPacket packet)
    {
        int beforeMoveInput=_moveInt;   //前回の入力
        _onMoveCalled = false;

        bool[] tof = packet.tofDetected;
        if (tof.Length >= 3)
        {
            _tof0 = tof[0];
            _tof1 = tof[1];
            _tof2 = tof[2];

            if (_tof0)
            {
                _moveInt = 0;
                _onMoveCalled = true;
            }
            else if (_tof1)
            {
                _moveInt = 1;
                _onMoveCalled = true;
            }
            else if (_tof2)
            {
                _moveInt = 2;
                _onMoveCalled = true;
            }
            else
            {
                _moveInt = beforeMoveInput;
                _onMoveCalled = true;
            }
        }
        else
        {
            _tof0 = false;
            _tof1 = false;
            _tof2 = false;
        }
    }

    // --- CSVファイル出力メソッド ---
    public void ExportToCSV(string filePath)
    {
        StringBuilder sb = new StringBuilder();

        // CSVのヘッダー行（ToFの検出状態、Moveの入力値、OnMove呼び出しフラグを追加）
        sb.AppendLine("Timestamp,Accel_X,Accel_Y,Accel_Z,Gyro_X,Gyro_Y,Gyro_Z,IsJump,ToF_0,ToF_1,ToF_2,Move_X,Move_Y,OnMoveCalled");

        foreach (var log in _sensorLogs)
        {
            string line = $"{log.timestamp:F4}," +
                          $"{log.accel.x:F4},{log.accel.y:F4},{log.accel.z:F4}," +
                          $"{log.gyro.x:F4},{log.gyro.y:F4},{log.gyro.z:F4}," +
                          $"{(log.isJumpTriggered ? 1 : 0)}," +
                          $"{(log.tof0 ? 1 : 0)}," +
                          $"{(log.tof1 ? 1 : 0)}," +
                          $"{(log.tof2 ? 1 : 0)}," +
                          $"{log.moveInput:F1}," +
                          $"{(log.onMoveCalled ? 1 : 0)}";
            sb.AppendLine(line);
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"<color=cyan>センサーログをCSVに出力しました: {filePath}</color>");
    }
}