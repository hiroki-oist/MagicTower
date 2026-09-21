using UnityEngine;

/// <summary>
/// 塔全体の進行役。**シーンを階ごとに用意するのをやめ、
/// `.floor` データから毎回組み立てる**方式に本線を移すための入口。
///
/// 【設計方針・全体に適用】
///   原典 PC98版は階ごとのシーンを持たず、地図データを1つの配列に読み込んで描画する。
///   リメイクもそれに合わせ、`FloorData`（原典から確定した全50階）を
///   <see cref="FloorLoader"/> が毎回組み立て、
///   <see cref="GameRules"/> が原典と同じ順（移動→魔法攻撃→解錠判定）で一手を処理する。
///   これにより、手で作っていない F35 以降も含めて全階が出せる。
/// </summary>
public class TowerRuntime : MonoBehaviour
{
    [SerializeField] FloorLoader loader;
    [SerializeField] GameRules rules;
    [SerializeField] Transform player;

    public int CurrentFloor { get; private set; }

    /// <summary>これまでに到達した最も高い階。商店の変動価格が参照する。</summary>
    public int HighestFloorReached { get; private set; }

    void Awake()
    {
        if (rules == null) rules = GetComponent<GameRules>();
        if (rules != null) rules.OnUnlocked += ApplyUnlockToView;
    }

    void OnDestroy()
    {
        if (rules != null) rules.OnUnlocked -= ApplyUnlockToView;
    }

    /// <summary>指定した階に入る。原典の開始は 1階の (0,-5)。</summary>
    public void Enter(int floor, Vector2Int at)
    {
        var data = FloorData.Load(floor);
        if (data == null)
        {
            Debug.LogError($"TowerRuntime: Floor_{floor} のデータが読めません");
            return;
        }
        CurrentFloor = floor;
        if (floor > HighestFloorReached) HighestFloorReached = floor;

        if (loader != null) loader.Build(data);
        if (rules != null) rules.EnterFloor(data);
        MovePlayerTo(at);
    }

    /// <summary>階段で上がる。原典と同じく 44階（幻の階）は飛ばす。</summary>
    public bool GoUp()
    {
        var r = FloorTransition.GoUp(CurrentFloor);
        if (!r.Ok) { Debug.Log($"TowerRuntime: 上がれない ({r.Reason})"); return false; }
        Enter(r.Floor, r.Arrive);
        return true;
    }

    /// <summary>階段で下りる。こちらも 44階は飛ばす。</summary>
    public bool GoDown()
    {
        var r = FloorTransition.GoDown(CurrentFloor);
        if (!r.Ok) { Debug.Log($"TowerRuntime: 下りられない ({r.Reason})"); return false; }
        Enter(r.Floor, r.Arrive);
        return true;
    }

    /// <summary>
    /// 翼で移動する。**階段と違い 44階を飛ばさない**ので、
    /// これが幻の階へ行く唯一の方法になる（原典ヒント214）。
    /// </summary>
    public bool UseWing(bool up)
    {
        int to = SpecialItemEffects.WingFloor(CurrentFloor, up);
        if (to < FloorConnections.MinFloor || to > FloorConnections.MaxFloor) return false;
        var r = up ? FloorTransition.WingUp(CurrentFloor) : FloorTransition.WingDown(CurrentFloor);
        Enter(to, r.Ok ? r.Arrive : new Vector2Int(0, 0));
        return true;
    }

    void MovePlayerTo(Vector2Int at)
    {
        if (player != null) player.position = new Vector3(at.x, at.y, player.position.z);
    }

    /// <summary>解錠が起きたら、その結果を見た目にも反映する。</summary>
    void ApplyUnlockToView(SealedDoors.Unlock u)
    {
        if (loader == null) return;
        foreach (var r in u.Results)
        {
            if (r.Kind == "表駆動" || r.Kind == "封印") continue;
            // 盤面と同じ変換を通してから見た目の鍵に直す。
            loader.Refresh(r.Pos, FloorState.ViewKeyOf(FloorState.SymbolOfKind(r.Kind)));
        }
    }
}
