using System;
using TMPro;
using UnityEngine;

public class system_event : MonoBehaviour
{
    [SerializeField] private system_itemsSpawner[] itemsSpawnerSys = new system_itemsSpawner[3];
    [SerializeField] private system_playerDevice playerDeviceSys;
    [SerializeField] private system_scores scoresSys;
    [SerializeField] private GameObject[] fireObjs = new GameObject[3];
    [SerializeField] private GameObject scoreBoard;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialize();
    }

    void initialize()
    {
        for (int i = 0; i < 3; i++)
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
        playerDeviceSys.BakingSpeedChange(bakingCoe, stoveNum);
    }

    bool addFlag = false;
    void EventAddPancake()
    {
        int second = (int)scoresSys.timer % 60;

        if (second != 59)
        {
            addFlag = false;
            return;
        }

        if (addFlag) return;

        addFlag = true;
        playerDeviceSys.DecorationAdd(0);
    }

    void EventAddDeco()
    {   
        int[] nums = new int[2];
        nums = UIManager.GetItemNums(ItemsType.good);
        if(nums[0] >= nums[1])
        {
            UIManager.ResetItemUI(ItemsType.good);
            playerDeviceSys.DecorationAdd((int)ItemsType.good + 1);
        }
    }

    void EventGameOver()
    {

    }

    bool[] eventFlag = { false, false };

    void EventManager()
    {

        if ((int)(scoresSys.timer / 10) % 3 == 2 && !eventFlag[0])
        {
            EventBigFire(10.0f, eventFlag[0]);
            eventFlag[0] = true;
        }
        else if ((int)(scoresSys.timer / 10) % 3 != 2 && eventFlag[0])
        {
            EventBigFire(1.0f, eventFlag[0]);
            eventFlag[0] = false;
        }

        EventAddPancake();
        EventAddDeco();
    }
}
