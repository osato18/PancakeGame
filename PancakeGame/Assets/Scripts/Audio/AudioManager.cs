using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [Header("BGMの音源ファイルをアタッチ")]
    public AudioClip[] bgmClips;
    [Header("SEの音源ファイルをアタッチ")]
    public AudioClip[] seClips;

    private AudioSource bgmAudioSource;
    private AudioSource seAudioSource;

    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> seDictionary = new Dictionary<string, AudioClip>();

    private bool burn;

    private void Awake()
    {
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

        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        seAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.loop = true;

        foreach (var bgm in bgmClips)
        {
            if (bgm != null && !bgmDictionary.ContainsKey(bgm.name))
            {
                bgmDictionary.Add(bgm.name, bgm);
            }
        }

        foreach (var se in seClips)
        {
            if (se != null && !seDictionary.ContainsKey(se.name))
            {
                seDictionary.Add(se.name, se);
            }
        }
    }

    public static void PlayBGM(string name)
    {
        if (instance == null) return;

        if (instance.bgmDictionary.TryGetValue(name, out AudioClip clip))
        {
            instance.bgmAudioSource.clip = clip;
            instance.bgmAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("指定されたBGM" + name + "が見つかりません");
        }
    }

    public static void PlaySE(string name)
    {
        if (instance == null) return;

        if (instance.seDictionary.TryGetValue(name, out AudioClip clip))
        {
                            
            instance.seAudioSource.PlayOneShot(clip);
        }
        
        else
        {
            Debug.LogWarning("指定されたSE" + name + "が見つかりません");
        }
    }

}
