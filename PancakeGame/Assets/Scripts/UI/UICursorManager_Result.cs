using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UICursorManager_Result : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button[] buttons;

    // 現在選択しているボタンの番号
    private int _currentIndex = 0;

    /// <summary>
    /// Move入力
    /// 0：左、1：中央、2：右
    /// </summary>
    public void OnMove(InputValue value)
    {
        if (buttons == null || buttons.Length == 0)
            return;

        // ボタンが1つの場合は常に[0]を選択
        if (buttons.Length == 1)
        {
            _currentIndex = 0;
        }
        else
        {
            _currentIndex = Mathf.Clamp(_currentIndex, 0, buttons.Length - 1);
        }

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

        if (buttons == null || buttons.Length == 0)
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
    if (buttons == null)
        return;

    for (int i = 0; i < buttons.Length; i++)
    {
        if (buttons[i] == null)
            continue;

        ColorBlock colors = buttons[i].colors;

        Color targetColor = (i == _currentIndex)
            ? Color.red
            : Color.white;

        // 各状態の色を統一
        colors.normalColor = targetColor;
        colors.highlightedColor = targetColor;
        colors.selectedColor = targetColor;
        colors.pressedColor = targetColor;

        buttons[i].colors = colors;
    }
}

    public void ReturnStartButtonClick()
    {
        Debug.Log("スタートに戻る");
        SceneManager.LoadScene("Start");
    }
}