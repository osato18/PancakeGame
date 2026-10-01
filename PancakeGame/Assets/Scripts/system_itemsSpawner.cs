using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class system_itemsSpawner : MonoBehaviour
{
    [SerializeField] private GameObject itemObj;
    [SerializeField] private Collider p_col;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnSpan());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator SpawnSpan()
    {
        yield return new WaitForSeconds(5f);
        SpawnObj(itemObj);
        StartCoroutine(SpawnSpan()); 
    }


    void SpawnObj(GameObject obj)
    {
        GameObject spawnObj = Instantiate(obj, gameObject.transform.position, gameObject.transform.rotation);
        system_items system_Items = spawnObj.GetComponent<system_items>();
        system_Items.p_col = p_col;
    }
}
