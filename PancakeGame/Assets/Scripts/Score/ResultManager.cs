using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

public class ResultManager : MonoBehaviour
{
    [Header("UI 参照")]
    [SerializeField] private TextMeshProUGUI resultText;       // リザルト表示用テキスト[cite: 10]
    [SerializeField] private GameObject returnStartButton;     // タイトルへ戻るなどのボタン[cite: 10]

    [Header("スコア設定")]
    [SerializeField] private int baseScorePerPancake = 100;    // パンケーキ1枚あたりの基準点数[cite: 10]
    [SerializeField] private int strawberryScore = 50;         // イチゴ1つあたりのスコア[cite: 10]
    [SerializeField] private int creamScore = 30;              // クリーム1つあたりのスコア[cite: 10]
    [SerializeField] private int blueberryScore = 40;                  //ブルーベリー１つあたりのスコア

    [Header("名付けのアイテムの個数閾値")]
    [SerializeField] private int namedStrawberryCount = 20;
    [SerializeField] private int namedCreamCount = 20;
    [SerializeField] private int namedBlueberryCount = 20;

    //アイテム個数のカウント
    private int pancakeCount = 0;
    private int strawberryCount = 0;
    private int creamCount = 0;
    private int blueberryCount = 0;

    //二つ名の文字列
    private String namedStrawberryStr="";
    private String namedCreamStr="";
    private String namedBlueberryStr="";


    void Start()
    {
        if (returnStartButton != null)
        {
            returnStartButton.SetActive(false);
        }

        StartCoroutine(ResultSequenceRoutine());
    }

    IEnumerator ResultSequenceRoutine()
    {
        // ① 演出部（2秒待機）[cite: 10]
        Debug.Log("① 演出部開始（待機中...）");
        yield return new WaitForSeconds(2.0f);

        // ② スコアの計算と画面への表示[cite: 10]
        Debug.Log("② スコア表示処理を実行");
        CalculateAndDisplayScore();

        // 2秒待機[cite: 10]
        yield return new WaitForSeconds(2.0f);

        // ③ ReturnStartButtonを有効化[cite: 10]
        if (returnStartButton != null)
        {
            returnStartButton.SetActive(true);
            Debug.Log("③ ReturnStartButton を有効化しました");
        }
    }

    void CalculateAndDisplayScore()
    {
        if (GameDataHolder.SavedVisualObjs == null)
        {
            Debug.LogWarning("保存されたパンケーキのデータ（GameDataHolder.SavedVisualObjs）がありません。");
            if (resultText != null)
            {
                resultText.text = "スコアデータがありません。";
            }
            return;
        }

        // ① Score Earned は system_scores から引き継いだスコアを使用
        int scoreEarned = GameDataHolder.SavedScore;

        pancakeCount = 0;
        strawberryCount = 0;
        creamCount = 0;
        blueberryCount = 0;

        // 保存された各スロット（段）の情報を走査して集計[cite: 6, 7]
        for (int i = 0; i < GameDataHolder.SavedVisualObjs.Length; i++)
        {
            VisualObj slot = GameDataHolder.SavedVisualObjs[i];

            // 土台（パンケーキ）の集計
            if (slot.is_base)
            {
                pancakeCount++;
            }

            // 具材（トッピングの種類: objTypeNums）の集計[cite: 6, 7]
            if (slot.objTypeNums != null)
            {
                foreach (int typeNum in slot.objTypeNums)
                {
                    switch (typeNum)
                    {
                        case 0:
                            // パンケーキ本体などの処理
                            break;
                        case 1:
                            strawberryCount++; // イチゴ
                            break;
                        case 2:
                            creamCount++;      // クリーム
                            break;
                        case 3:
                            blueberryCount++;   // ブルーベリー
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // ② Pancake Bonus = リザルト時のパンケーキの枚数 × パンケーキの基準点数
        int pancakeBonus = pancakeCount * baseScorePerPancake;

        // デコレーションボーナスの計算
        int strawberryBonus = strawberryCount * strawberryScore;
        int creamBonus = creamCount * creamScore;
        int blueberryBonus = blueberryCount * blueberryScore;

        // 合計スコアの計算（Score Earned と 各種ボーナスを合算）
        int totalScore = scoreEarned + pancakeBonus + strawberryBonus + creamBonus + blueberryBonus;

        // リザルトテキストに反映[cite: 8, 10]
        if (resultText != null)
        {
            resultText.text =
                $"<Score>\n" +
                $"Score Earned : {scoreEarned:D4}\n" +
                $"Pancake Bonus : {pancakeBonus:D4} × {pancakeCount}\n" +
                $"Decoration Bonus : \n" +
                $"- Strawberry : {strawberryBonus:D4} × {strawberryCount}\n" +
                $"- Cream : {creamBonus:D4} × {creamCount}\n" +
                $"- Blueberry : {blueberryBonus:D4} × {blueberryCount}\n" +
                $"-------------------------------------------------\n" +
                $"Total Score : {totalScore:D5}\n"+
                $"You made the {namedStrawberryStr} {namedCreamStr} {namedBlueberryStr} pancakes";
        }

        Debug.Log($"リザルト計算完了 - 合計スコア: {totalScore}");
    }

    private void NamedPancake()
    {
        if (strawberryCount>namedStrawberryCount)
        {
            namedStrawberryStr="Strawberry Very Very";
        }
        if (creamCount>namedCreamCount)
        {
            namedCreamStr="Cream Very Very";
        }
        if (blueberryCount>namedBlueberryCount)
        {
            namedBlueberryStr="Blueberry Very Very";
        }
    }
}