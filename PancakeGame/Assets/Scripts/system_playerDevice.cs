using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class system_playerDevice : MonoBehaviour
{
    [SerializeField] private GameObject[] linesObj;
    [SerializeField] private GameObject skilletObj;
    [SerializeField] private GameObject pancakeObj;
    [SerializeField] private GameObject[] visualTransObjs = new GameObject[5];
    [SerializeField] private GameObject[] visualPrefab;
    [SerializeField] private GameObject[] placePrefab;
    [SerializeField] private GameObject[] topPlacePrefab;
    [SerializeField] private GameObject overBakePrefab;
    private Skillet skillet;

    [SerializeField] private system_scores scoreSystem;

    [SerializeField] private InputAction skilletAction;
    [SerializeField] private InputAction pancakeAction;

    [SerializeField] private float s_moveSpeed;
    [SerializeField] private float p_jumpTime;
    [SerializeField] private float p_topPos;
    [SerializeField] private float p_bakingSpeed;
    [SerializeField] private float p_bakingAddScore;
    [SerializeField] private float p_overbakingScore;
    [SerializeField] private float p_bakingResetScore;


    

    private float GRAVITY = 3.141592f;

    private float moveTime = 0.0f;

    private float[] bakingCoe = { 1.0f, 1.0f, 1.0f };

    private int visualObjNum;
    private GameObject[,] stay_visualObjs = new GameObject[4, 5];

    private List<GameObject> add_visualObjs = new List<GameObject>();

    private bool[] addFlag = new bool[4];

    public event Action JumpSucceeded;
    public event Action<int> LaneArrived;
    public event Action<int> DecorationPlaced;
    public event Action BurnStarted;
    public event Action BurnFinished;
    public bool TutorialControlsEnabled { get; set; } = true;
    public bool TutorialJumpAllowed { get; set; } = true;
    public bool TutorialBakingEnabled { get; set; }
    public bool IsFlying => skillet.pancake.flying;
    public bool IsBurning => skillet.pancake.overBake;
    public int CurrentLane => skillet.stoveNowNum;
    public float PlayerX => skilletObj.transform.position.x;
    public float LaneX(int lane) => linesObj[lane].transform.position.x;
    public bool HasPancake => visualObjNum > 0;
    public Collider PancakeCollider => skillet.pancake.col;
    private Coroutine burnRoutine;
    private float lastJumpPhysicsTime = float.NegativeInfinity;

    void Awake()
    {
        // Initialize the actual struct before input callbacks can run.
        if (pancakeObj == null || !pancakeObj.TryGetComponent<Rigidbody>(out var pancakeBody))
        {
            Debug.LogError("system_playerDevice: Pancake Obj に Rigidbody の付いたオブジェクトを設定してください。", this);
            enabled = false;
            return;
        }

        if (p_jumpTime <= 0.0f)
        {
            Debug.LogError("system_playerDevice: P Jump Time は 0 より大きい値にしてください。", this);
            enabled = false;
            return;
        }

        SkilletInit(ref skillet, pancakeBody);

        skilletAction = InputSystem.actions?.FindAction("Player/Move");
        pancakeAction = InputSystem.actions?.FindAction("Player/Jump");
        if (skilletAction == null || pancakeAction == null)
        {
            Debug.LogError("system_playerDevice: Input Actions に Player/Move と Player/Jump を設定してください。", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        // 登録
        skilletAction.performed += OnMove;
        skilletAction.canceled += OnTutorialMoveCanceled;
        pancakeAction.started += OnJump;
    }
    void OnDisable()
    {
        // 登録解除
        if (skilletAction != null)
        {
            skilletAction.performed -= OnMove;
            skilletAction.canceled -= OnTutorialMoveCanceled;
        }
        if (pancakeAction != null)
        {
            pancakeAction.started -= OnJump;
        }
    }

    // Update is called once per frame

    void SkilletInit(ref Skillet skillet, Rigidbody pancakeBody)
    {
        for(int i = 0; i < addFlag.Length; i++)
        {
            addFlag[i] = false;
        }
        skillet.obj = skilletObj;
        skillet.linesObj = linesObj;
        skillet.pancake.obj = pancakeObj;
        skillet.pancake.col = skillet.pancake.obj.GetComponent<Collider>();
        skillet.pancake.rb = pancakeBody;

        skillet.MoveSpeed = s_moveSpeed;

        skillet.stoveNowNum = 1;
        skillet.stoveNextNum = 1;

        skillet.obj.transform.position = skillet.linesObj[skillet.stoveNowNum].transform.position;

        skillet.pancake.visualObjs = new VisualObj[5];
        for (int i = 0; i < skillet.pancake.visualObjs.Length; i++)
        {
            skillet.pancake.visualObjs[i].obj = visualTransObjs[i];
            skillet.pancake.visualObjs[i].objTypeNums = new List<int>();
        }
        visualObjNum = 0;
        DecorationAdd(0);

        // Wait for actual contact with the pan before allowing the first jump.
        skillet.pancake.flying = true;
        skillet.pancake.bakingScore = 0f;
        skillet.pancake.jumpTime = p_jumpTime;
        skillet.pancake.topPos = p_topPos;
        skillet.pancake.surface = 0;
        skillet.pancake.bakingSpeed = p_bakingSpeed;
        skillet.pancake.overBake = false;

        //UIManager.UpdateBakeUI(skillet.pancake.bakingScore);
    }

    void Update()
    {
        SkilletMove(ref skillet);
        BakingScoring(ref skillet, ref skillet.pancake);
        OverBakeDemerit();

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            DecorationAdd(0);
        }
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            DecorationAdd(1);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            DecorationAdd(2);
        }
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            DecorationAdd(3);
        }
    }

    void FixedUpdate()
    {
        VirtualPancake(add_visualObjs);
    }

    void LocalPosRivise(Transform retransform, float moveDis)
    {
        Vector3 temp = retransform.transform.localPosition;
        temp.y += moveDis;
        retransform.transform.localPosition = temp;

    }

    void SkilletControl(ref Skillet skillet, int moveInt)
    {
        if (system_tutorial.IsRunning && !TutorialControlsEnabled) return;
        if (moveInt == 0)   //左
        {
            skillet.stoveNextNum = 0;
            Debug.Log("SkilletControl: Move Right" + skillet.stoveNowNum + skillet.stoveNextNum);
        }
        else if (moveInt == 1)  //真ん中
        {
            skillet.stoveNextNum = 1;
            Debug.Log("SkilletControl: Move center" + skillet.stoveNowNum + skillet.stoveNextNum);

        }
        else if (moveInt == 2)  //右
        {
            skillet.stoveNextNum = 2;
            Debug.Log("SkilletControl: Move Left" + skillet.stoveNowNum + skillet.stoveNextNum);
        }
    }

    void SkilletMove(ref Skillet skillet)
    {
        if (!(skillet.stoveNowNum == skillet.stoveNextNum))
        {
            moveTime += Time.deltaTime * skillet.MoveSpeed;
            Debug.Log($"SkilletMove: MoveTime {moveTime}, NowNum {skillet.stoveNowNum}, NextNum {skillet.stoveNextNum}");
            skillet.obj.transform.position = Vector3.Lerp(skillet.linesObj[skillet.stoveNowNum].transform.position, skillet.linesObj[skillet.stoveNextNum].transform.position, moveTime);


            if (moveTime >= 1.0f)
            {
                skillet.stoveNowNum = skillet.stoveNextNum;
                moveTime = 0.0f;
                LaneArrived?.Invoke(skillet.stoveNowNum);
            }
        }
    }

    void ResetBakingScore(ref Pancake pancake)
    {
        pancake.bakingScore -= p_bakingResetScore;
        if (pancake.bakingScore < 0f)
        {
            pancake.bakingScore = 0f;
        }
        scoreSystem.SetFireScore(pancake.bakingScore);
        UIManager.UpdateBakeUI(pancake.bakingScore);
    }


    void PancakeMove(ref Pancake pancake)
    {
        if (system_tutorial.IsRunning && (!TutorialControlsEnabled || !TutorialJumpAllowed)) return;
        if (!pancake.flying)
        {
            ResetBakingScore(ref pancake);
            skillet.pancake.flying = true;
            //ジャンプ時の効果音再生
            AudioManager.PlaySE("flypancake");

            lastJumpPhysicsTime = Time.fixedTime;

            Debug.Log("GameJump");
            float f_Vel = pancake.topPos / pancake.jumpTime + GRAVITY * pancake.jumpTime / 2;
            pancake.rb.AddForce(new Vector3(0.0f, f_Vel, 0.0f), ForceMode.VelocityChange);

            JumpSucceeded?.Invoke();
        }
    }

    void VirtualPancake(List<GameObject> visualObjs)
    {
        Vector3 targetPosition = skilletObj.transform.position;
        for (int i = 0; i < visualObjs.Count; i++)
        {
            if (visualObjs[i] == null)
            {
                break;
            }
            // Preserve the physics body's height and velocity so gravity and AddForce control Y.
            Vector3 position = visualObjs[i].transform.position;
            position.x = targetPosition.x;
            position.z = targetPosition.z;
            visualObjs[i].transform.position = position;
        }

    }

    float spanTimer = 0f;
    void OverBakeDemerit()
    {

        if (skillet.pancake.overBake)
        {
            spanTimer += Time.deltaTime;
            if (spanTimer >= 5f)
            {
                scoreSystem.AddScore(-50);
                
                spanTimer = 0f;
            }
        }
        else if (spanTimer > 0.001)
        {
            spanTimer = 0;
        }

    }

    IEnumerator overBakePan(float waitSpan)
    {
        if (overBakePrefab != null) ReplaceDecoration(0, 0, overBakePrefab);
        // パンケーキこげたときの音再生（Iwama_EffectPlayerの演出）
        AudioManager.PlaySE("burned");

        yield return new WaitForSeconds(waitSpan);
        DecorationDelete();
        yield return new WaitForSeconds(0.1f);
        ResetTutorialBake();
        burnRoutine = null;
        BurnFinished?.Invoke();
    }


    void OverBakingCheck(ref Pancake pancake)
    {
        if (pancake.bakingScore >= p_overbakingScore && !pancake.overBake)
        {
            pancake.overBake = true;
            BurnStarted?.Invoke();
            burnRoutine = StartCoroutine(overBakePan(0.5f));
        }
    }



    float bakingTimer = 0f;
    void BakingScoring(ref Skillet skillet, ref Pancake pancake)
    {
        if (system_tutorial.IsRunning && !TutorialBakingEnabled) return;
        if (visualObjNum <= 0)
        {
            return;
        }
        if (!pancake.flying)
        {
            pancake.bakingScore += bakingCoe[skillet.stoveNowNum] * pancake.bakingSpeed * Time.deltaTime;
        }
        if (pancake.overBake)
        {
            pancake.bakingScore = 0f;
        }
        if (!pancake.flying)
        {
            scoreSystem.SetFireScore(pancake.bakingScore);
            bakingTimer += Time.deltaTime * bakingCoe[skillet.stoveNowNum];
            if (bakingTimer >= 1)
            {
                scoreSystem.AddScore((int)(pancake.bakingScore * p_bakingAddScore));
                bakingTimer = 0f;
            }
            UIManager.UpdateBakeUI(pancake.bakingScore);
        }
        OverBakingCheck(ref pancake);
    }


    public void BakingSpeedChange(float coe, int stoveNum)
    {
        bakingCoe[stoveNum] = coe;
    }

    // visualObjNum is the number of occupied pancake levels.
    bool TryGetDecorationIndex(int typeNum, out int index)
    {
        if(visualObjNum <= 0)
        {
            visualObjNum = 0;
        }
        else if(!skillet.pancake.visualObjs[visualObjNum - 1].is_base && typeNum != 0)
        {
            visualObjNum--;
        }
        index = visualObjNum;
        if (typeNum < 0 || typeNum >= stay_visualObjs.GetLength(0) ||
            index < 0 || index >= skillet.pancake.visualObjs.Length)
            return false;

        return stay_visualObjs[typeNum, index] == null;
    }

    bool ValidatePlacedDecoration(int typeNum)
    {
        if (typeNum == 1 && (topPlacePrefab == null || topPlacePrefab.Length <= typeNum || topPlacePrefab[typeNum] == null))
        {
            Debug.LogError("system_playerDevice: Top Place Prefab を設定してください。", this);
            return false;
        }

        if (placePrefab == null || typeNum >= placePrefab.Length || placePrefab[typeNum] == null)
        {
            Debug.LogError($"system_playerDevice: Place Prefab[{typeNum}] を設定してください。", this);
            return false;
        }

        // Check every destination because this decoration can move down later.
        foreach (VisualObj slot in skillet.pancake.visualObjs)
        {
            if (slot.obj == null)
            {
                Debug.LogError($"system_playerDevice: 種類 {typeNum} の配置位置を全段に設定してください。", this);
                return false;
            }
        }
        return true;
    }

    public void DecorationAdd(int addNum)
    {
        if (!TryGetDecorationIndex(addNum, out _) && !ValidatePlacedDecoration(addNum)) return;
        if (visualPrefab == null || addNum >= visualPrefab.Length || visualPrefab[addNum] == null)
        {
            Debug.LogError($"system_playerDevice: Visual Prefab[{addNum}] を設定してください。", this);
            return;
        }

        GameObject cloneObj = Instantiate(visualPrefab[addNum], skilletObj.transform.position + new Vector3(0f, 10f, 0f), skilletObj.transform.rotation);
        system_visualObjs sysVis = cloneObj.GetComponent<system_visualObjs>();
        if (sysVis == null)
        {
            Debug.LogError("system_playerDevice: 落下用Prefabに system_visualObjs が必要です。", this);
            Destroy(cloneObj);
            return;
        }
        sysVis._PlayerDevice = this;
        add_visualObjs.Add(cloneObj);
    }

    public void DecorationActive(GameObject addObj, int typeNum)
    {
        if (!add_visualObjs.Remove(addObj)) return;

        // Recheck at landing: the stack may have changed during the fall.
        if (!TryGetDecorationIndex(typeNum, out int index) || !ValidatePlacedDecoration(typeNum)) return;

        VisualObj[] slots = skillet.pancake.visualObjs;
        Transform marker = slots[index].obj.transform;
        bool isTop = typeNum == 1 && index == visualObjNum - 1;
        GameObject prefab = isTop ? topPlacePrefab[typeNum] : placePrefab[typeNum];
        Vector3 spawnPosition = marker.position;

        GameObject instance = Instantiate(
            prefab, spawnPosition, marker.rotation, marker
        );

        if(typeNum == 1)
        {
            LocalPosRivise(instance.transform, -0.11f);
        }
        else if(typeNum == 2)
        {
            LocalPosRivise(instance.transform, -0.322f);
        }
        else if(typeNum == 3)
        {
            LocalPosRivise(instance.transform, -0.93f);
        }

        stay_visualObjs[typeNum, index] = instance;
        instance.SetActive(true);
        slots[index].objTypeNums.Add(typeNum);
        slots[index].is_base = slots[index].objTypeNums.Contains(0);
        if (!addFlag[typeNum])
        {
            addFlag[typeNum] = true;
        }
        visualObjNum++;
        if (typeNum == 0)
        {
            for(int i = 0; i < addFlag.Length; i++)
            {
                addFlag[i] = false;
            }
            // The previous top level is now covered by the new pancake.
            int previousTopIndex = index - 1;
            for(int i = 0; i < stay_visualObjs.GetLength(0); i++)
            {
                if (previousTopIndex >= 0 && stay_visualObjs[i, previousTopIndex] != null)
                {
                    foreach(int objTypeNum in slots[previousTopIndex].objTypeNums)
                    {
                        ReplaceDecoration(previousTopIndex, objTypeNum, placePrefab[objTypeNum]);   
                    }
                }
            }
        }
        DecorationPlaced?.Invoke(typeNum);
    }

    void ReplaceDecoration(int index, int  typeNum, GameObject prefab)
    {
        GameObject oldObj = stay_visualObjs[typeNum, index];
        if (oldObj == null) return;
        if (prefab == null)
        {
            Debug.LogError("system_playerDevice: 通常のイチゴ用 Place Prefab[1] を設定してください。", this);
            return;
        }

        Transform marker = skillet.pancake.visualObjs[index].obj.transform;
        GameObject newObj = Instantiate(prefab, marker.position, marker.rotation, marker);
        //こげエフェクト生成
        EffectManager.PlayEffect("PancakeBurn_Smoke",marker.position);
        

        if(typeNum == 1)
        {
            LocalPosRivise(newObj.transform, -0.11f);
        }
        else if(typeNum == 2)
        {
            LocalPosRivise(newObj.transform, -0.322f);
        }
        else if(typeNum == 3)
        {
            LocalPosRivise(newObj.transform, -0.93f);
        }

        // Destroy is deferred, so hide the old appearance immediately.
        oldObj.SetActive(false);
        Destroy(oldObj);
        stay_visualObjs[typeNum, index] = newObj;
        newObj.SetActive(true);
    }

    public void DecorationDelete()
    {
        VisualObj[] slots = skillet.pancake.visualObjs;
        if (visualObjNum <= 0) return;
        do
        {
            // Delete all placed decorations at the bottom, leaving the markers intact.
            for (int type = 0; type < stay_visualObjs.GetLength(0); type++)
            {
                GameObject instance = stay_visualObjs[type, 0];
                if (instance != null)
                {
                    instance.SetActive(false);
                    Destroy(instance);
                }
                stay_visualObjs[type, 0] = null;
            }

            // Move instances and metadata down; never replace the marker references.
            for (int i = 0; i < slots.Length - 1; i++)
            {
                for (int type = 0; type < stay_visualObjs.GetLength(0); type++)
                {
                    GameObject instance = stay_visualObjs[type, i + 1];
                    stay_visualObjs[type, i] = instance;
                    stay_visualObjs[type, i + 1] = null;
                    if (instance == null) continue;

                    Transform marker = slots[i].obj.transform;
                    instance.transform.SetParent(marker, true);
                    instance.transform.SetPositionAndRotation(marker.position, marker.rotation);
                    if(type == 1)
                    {
                        LocalPosRivise(instance.transform, -0.11f);   
                    }
                    else if (type == 2)
                    {
                        LocalPosRivise(instance.transform, -0.322f);
                    }
                    else if (type == 3)
                    {
                        LocalPosRivise(instance.transform, -0.93f);
                    }
                }
                slots[i].objTypeNums = new List<int>(slots[i + 1].objTypeNums);
                slots[i].is_base = slots[i].objTypeNums.Contains(0);
            }

            slots[slots.Length - 1].objTypeNums.Clear();
            slots[slots.Length - 1].is_base = false;
            visualObjNum = 0;
            for (int i = 0; i < slots.Length; i++)
                if (stay_visualObjs[0, i] != null) visualObjNum++;
            visualObjNum++;
        }
        while (visualObjNum > 0 && !slots[0].is_base);
        if (visualObjNum == 0 && !system_tutorial.IsRunning)
            system_Scene.EventSceneManager("Result");
        
    }
    void OnMove(InputAction.CallbackContext ctx)
    {
        //入力型を判定、テスト用にVector2も使えるようにしている
        if (ctx.valueType == typeof(int))   //本番センサー
        {
            int moveInt = ctx.ReadValue<int>();
            SkilletControl(ref skillet, moveInt);
        }
        else if (ctx.valueType == typeof(Vector2))  //テスト用
        {
            Vector2 moveVec = ctx.ReadValue<Vector2>();
            SkilletControl(ref skillet, (int)moveVec.x + 1);
        }
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        PancakeMove(ref skillet.pancake);
    }

    void OnTutorialMoveCanceled(InputAction.CallbackContext ctx)
    {
        if (system_tutorial.IsRunning && ctx.valueType == typeof(Vector2))
            SkilletControl(ref skillet, 1);
    }

    public void ResetTutorialBake()
    {
        skillet.pancake.bakingScore = 0f;
        bakingTimer = 0f;
        spanTimer = 0f;
        scoreSystem.SetFireScore(0f);
        UIManager.UpdateBakeUI(0f);
        skillet.pancake.overBake = false;
    }

    // Used between lessons: cancel falling decorations and start with an empty stack.
    public void ClearTutorialStack()
    {
        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = null;
        foreach (GameObject falling in add_visualObjs)
            if (falling != null) { falling.SetActive(false); Destroy(falling); }
        add_visualObjs.Clear();
        for (int i = 0; i < stay_visualObjs.GetLength(1); i++)
        {
            for (int type = 0; type < stay_visualObjs.GetLength(0); type++)
            {
                GameObject placed = stay_visualObjs[type, i];
                if (placed != null) { placed.SetActive(false); Destroy(placed); }
                stay_visualObjs[type, i] = null;
            }
            skillet.pancake.visualObjs[i].objTypeNums.Clear();
            skillet.pancake.visualObjs[i].is_base = false;
        }
        visualObjNum = 0;
        ResetTutorialBake();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("visual"))
        {
          skillet.pancake.flying = false;
          EffectManager.PlayEffect("Pancake_LandingSmoke_Opaque (1)",pancakeObj.transform.position);
          AudioManager.PlaySE("pancakedown");  
        } 
        CheckPancakeLanding(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        CheckPancakeLanding(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody == skillet.pancake.rb) skillet.pancake.flying = true;
    }

    void CheckPancakeLanding(Collision collision)
    {
        // This script is on the pan. Only its contact with the pancake body is a landing.
        // Contacts with the stove, items, or falling decorations must not unlock jumping.
        if (collision.rigidbody != skillet.pancake.rb ||
            Time.fixedTime <= lastJumpPhysicsTime + Time.fixedDeltaTime ||
            skillet.pancake.rb.linearVelocity.y > 0.1f) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            // From the pan's side, the pancake above it pushes downwards.
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                skillet.pancake.flying = false;
                return;
            }
        }
    }

    public bool AddFlagCheck(int typeNum)
    {
        return addFlag[typeNum];
    }


    //リザルトスコアの格納
    public void SaveDataForScoreCalculation()
    {
        // 構造体配列をディープコピー（参照を切って複製）して保持する
        int length = skillet.pancake.visualObjs.Length;
        GameDataHolder.SavedVisualObjs = new VisualObj[length];
        
        for (int i = 0; i < length; i++)
        {
            GameDataHolder.SavedVisualObjs[i] = new VisualObj
            {
                obj = null, // シーン移行で破棄されるためGameObjectの参照は基本切るか、必要ならプレハブID等にする
                objTypeNums = new List<int>(skillet.pancake.visualObjs[i].objTypeNums),
                is_base = skillet.pancake.visualObjs[i].is_base
            };
        }
        GameDataHolder.SavedVisualObjNum = visualObjNum;
        
        Debug.Log("リザルト用のパンケーキデータを保存しました。");
    }
}

public struct Skillet
{
    public GameObject obj;
    public GameObject[] linesObj;
    public int stoveNowNum;
    public int stoveNextNum;
    public float MoveSpeed;

    public Pancake pancake;
}

public struct Pancake
{
    public GameObject obj;
    public VisualObj[] visualObjs;
    public Rigidbody rb;
    public float bakingScore;
    public float bakingSpeed;
    public bool overBake;
    public float jumpTime;
    public float topPos;
    public bool flying;
    public int surface;
    public Collider col;
}

public struct VisualObj
{
    public GameObject obj;
    public List<int> objTypeNums;
    public bool is_base;
}