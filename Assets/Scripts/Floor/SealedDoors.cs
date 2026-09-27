using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 門（封印扉）の解錠表。データは Resources/SealedDoors.txt。
///
/// 原典（PC98版 MTE.EXE の判定関数 0x4762）はこう書かれている:
/// <code>
///   if (現在階 == F && 一度きりフラグ == 0 && 守護マスがすべて床(6)) {
///       フラグ = 1;
///       結果を地図に適用する;   // 門を消す／道をつなぐ／アイテムを出す
///   }
/// </code>
///
/// 敵を倒すとそのマスは床になるので、**「守護マスが床である」＝「そこの敵を倒した」**。
/// 原典ヒント「門があれば、その門を守っているモンスターが必ず同じ階にいるはずだ」の実装が これ。
///
/// 【設計方針・全体に適用】
///   門を「扉1枚と守護者1体」の組で持つのをやめ、**原典どおり
///   「守護マスの集合 → 地図の書き換えの集合」**として持つ。
///   1つの解錠が複数マスを変える（F41 は降臨の翼の出現と壁3マスの追加を同時に行う）ため。
/// </summary>
public class SealedDoors
{
    /// <summary>解錠で起きる地図の書き換え1件。</summary>
    public struct Result
    {
        public Vector2Int Pos;
        /// <summary>"床" / "門" / "壁" / "隠し壁" / "上り" / "IDnn" / "表駆動"</summary>
        public string Kind;
    }

    /// <summary>解錠のきまり1件。</summary>
    public struct Unlock
    {
        public int Floor;
        /// <summary>原典の一度きりフラグの番号。同じ階の複数の門を区別する。</summary>
        public int FlagId;
        /// <summary>ここが全部床になったら発火する。</summary>
        public List<Vector2Int> Guards;
        /// <summary>
        /// 逆に、ここが床で**ない**（＝何かが乗っている）ことを求めるマス。
        /// 原典 0x5d37〜0x5d5e（49階のサイコロの５の目）だけがこれを使う。
        /// </summary>
        public List<Vector2Int> AntiGuards;
        /// <summary>
        /// これが立っていないと判定に入らない一度きりフラグ。無ければ -1。
        /// 原典は4件だけこれを持つ（0x5d←0x5c / 0x68←0x67 / 0x6a←0x69 / 0x84←0x83）。
        /// 守護マスが最初から床でも、そのできごとが起きるまで発火しないようにするための鍵。
        /// </summary>
        public int RequiresFlag;
        public List<Result> Results;
        /// <summary>
        /// 解錠と一緒に出すセリフの番号。無ければ -1。
        /// 原典はブロックのボスを倒したとき、**褒美を並べる前に**ボスの最期の
        /// セリフを出す（10階 0x4e5b→106 / 20階 0x531d→122）。
        /// </summary>
        public int Record;
        /// <summary>
        /// そのセリフの前に置く間（垂直帰線の回数＝1/60秒）。原典は 0x32＝50。
        /// 倒した直後に窓が開くと、斬り合いの絵と重なって見える。
        /// </summary>
        public int WaitFrames;
    }

    readonly List<Unlock> unlocks = new List<Unlock>();

    static SealedDoors _instance;
    public static SealedDoors Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static SealedDoors LoadDefault()
    {
        var s = new SealedDoors();
        var asset = Resources.Load<TextAsset>("SealedDoors");
        if (asset == null) Debug.LogError("SealedDoors: Resources/SealedDoors.txt が見つかりません");
        else s.Parse(asset.text);
        return s;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            // 行のうしろに付けた覚え書き（「# 動画 Part1 t=700 と一致」など）は読み飛ばす
            int note = line.IndexOf('#');
            if (note > 0) line = line.Substring(0, note).Trim();
            var p = line.Split('|');
            if (p.Length < 4) continue;
            int floor;
            if (!int.TryParse(p[0].Trim(), out floor)) continue;

            var u = new Unlock
            {
                Floor = floor,
                FlagId = ParseFlag(p[1].Trim()),
                Guards = ParseGuards(p[2], false),
                AntiGuards = ParseGuards(p[2], true),
                RequiresFlag = ParseRequires(p[2]),
                Results = ParseResults(p[3]),
                Record = p.Length > 4 ? ParseRecord(p[4]) : -1,
                WaitFrames = p.Length > 4 ? ParseWait(p[4]) : 0
            };
            unlocks.Add(u);
        }
    }

    static int ParseFlag(string s)
    {
        if (s.StartsWith("0x") || s.StartsWith("0X"))
        {
            int v;
            if (int.TryParse(s.Substring(2), System.Globalization.NumberStyles.HexNumber,
                             System.Globalization.CultureInfo.InvariantCulture, out v)) return v;
        }
        int n;
        return int.TryParse(s, out n) ? n : -1;
    }

    /// <summary>"!" が付いたマスは「床でないこと」を求める。</summary>
    static List<Vector2Int> ParseGuards(string s, bool negated)
    {
        var list = new List<Vector2Int>();
        foreach (var part in s.Split(';'))
        {
            var t = part.Trim();
            if (t.Length == 0) continue;
            bool neg = t[0] == '!';
            if (neg) t = t.Substring(1).Trim();
            if (neg != negated) continue;
            var xy = t.Split(',');
            int x, y;
            if (xy.Length == 2 && int.TryParse(xy[0].Trim(), out x) && int.TryParse(xy[1].Trim(), out y))
                list.Add(new Vector2Int(x, y));
        }
        return list;
    }

    /// <summary>守護マスの欄に書かれた "フラグ:0xNN" を読む。無ければ -1。</summary>
    static int ParseRequires(string s)
    {
        foreach (var part in s.Split(';'))
        {
            var t = part.Trim();
            if (!t.StartsWith("フラグ:")) continue;
            return ParseFlag(t.Substring("フラグ:".Length).Trim());
        }
        return -1;
    }

    /// <summary>5つ目の欄の「セリフNNN」を読む。無ければ -1。</summary>
    static int ParseRecord(string s)
    {
        foreach (var part in s.Split(';'))
        {
            var t = part.Trim();
            if (!t.StartsWith(RecordWord)) continue;
            int n;
            if (int.TryParse(t.Substring(RecordWord.Length).Trim(), out n)) return n;
        }
        return -1;
    }

    /// <summary>5つ目の欄の「待つN」を読む。無ければ 0。</summary>
    static int ParseWait(string s)
    {
        foreach (var part in s.Split(';'))
        {
            var t = part.Trim();
            if (!t.StartsWith(WaitWord)) continue;
            int n;
            if (int.TryParse(t.Substring(WaitWord.Length).Trim(), out n)) return n;
        }
        return 0;
    }

    const string RecordWord = "セリフ";
    const string WaitWord = "待つ";

    static List<Result> ParseResults(string s)
    {
        var list = new List<Result>();
        foreach (var part in s.Trim().Split(' '))
        {
            var t = part.Trim();
            if (t.Length == 0 || !t.StartsWith("(")) continue;
            int close = t.IndexOf(')');
            int eq = t.IndexOf('=');
            if (close < 0 || eq < close) continue;
            var xy = t.Substring(1, close - 1).Split(',');
            int x, y;
            if (xy.Length != 2 || !int.TryParse(xy[0].Trim(), out x) || !int.TryParse(xy[1].Trim(), out y))
                continue;
            list.Add(new Result { Pos = new Vector2Int(x, y), Kind = t.Substring(eq + 1) });
        }
        return list;
    }

    public IEnumerable<Unlock> ForFloor(int floor)
    {
        foreach (var u in unlocks) if (u.Floor == floor) yield return u;
    }

    /// <summary>
    /// 守護マスが全部空いているか。
    /// <paramref name="isCleared"/> は「そのマスが床になっているか」を返す関数。
    /// </summary>
    public static bool IsSatisfied(Unlock u, Func<Vector2Int, bool> isCleared,
                                   Func<int, bool> hasFlag = null)
    {
        if (isCleared == null) return false;
        if (u.RequiresFlag >= 0 && (hasFlag == null || !hasFlag(u.RequiresFlag))) return false;
        foreach (var g in u.Guards) if (!isCleared(g)) return false;
        if (u.AntiGuards != null)
            foreach (var g in u.AntiGuards) if (isCleared(g)) return false;
        return true;
    }

    public int Count { get { return unlocks.Count; } }

    /// <summary>妖精の物体ID（原典 ID23）。</summary>
    public const string FairyKind = "ID23";
}
