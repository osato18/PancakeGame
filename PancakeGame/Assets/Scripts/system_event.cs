using System;
using System.Collections;
using TMPro;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class system_event : MonoBehaviour
{
    [SerializeField] private system_itemsSpawner[] itemsSpawnerSys = new system_itemsSpawner[3];
    [SerializeField] private system_playerDevice playerDeviceSys;
    [SerializeField] private system_scores scoresSys;
    [SerializeField] private GameObject[] normalfireObjs=new GameObject[3];//通常のコンロの火
    [SerializeField] private GameObject[] fireObjs = new GameObject[3];
    [SerializeField] private GameObject scoreBoard;
    [SerializeField] private GameObject[] cautionObj = new GameObject[3];
    public bool TutorialReady => fireObjs != null && fireObjs.Length >= 3 &&
        fireObjs[0] != null && fireObjs[1] != null && fireObjs[2] != null &&
        cautionObj != null && cautionObj.Length >= 3 &&
        cautionObj[0] != null && cautionObj[1] != null && cautionObj[2] != null;
    public int TutorialFireLane { get; private set; } = -1;
    public bool TutorialFireActive => TutorialFireLane >= 0 && fireObjs[TutorialFireLane].activeInHierarchy;

    public void ShowTutorialFireWarning(int lane)
    {
        SetTutorialFire(-1);
        if (lane >= 0 && lane < cautionObj.Length && cautionObj[lane] != null)
            cautionObj[lane].SetActive(true);
    }

    public void SetTutorialFire(int lane)
    {
        StopAllCoroutines();
        for (int i = 0; i < fireObjs.Length; i++)
        {
            fireObjs[i].SetActive(i == lane);
            playerDeviceSys.BakingSpeedChange(i == lane ? 10f : 1f, i);
        }
        foreach (GameObject caution in cautionObj)
            if (caution != null) caution.SetActive(false);
        TutorialFireLane = lane;
    }
    [SerializeField] private float bigFireSpeedCoe;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialize();
        //BGM再生
        AudioManager.PlayBGM("playingBGM");
    }

    void initialize()
    {
        for (int i = 0; i < 3; i++)
        {
            fireObjs[i].SetActive(false);
        }
                for(int i = 0; i < cautionObj.Length; i++)
        {
            cautionObj[i].SetActive(false);
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (system_tutorial.IsRunning) return;
        EventManager();
    }

    IEnumerator EventFireCaution(int stoveNum, float waitSpan, Action nextAction)
    {

        cautionObj[stoveNum].SetActive(true);
        //警告音再生
        AudioManager.PlaySE("Nearfire");
        yield return new WaitForSeconds(waitSpan);
        cautionObj[stoveNum].SetActive(false);
         //通常火false
        //normalfireObjs[stoveNum].SetActive(false);
        //燃えているSE再生
        AudioManager.PlaySE("Nowfire");
        
        nextAction?.Invoke();
    }

    int stoveNum = 0;
    void EventBigFire(float bakingCoe, bool eventFlag)
    {
        if (!eventFlag)
        {
            int r = UnityEngine.Random.Range(0, 1000) % 3;
            stoveNum = r;
           
            StartCoroutine(EventFireCaution(stoveNum, 1.0f, () => fireObjs[stoveNum].SetActive(true)));
            
        }
        else
        {
            //普通の火戻す
            //normalfireObjs[stoveNum].SetActive(true);
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
        for(int i = 0; i < 3; i++)
        {
            ItemsType type = ItemsType.strawberry + i;
            nums = UIManager.GetItemNums(type);
            if (nums[0] >= nums[1])
            {
                UIManager.ResetItemUI(type);
                playerDeviceSys.DecorationAdd((int)type + 1);
            }
        }
    }
    
    bool[] eventFlag = { false, false };

    void EventManager()
    {

        if ((int)(scoresSys.timer / 10) % 3 == 2 && !eventFlag[0])
        {
            EventBigFire(bigFireSpeedCoe, eventFlag[0]);
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
