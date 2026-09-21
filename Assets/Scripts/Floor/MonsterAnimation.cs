using UnityEngine;

/// <summary>
/// 盤面の敵と人の絵を2コマで動かす仕掛け。原典 0x92a4 をそのまま写したもの。
///
/// 原典はこう動く:
/// <code>
///   1回呼ばれるごとに **5マス** ぶん進む（[bp-6] = 5 の繰り返し）
///   マスの番号 [0x17ea] を1つ進める。121 になったら0に戻し、
///   そのとき **コマの旗 [0x17e8] を入れ替える**（0↔1）
///   そのマスの属性が 7（人や物）・8（敵）・0xa（隣に届く敵）なら絵を貼り直す
///   旗が1なら表 DS:0x80a で引いた番号のまま、0なら **+1**（＝次の絵＝2コマ目）
/// </code>
///
/// 呼ばれるのは「1歩動くとき」（移動の演出 0xa276 の中から6回）と、
/// 場面の切り替えや待ちの間（0x9288 が回数ぶん呼ぶ）。
/// つまり **1歩ごとに 6×5 = 30マス** ぶん塗り直され、
/// 121マスなめ終わる **およそ4歩に1度** コマが入れ替わる。
///
/// 絵が一斉に切り替わるのではなく、盤面を舐めるように順に変わっていくのが原典の見え方。
/// </summary>
public class MonsterAnimation
{
    /// <summary>盤面のマスの数（11×11）。原典は 0x79 で番号を0に戻す。</summary>
    public const int Cells = 121;

    /// <summary>1回のひと押しで進むマス数（原典 [bp-6] = 5）。</summary>
    public const int CellsPerTick = 5;

    /// <summary>1歩ぶんのひと押しの回数（移動の演出 0xa276 の中に6か所）。</summary>
    public const int TicksPerStep = 6;

    /// <summary>次に塗り直すマスの番号（原典 [0x17ea]）。</summary>
    public int Cursor { get; private set; }

    /// <summary>今どちらのコマを貼るか（原典 [0x17e8]。true = 1コマ目）。</summary>
    public bool FirstFrame { get; private set; }

    /// <summary>マスごとに、最後に貼ったのがどちらのコマか。</summary>
    readonly bool[] painted = new bool[Cells];

    public MonsterAnimation()
    {
        FirstFrame = true;
        for (int i = 0; i < Cells; i++) painted[i] = true;
    }

    /// <summary>原典のマスの番号（0〜120）から盤面の座標へ。行を先に数える。</summary>
    public static Vector2Int CellOf(int index)
    {
        return new Vector2Int(index % 11 - 5, 5 - index / 11);
    }

    /// <summary>盤面の座標から原典のマスの番号へ。</summary>
    public static int IndexOf(Vector2Int cell)
    {
        return (5 - cell.y) * 11 + (cell.x + 5);
    }

    /// <summary>そのマスに今どちらのコマが出ているか。true = 1コマ目。</summary>
    public bool FrameAt(Vector2Int cell)
    {
        int i = IndexOf(cell);
        return i < 0 || i >= Cells ? true : painted[i];
    }

    /// <summary>
    /// ひと押し。5マスぶん塗り直して、121マスを越えたらコマを入れ替える。
    /// 塗り直したマスを返す（見た目の層はそこだけ貼り替えればよい）。
    /// </summary>
    public System.Collections.Generic.List<Vector2Int> Tick()
    {
        var touched = new System.Collections.Generic.List<Vector2Int>(CellsPerTick);
        for (int n = 0; n < CellsPerTick; n++)
        {
            // 原典は先に番号を進めてから、越えていたら戻してコマを入れ替える
            Cursor++;
            if (Cursor >= Cells)
            {
                Cursor = 0;
                FirstFrame = !FirstFrame;
            }
            painted[Cursor] = FirstFrame;
            touched.Add(CellOf(Cursor));
        }
        return touched;
    }

    /// <summary>1歩ぶん（6回）まとめて進める。</summary>
    public System.Collections.Generic.List<Vector2Int> Step()
    {
        var all = new System.Collections.Generic.List<Vector2Int>();
        for (int i = 0; i < TicksPerStep; i++) all.AddRange(Tick());
        return all;
    }

    /// <summary>階を移ったときなど、最初の状態に戻す。</summary>
    public void Reset()
    {
        Cursor = 0;
        FirstFrame = true;
        for (int i = 0; i < Cells; i++) painted[i] = true;
    }
}
