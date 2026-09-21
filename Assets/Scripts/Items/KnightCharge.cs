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

    /// <summary>始まりの場所から道順をたどったときに通るマス。</summary>
    public List<Vector2Int> Trace(Vector2Int from)
    {
        var list = new List<Vector2Int>();
        var at = from;
        foreach (var r in runs)
            foreach (var d in r.Steps) { at += d; list.Add(at); }
        return list;
    }
}
