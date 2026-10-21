using UnityEngine;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    private static EffectManager instance;

    [Header("エフェクトのプレハブをアタッチ")]
    public GameObject[] effectPrefabs;

    private Dictionary<string, GameObject> effectDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // AudioManagerと同様のシングルトン＆DontDestroyOnLoadの設定
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // プレハブの配列を辞書に登録[cite: 1]
        foreach (var effect in effectPrefabs)
        {
            if (effect != null && !effectDictionary.ContainsKey(effect.name))
            {
                effectDictionary.Add(effect.name, effect);
            }
        }
    }

    /// <summary>
    /// 指定した名前のエフェクトを指定座標に再生する
    /// </summary>
    /// <param name="name">エフェクト（プレハブ）の名前</param>
    /// <param name="position">生成する座標</param>
    public static void PlayEffect(string name, Vector3 position)
    {
        if (instance == null) return;

        if (instance.effectDictionary.TryGetValue(name, out GameObject prefab))
        {
            // 指定された座標にエフェクトを生成
            Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("指定されたエフェクト " + name + " が見つかりません");
        }
    }
}