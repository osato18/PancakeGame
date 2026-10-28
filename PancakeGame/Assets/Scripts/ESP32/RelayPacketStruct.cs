using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RelayPacket
{
    [MarshalAs(UnmanagedType.U1)]
    public bool shakeFlag;
    
    public Vector3 imuAccel; // float x 3 (12 byte)
    public Vector3 imuGyro;  // float x 3 (12 byte)
    
    [MarshalAs(UnmanagedType.U1)]
    public bool buttonState;
    
    // bool 3つ分のフィールド（Cの bool tofDetected[3] に対応）
    [MarshalAs(UnmanagedType.U1)]
    public bool tofDetected0;
    [MarshalAs(UnmanagedType.U1)]
    public bool tofDetected1;
    [MarshalAs(UnmanagedType.U1)]
    public bool tofDetected2;

    // 配列風にアクセスできる便利なプロパティ
    public bool[] tofDetected
    {
        get => new bool[] { tofDetected0, tofDetected1, tofDetected2 };
    }
}