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

        // 会話や演出が出ている間は、どのキーでも1つ送るだけ（原典もキー待ち）
        if (Game.IsShowingPage)
        {
            if (Input.anyKeyDown) Game.NextPage();
            return;
        }

        // メニュー。原典は鍵 0x1c(リターン) と 0x34(スペース) のどちらでも開く
        if (Game.IsMenuOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Game.MenuBack(); return; }
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
            }
            else if (MenuKeyDown()) Game.MenuConfirm();
            if (sound != null) sound.Flush(Game);
            return;
        }
        if (MenuKeyDown()) { Game.OpenMenu(); return; }

        var dir = ReadInput();
        if (dir == Vector2Int.zero) return;

        int floorBefore = Game.Player.Floor;
        Game.TryStep(dir);

        if (Game.Player.Floor != floorBefore) RebuildView();
        else SyncChangedCells();
        PlacePlayerView();
        if (sound != null) sound.Flush(Game);      // 本線が積んだ音を鳴らす
    }

    /// <summary>メニューの開け閉めに使う鍵（原典 0x1c / 0x34）。</summary>
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

    /// <summary>階が変わったので全部作り直す。</summary>
    public void RebuildView()
    {
        if (loader == null || Game == null) return;
        loader.Build(FloorData.Load(Game.Player.Floor));
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
