using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class BluetoothReceiver : MonoBehaviour
{
    private SerialPort bluetooth;
    private ConcurrentQueue<RelayPacket> receiveQueue = new();

    private Thread receiveThread;
    private bool running;

    private ESP32ControllerParser parser;

    // パケットサイズ: Header (2bytes) + RelayPacket (28bytes) = 30bytes
    private const int PACKET_SIZE = 30;
    private const int PAYLOAD_SIZE = 28;

    void Start()
    {
        parser = new ESP32ControllerParser();
        
        bluetooth = new SerialPort("COM5", 115200);
        bluetooth.Open();

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

                    // ヘッダー検索とパケット抽出
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
                            // ヘッダーが見つかるまで先頭を切り捨てる
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
                Debug.LogError($"Bluetooth受信エラー: {ex.Message}");
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

        if (receiveThread != null)
        {
            receiveThread.Join();
        }

        if (bluetooth != null && bluetooth.IsOpen)
        {
            bluetooth.Close();
        }
    }
}