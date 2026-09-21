using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 金騎士の突撃の道順。データは Resources/KnightCharge.txt
/// （`docs/make_knight_charge.py` が MTE.EXE 0x325e から取り出す）。
///
/// 原典は**向きを1バイトずつ並べた表**を持っていて、それを順に読みながら
/// 金騎士の絵を動かす（`[bp + si - 0x92]` を si を増やしながら引く）。
///
///   1 = X+1（右） / 2 = X-1（左） / 3 = Y+1（画面の下） / 4 = Y-1（上） / 5 = 区切り
///
/// **原典の Y は下へ増える**ので、盤面（y は上が＋）では 3 が y-1、4 が y+1 になる。
/// 全89歩・14まとまりで、ジグザグに降りていって最後にまっすぐ下りてから上へ戻る。
/// </summary>
public class KnightCharge
{
    /// <summary>
    /// 最初のひと振りの始まり。原典 0x3547/0x354f が X=5, Y=7 を置く＝盤面の (0,-2)。
    /// そこは同じ場面が門を書き込むマスで、**金騎士は退路の門から現れて斬りかかる**。
    /// </summary>
    public static readonly Vector2Int FirstStart = new Vector2Int(0, -2);

    /// <summary>
    /// 2振り目からの始まり。区切りのたびに原典 0x35e0〜0x35e7 が X=5, Y=5 に戻す
    /// ＝**盤面の中央 (0,0)**。だから14回とも盤面からはみ出さない。
    /// </summary>
    public static readonly Vector2Int RunStart = new Vector2Int(0, 0);

    /// <summary>1まとまり（区切りまでの連続した動き）。</summary>
    public class Run
    {
        public List<Vector2Int> Steps = new List<Vector2Int>();
    }

    readonly List<Run> runs = new List<Run>();

    static KnightCharge _instance;
    public static KnightCharge Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static KnightCharge LoadDefault()
    {
        var k = new KnightCharge();
        var asset = Resources.Load<TextAsset>("KnightCharge");
        if (asset == null) Debug.LogError("KnightCharge: Resources/KnightCharge.txt が見つかりません");
        else k.Parse(asset.text);
        return k;
    }

    /// <summary>原典の向き（1〜4）を盤面の向きに直す。</summary>
    public static Vector2Int DirOf(int code)
    {
        switch (code)
        {
            case 1: return new Vector2Int(1, 0);    // X+1
            case 2: return new Vector2Int(-1, 0);   // X-1
            case 3: return new Vector2Int(0, -1);   // 原典の Y+1（画面の下）
            case 4: return new Vector2Int(0, 1);    // 原典の Y-1（上）
        }
        return Vector2Int.zero;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            line = line.Trim();
            if (line.Length == 0) continue;

            var run = new Run();
            foreach (var t in line.Split(' '))
            {
                int n;
                if (!int.TryParse(t.Trim(), out n)) continue;
                var d = DirOf(n);
                if (d != Vector2Int.zero) run.Steps.Add(d);
            }
            if (run.Steps.Count > 0) runs.Add(run);
        }
    }

    /// <summary>まとまりの数（原典の区切りの数）。</summary>
    public int RunCount { get { return runs.Count; } }

    /// <summary>全部の歩数。</summary>
    public int StepCount
    {
        get { int n = 0; foreach (var r in runs) n += r.Steps.Count; return n; }
    }

    public IEnumerable<Run> Runs { get { return runs; } }

    /// <summary>
    /// ひと振りぶんの通り道。原典は**振るたびに始まりへ戻す**ので、
    /// 1振り目は門のマス、2振り目からは盤面の中央から始まる。
    /// </summary>
    public List<Vector2Int> TraceRun(int index)
    {
        var list = new List<Vector2Int>();
        if (index < 0 || index >= runs.Count) return list;
        var at = index == 0 ? FirstStart : RunStart;
        foreach (var d in runs[index].Steps) { at += d; list.Add(at); }
        return list;
    }

    /// <summary>14回ぶんを順につないだ通り道。</summary>
    public List<Vector2Int> Trace()
    {
        var list = new List<Vector2Int>();
        for (int i = 0; i < runs.Count; i++) list.AddRange(TraceRun(i));
        return list;
    }
}
