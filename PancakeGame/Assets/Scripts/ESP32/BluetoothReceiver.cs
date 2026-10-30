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
    private ConcurrentQueue<string> errorQueue = new(); // エラー文字列保持用
    private Thread receiveThread;
    private bool running;

    private ESP32ControllerParser parser;

    // ESP32の実際の送信用構造体に合わせたサイズ（ヘッダー2B + ペイロード29B = 31B）
    private const int PACKET_SIZE = 31;
    private const int PAYLOAD_SIZE = 29;

    //入力データ数値の閾値の為のScriptableObject
    [SerializeField] private ControllerSettings _controllerSettings;

    void Start()
    {
        parser = new ESP32ControllerParser(_controllerSettings);

        try
        {
            bluetooth = new SerialPort("COM7", 115200);
            bluetooth.Open();
            Debug.Log("COM7 ポートを正常に開きました");
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

                    // --- 混入したテキストやゴミを検知してスキップする処理 ---
                    while (true)
                    {
                        // 1. 同期ヘッダー (0xAA, 0xCC) の位置を探す
                        int headerIndex = -1;
                        for (int i = 0; i <= buffer.Count - 2; i++)
                        {
                            if (buffer[i] == 0xAA && buffer[i + 1] == 0xCC)
                            {
                                headerIndex = i;
                                break;
                            }
                        }

                        // ヘッダーが見つかった場合
                        if (headerIndex >= 0)
                        {
                            if (headerIndex > 0)
                            {
                                // ==========================================
                                // ★ 不一致（ヘッダーの手前にあるゴミ）を検知したときのデバッグログ
                                // ==========================================
                                string garbage = System.Text.Encoding.ASCII.GetString(buffer.GetRange(0, headerIndex).ToArray());
                                Debug.LogWarning($"[データ不一致・ゴミ検知] 正しいヘッダーの手前にある {headerIndex} バイトをスキップします。\n【混入していたデータ】: {garbage.Trim()}");

                                buffer.RemoveRange(0, headerIndex);
                            }

                            // 2. ヘッダーが先頭にきた状態で、1パケット分（31バイト）たまっているか確認
                            if (buffer.Count >= PACKET_SIZE)
                            {
                                byte[] packetBytes = buffer.GetRange(2, PAYLOAD_SIZE).ToArray();
                                RelayPacket packet = ByteArrayToStructure<RelayPacket>(packetBytes);

                                receiveQueue.Enqueue(packet);
                                buffer.RemoveRange(0, PACKET_SIZE);
                            }
                            else
                            {
                                // ヘッダーはあるが、パケット全体のデータがまだ揃っていないので次の受信を待つ
                                break;
                            }
                        }
                        else
                        {
                            // ==========================================
                            // ★ バッファ内にヘッダーが一切見つからない場合のデバッグログ
                            // ==========================================
                            if (buffer.Count > PACKET_SIZE * 20)
                            {
                                string overflowGarbage = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
                                Debug.LogError($"[ヘッダー未検出] 有効なヘッダー(0xAA, 0xCC)が一切見つからないため、溜まったバッファ({buffer.Count}バイト)を破棄します。\n【受信データ全文】: {overflowGarbage}");

                                buffer.RemoveRange(0, buffer.Count - PACKET_SIZE);
                            }
                            break;
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
        while (errorQueue.TryDequeue(out string errorMsg))
        {
            Debug.LogError(errorMsg);
        }

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

        if (parser != null)
        {
            string fileName = $"ESP32_SensorLog_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            parser.ExportToCSV(filePath);
        }
    }
}