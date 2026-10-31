using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class system_scores : MonoBehaviour
{
    public event System.Action ScoreChanged;
    [SerializeField] private TextMeshProUGUI scoreTxt;
    [SerializeField] private TextMeshProUGUI timerText;
    private int score = 0;
    private float bakingScore = 0.0f;

    public float timer = 0.0f;

    [Header("時間制限[s]")]
    [SerializeField] private float maxTime;

    // タイマー終了時に一度だけ遷移処理を呼ぶためのフラグ
    private bool isTimesUp = false;

    void Start()
    {

    }

    void Update()
    {
        if (system_tutorial.IsRunning) return;
        Timer();
    }

    public void SetScore(int setScore)
    {
        bool changed = setScore != score;
        score = setScore;
        UIManager.UpdateScoreUI(score);
        if (changed) ScoreChanged?.Invoke();
    }

    public void AddScore(int addScore)
    {
        score += addScore;
        UIManager.UpdateScoreUI(score);
        if (addScore != 0) ScoreChanged?.Invoke();
    }

    public void SetFireScore(float setFireScore)
    {
        bakingScore = setFireScore;
    }

    public void AddFireScore(float addFireScore)
    {
        bakingScore += addFireScore;
    }

    void Timer()
    {
        // すでにタイムアップしていれば何もしない
        if (isTimesUp) return;

        // イベント用経過時間計算
        timer += Time.deltaTime;

        // カウントダウンタイマー用
        if (maxTime > 0f)
        {
            maxTime -= Time.deltaTime;
            
            // タイマーが 0 以下になった瞬間
            if (maxTime <= 0f)
            {
                maxTime = 0f;
                isTimesUp = true;
                TimesUpAction(); // タイムアップ時の処理を実行
            }
        }
        UIManager.UpdateTimerUI(maxTime);
    }

    // ★ タイムアップ時にデータ保存とシーン遷移を行うメソッド
    void TimesUpAction()
    {
        Debug.Log("タイムアップ！リザルトへ移行します。");

        GameDataHolder.SavedScore = score;

        // シーン内から system_playerDevice を取得してデータを保存する
        system_playerDevice playerDevice = FindAnyObjectByType<system_playerDevice>();
        if (playerDevice != null)
        {
            playerDevice.SaveDataForScoreCalculation();
        }
        else
        {
            Debug.LogWarning("system_playerDevice が見つかりませんでした。データが保存されません。");
        }

        // Resultシーンへ遷移
        system_Scene.EventSceneManager("Result");
    }
}