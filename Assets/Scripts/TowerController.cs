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
    [SerializeField] SoundPlayer sound;

    public TowerGame Game { get; private set; }

    Transform playerView;

    void Start()
    {
        if (loader == null) loader = GetComponent<FloorLoader>();
        if (sound == null) sound = GetComponent<SoundPlayer>();
        if (sound == null) sound = gameObject.AddComponent<SoundPlayer>();
        Game = new TowerGame(PlayerState.NewGame());
        RebuildView();
        // 原典は序章のあと（ゼノに奪われた直後）に本編の曲を鳴らす。
        // リメイクでは塔に入った時点で流し、あとは合図に任せる。
        sound.PlayCue(SoundCues.FieldMusic);
        Game.ConsumeSounds();
    }

    void Update()
    {
        if (Game == null) return;

        int floorBefore = Game.Player.Floor;
        bool acted;

        if (Game.IsShowingPage)
        {
            // 会話や演出が出ている間は、どのキーでも1つ送るだけ（原典もキー待ち）。
            // **送った拍子に階が変わることがある**（序章でゼノに奪われて2階の牢に移る、
            // 終幕で50階まで駆け上がる）ので、送ったあとも必ず見た目を合わせ直す。
            acted = Input.anyKeyDown;
            if (acted) Game.NextPage();
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
            acted = dir != Vector2Int.zero;
            if (acted) Game.TryStep(dir);
        }

        if (!acted) return;
        AfterAction(floorBefore);
    }

    /// <summary>メニューの中の一手。何かしたら true。</summary>
    bool StepMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Game.MenuBack(); return true; }

        var move = ReadInput();
        if (move.y != 0)
        {
            // 段によって上下の意味が変わる。
            //   本体 … 項目を選ぶ／スロット … 保存先を選ぶ／飛翔 … 階を選ぶ
            switch (Game.Stage)
            {
                case TowerGame.MenuStage.Root:         Game.MenuMove(-move.y); break;
                case TowerGame.MenuStage.FlightFloors: Game.FlightMove(move.y); break;
                default:                               Game.SlotMove(-move.y); break;
            }
            return true;
        }
        if (MenuKeyDown()) { Game.MenuConfirm(); return true; }
        return false;
    }

    /// <summary>
    /// 一手ぶんの後始末。**階が変わっていれば作り直し、変わっていなければ差分だけ**直す。
    /// 会話を送っただけ・メニューを触っただけでも階が変わることがある
    /// （序章のゼノ、飛翔のオーブ、終幕の駆け上がり）ので、入口はここ1つにまとめてある。
    /// </summary>
    void AfterAction(int floorBefore)
    {
        if (Game.Player.Floor != floorBefore) RebuildView();
        else SyncChangedCells();
        PlacePlayerView();
        if (sound != null) sound.Flush(Game);      // 本線が積んだ音を鳴らす
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

    static Vector2Int ReadInput()
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
