using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Set the mode before player/spawner Awake and Start can launch normal game events.
[DefaultExecutionOrder(-500)]
public class system_tutorial : MonoBehaviour
{
    public static bool IsRunning { get; private set; }
    private static system_tutorial owner;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialTitleText;
    [SerializeField] private TextMeshProUGUI tutorialExplainText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_FontAsset tutorialFont;
    private TMP_FontAsset runtimeFont;
    [SerializeField] private system_playerDevice player;
    [SerializeField] private system_event gameEvents;
    [SerializeField] private system_itemsSpawner[] spawners;
    [SerializeField, Min(0.1f)] private float practiceSpawnInterval = 1f;
    [SerializeField, Min(1f)] private float survivalSeconds = 10f;
    [SerializeField, Min(0.1f)] private float fireLaneRadius = 0.8f;
    [SerializeField, Min(0.1f)] private float fireContactSeconds = 3f;
    [SerializeField] private UnityEvent onCompleted;
    private GameObject gaugeHighlight;
    private CanvasGroup highlightAlpha;
    [SerializeField, Min(0.1f)] private float scoreHighlightSeconds = 2f;
    private system_scores scoreSource;
    private GameObject scoreHighlight;
    private CanvasGroup scoreHighlightAlpha;
    private float scoreHighlightUntil;

    private enum Step { None, Jump, Lane, GoodItem, Decoration, Avoid, Bake, Fire, Complete }
    private Step step;
    private readonly TutorialProgress progress = new TutorialProgress();


    private int placementVersion, lastPlacedType, burnVersion;
    private bool burned, waitingForContinue, continueRequested, failed, subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { IsRunning = false; owner = null; }

    void Awake()
    {
        if (owner != null && owner != this) { enabled = false; return; }
        owner = this;
        IsRunning = true;
    }

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<system_playerDevice>();
        if (gameEvents == null) gameEvents = FindFirstObjectByType<system_event>();
        if (spawners == null || spawners.Length == 0)
            spawners = FindObjectsByType<system_itemsSpawner>(FindObjectsSortMode.None);
        BuildUIIfNeeded();
        continueButton.onClick.AddListener(ContinueTutorial);
        if (player == null || gameEvents == null || !gameEvents.TutorialReady || spawners.Length == 0)
        { Fail("プレイヤー・火事と警告のオブジェクト・アイテムSpawnerの設定を確認してください。"); return; }
        foreach (var spawner in spawners)
            if (spawner == null || !spawner.TutorialReady)
            { Fail("Spawnerの加算/減算Prefab、地上/空中の生成位置、プレイヤーColliderを設定してください。"); return; }
        int[] gauge = UIManager.GetItemNums(ItemsType.strawberry);
        if (gauge[1] <= 0) { Fail("加算アイテムゲージの最大数を1以上に設定してください。"); return; }
        player.JumpSucceeded += OnJumpSucceeded;
        player.JumpRequested += ContinueTutorial;
        player.LaneArrived += OnLaneArrived;
        player.DecorationPlaced += OnDecorationPlaced;
        player.BurnStarted += OnBurnStarted;
        scoreSource = FindFirstObjectByType<system_scores>();
        if (scoreSource != null) scoreSource.ScoreChanged += OnScoreChanged;
        subscribed = true;
        player.TutorialBakingEnabled = false;
        StartCoroutine(TutorialManager());
    }

    void Update()
    {
        if (highlightAlpha != null)
            highlightAlpha.alpha = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
        if (scoreHighlight != null)
        {
            scoreHighlight.SetActive(Time.unscaledTime < scoreHighlightUntil);
            scoreHighlightAlpha.alpha = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
        }
    }

    public void ContinueTutorial()
    {
        if (waitingForContinue) continueRequested = true;
    }

    void OnJumpSucceeded() { if (step == Step.Jump) progress.RecordJump(); }
    void OnLaneArrived(int lane)
    {
        if (step == Step.Lane && lane >= 0 && lane < 3) progress.RecordLane(lane);
    }
    void OnDecorationPlaced(int type) { lastPlacedType = type; placementVersion++; }
    void OnBurnStarted() { burned = true; burnVersion++; }

    void OnScoreChanged()
    {
        if (!IsRunning || failed || step == Step.Complete) return;
        if (scoreHighlight == null)
            scoreHighlight = CreateHighlight(UIManager.ScoreRect, "TutorialScoreHighlight", out scoreHighlightAlpha);
        if (scoreHighlight == null) return;
        scoreHighlightUntil = Time.unscaledTime + scoreHighlightSeconds;
        scoreHighlight.SetActive(true);
    }

    IEnumerator ShowLesson(string title, string explanation, RectTransform gauge = null)
    {
        HighlightGauge(gauge);
        step = Step.None;
        ClearItems();
        player.TutorialControlsEnabled = false;
        player.TutorialBakingEnabled = false;
        gameEvents.SetTutorialFire(-1);
        tutorialPanel.SetActive(true);
        tutorialTitleText.text = title;
        tutorialExplainText.text = explanation + "\n「はじめる」またはジャンプ入力で進む";
        continueRequested = false;
        waitingForContinue = true;
        continueButton.gameObject.SetActive(true);
        yield return null; // Never reuse the input which completed the previous lesson.
        yield return new WaitUntil(() => continueRequested);
        waitingForContinue = false;
        continueButton.gameObject.SetActive(false);
        player.TutorialControlsEnabled = true;
    }

    IEnumerator FreshPancake()
    {
        player.TutorialBakingEnabled = false;
        player.TutorialJumpAllowed = false;
        player.ClearTutorialStack();
        int previous = placementVersion;
        player.DecorationAdd(0);
        yield return WaitForPlacement(previous, 0);
        player.ResetTutorialBake();
        player.TutorialJumpAllowed = true;
    }

    IEnumerator WaitForPlacement(int previous, int type)
    {
        float deadline = Time.time + 15f;
        while (placementVersion == previous && Time.time < deadline) yield return null;
        if (placementVersion == previous || lastPlacedType != type)
            Fail("デコレーションを配置できません。Prefab・配置マーカー・落下物の衝突設定を確認してください。");
    }

    IEnumerator TutorialManager()
    {
        // Let all scene Start methods finish before clearing or spawning practice objects.
        yield return null;
        ClearItems();
        gameEvents.SetTutorialFire(-1);
        yield return FreshPancake();
        if (failed) yield break;

        yield return ShowLesson("① ジャンプ", "フライパンを振って、3回ジャンプしよう。\n着地してから次のジャンプ！");
        progress.Reset();
        step = Step.Jump;
        while (progress.Jumps < 3)
        {
            tutorialExplainText.text = $"ジャンプ成功：{Mathf.Min(progress.Jumps, 3)} / 3";
            yield return null;
        }

        yield return ShowLesson("② レーン移動", "左・中・右にそれぞれ2回到着しよう。\n最初からいる場所は数えません。");
        progress.Reset();
        step = Step.Lane;
        while (progress.Lanes[0] < 2 || progress.Lanes[1] < 2 || progress.Lanes[2] < 2)
        {
            tutorialExplainText.text = $"左 {Mathf.Min(progress.Lanes[0], 2)}/2　中 {Mathf.Min(progress.Lanes[1], 2)}/2　右 {Mathf.Min(progress.Lanes[2], 2)}/2";
            yield return null;
        }

        yield return ShowLesson("③ 加算アイテム", "地上で2個、ジャンプして空中のアイテムを2個取ろう。");
        progress.Reset();
        step = Step.GoodItem;
        while (progress.GroundItems < 2 || progress.AirItems < 2)
        {
            bool airborne = progress.GroundItems >= 2;
            tutorialExplainText.text = $"地上 {Mathf.Min(progress.GroundItems, 2)}/2　空中 {Mathf.Min(progress.AirItems, 2)}/2\n" +
                (airborne ? "高いアイテムをジャンプして取ろう！" : "低いアイテムを地上で取ろう！");
            yield return PracticeItem(ItemsType.strawberry, airborne);
            if (failed) yield break;
        }

        yield return ShowLesson("④ アイテムゲージ", "黄色い枠のゲージに注目！同じアイテムを集めて満タンにし、\nデコレーション層を完成させよう。", UIManager.ItemGaugeRect(ItemsType.strawberry));
        yield return FreshPancake();
        if (failed) yield break;
        UIManager.ResetItemUI(ItemsType.strawberry);
        step = Step.Decoration;
        while (UIManager.GetItemNums(ItemsType.strawberry)[0] < UIManager.GetItemNums(ItemsType.strawberry)[1])
        {
            int[] gauge = UIManager.GetItemNums(ItemsType.strawberry);
            tutorialExplainText.text = $"アイテムゲージ：{gauge[0]} / {gauge[1]}";
            yield return PracticeItem(ItemsType.strawberry, false);
            if (failed) yield break;
        }
        tutorialExplainText.text = "ゲージ満タン！デコレーションが載るのを見よう。";
        int beforeDecoration = placementVersion;
        player.DecorationAdd(1);
        yield return WaitForPlacement(beforeDecoration, 1);
        if (failed) yield break;
        UIManager.ResetItemUI(ItemsType.strawberry);

        yield return ShowLesson("⑤ 減算アイテム", "減算アイテムを3個連続で避けよう。接触すると連続回数は0に戻ります。");
        progress.Reset();
        step = Step.Avoid;
        while (progress.ConsecutiveAvoids < 3)
        {
            tutorialExplainText.text = $"連続回避：{progress.ConsecutiveAvoids} / 3\n今いるレーンに来るアイテムを、移動やジャンプで避けよう。";
            yield return PracticeItem(ItemsType.bad, false);
            if (failed) yield break;
        }

        yield return ShowLesson("⑥ 焼きゲージ", "黄色い枠の焼きゲージに注目！\n最初はジャンプを止めて焦げを体験します。その後はジャンプで焼きゲージを下げよう。", UIManager.BakeGaugeRect);
        yield return FreshPancake();
        if (failed) yield break;
        step = Step.Bake;
        burned = false;
        player.TutorialJumpAllowed = false;
        player.TutorialBakingEnabled = true;
        tutorialExplainText.text = "いまはジャンプできません。\n焼きゲージが満タンになると焦げます。\n焦げるとパンケーキが一つ消え、\nパンケーキが0個になるとゲームオーバーです\n焼きゲージに比例したスコアが加算されます";
        yield return new WaitUntil(() => burned);
        yield return new WaitUntil(() => !player.IsBurning);
        player.TutorialBakingEnabled = false;
        tutorialExplainText.text = "焦げたパンケーキを交換します。";
        yield return FreshPancake();
        if (failed) yield break;
        float safeTime = 0f;
        int observedBurn = burnVersion;
        player.TutorialBakingEnabled = true;
        while (safeTime < survivalSeconds)
        {
            if (burnVersion != observedBurn)
            {
                safeTime = 0f;
                yield return new WaitUntil(() => !player.IsBurning);
                yield return FreshPancake();
                if (failed) yield break;
                observedBurn = burnVersion;
                player.TutorialBakingEnabled = true;
            }
            if (player.HasPancake && !player.IsBurning) safeTime = TutorialProgress.AdvanceSafeTime(safeTime, Time.deltaTime, true, false);
            tutorialExplainText.text = $"ジャンプ解禁！焦がさずに {safeTime:F1} / {survivalSeconds:F0} 秒";
            yield return null;
        }

        yield return ShowLesson("⑦ 火事イベント", $"火事に合計{fireContactSeconds:F0}秒触れて、\n焼きゲージの上がり方を見よう。\nその後{survivalSeconds:F0}秒間避けよう。火事のコンロは変わりません。", UIManager.BakeGaugeRect);
        yield return FreshPancake();
        if (failed) yield break;
        step = Step.Fire;
        int fireLane = player.CurrentLane;
        gameEvents.ShowTutorialFireWarning(fireLane);
        tutorialExplainText.text = "警告が出たコンロで火事が起きます。警告マークに注目！";
        yield return new WaitForSeconds(1f);
        player.TutorialBakingEnabled = true;
        gameEvents.SetTutorialFire(fireLane);
        float contactTime = 0f;
        observedBurn = burnVersion;
        while (contactTime < fireContactSeconds)
        {
            if (burnVersion != observedBurn || !player.HasPancake)
            {
                yield return RefillFirePancake();
                if (failed) yield break;
                observedBurn = burnVersion;
            }
            contactTime = TutorialProgress.AdvanceContactTime(contactTime, Time.deltaTime,
                IsTouchingFire() && player.HasPancake && !player.IsBurning);
            tutorialExplainText.text = $"火事に触れよう：合計 {Mathf.Min(contactTime, fireContactSeconds):F1} / {fireContactSeconds:F0} 秒\n焼きゲージが速く上がります。焦げたら新しいパンケーキを追加します。";
            yield return null;
        }
        safeTime = 0f;
        while (safeTime < survivalSeconds)
        {
            if (burnVersion != observedBurn || !player.HasPancake)
            {
                safeTime = 0f;
                yield return RefillFirePancake();
                if (failed) yield break;
                observedBurn = burnVersion;
            }
            safeTime = TutorialProgress.AdvanceSafeTime(safeTime, Time.deltaTime, gameEvents.TutorialFireActive, IsTouchingFire());
            tutorialExplainText.text = $"火事のレーンから離れよう！連続回避 {safeTime:F1} / {survivalSeconds:F0} 秒\n接触すると計測をやり直します。";
            yield return null;
        }
        gameEvents.SetTutorialFire(-1);
        yield return ShowLesson("チュートリアル終了", "すべて達成しました！次へ進むとゲームを開始します。");
        step = Step.Complete;
        tutorialPanel.SetActive(false);
        Cleanup();
        onCompleted?.Invoke();
        SceneManager.LoadScene("InGame");
    }

    IEnumerator RefillFirePancake()
    {
        tutorialExplainText.text = "焦げたパンケーキを交換します。計測は補充後に再開します。";
        player.TutorialBakingEnabled = false;
        yield return new WaitUntil(() => !player.IsBurning);
        yield return FreshPancake();
        if (!failed) player.TutorialBakingEnabled = true;
    }

    void HighlightGauge(RectTransform target)
    {
        if (gaugeHighlight != null)
        {
            gaugeHighlight.SetActive(false);
            Destroy(gaugeHighlight);
        }
        highlightAlpha = null;
        gaugeHighlight = CreateHighlight(target, "TutorialGaugeHighlight", out highlightAlpha);
        if (target != null && target == UIManager.ItemGaugeRect(ItemsType.strawberry) &&
            UIManager.TryGetItemGaugeBounds(target, out Bounds bounds))
        {
            var rect = (RectTransform)gaugeHighlight.transform;
            rect.anchorMin = rect.anchorMax = target.pivot;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(bounds.center.x, bounds.center.y);
            rect.sizeDelta = new Vector2(bounds.size.x + 4f, bounds.size.y + 4f);
        }
    }

    static GameObject CreateHighlight(RectTransform target, string name, out CanvasGroup alpha)
    {
        alpha = null;
        if (target == null) return null;
        // A separate border remains visible even when the filled Image is empty.
        var highlight = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        var rect = (RectTransform)highlight.transform;
        rect.SetParent(target, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-2f, -2f);
        rect.offsetMax = new Vector2(2f, 2f);
        alpha = highlight.GetComponent<CanvasGroup>();
        alpha.blocksRaycasts = false;
        alpha.interactable = false;
        for (int i = 0; i < 4; i++)
        {
            var edge = new GameObject("Border", typeof(RectTransform), typeof(Image));
            var edgeRect = (RectTransform)edge.transform;
            edgeRect.SetParent(rect, false);
            bool horizontal = i < 2;
            float side = i % 2;
            edgeRect.anchorMin = horizontal ? new Vector2(0f, side) : new Vector2(side, 0f);
            edgeRect.anchorMax = horizontal ? new Vector2(1f, side) : new Vector2(side, 1f);
            edgeRect.sizeDelta = horizontal ? new Vector2(0f, 5f) : new Vector2(5f, 0f);
            edgeRect.anchoredPosition = Vector2.zero;
            var graphic = edge.GetComponent<Image>();
            graphic.color = new Color(1f, 0.85f, 0.1f);
            graphic.raycastTarget = false;
        }
        return highlight;
    }

    bool IsTouchingFire()
    {
        // A stove fire is a lane hazard; use actual X, including movement between lanes.
        return gameEvents.TutorialFireActive &&
            Mathf.Abs(player.PlayerX - player.LaneX(gameEvents.TutorialFireLane)) <= fireLaneRadius;
    }

    IEnumerator PracticeItem(ItemsType type, bool airborne)
    {
        system_itemsSpawner selected = spawners[0];
        foreach (var candidate in spawners)
            if (Mathf.Abs(candidate.SpawnX - player.PlayerX) < Mathf.Abs(selected.SpawnX - player.PlayerX))
                selected = candidate;
        system_items item = selected.SpawnTutorialItem(type, airborne);
        if (item == null) { Fail("練習用アイテムを生成できません。"); yield break; }
        Step itemStep = step;
        item.ConfigureTutorial();
        item.TutorialResolved = hit =>
        {
            if (step != itemStep) return;
            if (step == Step.GoodItem && hit)
            {
                progress.RecordGoodItem(airborne, player.IsFlying);
            }
            if (step == Step.Avoid) progress.RecordBadItem(hit);
        };
        while (item != null && !item.Resolved) yield return null;
        yield return new WaitForSeconds(practiceSpawnInterval);
    }

    void ClearItems()
    {
        if (spawners == null) return;
        foreach (var spawner in spawners) if (spawner != null) spawner.ClearItems();
    }

    void Fail(string message)
    {
        HighlightGauge(null);
        scoreHighlightUntil = 0f;
        if (scoreHighlight != null) scoreHighlight.SetActive(false);
        failed = true;
        step = Step.None;
        if (player != null) { player.TutorialControlsEnabled = false; player.TutorialBakingEnabled = false; }
        tutorialPanel.SetActive(true);
        tutorialTitleText.text = "チュートリアルの設定を確認してください";
        tutorialExplainText.text = message;
        continueButton.gameObject.SetActive(false);
        Debug.LogError(message, this);
    }

    void Cleanup()
    {
        if (owner != this) return;
        HighlightGauge(null);
        if (scoreSource != null) scoreSource.ScoreChanged -= OnScoreChanged;
        if (scoreHighlight != null)
        {
            scoreHighlight.SetActive(false);
            Destroy(scoreHighlight);
        }
        ClearItems();
        if (gameEvents != null && gameEvents.TutorialReady) gameEvents.SetTutorialFire(-1);
        if (player != null)
        {
            if (subscribed)
            {
                player.JumpSucceeded -= OnJumpSucceeded;
                player.JumpRequested -= ContinueTutorial;
                player.LaneArrived -= OnLaneArrived;
                player.DecorationPlaced -= OnDecorationPlaced;
                player.BurnStarted -= OnBurnStarted;
            }
            player.TutorialControlsEnabled = true;
            player.TutorialJumpAllowed = true;
        }
        subscribed = false;
        IsRunning = false;
        owner = null;
    }

    void OnDisable() { StopAllCoroutines(); Cleanup(); }
    void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(ContinueTutorial);
        Cleanup();
        if (runtimeFont != null) Destroy(runtimeFont);
    }

    void BuildUIIfNeeded()
    {
        if (tutorialPanel != null && tutorialTitleText != null && tutorialExplainText != null && continueButton != null) return;
        TMP_FontAsset font = tutorialFont;
        // The project's default Liberation Sans does not contain Japanese glyphs.
        // Windows development can use the installed font without copying it into the project.
        if (font == null)
        {
            runtimeFont = TMP_FontAsset.CreateFontAsset("Yu Gothic", "Regular");
            if (runtimeFont == null) runtimeFont = TMP_FontAsset.CreateFontAsset("Meiryo", "Regular");
            font = runtimeFont;
        }
        if (font == null)
            foreach (TMP_Text existing in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (existing.font != null) { font = existing.font; break; }
        GameObject canvasObj = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        tutorialPanel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)tutorialPanel.transform;
        rect.SetParent(canvasObj.transform, false);
        rect.anchorMin = new Vector2(0.28f, 0.66f);
        rect.anchorMax = new Vector2(0.71f, 0.95f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        tutorialPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.94f);
        tutorialTitleText = MakeText("Title", rect, font, new Vector2(0.03f, 0.7f), new Vector2(0.97f, 0.97f), 34);
        tutorialExplainText = MakeText("Explanation", rect, font, new Vector2(0.03f, 0.23f), new Vector2(0.97f, 0.69f), 26);
        GameObject button = new GameObject("Continue", typeof(RectTransform), typeof(Image), typeof(Button));
        var buttonRect = (RectTransform)button.transform;
        buttonRect.SetParent(rect, false);
        buttonRect.anchorMin = new Vector2(0.38f, 0.02f);
        buttonRect.anchorMax = new Vector2(0.62f, 0.23f);
        buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
        button.GetComponent<Image>().color = new Color(0.9f, 0.5f, 0.13f);
        continueButton = button.GetComponent<Button>();
        continueButton.targetGraphic = button.GetComponent<Image>();
        MakeText("Label", buttonRect, font, Vector2.zero, Vector2.one, 24).text = "はじめる / 次へ";
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, TMP_FontAsset font, Vector2 min, Vector2 max, float size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
}

// Pure progress rules; also exercised by Tests/Run-TutorialChecks.ps1.
public sealed class TutorialProgress
{
    public int Jumps { get; private set; }
    public int[] Lanes { get; } = new int[3];
    public int GroundItems { get; private set; }
    public int AirItems { get; private set; }
    public int ConsecutiveAvoids { get; private set; }
    public void Reset()
    {
        Jumps = GroundItems = AirItems = ConsecutiveAvoids = 0;
        System.Array.Clear(Lanes, 0, Lanes.Length);
    }
    public void RecordJump() { Jumps++; }
    public void RecordLane(int lane) { if (lane >= 0 && lane < 3) Lanes[lane]++; }
    public void RecordGoodItem(bool airborneItem, bool playerAirborne)
    {
        if (airborneItem && playerAirborne) AirItems++;
        else if (!airborneItem && !playerAirborne) GroundItems++;
    }
    public void RecordBadItem(bool hit) { ConsecutiveAvoids = hit ? 0 : ConsecutiveAvoids + 1; }
    public static float AdvanceSafeTime(float current, float delta, bool active, bool failed)
    {
        return !active || failed ? 0f : current + System.Math.Max(0f, delta);
    }
    public static float AdvanceContactTime(float current, float delta, bool touching)
    {
        return touching ? current + System.Math.Max(0f, delta) : current;
    }
}
