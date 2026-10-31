using System;
using Unity.VisualScripting;
using UnityEngine;

public class system_visualObjs : MonoBehaviour
{
    public system_playerDevice _PlayerDevice;
    [SerializeField] private int typeNum;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private bool activated;

    void OnCollisionEnter(Collision collision)
    {
        if (!isActiveAndEnabled || _PlayerDevice == null || activated ||
        collision.gameObject.layer == LayerMask.NameToLayer("item"))
        {
            return;
        }

        activated = true; // 呼び出す前に立てる
        _PlayerDevice.DecorationActive(gameObject, typeNum);
        Destroy(gameObject);
    }
}
