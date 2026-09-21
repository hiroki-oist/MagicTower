using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 場面（演出）の並び。データは Resources/Cutscenes.txt。
///
/// 原典は場面ごとに専用の関数を持っていて、その中でセリフや画面の切り替えを
/// 順に呼んでいる。ここはその並びだけをデータにしたもの。
/// 「題名」の行は <see cref="TitleScreen"/>（原典 0x6db2）を指す。
/// </summary>
public class Cutscenes
{
    /// <summary>題名画面を表す行。</summary>
    public const string TitleStep = "題名";

    /// <summary>ゼノに全てを奪われる処理を表す行（原典 0x06f1b）。</summary>
    public const string ZenoTakesStep = "奪われる";

    /// <summary>盗賊が脱け出して牢が開く処理を表す行（原典 0x070c8）。</summary>
    public const string PrisonOpensStep = "牢が開く";

    /// <summary>
    /// 金騎士が斬りかかる段（原典 0x325e の向きの表）。道順は <see cref="KnightCharge"/>。
    /// </summary>
    public const string ChargeStep = "斬りかかる";

    /// <summary>
    /// 閃光の段。原典は音7を鳴らして**自分の立っているマス**に当たりの絵を貼る
    /// （0x6d09〜0x6d42 と 0x73b1〜0x73c6）。戦闘や魔法攻撃と同じ絵。
    /// </summary>
    public const string FlashStep = "閃光";

    /// <summary>
    /// 塔を駆け上がる段（原典 0x2386〜0x23cd）。現在階を1つずつ50まで上げる演出のあと、
    /// 50階の (0,-1) に立たせる。
    /// </summary>
    public const string ClimbStep = "駆け上がる";

    /// <summary>終幕の1枚絵を表す行（原典 0xd45c）。</summary>
    public const string FinalScreenStep = "最後の画面";

    /// <summary>
    /// 地図の書き換えを表す行の頭。書き方は "地図 (x,y)=種別"。
    ///
    /// 原典の場面は、セリフを出すだけでなく**その場で地図を書き換える**ものが多い。
    /// 20階の「ボス登場」なら、退路の赤い扉が門に変わってボスが中央に現れる。
    /// これを演出の並びに素直に載せられるようにした（順番も原典のまま）。
    /// </summary>
    public const string MapStepPrefix = "地図 ";

    /// <summary>"地図 (x,y)=種別" を読む。違う形なら false。</summary>
    public static bool TryParseMap(string step, out Vector2Int pos, out string kind)
    {
        pos = Vector2Int.zero; kind = null;
        if (step == null || !step.StartsWith(MapStepPrefix)) return false;
        var t = step.Substring(MapStepPrefix.Length).Trim();
        int close = t.IndexOf(')'), eq = t.IndexOf('=');
        if (!t.StartsWith("(") || close < 0 || eq < close) return false;
        var xy = t.Substring(1, close - 1).Split(',');
        int x, y;
        if (xy.Length != 2 || !int.TryParse(xy[0].Trim(), out x) || !int.TryParse(xy[1].Trim(), out y))
            return false;
        pos = new Vector2Int(x, y);
        kind = t.Substring(eq + 1).Trim();
        return true;
    }

    readonly Dictionary<string, List<string>> scenes = new Dictionary<string, List<string>>();
    readonly Dictionary<string, string> sources = new Dictionary<string, string>();

    public IEnumerable<string> Names { get { return scenes.Keys; } }
    public int Count { get { return scenes.Count; } }

    static Cutscenes _instance;
    public static Cutscenes Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static Cutscenes LoadDefault()
    {
        var c = new Cutscenes();
        var asset = Resources.Load<TextAsset>("Cutscenes");
        if (asset == null) Debug.LogError("Cutscenes: Resources/Cutscenes.txt が見つかりません");
        else c.Parse(asset.text);
        return c;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 3) continue;

            var name = p[0].Trim();
            var steps = new List<string>();
            foreach (var s in p[2].Split(';'))
            {
                var t = s.Trim();
                if (t.Length > 0) steps.Add(t);
            }
            scenes[name] = steps;
            sources[name] = p[1].Trim();
        }
    }

    /// <summary>その場面の並び。無ければ null。</summary>
    public List<string> Steps(string name)
    {
        List<string> v;
        return name != null && scenes.TryGetValue(name, out v) ? v : null;
    }

    /// <summary>その場面の原典での場所（関数のアドレス）。</summary>
    public string Source(string name)
    {
        string v;
        return name != null && sources.TryGetValue(name, out v) ? v : null;
    }
}
