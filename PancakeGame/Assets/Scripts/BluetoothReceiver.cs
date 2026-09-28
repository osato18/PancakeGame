using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class BluetoothReceiver : MonoBehaviour
{
    private SerialPort bluetooth;

    private ConcurrentQueue<string> receiveQueue = new();

    private Thread receiveThread;
    private bool running;

    private ESP32ControllerParser parser;

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
        while (running)
        {
            if (bluetooth.BytesToRead > 0)
            {
                string data = bluetooth.ReadLine();

                receiveQueue.Enqueue(data);
            }
        }
    }

    void Update()
    {
        while (receiveQueue.TryDequeue(out string data))
        {
            Debug.Log($"受信: {data}");

            ParseData(data);
        }
    }

    private void ParseData(string data)
    {
        parser.Parse(data);
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