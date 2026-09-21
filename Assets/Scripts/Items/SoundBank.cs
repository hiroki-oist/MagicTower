using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 原典の音。データは Resources/Sounds.txt（docs/decode_sound.py が MTE.EXE から取り出したもの）。
///
/// 原典 PC98版は FM音源のドライバを EXE の中に抱えていて、ゲーム側は
/// <c>lcall 0x10da, 0x20b</c> に**音番号を1つ渡すだけ**（全61か所）。
/// ドライバの振り分け表がその番号のチャンネルデータを各チャンネルに入れて鳴らす。
///
///   音1・音2 … 4チャンネルの曲（BGM）
///   音6〜13  … 1チャンネルの効果音
///
/// 【設計方針・全体に適用】音そのものは持たず、**原典の譜面をそのまま数として持つ**。
/// 鳴らし方（合成）は後段に任せる。こうしておけば音源の作り方を変えても譜面は動かない。
/// </summary>
public class SoundBank
{
    /// <summary>譜面の1つ分。命令か音符。</summary>
    public struct Step
    {
        /// <summary>"note" / "rest" / "octave" / "octave+" / "octave-" / "volume" /
        /// "volume+" / "volume-" / "tempo" / "voice" / "length" / "end" / "loop" / "other"</summary>
        public string Kind;
        /// <summary>音符なら 0〜11 の半音（ド＝0）。命令なら引数。無ければ -1。</summary>
        public int Value;
        /// <summary>音長。原典の値で、実際の長さはこの3倍（原典 cs:0x1382）。省略は -1。</summary>
        public int Length;
        /// <summary>タイ（前の音と繋ぐ）。</summary>
        public bool Tie;
    }

    public class Channel
    {
        public int Number;
        public List<Step> Steps = new List<Step>();
    }

    readonly Dictionary<int, List<Channel>> sounds = new Dictionary<int, List<Channel>>();

    /// <summary>音符の名前（ド＝0）。</summary>
    public static readonly string[] NoteNames =
        { "c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b" };

    static SoundBank _instance;
    public static SoundBank Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static SoundBank LoadDefault()
    {
        var s = new SoundBank();
        var asset = Resources.Load<TextAsset>("Sounds");
        if (asset == null) Debug.LogError("SoundBank: Resources/Sounds.txt が見つかりません");
        else s.Parse(asset.text);
        return s;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 3) continue;
            int num, ch;
            if (!int.TryParse(p[0].Trim(), out num)) continue;
            if (!int.TryParse(p[1].Trim(), out ch)) continue;

            var c = new Channel { Number = ch };
            foreach (var t in p[2].Split(' '))
            {
                var tok = t.Trim();
                if (tok.Length > 0) c.Steps.Add(ParseStep(tok));
            }
            List<Channel> list;
            if (!sounds.TryGetValue(num, out list)) sounds[num] = list = new List<Channel>();
            list.Add(c);
        }
    }

    static Step ParseStep(string tok)
    {
        var s = new Step { Kind = "other", Value = -1, Length = -1 };

        int colon = tok.IndexOf(':');
        string head = colon >= 0 ? tok.Substring(0, colon) : tok;
        if (colon >= 0) int.TryParse(tok.Substring(colon + 1), out s.Length);
        if (s.Length == 0) s.Length = -1;

        if (head.EndsWith("~")) { s.Tie = true; head = head.Substring(0, head.Length - 1); }

        if (head == "end")  { s.Kind = "end";  return s; }
        if (head == "r")    { s.Kind = "rest"; return s; }
        // 第2音源（SSG）側のチャンネルの設定（原典の命令 0x59）。音符でも拍でもない。
        if (head.StartsWith("m") && head.Length > 1) { s.Kind = "mode"; return s; }
        if (head == "o+")   { s.Kind = "octave+"; return s; }
        if (head == "o-")   { s.Kind = "octave-"; return s; }
        if (head == "v+")   { s.Kind = "volume+"; return s; }
        if (head == "v-")   { s.Kind = "volume-"; return s; }

        for (int i = 0; i < NoteNames.Length; i++)
            if (head == NoteNames[i]) { s.Kind = "note"; s.Value = i; return s; }

        if (head.StartsWith("loop"))
        {
            s.Kind = "loop";
            var a = head.Substring(4);
            int addr;
            if (a.StartsWith("0x")
                && int.TryParse(a.Substring(2), System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture, out addr))
                s.Value = addr;
            return s;
        }

        if (head.Length > 1)
        {
            var arg = head.Substring(1);
            int v;
            bool ok = arg.StartsWith("0x")
                ? int.TryParse(arg.Substring(2), System.Globalization.NumberStyles.HexNumber,
                               System.Globalization.CultureInfo.InvariantCulture, out v)
                : int.TryParse(arg, out v);
            if (ok)
            {
                s.Value = v;
                switch (head[0])
                {
                    case 'o': s.Kind = "octave"; break;
                    case 'v': s.Kind = "volume"; break;
                    case 't': s.Kind = "tempo";  break;
                    case 'i': s.Kind = "voice";  break;
                    case 'l': s.Kind = "length"; break;
                }
            }
        }
        return s;
    }

    public IEnumerable<int> Numbers { get { return sounds.Keys; } }
    public int Count { get { return sounds.Count; } }

    /// <summary>その音のチャンネル。無ければ null。</summary>
    public List<Channel> Get(int number)
    {
        List<Channel> v;
        return sounds.TryGetValue(number, out v) ? v : null;
    }

    /// <summary>曲（チャンネルが2本以上）か。原典では音1と音2だけ。</summary>
    public bool IsMusic(int number)
    {
        var c = Get(number);
        return c != null && c.Count > 1;
    }
}
