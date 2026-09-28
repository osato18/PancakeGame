using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[InputControlLayout(stateType = typeof(ESP32ControllerState))]
public class ESP32Controller : InputDevice
{
    public ButtonControl buttonA { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();

        buttonA = GetChildControl<ButtonControl>("buttonA");
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
public struct ESP32ControllerState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('E', 'S', 'P', '3');

    [InputControl(name = "buttonA", layout = "Button")]
    public byte buttonA;
}