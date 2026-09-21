using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 原典の規則を実際の進行につなぐ役。
///
/// 原典 PC98版の一手は、おおまかにこの順で進む:
/// <code>
///   1. プレーヤーが動く
///   2. 隣接マスを見て魔法攻撃が発動する（魔導師の挟み撃ち／魔術士の隣接攻撃）
///   3. 解錠条件をまとめて判定する（MTE.EXE 0x4762）
/// </code>
/// 戦闘で敵を倒したときも、そのマスを床にしてから 3 を行う。
/// ここではその流れだけを持ち、個々の計算は
/// <see cref="MagicAttack"/> / <see cref="SealedDoors"/> / <see cref="FloorState"/> に任せる。
/// </summary>
public class GameRules : MonoBehaviour
{
    /// <summary>
    /// シーンに置かれている GameRules。既存の経路から呼び出すための入口。
    /// 置かれていなければ null で、その場合は何も起きない（旧シーンはそのまま動く）。
    /// </summary>
    public static GameRules Active { get; private set; }

    public FloorState Floor { get; private set; }

    void Awake()   { if (Active == null) Active = this; }
    void OnDestroy(){ if (Active == this) Active = null; }

    /// <summary>世界座標を盤面のマスに直す。FloorLoader は (x,y,0) に置くので丸めるだけ。</summary>
    public static Vector2Int ToCell(Vector3 world)
    {
        return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
    }

    /// <summary>解錠が発火したときに呼ばれる（見た目の更新に使う）。</summary>
    public System.Action<SealedDoors.Unlock> OnUnlocked;

    /// <summary>魔法攻撃を受けたときに呼ばれる。引数は Messages.txt のキー。</summary>
    public System.Action<string, int> OnMagicAttack;

    public void EnterFloor(FloorData data)
    {
        Floor = new FloorState(data);
        Evaluate();
    }

    /// <summary>敵を倒した。原典と同じく**そのマスを床にする**。</summary>
    public void OnEnemyDefeated(Vector2Int at)
    {
        if (Floor == null) return;
        Floor.Clear(at);
        Evaluate();
    }

    /// <summary>
    /// プレーヤーが動いたあとの処理。魔法攻撃を先に解決し、そのあと解錠を判定する。
    /// </summary>
    /// <returns>魔法攻撃を受けたあとの生命力。</returns>
    public int OnPlayerMoved(Vector2Int to, int hp, int shieldRank)
    {
        if (Floor == null) return hp;

        var ids = ObjectIds.Instance;
        var n = Floor.NeighborsOf(to, ids.Of);
        string key;
        int after = MagicAttack.Apply(hp, shieldRank, n, out key);
        if (key != null && OnMagicAttack != null) OnMagicAttack(key, hp - after);

        Evaluate();
        return after;
    }

    void Evaluate()
    {
        if (Floor == null) return;
        List<SealedDoors.Unlock> fired = Floor.ApplyUnlocks();
        if (OnUnlocked == null) return;
        foreach (var u in fired) OnUnlocked(u);
    }
}
