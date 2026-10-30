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
            EffectManager.PlayEffect("ItemGetEffect_Sphere005_Particle_Glow",gameObject.transform.position);
            Debug.Log("OnJump!");
        }    
    }
}
