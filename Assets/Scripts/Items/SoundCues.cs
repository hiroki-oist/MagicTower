using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// できごと → 原典の音番号。データは Resources/SoundCues.txt。
///
/// 原典はゲーム側から <c>lcall 0x10da, 0x20b</c> に音番号を渡すだけで、
/// その呼び出しは EXE 全体で61か所ある。それを「どの関数の中か」で仕分けると
/// 16通りの合図に収まった（表の右端に原典の場所を控えてある）。
///
/// 【設計方針・全体に適用】本線のコードは**合図の名前でしか音を呼ばない**。
/// 番号と譜面（<see cref="SoundBank"/>）はデータ側に閉じ込める。
/// </summary>
public class SoundCues
{
    readonly Dictionary<string, int> cues = new Dictionary<string, int>();
    readonly Dictionary<string, string> sources = new Dictionary<string, string>();

    static SoundCues _instance;
    public static SoundCues Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static SoundCues LoadDefault()
    {
        var c = new SoundCues();
        var asset = Resources.Load<TextAsset>("SoundCues");
        if (asset == null) Debug.LogError("SoundCues: Resources/SoundCues.txt が見つかりません");
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
            int n;
            if (!int.TryParse(p[1].Trim(), out n)) continue;
            cues[p[0].Trim()] = n;
            sources[p[0].Trim()] = p[2].Trim();
        }
    }

    public int Count { get { return cues.Count; } }
    public IEnumerable<string> Names { get { return cues.Keys; } }

    /// <summary>合図の音番号。無ければ -1。</summary>
    public int Number(string cue)
    {
        int v;
        return cue != null && cues.TryGetValue(cue, out v) ? v : -1;
    }

    /// <summary>その合図が原典のどこから来ているか。</summary>
    public string Source(string cue)
    {
        string v;
        return cue != null && sources.TryGetValue(cue, out v) ? v : null;
    }

    // 本線から呼ぶ合図の名前。綴り違いで静かに鳴らない事故を防ぐ。
    public const string TakeItem   = "拾う";
    public const string OpenDoor   = "扉を開ける";
    public const string Stairs     = "階段";
    public const string TalkMaster = "仙人と話す";
    public const string TalkShop   = "商人と話す";
    public const string Reveal     = "現れる";
    public const string Slash      = "斬りかかる";
    public const string Hit        = "命中";
    public const string Magic      = "魔法攻撃";
    public const string SnowMelt   = "雪の結晶";
    public const string Unlock     = "解錠";
    public const string BigUnlock  = "大きな解錠";
    public const string UseSpecial = "特殊アイテム";
    public const string SceneBeat  = "場面の節目";
    public const string FieldMusic = "本編の曲";
    public const string TitleMusic = "題名の曲";
    public const string OpenGate   = "門が開く";
    public const string WallAppear = "壁が現れる";
    public const string SceneBeat2 = "場面のできごと";
    public const string Receding   = "遠ざかる";

    /// <summary>
    /// ★**原典に無い合図**（作者の指定）。ボタンを触ったとき・決めたときの「ピッ」。
    /// 譜面は Resources/UiSounds.txt（原典の Sounds.txt とは別の表）。
    /// </summary>
    public const string Tap = "触る";
}
