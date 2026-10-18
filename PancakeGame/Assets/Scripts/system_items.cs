using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class system_items : MonoBehaviour
{
    public system_scores scoresSystem;
    [SerializeField] private ItemsType itemType;
    public Collider p_col;
    private Items item;

    [SerializeField] private float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        item.col = gameObject.GetComponent<Collider>();
        item.rb = gameObject.GetComponent<Rigidbody>();
        item.rb.AddForce(0.0f, 0.0f, speed, ForceMode.VelocityChange);
        item.type = itemType;
        StartCoroutine(Destroy());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Scoring(ref Items items)
    {
        if (items.type.Equals(ItemsType.good))
        {
            scoresSystem.AddScore(100);
        }
        else if (items.type.Equals(ItemsType.bad))
        {
            scoresSystem.AddScore(-100);
        }
    } 

    void OnTriggerEnter(Collider other)
    {
        if (other.Equals(p_col))
        {
            Scoring(ref item);
            Destroy(gameObject);
        }
    }
    IEnumerator Destroy()
    {
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }
}

public struct Items
{
    public Collider col;
    public Rigidbody rb;
    public ItemsType type;
}

public enum ItemsType
{
    good,
    bad
}