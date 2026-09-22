using UnityEngine;

/// <summary>
/// シーン上でゲームを動かす役。`TowerGame`（原典の規則）と
/// `FloorLoader`（見た目）をつなぐ。
///
/// 盤面の正は常に `TowerGame.Floor` 側にあり、見た目はその写しでしかない。
/// マスが変わったら差分だけ作り直す。
/// </summary>
public class TowerController : MonoBehaviour
{
    [SerializeField] FloorLoader loader;
    [SerializeField] Sprite playerSprite;
    /// <summary>
    /// 主人公の2コマ目（PLAYER2.P58）。原典の移動の演出（0xa276）は
    /// **左右に動くときだけ**途中で絵を入れ替える（向き1/2 は元の行 0x100 → 0x120、
    /// 上下の向き3/4 は 0x140 のまま）。
    /// </summary>
    [SerializeField] Sprite playerSprite2;
    [SerializeField] MonsterAnimator monsters;
    [SerializeField] SoundPlayer sound;
    [SerializeField] BattleView battle;

    /// <summary>勇者の絵。戦闘の帯（<see cref="BattleBandView"/>）も同じ絵を使う。</summary>
    public Sprite PlayerSprite { get { return playerSprite; } }

    /// <summary>
    /// 音を鳴らす役。ふだんは本線が積んだものを一手ごとに流すが、
    /// **1枚絵の中で回る演出**（終幕の塔）は本線の外で進むので、そこからも鳴らせるようにする。
    /// </summary>
    public SoundPlayer Sound { get { return sound; } }

    public TowerGame Game { get; private set; }

    Transform playerView;

    void Start()
    {
        if (loader == null) loader = GetComponent<FloorLoader>();
        if (sound == null) sound = GetComponent<SoundPlayer>();
        if (sound == null) sound = gameObject.AddComponent<SoundPlayer>();
        StartNewGame();
    }

    /// <summary>
    /// 最初から始める。原典のゲームオーバー（0x097ac）は
    /// キーを待って **INITIAL.DAT を読み直す**ので、同じ結果になるようにする。
    /// </summary>
    void StartNewGame()
    {
        StopShows();
        Game = new TowerGame(PlayerState.NewGame());
        RebuildView();
        // 原典は序章のあと（ゼノに奪われた直後）に本編の曲を鳴らす。
        // リメイクでは塔に入った時点で流し、あとは合図に任せる。
        sound.PlayCue(SoundCues.FieldMusic);
        Game.ConsumeSounds();
    }

    void Awake()
    {
        // **スマホは既定で30コマ/秒**（Unity の iOS/Android の既定）。
        // 演出はどれも「1コマ＝1/60秒」で数えているので、そのままだと
        // 扉が開く動き・ゼノの登場・人の歩きが**軒並み半分の速さ**になる
        // （実機で遅くなったという報告がこれ）。60に上げてそろえる。
        Application.targetFrameRate = TargetFrameRate;
    }

    /// <summary>出したい1秒あたりのコマ数。演出の数え方（1/60秒）と合わせる。</summary>
    public const int TargetFrameRate = 60;

    void Update()
    {
        if (Game == null) return;
        if (sliding || drawing) return;   // 演出の最中は入力を受けない（原典も同じ）
        // 戦闘の演出が終わるまで次の一手を受けない。原典の戦闘処理（0x6082）は
        // 斬り合いが片付くまで**その場で回り続ける**ので、途中で動くことはできない。
        if (battle != null && battle.IsPlaying) return;

        int floorBefore = Game.Player.Floor;
        var posBefore = Game.Player.Position;
        bool acted;

        if (Game.IsShowingPage)
        {
            // 会話や演出が出ている間は、どのキーでも1つ送るだけ（原典もキー待ち）。
            // **送った拍子に階が変わることがある**（序章でゼノに奪われて2階の牢に移る、
            // 終幕で50階まで駆け上がる）ので、送ったあとも必ず見た目を合わせ直す。
            if (Game.IsChoosing)
            {
                // 「◎はい／◎いいえ」や祭壇の望み。原典 0x9e78 は
                // 上下で印を動かし、リターンかスペースで決める。
                var move = ReadInputOnce();
                if (move.y != 0) { Game.MoveChoice(move.y > 0 ? -1 : 1); acted = true; }
                else if (MenuKeyDown()) { Game.ConfirmChoice(); Game.NextPage(); acted = true; }
                else acted = false;
            }
            else
            {
                acted = Input.anyKeyDown;
                if (acted)
                {
                    // ＧＡＭＥ　ＯＶＥＲ のあとは原典と同じく最初から
                    if (Game.IsGameOver) { StartNewGame(); return; }
                    Game.NextPage();
                }
            }
        }
        else if (Game.IsMenuOpen)
        {
            acted = StepMenu();
        }
        else if (FastModeKeyDown())
        {
            // 原典は鍵 0x70（PC98のシフト）で高速モードを入り切りする（0xa927）
            Game.ToggleFastMode();
            acted = true;
        }
        else if (MenuKeyDown())
        {
            // 原典は鍵 0x1c(リターン) と 0x34(スペース) のどちらでもメニューに入る
            Game.OpenMenu();
            acted = true;
        }
        else
        {
            var dir = ReadInput();
            // 鍵が押されていなければ、触っている十字キーの向きを使う
            if (dir == Vector2Int.zero) dir = TouchDir;
            // 壁にぶつかったら、その向きは**キーを離すまで**もう試さない。
            // 押しっぱなしのまま毎コマ叩き続けると、判定が鳴りっぱなしになる。
            // ぶつかったときの音は**足さない**（原典に無く、作者もそれでよいとのこと）。
            if (dir == Vector2Int.zero) blockedDir = Vector2Int.zero;
            acted = dir != Vector2Int.zero && dir != blockedDir;
            if (acted)
            {
                var before = Game.Player.Position;
                Game.TryStep(dir);
                if (Game.Player.Position == before && !Game.IsShowingPage && !Game.IsMenuOpen)
                    blockedDir = dir;      // 動けなかった
                else
                    blockedDir = Vector2Int.zero;
            }
        }

        if (!acted) return;
        AfterAction(floorBefore, posBefore);
    }

    /// <summary>
    /// 十字キーを触って押している向き（<see cref="DPadView"/> が毎コマ入れる）。
    /// 押していなければ <see cref="Vector2Int.zero"/>。鍵と同じ扱いで、
    /// 押しっぱなしにすれば歩き続ける。
    /// </summary>
    public Vector2Int TouchDir { get; set; }

    /// <summary>
    /// 持ち物の帯を触ったとき（作者の指定。並びは <see cref="PhoneScreen"/>）。
    /// 番号は持ち物の欄の並び（原典 0x2f8c の 物体ID 36〜50）。
    /// </summary>
    public void TapItem(int slot)
    {
        if (Game == null) return;
        int floorBefore = Game.Player.Floor;
        var posBefore = Game.Player.Position;
        if (Game.TapItem(SpecialItems.PanelKind(slot))) AfterAction(floorBefore, posBefore);
    }

    /// <summary>下の「ダメージ表示」のボタン。勇者のオーブの一覧を出す。</summary>
    public void ShowDamage()
    {
        if (Game == null || Game.IsShowingPage || Game.IsMenuOpen) return;
        Game.ShowHeroOrb();
    }

    /// <summary>メニューの中の一手。何かしたら true。</summary>
    bool StepMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Game.MenuBack(); return true; }

        var move = ReadInputOnce();
        if (move.y != 0)
        {
            // 段によって上下の意味が変わる。
            //   本体 … 項目を選ぶ／スロット … 保存先を選ぶ／飛翔 … 階を選ぶ
            switch (Game.Stage)
            {
                case TowerGame.MenuStage.Root:         Game.MenuMove(-move.y); break;
                case TowerGame.MenuStage.FlightFloors: Game.FlightMove(move.y); break;
                case TowerGame.MenuStage.Items:        Game.ItemMove(-move.y); break;
                default:                               Game.SlotMove(-move.y); break;
            }
            return true;
        }
        if (MenuKeyDown())
        {
            Game.MenuConfirm();
            // 「ゲーム終了」。原典は DOS へ戻るので、こちらは遊びを閉じる
            // （エディタで試している間は再生を止める）。
            if (Game.Quitting) QuitGame();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 一手ぶんの後始末。**階が変わっていれば作り直し、変わっていなければ差分だけ**直す。
    /// 会話を送っただけ・メニューを触っただけでも階が変わることがある
    /// （序章のゼノ、飛翔のオーブ、終幕の駆け上がり）ので、入口はここ1つにまとめてある。
    /// </summary>
    void AfterAction(int floorBefore, Vector2Int posBefore)
    {
        if (Game.Player.Floor != floorBefore) RebuildView();
        else SyncChangedCells();
        // 隣のマスへ1歩動いたときだけ滑らせる（原典 0xa276。階が変わる・
        // 戦闘の踏み込み・階段や門で飛ぶときは、原典もこの演出を通らない）
        var now = Game.Player.Position;
        var d = now - posBefore;
        bool oneStep = Game.Player.Floor == floorBefore
                       && (Mathf.Abs(d.x) + Mathf.Abs(d.y)) == 1;
        if (oneStep && isActiveAndEnabled) slidingRoutine = StartCoroutine(Slide(posBefore, now));
        else PlacePlayerView();

        if (sound != null) sound.Flush(Game);      // 本線が積んだ音を鳴らす
    }

    /// <summary>遊びを閉じる。原典のメニューの「ゲーム終了」（DOS へ戻る）に当たる。</summary>
    static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>移動の演出の最中か。</summary>
    bool sliding;

    Coroutine slidingRoutine, drawingRoutine;

    /// <summary>進めなかった向き。キーを離すまで同じ向きは試さない。</summary>
    Vector2Int blockedDir;

    /// <summary>出している演出を止めて、見た目を整った形に戻す。</summary>
    void StopShows()
    {
        if (slidingRoutine != null) { StopCoroutine(slidingRoutine); slidingRoutine = null; }
        if (drawingRoutine != null) { StopCoroutine(drawingRoutine); drawingRoutine = null; }
        sliding = drawing = false;
        if (playerView != null) playerView.gameObject.SetActive(true);
    }

    /// <summary>階を描き出している最中か。</summary>
    bool drawing;

    /// <summary>
    /// 階に入ったときの描き出し。原典（0xafa6）は盤面を一度に出さず、
    /// **121マスを渦巻きの順に1つずつ**描き、**3マスごとに縦帰線を1回待つ**
    /// （高速モードなら10マスごと）。表は <see cref="FloorDrawOrder"/>。
    /// </summary>
    System.Collections.IEnumerator DrawFloor()
    {
        drawing = true;
        var byCell = new System.Collections.Generic.Dictionary<Vector2Int,
                         System.Collections.Generic.List<SpriteRenderer>>();
        foreach (Transform child in loader.Container)
        {
            var name = child.name;
            int cut = name.LastIndexOf('_');
            if (cut <= 0) continue;
            int cut2 = name.LastIndexOf('_', cut - 1);
            if (cut2 <= 0) continue;
            int x, y;
            if (!int.TryParse(name.Substring(cut2 + 1, cut - cut2 - 1), out x)) continue;
            if (!int.TryParse(name.Substring(cut + 1), out y)) continue;
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            var key = new Vector2Int(x, y);
            if (!byCell.ContainsKey(key))
                byCell[key] = new System.Collections.Generic.List<SpriteRenderer>();
            byCell[key].Add(sr);
            sr.enabled = false;
        }
        if (playerView != null) playerView.gameObject.SetActive(false);

        int per = Game.Player.FastMode
                  ? FloorDrawOrder.FastCellsPerWait : FloorDrawOrder.CellsPerWait;
        int n = 0;
        foreach (var cell in FloorDrawOrder.Instance.Cells)
        {
            System.Collections.Generic.List<SpriteRenderer> here;
            if (byCell.TryGetValue(cell, out here))
                foreach (var sr in here) if (sr != null) sr.enabled = true;
            n++;
            if (n % per == 0) yield return new WaitForSeconds(MonsterAnimation.FrameSeconds);
        }
        foreach (var list in byCell.Values)
            foreach (var sr in list) if (sr != null) sr.enabled = true;
        if (playerView != null) playerView.gameObject.SetActive(true);
        drawing = false;
        drawingRoutine = null;
    }

    /// <summary>
    /// 1マスぶん主人公を滑らせる。原典（0xa276）は**どの向きも16コマ**、
    /// 高速モード（旗31）なら8コマで、**1コマごとに敵と人の絵を1回動かす**（0x92a4）。
    /// 1コマの長さは原典からは出せないので <see cref="MonsterAnimation.FrameSeconds"/> に置く。
    /// </summary>
    System.Collections.IEnumerator Slide(Vector2Int from, Vector2Int to)
    {
        sliding = true;
        bool fast = Game.Player.FastMode;
        int n = fast ? MonsterAnimation.FastTicksPerStep : MonsterAnimation.TicksPerStep;
        var a = new Vector3(from.x, from.y, -1f);
        var b = new Vector3(to.x, to.y, -1f);
        // 左右に動くときは途中で2コマ目に入れ替える（原典 0xa276 の 0x100 → 0x120）。
        bool sideways = from.y == to.y && from.x != to.x;
        var sr = playerView != null ? playerView.GetComponent<SpriteRenderer>() : null;
        // **経過時間で進める**。コマ送りで待つと、画面が60コマ/秒のときに
        // 1コマが1/60秒になってしまい、指定の何倍も遅くなる（作者から
        // 「遅すぎる」と報告されたのがこれ）。
        float dur = MonsterAnimation.StepSeconds(fast);
        float t = 0f;
        PlacePlayerView();
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            playerView.position = Vector3.Lerp(a, b, k);
            if (sr != null && sideways && playerSprite2 != null)
                sr.sprite = k <= 0.5f ? playerSprite : playerSprite2;
            // 敵と人の絵は原典どおり**コマ数ぶん**動かす（0x92a4）
            yield return null;
        }
        if (sr != null && playerSprite != null) sr.sprite = playerSprite;   // 立ち姿へ戻す
        PlacePlayerView();
        sliding = false;
        slidingRoutine = null;
    }

    /// <summary>メニューの開け閉めに使う鍵（原典 0x1c / 0x34）。</summary>
    /// <summary>高速モードの切り替え鍵。原典は 0x70 ＝ PC98 のシフト。</summary>
    static bool FastModeKeyDown()
    {
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
    }

    static bool MenuKeyDown()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space);
    }

    /// <summary>
    /// 進みたい向き。**押しっぱなしで進み続ける**。
    /// 原典は1歩ごとにキーを読み直す作りだが、そのぶん移動の演出（16コマ）が
    /// 区切りになっていて押しっぱなしでも歩き続ける。ここも同じ感じにする。
    /// </summary>
    static Vector2Int ReadInput()
    {
        if (Input.GetKey(KeyCode.UpArrow)    || Input.GetKey(KeyCode.W)) return new Vector2Int(0, 1);
        if (Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) return new Vector2Int(0, -1);
        if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) return new Vector2Int(-1, 0);
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) return new Vector2Int(1, 0);
        return Vector2Int.zero;
    }

    /// <summary>メニューの中は1回ずつ。押しっぱなしで飛ばないようにする。</summary>
    static Vector2Int ReadInputOnce()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W)) return new Vector2Int(0, 1);
        if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S)) return new Vector2Int(0, -1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) return new Vector2Int(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return new Vector2Int(1, 0);
        return Vector2Int.zero;
    }

    /// <summary>
    /// 階が変わったので全部作り直す。
    ///
    /// **初期配置をそのまま敷くだけでは駄目**で、その階で既に起きたこと
    /// （開けた扉・倒した敵・イベントで出た物・開いた牢）を上から反映する。
    /// `TowerGame` は階ごとの盤面を覚えているので、そちらを正として差を当てる。
    /// </summary>
    public void RebuildView()
    {
        if (loader == null || Game == null) return;
        var data = FloorData.Load(Game.Player.Floor);
        loader.Build(data);

        // 初期配置と違うマスだけ作り直す。全部を作り直すと
        // 3x3のボス（1体だけ出す約束）が9体に増えてしまう。
        var initial = new FloorState(data);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var p = new Vector2Int(x, y);
                var now = Game.Floor.At(p);
                if (now == initial.At(p)) continue;
                loader.Refresh(p, FloorState.ViewKeyOf(now));
            }

        Game.Floor.ConsumeDirty();     // 作り直したので差分は無かったことにする
        PlacePlayerView();

        // 原典は階に入ると盤面を渦巻きの順に1マスずつ描く（0xafa6）。
        // **前の描き出しや移動が走っていたら止める** — 階が変わると前の階の絵を
        // 出し続けることになるし、2つ走ると入力の止め方が食い違う。
        StopShows();
        if (isActiveAndEnabled) drawingRoutine = StartCoroutine(DrawFloor());
    }

    /// <summary>変わったマスだけ作り直す。</summary>
    void SyncChangedCells()
    {
        if (loader == null || Game == null) return;
        foreach (var p in Game.Floor.ConsumeDirty())
        {
            // 盤面の記号（"#" や ">"）のままではプレハブを引けないので、見た目の鍵に直す。
            loader.Refresh(p, FloorState.ViewKeyOf(Game.Floor.At(p)));
        }
    }

    void PlacePlayerView()
    {
        if (playerView == null)
        {
            var go = new GameObject("PlayerView");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = playerSprite;
            sr.sortingOrder = 100;
            playerView = go.transform;
            playerView.SetParent(transform, false);
        }
        var at = Game.Player.Position;
        playerView.position = new Vector3(at.x, at.y, -1f);
    }
}
