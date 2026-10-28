using UnityEngine;

public class system_visualObjs : MonoBehaviour
{
    public system_playerDevice _PlayerDevice;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer != LayerMask.NameToLayer("item"))
        {
            _PlayerDevice.PancakeActive(gameObject);
            Destroy(this.gameObject);
        }
    }
}
