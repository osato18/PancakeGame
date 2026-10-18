using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class system_scores : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreTxt;
    [SerializeField] private TextMeshProUGUI fireScoreTxt;
    [SerializeField] private Slider bakingSlider;
    private int score = 0;
    private float bakingScore = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetScore(int setScore)
    {
        score = setScore;
        scoreTxt.text = "Score: " + score.ToString();
    }

    public void AddScore(int addScore)
    {
        score += addScore;
        scoreTxt.text = "Score: " + score.ToString();
    }

    public void SetFireScore(float setFireScore)
    {
        bakingScore = setFireScore;
        fireScoreTxt.text = "Baking Score: " + bakingScore.ToString("F2");
        bakingSlider.value = bakingScore;
    }

    public void AddFireScore(float addFireScore)
    {
        bakingScore += addFireScore;
        fireScoreTxt.text = "Baking Score: " + bakingScore.ToString("F2");
        bakingSlider.value = bakingScore;
    }
}
