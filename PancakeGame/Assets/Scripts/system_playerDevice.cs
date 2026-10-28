using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class system_playerDevice : MonoBehaviour
{
    [SerializeField] private GameObject[] linesObj;
    [SerializeField] private GameObject skilletObj;
    [SerializeField] private GameObject pancakeObj;
    [SerializeField] private GameObject[] visualObjs = new GameObject[5];
    [SerializeField] private GameObject[] visualPrefab;
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

    private List<GameObject> add_visualObjs = new List<GameObject>();

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
        skilletAction.canceled += OnMoveCanceled;
        pancakeAction.started += OnJump;
    }
    void OnDisable()
    {
        // 登録解除
        if (skilletAction != null)
        {
            skilletAction.performed -= OnMove;
            skilletAction.canceled -= OnMoveCanceled;
        }
        if (pancakeAction != null)
        {
            pancakeAction.started -= OnJump;
        }
    }

    // Update is called once per frame

    void SkilletInit(ref Skillet skillet, Rigidbody pancakeBody)
    {
        skillet.obj = skilletObj;
        skillet.linesObj = linesObj;
        skillet.pancake.obj = pancakeObj;
        skillet.pancake.col = skillet.pancake.obj.GetComponent<Collider>();
        skillet.pancake.rb = pancakeBody;

        skillet.MoveSpeed = s_moveSpeed;

        skillet.stoveNowNum = 1;
        skillet.stoveNextNum = 1;

        skillet.obj.transform.position = skillet.linesObj[skillet.stoveNowNum].transform.position;

        skillet.pancake.visualObjs = new visualObj[5];
        for(int i = 0; i < skillet.pancake.visualObjs.Length; i++)
        {
            skillet.pancake.visualObjs[i].obj = visualObjs[i];
            skillet.pancake.visualObjs[i].obj.SetActive(false);
        }
        visualObjNum = 0;
        PancakeAdd(0);

        skillet.pancake.flying = false;
        skillet.pancake.bakingScore = 0f;
        skillet.pancake.jumpTime = p_jumpTime;
        skillet.pancake.topPos = p_topPos;
        skillet.pancake.surface = 0;
        skillet.pancake.bakingSpeed = p_bakingSpeed;
        skillet.pancake.overBake = new bool[2];
        skillet.pancake.overBake[0] = false;
        skillet.pancake.overBake[1] = false;
    }

    void Update()
    {
        SkilletMove(ref skillet);
        BakingScoring(ref skillet, ref skillet.pancake);
        OverBakeDemerit();
    }

    void FixedUpdate()
    {
        virtualPancake(add_visualObjs);
    }

    void SkilletControl(ref Skillet skillet, Vector2 moveVec)
    {
        if (!skillet.pancake.flying)
        {
            if (moveVec.x > 0)
            {
                skillet.stoveNextNum = 2;
                Debug.Log("SkilletControl: Move Right" + skillet.stoveNowNum + skillet.stoveNextNum);
            }
            else if (moveVec.x < 0)
            {
                skillet.stoveNextNum = 0;
                Debug.Log("SkilletControl: Move Left" + skillet.stoveNowNum + skillet.stoveNextNum);
            }
            else if (moveVec.x == 0)
            {
                skillet.stoveNextNum = 1;
                Debug.Log("SkilletControl: Move center" + skillet.stoveNowNum + skillet.stoveNextNum);
            }
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
            }
        }
    }

    void PancakeMove(ref Pancake pancake)
    {
        if (!pancake.flying && (skillet.stoveNowNum == skillet.stoveNextNum))
        {
            Debug.Log("GameJump");
            float f_Vel = pancake.topPos / pancake.jumpTime + GRAVITY * pancake.jumpTime / 2;
            pancake.rb.AddForce(new Vector3(0.0f, f_Vel, 0.0f), ForceMode.VelocityChange);

            if (pancake.surface == 0)
            {
                ResetBakingScore(ref pancake);
                pancake.surface = 1;
            }
            else if (pancake.surface == 1)
            {
                ResetBakingScore(ref pancake);
                pancake.surface = 0;
            }

            skillet.pancake.flying = true;
        }
    }

    void virtualPancake(List<GameObject> visualObjs)
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

        if (skillet.pancake.overBake[skillet.pancake.surface])
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


    void OverBakingCheck(ref Pancake pancake)
    {
        if (pancake.bakingScore >= p_overbakingScore && !pancake.overBake[pancake.surface])
        {
            pancake.overBake[pancake.surface] = true;
        }
        if (pancake.overBake[0] && pancake.overBake[1])
        {
            skillet.pancake.overBake[0] = false;
            skillet.pancake.overBake[1] = false;
            pancakeDelete();
        }
    }

    float bakingTimer = 0f;
    void BakingScoring(ref Skillet skillet, ref Pancake pancake)
    {
        pancake.bakingScore += bakingCoe[skillet.stoveNowNum] * pancake.bakingSpeed * Time.deltaTime;
        if (pancake.overBake[pancake.surface])
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
        }
        OverBakingCheck(ref pancake);
    }

    void ResetBakingScore(ref Pancake pancake)
    {
        pancake.bakingScore -= p_bakingResetScore;
        if (pancake.bakingScore < 0f)
        {
            pancake.bakingScore = 0f;
        }
    }

    public void bakingSpeedChange(float coe, int stoveNum)
    {
        bakingCoe[stoveNum] = coe;
    }

    public void PancakeAdd(int addNum)
    {
        if(visualObjNum >= 5) return;

        GameObject cloneObj = Instantiate(visualPrefab[addNum], skilletObj.transform.position + new Vector3(0f, 10f, 0f), skilletObj.transform.rotation);
        system_visualObjs sysVis = cloneObj.GetComponent<system_visualObjs>();
        sysVis._PlayerDevice = this;
        add_visualObjs.Add(cloneObj);
        if (addNum == 0)
        {
            skillet.pancake.visualObjs[visualObjNum].is_base = true;
        }
        else
        {
            skillet.pancake.visualObjs[visualObjNum].is_base = false;
        }
    }

    public void PancakeActive(GameObject addObj)
    {
        skillet.pancake.visualObjs[visualObjNum].obj.SetActive(true);
        visualObjNum++;
        add_visualObjs.Remove(addObj);
    }

    public void pancakeDelete()
    {
        if(visualObjNum < 0) return;
        skillet.pancake.visualObjs[visualObjNum - 1].obj.SetActive(false);
        skillet.pancake.obj.transform.position += new Vector3(0f, 0.13f, 0f);
        visualObjNum--;
        if (!skillet.pancake.visualObjs[visualObjNum].is_base)
        {
            pancakeDelete();
        }
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 move = ctx.ReadValue<Vector2>();
        SkilletControl(ref skillet, move);
    }
    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        PancakeMove(ref skillet.pancake);
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer != LayerMask.NameToLayer("visual"))   skillet.pancake.flying = false;
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
    public visualObj[] visualObjs;
    public Rigidbody rb;
    public float bakingScore;
    public float bakingSpeed;
    public bool[] overBake;
    public float jumpTime;
    public float topPos;
    public bool flying;
    public int surface;
    public Collider col;
}

public struct visualObj
{
    public GameObject obj;
    public bool is_base;
}
