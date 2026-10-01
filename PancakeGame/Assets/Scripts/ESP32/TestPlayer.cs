using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayer : MonoBehaviour
{
    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("OnJump!");
        }    
    }
}
