using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UICursorManager : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button[] buttons;

    // 現在選択しているボタンの番号
    private int _currentIndex = 1;

    private void Start()
    {
        // 起動時に初期選択（中央：1）の色を反映させる
        UpdateSelection();
    }

    /// <summary>
    /// Move入力
    /// 0：左、1：中央、2：右
    /// </summary>
    public void OnMove(InputValue value)
    {
        int input = (int)value.Get<int>();
        if (input < 0 || input > 2)
            return;

        _currentIndex = input;

        UpdateSelection();
    }

    /// <summary>
    /// Jump入力
    /// 選択中のボタンのOnClickを実行
    /// </summary>
    public void OnJump(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (buttons == null || buttons.Length != 3)
            return;

        if (buttons[_currentIndex] == null)
            return;

        buttons[_currentIndex].onClick.Invoke();
    }

    /// <summary>
    /// 選択状態の更新
    /// </summary>
    private void UpdateSelection()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            ColorBlock colors = buttons[i].colors;

            // 選択中の場合は赤、そうでない場合は白にする
            Color targetColor = (i == _currentIndex) ? Color.red : Color.white;

            // 各状態の色を統一して上書きする
            colors.normalColor = targetColor;
            colors.highlightedColor = targetColor;
            colors.selectedColor = targetColor;
            colors.pressedColor = targetColor;

            buttons[i].colors = colors;
        }
    }

    public void StartButtonClick()
    {
        Debug.Log("スタート！");
        SceneManager.LoadScene("EffectTest");
    }

    public void TutorialButtonClick()
    {
        Debug.Log("チュートリアル");
        //SceneManager.LoadScene("Tutorial");
    }

    public void CreditButtonClick()
    {
        Debug.Log("クレジット");
    }

    public void ReturnStartButtonClick()
    {
        Debug.Log("スタートに戻る");
        SceneManager.LoadScene("Start");
    }
}