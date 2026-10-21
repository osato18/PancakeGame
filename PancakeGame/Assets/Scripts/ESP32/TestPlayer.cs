using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayer : MonoBehaviour
{
    void Start()
    {
        AudioManager.PlayBGM("test_battle");
    }
    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            AudioManager.PlaySE("test_se");
            EffectManager.PlayEffect("TestEffect_NotLoop",gameObject.transform.position);
            Debug.Log("OnJump!");
        }    
    }
}
