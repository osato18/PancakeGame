using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;


public class system_playerDevice : MonoBehaviour
{
    [SerializeField] private GameObject[] linesObj;
    [SerializeField] private GameObject skilletObj;
    [SerializeField] private GameObject pancakeObj;
    private Skillet skillet;

    [SerializeField] private InputAction skilletAction;
    [SerializeField] private InputAction pancakeAction;

    [SerializeField] private float p_jumpTime;
    [SerializeField] private float p_topPos;

    private float GRAVITY = 3.141592f;

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

    }

    void SkilletInit(ref Skillet skillet, Rigidbody pancakeBody)
    {
        skillet.obj = skilletObj;
        skillet.linesObj = linesObj;
        skillet.pancake.obj = pancakeObj;
        skillet.pancake.col = skillet.pancake.obj.GetComponent<Collider>();
        skillet.pancake.rb = pancakeBody;

        skillet.stoveNum = 1;

        skillet.pancake.flying = false;
        skillet.pancake.bakingScore = new float[2];
        skillet.pancake.jumpTime = p_jumpTime;
        skillet.pancake.topPos = p_topPos;
        skillet.pancake.surfase = 0;
    }

    void SkilletMove(Skillet skillet, Vector2 moveVec)
    {
        if (!skillet.pancake.flying)
        {
            if (moveVec.x > 0 && skillet.stoveNum < 2)
            {
                skillet.stoveNum++;
            }
            else if (moveVec.x > 0 && skillet.stoveNum > 0)
            {
                skillet.stoveNum--;
            }
        }
    }

    void PancakeMove(Pancake pancake)
    {
        if (!pancake.flying)
        {
            float f_Vel = pancake.topPos / pancake.jumpTime + GRAVITY * pancake.jumpTime / 2;
            pancake.rb.AddForce(new Vector3(0.0f, f_Vel, 0.0f), ForceMode.VelocityChange);
            skillet.pancake.flying = true;
        }
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 move = ctx.ReadValue<Vector2>();
        SkilletMove(skillet, move);
    }
    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {

    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        PancakeMove(skillet.pancake);
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
    public int stoveNum;

    public Pancake pancake;
}

public struct Pancake
{
    public GameObject obj;
    public Rigidbody rb;
    public float[] bakingScore;
    public float jumpTime;
    public float topPos;
    public bool flying;
    public int surfase;
    public Collider col;
}
