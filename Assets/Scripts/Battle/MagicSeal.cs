using UnityEngine;

/// <summary>
/// サイコロの５の目による封印。原典 PC98版 MTE.EXE 0x5cdb〜0x5e16。
///
/// <code>
///   if (現在階 == 49 &amp;&amp; フラグ131 == 0
///       &amp;&amp; (0,+4)==床 &amp;&amp; (-1,+3)==床 &amp;&amp; (+1,+3)==床 &amp;&amp; (0,+2)==床
///       &amp;&amp; (-1,+4)!=床 &amp;&amp; (+1,+4)!=床 &amp;&amp; (-1,+2)!=床 &amp;&amp; (+1,+2)!=床) {
///       フラグ131 = 1;
///       セリフ156「うっ……この陣形は……まずいっ！」;
///       モンスター表のID91(ゼノ)を 生命力800 / 攻撃500 / 防御100 に書き換える;
///   }
/// </code>
///
/// 中央 (0,+3) に立つプレーヤーと四隅の敵4体で**サイコロの５の目**になる。
/// 原典ヒント97「サイコロの５の目の形には、魔を封じ込める力があるという。
/// このブロックのボスと戦う時には、このことを頭にいれておきなされ。」の正体がこれ。
/// もとの 8000/5000/1000 がちょうど**10分の1**になる。
/// </summary>
public static class MagicSeal
{
    public const int Floor = 49;
    public const int FlagId = 131;
    public const int Record = 156;

    /// <summary>封じられる相手。ID91 = 魔導師ゼノ。</summary>
    public const int BossId = 91;
    public const string BossPrefab = "Boss_Zeno";

    public static readonly Vector2Int Center = new Vector2Int(0, 3);

    /// <summary>ここに敵がいること（サイコロの５の目の四隅）。</summary>
    public static readonly Vector2Int[] Corners =
    {
        new Vector2Int(-1, 4), new Vector2Int(1, 4),
        new Vector2Int(-1, 2), new Vector2Int(1, 2),
    };

    /// <summary>ここが空いていること（四隅の間）。</summary>
    public static readonly Vector2Int[] Edges =
    {
        new Vector2Int(0, 4), new Vector2Int(-1, 3),
        new Vector2Int(1, 3), new Vector2Int(0, 2),
    };

    public const int SealedHp = 800, SealedAttack = 500, SealedDefense = 100;

    /// <summary>その盤面が封印の形になっているか。</summary>
    public static bool IsFormed(FloorState floor)
    {
        if (floor == null) return false;
        foreach (var e in Edges)   if (!floor.IsCleared(e)) return false;
        foreach (var c in Corners) if (floor.IsCleared(c)) return false;
        return true;
    }

    /// <summary>封印済みならゼノの数値を書き換える。</summary>
    public static void Adjust(PlayerState player, string enemyPrefab,
                              ref int hp, ref int attack, ref int defense)
    {
        if (player == null || enemyPrefab != BossPrefab) return;
        if (!player.EventFlags.Contains(FlagId)) return;
        hp = SealedHp; attack = SealedAttack; defense = SealedDefense;
    }
}
