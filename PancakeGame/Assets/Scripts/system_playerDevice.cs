using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class system_playerDevice : MonoBehaviour
{
    [SerializeField] private GameObject[] linesObj;
    [SerializeField] private GameObject skilletObj;
    [SerializeField] private GameObject pancakeObj;
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
        virtualPancake(ref skillet.pancake.visualObjs);
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
        if (!pancake.flying && !pancake.visualObjs[0].flying && (skillet.stoveNowNum == skillet.stoveNextNum))
        {
            skillet.pancake.flying = true;
            pancake.visualObjs[0].flying = true;

            Debug.Log("GameJump");
            float f_Vel = pancake.topPos / pancake.jumpTime + GRAVITY * pancake.jumpTime / 2;
            pancake.rb.AddForce(new Vector3(0.0f, f_Vel, 0.0f), ForceMode.VelocityChange);
            for (int i = pancake.visualObjs.Length - 1; i >= 0; i--)
            {
                if (pancake.visualObjs[i].obj == null || pancake.visualObjs[i].rb == null)
                {
                    continue;
                }
                pancake.visualObjs[i].rb.AddForce(new Vector3(0.0f, f_Vel + i, 0.0f), ForceMode.VelocityChange);
            }


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

        }
    }

    void virtualPancake(ref visualObj[] visualObjs)
    {
        Vector3 targetPosition = skilletObj.transform.position;
        for (int i = 0; i < visualObjs.Length; i++)
        {
            if (visualObjs[i].obj == null || visualObjs[i].rb == null)
            {
                break;
            }
            // Preserve the physics body's height and velocity so gravity and AddForce control Y.
            Rigidbody body = visualObjs[i].rb;
            Vector3 position = body.position;
            position.x = targetPosition.x;
            position.z = targetPosition.z;
            body.position = position;
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
            //scoreSystem.SetFireScore(pancake.bakingScore);
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
        if(visualObjNum >= 5)
        {
            return;
        }
        skillet.pancake.visualObjs[visualObjNum].obj = Instantiate(visualPrefab[addNum], pancakeObj.transform.position + new Vector3(0f, 10f, 0f), pancakeObj.transform.rotation);
        skillet.pancake.visualObjs[visualObjNum].obj.transform.SetParent(pancakeObj.transform);
        skillet.pancake.visualObjs[visualObjNum].rb = skillet.pancake.visualObjs[visualObjNum].obj.GetComponent<Rigidbody>();
        skillet.pancake.visualObjs[visualObjNum].flying = false;
        if (addNum == 0)
        {
            skillet.pancake.visualObjs[visualObjNum].is_base = true;
        }
        else
        {
            skillet.pancake.visualObjs[visualObjNum].is_base = false;
        }
        visualObjNum++;
    }

    public void pancakeDelete()
    {
        if(visualObjNum < 0)
        {
            return;
        }
        Destroy(skillet.pancake.visualObjs[0].obj);
        skillet.pancake.overBake[0] = false;
        skillet.pancake.overBake[1] = false;
        skillet.pancake.surface = 0;
        visualObjNum--;
        for (int i = 1; i < 5; i++)
        {
            if (skillet.pancake.visualObjs[i].obj == null)
            {
                break;
            }
            skillet.pancake.visualObjs[i - 1] = skillet.pancake.visualObjs[i];
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
        if(collision.gameObject == pancakeObj)
        {
            skillet.pancake.flying = false;    
        }
        else if(collision.gameObject == skillet.pancake.visualObjs[0].obj)
        {
            skillet.pancake.visualObjs[0].flying = false;
        }
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
    public Rigidbody rb;
    public bool is_base;
    public bool flying;
}
