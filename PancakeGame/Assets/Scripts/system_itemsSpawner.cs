using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using System.Reflection;
using System;
using Unity.Mathematics;
using UnityEditor.IMGUI.Controls;

public class system_itemsSpawner : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private system_scores scoresSystem;
    [SerializeField] private GameObject[] itemObj;
    [SerializeField] private Transform[] spawnTrans = new Transform[2];
    [SerializeField] private Collider p_col;

    [SerializeField] private float minSpanTime = 3f;
    [SerializeField] private float rangeTime = 5f;

    public List<system_items> itemsSystem = new List<system_items>();
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
        float span =  rangeTime * math.abs(math.sin(2 * math.PI * UnityEngine.Random.value)) + minSpanTime;
        yield return new WaitForSeconds(span);

        if(UnityEngine.Random.Range(0, 1000) % 2 == 0)
        {
            SpawnObj(itemObj[0]);
        }
        else
        {
            SpawnObj(itemObj[1]);
        }
        
        StartCoroutine(SpawnSpan()); 
    }


    void SpawnObj(GameObject obj)
    {
        int r = UnityEngine.Random.Range(0, 1000) % 2;
        GameObject spawnObj = Instantiate(obj, spawnTrans[r].position, spawnTrans[r].rotation);
        system_items system_Items = spawnObj.GetComponent<system_items>();
        system_Items.p_col = p_col;
        system_Items.scoresSystem = scoresSystem;
        system_Items.uiManager = uiManager;
        int i = 0;
        foreach (system_items item in itemsSystem)
        {
            if (item.Equals(null))
            {
                itemsSystem.Insert(i, system_Items);
                break;
            }
            i++;
        }
    }

    public void lineItemSpeedChange(float coe)
    {
        foreach (system_items i_sys in itemsSystem)
        {
            i_sys.speedChange(coe);
        }
    }
}
