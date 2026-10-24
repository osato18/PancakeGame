using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICursorManager : MonoBehaviour
{
    [Tooltip("追従させたいカーソルUIのRectTransform")]
    [SerializeField] private RectTransform cursorRectTransform;

    [Tooltip("ボタンの中心からのオフセット（必要に応じて微調整用）")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    private void Update()
    {
        // 現在EventSystemで選択されているゲームオブジェクトを取得
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected != null)
        {
            // 選択されたオブジェクトがUI要素（Selectable）を持っているか確認
            Selectable selectable = currentSelected.GetComponent<Selectable>();
            if (selectable != null)
            {
                RectTransform targetRect = currentSelected.GetComponent<RectTransform>();
                if (targetRect != null && cursorRectTransform != null)
                {
                    // ワールド座標ベースでカーソルの位置を選択されたボタンの位置に合わせる
                    Vector3 targetPosition = targetRect.position;
                    cursorRectTransform.position = targetPosition + (Vector3)offset;

                    // カーソルが非表示になっていれば表示する
                    if (!cursorRectTransform.gameObject.activeSelf)
                    {
                        cursorRectTransform.gameObject.SetActive(true);
                    }
                }
            }
        }
        else
        {
            // 何も選択されていない場合はカーソルを非表示にする（必要に応じて）
            if (cursorRectTransform != null && cursorRectTransform.gameObject.activeSelf)
            {
                cursorRectTransform.gameObject.SetActive(false);
            }
        }
    }
}