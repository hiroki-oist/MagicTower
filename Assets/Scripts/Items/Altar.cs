using UnityEngine;

/// <summary>
/// 祭壇への寄進。原典 MTE.EXE の祭壇処理（0x42aa）より。
///
/// <code>
///   C = これまでの寄進回数
///   寄進額     = min( 10 × (C×(C+1) + 2), 9990 )
///   生命力回復 = (C+1) × 100
///   ブロック   = (現在階 − 1) / 10 + 1
///   攻撃力アップ = 2 × ブロック
///   防御力アップ = 4 × ブロック
/// </code>
///
/// 防御の伸びが攻撃の2倍なのは、原典ヒント
/// 「祭壇に寄進した時は攻撃力よりも防御力を主に上げてもらった方がいいでしょう」と対応する。
/// ただし同じヒントは「攻撃力を全く上げないと後で倒せない敵が出てくる」とも言っている。
/// </summary>
public static class Altar
{
    /// <summary>寄進額の上限（原典の 0x2706）。</summary>
    public const int MaxCost = 9990;

    /// <summary>C 回目までの寄進を終えている状態で、次に求められる寄進額。</summary>
    public static int Cost(int donationCount)
    {
        int c = Mathf.Max(0, donationCount);
        return Mathf.Min(10 * (c * (c + 1) + 2), MaxCost);
    }

    /// <summary>「生命力を N ポイント回復」の N。</summary>
    public static int HealAmount(int donationCount)
    {
        return (Mathf.Max(0, donationCount) + 1) * 100;
    }

    /// <summary>階から求まるブロック番号（1〜5）。</summary>
    public static int Block(int floor)
    {
        return Mathf.Max(0, floor - 1) / 10 + 1;
    }

    /// <summary>「攻撃力を N ポイントアップ」の N。</summary>
    public static int AttackGain(int floor) { return 2 * Block(floor); }

    /// <summary>「防御力を N ポイントアップ」の N。</summary>
    public static int DefenseGain(int floor) { return 4 * Block(floor); }
}
