using System.Collections.Generic;
using UnityEngine;

public static class GameDataHolder
{
    // リザルトに持ち越したい VisualObj の情報を保存するリストや配列
    public static VisualObj[] SavedVisualObjs;
    public static int SavedVisualObjNum;
    
    // ★追加: ゲーム中のスコア（Score Earned）を保持する変数
    public static int SavedScore;
}