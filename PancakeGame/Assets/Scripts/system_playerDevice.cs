using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;


public class system_playerDevice : MonoBehaviour
{
    [SerializeField] private GameObject[] linesObj;
    [SerializeField] private GameObject skilletObj;
    [SerializeField] private GameObject pancakeObj;
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
    void Update()
    {
        SkilletMove(ref skillet);
        BakingScoring(ref skillet.pancake);
        OverBakeDemerit();
    }

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
        if(!(skillet.stoveNowNum == skillet.stoveNextNum))
        {


            moveTime += Time.deltaTime * skillet.MoveSpeed;
            Debug.Log($"SkilletMove: MoveTime {moveTime}, NowNum {skillet.stoveNowNum}, NextNum {skillet.stoveNextNum}");
            skillet.obj.transform.position = Vector3.Lerp(skillet.linesObj[skillet.stoveNowNum].transform.position, skillet.linesObj[skillet.stoveNextNum].transform.position, moveTime);


            if(moveTime >= 1.0f)
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

            if(pancake.surface == 0)
            {
                ResetBakingScore(ref pancake);
                pancake.surface = 1;
            }
            else if(pancake.surface == 1)
            {
                ResetBakingScore(ref pancake);
                pancake.surface = 0;
            }

            skillet.pancake.flying = true;
        }
    }

    float spanTime = 0f;
    void OverBakeDemerit()
    {
        
        if (skillet.pancake.overBake[skillet.pancake.surface])
        {
            spanTime += Time.deltaTime;
            if(spanTime >= 5f)
            {
                scoreSystem.AddScore(-50);
                spanTime = 0f;
            }
        }
        else if(spanTime > 0.001)
        {
            spanTime = 0;
        }
        
    }


    void OverBakingCheck(ref Pancake pancake)
    {
        if (pancake.bakingScore >= p_overbakingScore)
        {
            pancake.overBake[pancake.surface] = true;
        }
    }

    float bakingTimer = 0f;
    void BakingScoring(ref Pancake pancake)
    {
        pancake.bakingScore += pancake.bakingSpeed * Time.deltaTime;
        if (pancake.overBake[pancake.surface])
        {
            pancake.bakingScore = 0f;
        }
        if (!pancake.flying)
        {
            scoreSystem.SetFireScore(pancake.bakingScore);
            bakingTimer += Time.deltaTime;
            if(bakingTimer >= 1)
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
        if(pancake.bakingScore < 0f)
        {
            pancake.bakingScore = 0f;
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
        skillet.pancake.flying = false;
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
