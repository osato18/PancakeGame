using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[InputControlLayout(stateType = typeof(ESP32ControllerState))]
public class ESP32Controller : InputDevice
{
    public ButtonControl shakeFlag { get; private set; }
    public Vector3Control imuAccel { get; private set; }
    public Vector3Control imuGyro { get; private set; }
    public ButtonControl buttonA { get; private set; }
    public IntegerControl tofSensor { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();

        shakeFlag = GetChildControl<ButtonControl>("shakeFlag");
        imuAccel = GetChildControl<Vector3Control>("imuAccel");
        imuGyro = GetChildControl<Vector3Control>("imuGyro");
        buttonA = GetChildControl<ButtonControl>("buttonA");
        tofSensor = GetChildControl<IntegerControl>("tofSensor");
    }

    public static ESP32Controller current { get; private set; }

    protected override void OnAdded()
    {
        base.OnAdded();
        current = this;
    }

    protected override void OnRemoved()
    {
        base.OnRemoved();
        if (current == this)
            current = null;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ESP32ControllerState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('E', 'S', 'P', '3');

    [InputControl(name = "shakeFlag", layout = "Button")]
    public byte shakeFlag;

    [InputControl(name = "imuAccel", layout = "Vector3")]
    public Vector3 imuAccel;

    [InputControl(name = "imuGyro", layout = "Vector3")]
    public Vector3 imuGyro;

    [InputControl(name = "buttonA", layout = "Button")]
    public byte buttonA;

    [InputControl(name = "tofSensor", layout = "Integer")]
    public ushort tofSensor;
}