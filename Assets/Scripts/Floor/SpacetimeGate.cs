using UnityEngine;

/// <summary>
/// 時空の門。**50階へ行く唯一の方法**。
/// 原典 PC98版 MTE.EXE の 0x22cc〜0x23e5 より。
///
/// <code>
///   現在階 == 24
///   かつ プレーヤーが (0,-1)              （原典は X=5, Y=6）
///   かつ マス(0,0) が門(4) になっている
///     → 現在階を24から50まで1階ずつ上げる演出を流し、
///       50階の (0,-1) に到着する
/// </code>
///
/// 【物語との対応】
///   26階はマグマで埋まった菱形の部屋で、中央に**人形**が置かれている。
///   触れるとゼノの声がする:
///     「・・・ジ・・・トキハキタレリ・・・エラバレシモノヨ、シヲオソレヌナラバ
///       ジクウノモンヲクグリテ、ワガモトヘクルガヨイ・・・」
///     「これはっ……！　人形だっ！」
///   24階は26階と同じ形の部屋だが中央が空いていて、そこに門が現れる。
///
///   原典ヒント「この塔は５０階建てだが、実際には４９階までしか行けないようだ」は
///   **階段で行けるのは49階まで**という意味だった。
/// </summary>
public static class SpacetimeGate
{
    /// <summary>門がある階。</summary>
    public const int GateFloor = 24;

    /// <summary>門そのものの位置。</summary>
    public static readonly Vector2Int GatePos = new Vector2Int(0, 0);

    /// <summary>門をくぐるために立つ位置（門の真下）。</summary>
    public static readonly Vector2Int StandPos = new Vector2Int(0, -1);

    /// <summary>行き先。</summary>
    public const int DestinationFloor = 50;
    public static readonly Vector2Int DestinationPos = new Vector2Int(0, -1);

    /// <summary>ゼノの伝言を告げる人形がある階と位置。</summary>
    public const int DollFloor = 26;
    public static readonly Vector2Int DollPos = new Vector2Int(0, 0);

    /// <summary>
    /// くぐれる条件を満たしているか。
    /// <paramref name="cellAt"/> は盤面のマスの中身を返す関数。
    /// </summary>
    public static bool CanEnter(int floor, Vector2Int playerPos,
                                System.Func<Vector2Int, string> cellAt)
    {
        if (floor != GateFloor) return false;
        if (playerPos != StandPos) return false;
        if (cellAt == null) return false;
        var c = cellAt(GatePos);
        return c == "s" || c == "門";      // 盤面上の門の表現
    }

    /// <summary>
    /// 演出で通過する階の並び。原典は24階から50階まで1階ずつ上がる。
    /// </summary>
    public static System.Collections.Generic.IEnumerable<int> RisingFloors()
    {
        for (int f = GateFloor; f <= DestinationFloor; f++) yield return f;
    }
}
