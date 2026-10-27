using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using NUnit.Framework;

public class UIManager : MonoBehaviour
{
    [Header("各アイテムゲージのImage")]
    [SerializeField] private Image[] _itemGauges;

    [Header("焼きゲージのImage")]
    [SerializeField] private Image _bakeGauge;

    [Header("焼きゲージの最大値(system_playerDeviceのp_overbakingScoreと同じ値にする)")]
    [SerializeField] private float _maxBake;

    [Header("スコア表示のTextオブジェクト")]
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("タイマー表示用のTextオブジェクト")]
    [SerializeField] private TextMeshProUGUI _timerText;

    private static UIManager _instance;

    //test
    public int testCurrentItemNum;

    private void Awake()
    {
        _instance = this;
    }

    private void Update()
    {
        UIManager.UpdateItemUI(ItemsType.good, testCurrentItemNum);
    }

    public static void UpdateItemUI(ItemsType updateItemType, int currentItemNum)
    {
        _instance._itemGauges[(int)updateItemType].fillAmount = (float)currentItemNum / 5;
        Debug.Log(_instance._itemGauges[(int)updateItemType].fillAmount);
    }

    public static void ResetItemUI(ItemsType resetItemType)
    {
        _instance._itemGauges[(int)resetItemType].fillAmount = 0;
    }

    //PlayerDeviceから呼ぶ
    //焼きゲージの値を計算した後にこの関数を呼ぶ
    public static void UpdateBakeUI(float currentBake)
    {
        float updateBake = currentBake / _instance._maxBake;
        _instance._bakeGauge.fillAmount = updateBake;
    }

    //スコアの文字表示
    public static void UpdateScoreUI(int currentScore)
    {
        _instance._scoreText.text = currentScore.ToString();
    }

    //タイマーの文字更新
    public static void UpdateTimerUI(float currentTime)
    {
        TimeSpan time = TimeSpan.FromSeconds(currentTime);
        _instance._timerText.text = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}:{time.Milliseconds:D3}";
    }
}
