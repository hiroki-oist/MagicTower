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

    /// <summary>
    /// 序章の1枚絵を表す行（MTOPSTR）。原典は**絵の中に3行とも描いてあり**、
    /// 行ごとに色の索引（1・2・3）を分けておいて、パレットを差し替えて
    /// 1行ずつ白くする（`lcall 0xf5f,0x461`。動画 Part1 t≈34〜41）。
    /// リメイクは色を変えた4枚（`Prologue_0`〜`3`）を順に出す。
    /// </summary>
    public const string PrologueStep = "序章の絵";

    /// <summary>序章の絵の行数（3行）。</summary>
    public const int PrologueLines = 3;

    /// <summary>
    /// 終幕の塔の絵（MTOWER1/2）。原典はパレットでうっすら→はっきりと出す
    /// （0xcd2e〜、`lcall 0xf5f,0x416` を回数ぶん）。
    /// </summary>
    public const string EndingTowerStep = "塔の絵";

    /// <summary>
    /// 終わりの言葉（MTENDMJ）。白地に黒の1枚絵で、**段落が1つずつ現れる**
    /// （動画 Part2 t≈1810〜1825）。
    /// </summary>
    public const string EndingTextStep = "終わりの言葉";

    /// <summary>終わりの言葉の段落の数。</summary>
    public const int EndingTextParagraphs = 4;

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

    /// <summary>
    /// 20階のボスが現れる段（原典 0x03044 の 0x3119〜0x3194）。
    /// 規則は <see cref="BossAppear"/>。退路の門を置いたあと、地図の三つ組より前に入る。
    /// </summary>
    public const string BossAppearStep = "ボスが現れる";

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

    /// <summary>
    /// 地図の書き換え1つぶん。"地図 (x,y)=種別" と、あれば "点滅N,W"。
    ///
    /// 原典の場面はものを出すとき、**そのマスを床とものとで交互に描いて点滅させる**
    /// （3階の 0x6ae0 は ゼノを50回、魔導師を各15回）。回数のあとの数は、
    /// 点滅が終わってからの待ち（原典の `lcall 0xf5f,0x3f2` に渡す垂直帰線の回数）。
    /// </summary>
    public struct MapStep
    {
        public Vector2Int Pos;
        public string Kind;
        /// <summary>床とものを入れ替える回数。0なら点滅させない。</summary>
        public int Blinks;
        /// <summary>点滅のあとの待ち（垂直帰線の回数＝1/60秒）。</summary>
        public int Wait;
    }

    /// <summary>点滅の書き方の頭。</summary>
    public const string BlinkPrefix = "点滅";

    /// <summary>"地図 (x,y)=種別 [点滅N,W]" を読む。違う形なら false。</summary>
    public static bool TryParseMap(string step, out MapStep map)
    {
        map = new MapStep();
        if (step == null || !step.StartsWith(MapStepPrefix)) return false;
        var t = step.Substring(MapStepPrefix.Length).Trim();
        int close = t.IndexOf(')'), eq = t.IndexOf('=');
        if (!t.StartsWith("(") || close < 0 || eq < close) return false;
        var xy = t.Substring(1, close - 1).Split(',');
        int x, y;
        if (xy.Length != 2 || !int.TryParse(xy[0].Trim(), out x) || !int.TryParse(xy[1].Trim(), out y))
            return false;
        map.Pos = new Vector2Int(x, y);

        var rest = t.Substring(eq + 1).Trim();
        int sp = rest.IndexOf(' ');
        if (sp > 0)
        {
            var tail = rest.Substring(sp + 1).Trim();
            rest = rest.Substring(0, sp);
            if (tail.StartsWith(BlinkPrefix))
            {
                var nums = tail.Substring(BlinkPrefix.Length).Split(',');
                int b, w;
                if (nums.Length > 0 && int.TryParse(nums[0].Trim(), out b)) map.Blinks = b;
                if (nums.Length > 1 && int.TryParse(nums[1].Trim(), out w)) map.Wait = w;
            }
        }
        map.Kind = rest;
        return true;
    }

    /// <summary>種別だけ要るとき用。</summary>
    public static bool TryParseMap(string step, out Vector2Int pos, out string kind)
    {
        MapStep m;
        bool ok = TryParseMap(step, out m);
        pos = m.Pos; kind = m.Kind;
        return ok;
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
