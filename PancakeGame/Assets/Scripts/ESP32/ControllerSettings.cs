using UnityEngine;

// 右クリックのCreateメニューに追加するための属性
[CreateAssetMenu(fileName = "ESP32ControllerSettings", menuName = "ScriptableObjects/ControllerSettings")]
public class ControllerSettings : ScriptableObject
{
    // Inspectorで編集したい変数を定義する
    public float jumpRangeMin=0;
    public float jumpRangeMax=0;
}