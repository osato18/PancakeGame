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
    
    public ushort tofSensor; // uint16_t (2 byte)
}