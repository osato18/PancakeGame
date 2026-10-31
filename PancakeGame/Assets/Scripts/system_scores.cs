using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class system_scores : MonoBehaviour
{
    public event System.Action ScoreChanged;
    [SerializeField] private TextMeshProUGUI scoreTxt;
    //[SerializeField] private TextMeshProUGUI fireScoreTxt;
    //[SerializeField] private Slider bakingSlider;
    [SerializeField] private TextMeshProUGUI timerText;
    private int score = 0;
    private float bakingScore = 0.0f;

    public float timer = 0.0f;

    [Header("時間制限[s]")]
    [SerializeField] private float maxTime;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
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
        //scoreTxt.text = "Score: " + score.ToString();
    }

    public void AddScore(int addScore)
    {
        score += addScore;
        UIManager.UpdateScoreUI(score);
        if (addScore != 0) ScoreChanged?.Invoke();
        //scoreTxt.text = "Score: " + score.ToString();
    }

    public void SetFireScore(float setFireScore)
    {
        bakingScore = setFireScore;
        //fireScoreTxt.text = "Baking Score: " + bakingScore.ToString("F2");
        //bakingSlider.value = bakingScore;
    }

    public void AddFireScore(float addFireScore)
    {
        bakingScore += addFireScore;
        //fireScoreTxt.text = "Baking Score: " + bakingScore.ToString("F2");
        //bakingSlider.value = bakingScore;
    }

    void Timer()
    {
        //イベント用経過時間計算
        timer += Time.deltaTime;
        //timerText.text = "Time: " + timer.ToString("F1");

        //カウントダウンタイマー用
        if (maxTime > 0f)
        {
            maxTime -= Time.deltaTime;
            maxTime = Mathf.Max(maxTime, 0f);
        }
        UIManager.UpdateTimerUI(maxTime);
    }
}
