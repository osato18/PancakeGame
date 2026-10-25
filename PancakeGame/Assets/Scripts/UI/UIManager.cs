using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("各アイテムゲージのImage")]
    [SerializeField] private Image[] _itemGauges;

    [Header("焼きゲージのImage")]
    [SerializeField] private Image _bakeGauge;

    [Header("焼きゲージの最大値")]
    [SerializeField] private float _maxBake;
    private static UIManager _instance;

    private void Awake()
    {
        _instance = this;
    }
    private void Start()
    {

    }

    public static void UpdateItemUI(ItemsType updateItemType,int currentItemNum)
    {
        _instance._itemGauges[(int)updateItemType].fillAmount=currentItemNum/5;
    }

    public static void ResetItemUI(ItemsType resetItemType)
    {
        _instance._itemGauges[(int)resetItemType].fillAmount=0;
    }

    //PlayerDeviceから呼ぶ
    //焼きゲージの値を計算した後にこの関数を呼ぶ
    public static void UpdateBakeUI(float currentBake)
    {
        float updateBake = currentBake / _instance._maxBake;
        _instance._bakeGauge.fillAmount=updateBake;
    }
}
