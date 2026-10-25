using System;
using UnityEngine;

public class system_event : MonoBehaviour
{
    [SerializeField] private system_itemsSpawner[] itemsSpawnerSys = new system_itemsSpawner[3];
    [SerializeField] private system_playerDevice playerDeviceSys;
    [SerializeField] private GameObject[] fireObjs = new GameObject[3];
    private float timer = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < 3; i++)
        {
            fireObjs[i].SetActive(false);
        }
    }

    
    // Update is called once per frame
    void Update()
    {
        EventManager();
    }

    int stoveNum = 0;
    void EventBigFire(float bakingCoe, bool eventFlag)
    {
        if (!eventFlag)
        {
            int r = UnityEngine.Random.Range(0, 1000) % 3;
            stoveNum = r;
            fireObjs[stoveNum].SetActive(true);
        }
        else
        {
            fireObjs[stoveNum].SetActive(false);
        }
        playerDeviceSys.bakingSpeedChange(bakingCoe, stoveNum);
    }

    bool[] eventFlag = {false};
    
    void EventManager()
    {
        timer += Time.deltaTime;
        
        
        if((int)(timer / 10) % 3 == 2 && !eventFlag[0])
        {
            EventBigFire(10.0f, eventFlag[0]);
            eventFlag[0] = true;
        }
        if((int)(timer / 10) % 3 != 2 && eventFlag[0])
        {
            EventBigFire(1.0f, eventFlag[0]);
            eventFlag[0] = false;
        }

    }
}
