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

    /// <summary>
    /// 話しかけたあと盗賊が歩いて去る処理（原典 0x003e0 の 0x4c9〜0x520）。
    /// セリフを**読み終えてから**動くので、会話の頁のあとに積む。
    /// </summary>
    public const string ThiefLeavesStep = "盗賊が去る";

    /// <summary>盗賊が脱け出して牢が開く処理を表す行（原典 0x070c8）。</summary>
    public const string PrisonOpensStep = "牢が開く";

    /// <summary>
    /// 金騎士が斬りかかる段（原典 0x325e の向きの表）。道順は <see cref="KnightCharge"/>。
    ///
    /// 原典は**6回に分けて**斬りかかり、その合間にセリフを挟む。
    /// 道順の表は14まとまりで、1回目が1〜3、2回目が4〜6、3回目が7〜9、
    /// 4回目が10〜12、5回目が13、6回目が14（0x325e の外側の輪が
    /// `cmp [bp-0xfe], 3/6/9/0xc` と数えている）。
    /// "斬りかかる 1-3" のように何番目から何番目までかを書く。
    /// </summary>
    public const string ChargeStep = "斬りかかる";

    /// <summary>"斬りかかる 1-3" の範囲を読む。番号は1から。無ければ全部。</summary>
    public static void ParseChargeRange(string step, int runCount, out int from, out int to)
    {
        from = 0; to = runCount - 1;
        if (step == null || !step.StartsWith(ChargeStep)) return;
        var t = step.Substring(ChargeStep.Length).Trim();
        if (t.Length == 0) return;
        var p = t.Split('-');
        int a, b;
        if (!int.TryParse(p[0].Trim(), out a)) return;
        b = a;
        if (p.Length > 1 && !int.TryParse(p[1].Trim(), out b)) b = a;
        from = Mathf.Clamp(a - 1, 0, runCount - 1);
        to = Mathf.Clamp(b - 1, from, runCount - 1);
    }

    /// <summary>
    /// 場面の中で戦う段の頭。書き方は "戦闘 プレハブ名"。
    /// 原典 0x68b0（32階の金騎士）は歩かせたあと `0x6082` を直に呼ぶ。
    /// 踏み込みではないので**主人公は動かないし、マスも空けない**。
    /// </summary>
    public const string BattlePrefix = "戦闘 ";

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
    /// 10階で骸骨に囲まれる段（原典 0x03e0a）。台本は Resources/BossSkeletons.txt に
    /// あって（書き出しは docs/make_boss_skeletons.py）、読むときにこの1行を
    /// その並びへ広げる。原典が持っている8本の向きの表がそのまま入っている。
    /// </summary>
    public const string AmbushStep = "骸骨に囲まれる";

    /// <summary>台本を置いてある名前。</summary>
    public const string AmbushScript = "BossSkeletons";

    /// <summary>
    /// 主人公が1歩押し上げられる段（原典 0x3ebc〜0x3edd）。
    /// 10階の場面は (0,0) で始まって、主人公を上へ1マス滑らせてから囲む。
    /// </summary>
    public const string PushUpStep = "押し上げられる";

    /// <summary>
    /// 人が歩いてくる行の頭。書き方は "歩き 名前 (x,y) 1,2,3"。
    /// 数字は**原典の向きの表そのまま**（1=右 / 2=左 / 3=画面の下 / 4=上）。
    /// </summary>
    public const string WalkPrefix = "歩き ";

    /// <summary>扉が開く行の頭。書き方は "開く (x,y)"（原典 0x11b8）。</summary>
    public const string DoorOpenPrefix = "開く ";

    /// <summary>扉が閉まる行の頭。書き方は "閉まる (x,y)"（原典 0x1276）。</summary>
    public const string DoorClosePrefix = "閉まる ";

    /// <summary>
    /// 立っている人を消す行の頭。書き方は "消す (x,y)"。
    /// 原典 0x45be はそのマスを地図のとおりに描き直す＝盤面に居ない人は消える。
    /// </summary>
    public const string VanishPrefix = "消す ";

    /// <summary>向きの番号 → 盤面の1歩。原典の Y は下へ増えるので 3 が y-1。</summary>
    public static Vector2Int StepOf(int dir)
    {
        switch (dir)
        {
            case 1: return new Vector2Int(1, 0);
            case 2: return new Vector2Int(-1, 0);
            case 3: return new Vector2Int(0, -1);
            case 4: return new Vector2Int(0, 1);
        }
        return Vector2Int.zero;
    }

    /// <summary>"歩き 名前 (x,y) 1,2,3" を読む。違う形なら false。</summary>
    public static bool TryParseWalk(string step, out string prefab,
                                    out Vector2Int from, out List<Vector2Int> path)
    {
        prefab = null; from = Vector2Int.zero; path = null;
        if (step == null || !step.StartsWith(WalkPrefix)) return false;
        var t = step.Substring(WalkPrefix.Length).Trim();
        int open = t.IndexOf('('), close = t.IndexOf(')');
        if (open <= 0 || close < open) return false;
        prefab = t.Substring(0, open).Trim();
        if (!TryPos(t.Substring(open, close - open + 1), out from)) return false;
        path = new List<Vector2Int>();
        var at = from;
        foreach (var s in t.Substring(close + 1).Split(','))
        {
            int d;
            if (!int.TryParse(s.Trim(), out d)) continue;
            var move = StepOf(d);
            if (move == Vector2Int.zero) continue;
            at += move;
            path.Add(at);
        }
        return prefab.Length > 0 && path.Count > 0;
    }

    /// <summary>"開く (x,y)" のような行を読む。違う形なら false。</summary>
    public static bool TryParseCell(string step, string prefix, out Vector2Int pos)
    {
        pos = Vector2Int.zero;
        if (step == null || !step.StartsWith(prefix)) return false;
        return TryPos(step.Substring(prefix.Length), out pos);
    }

    /// <summary>"(x,y)" を読む。</summary>
    static bool TryPos(string text, out Vector2Int pos)
    {
        pos = Vector2Int.zero;
        if (text == null) return false;
        var t = text.Trim();
        int close = t.IndexOf(')');
        if (!t.StartsWith("(") || close < 0) return false;
        var xy = t.Substring(1, close - 1).Split(',');
        int x, y;
        if (xy.Length != 2 || !int.TryParse(xy[0].Trim(), out x) || !int.TryParse(xy[1].Trim(), out y))
            return false;
        pos = new Vector2Int(x, y);
        return true;
    }

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
                if (t.Length == 0) continue;
                // 長い段は別の帳面に書いてある（原典から書き出したもの）
                if (t == AmbushStep) steps.AddRange(Script(AmbushScript));
                else steps.Add(t);
            }
            scenes[name] = steps;
            sources[name] = p[1].Trim();
        }
    }

    /// <summary>
    /// 別の帳面に書いてある段の並び。1行が1手で、# から後ろは覚え書き。
    /// </summary>
    public static List<string> Script(string name)
    {
        var out_ = new List<string>();
        var asset = Resources.Load<TextAsset>(name);
        if (asset == null)
        {
            Debug.LogError("Cutscenes: Resources/" + name + ".txt が見つかりません");
            return out_;
        }
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            int sharp = line.IndexOf('#');
            if (sharp >= 0) line = line.Substring(0, sharp).Trim();
            if (line.Length > 0) out_.Add(line);
        }
        return out_;
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
