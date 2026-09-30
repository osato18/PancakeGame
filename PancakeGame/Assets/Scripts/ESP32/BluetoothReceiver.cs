using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Unity.VisualScripting;

public class BluetoothReceiver : MonoBehaviour
{
    private SerialPort bluetooth;
    private ConcurrentQueue<RelayPacket> receiveQueue = new();
    private ConcurrentQueue<string> errorQueue = new(); // エラー文字列保持用

    private Thread receiveThread;
    private bool running;

    private ESP32ControllerParser parser;

    // ESP32の実際の送信用構造体に合わせたサイズ（ヘッダー2B + ペイロード26B = 28B）
    private const int PACKET_SIZE = 28;
    private const int PAYLOAD_SIZE = 26;

    //入力データ数値の閾値の為のScriptableObject
    [SerializeField]private ControllerSettings _controllerSettings;

    void Start()
    {
        parser = new ESP32ControllerParser(_controllerSettings);
        
        try
        {
            bluetooth = new SerialPort("COM5", 115200);
            bluetooth.Open();
            Debug.Log("COM5 ポートを正常に開きました");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ポートオープンエラー: {ex.Message}");
            return;
        }

        running = true;
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        List<byte> buffer = new List<byte>();
        byte[] tempBuffer = new byte[256];

        while (running)
        {
            try
            {
                if (bluetooth != null && bluetooth.IsOpen && bluetooth.BytesToRead > 0)
                {
                    int readBytes = bluetooth.Read(tempBuffer, 0, tempBuffer.Length);
                    for (int i = 0; i < readBytes; i++)
                    {
                        buffer.Add(tempBuffer[i]);
                    }

                    while (buffer.Count >= PACKET_SIZE)
                    {
                        if (buffer[0] == 0xAA && buffer[1] == 0xCC)
                        {
                            byte[] packetBytes = buffer.GetRange(2, PAYLOAD_SIZE).ToArray();
                            RelayPacket packet = ByteArrayToStructure<RelayPacket>(packetBytes);
                            
                            receiveQueue.Enqueue(packet);
                            buffer.RemoveRange(0, PACKET_SIZE);
                        }
                        else
                        {
                            buffer.RemoveAt(0);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                // サブスレッドで直接ログを出さずキューに溜める
                errorQueue.Enqueue($"Bluetooth受信エラー: {ex.Message}");
            }
        }
    }

    private T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        }
        finally
        {
            handle.Free();
        }
    }

    void Update()
    {
        // メインスレッドでエラーログを出力
        while (errorQueue.TryDequeue(out string errorMsg))
        {
            Debug.LogError(errorMsg);
        }

        // 受信パケットの処理
        while (receiveQueue.TryDequeue(out RelayPacket packet))
        {
            ParseData(packet);
        }
    }

    private void ParseData(RelayPacket packet)
    {
        parser.Parse(packet);
    }

    private void OnDestroy()
    {
        running = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join();
        }

        if (bluetooth != null && bluetooth.IsOpen)
        {
            bluetooth.Close();
        }
    }
}