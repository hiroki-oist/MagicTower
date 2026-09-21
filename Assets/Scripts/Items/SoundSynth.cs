using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 原典の譜面（<see cref="SoundBank"/>）を鳴らせる波にする。
///
/// 音程は**原典の音程表そのもの**を使う。ドライバは YM2203 の F-Number を
/// 半音ごとに持っていて（Resources/SoundPitch.txt）、オクターブは
/// <c>o n</c> が block = n−1 として効く。周波数は OPN の式
/// <code>f = fnum × 3993600 / (144 × 2^(20 − block))</code>
/// で、<c>o4</c> のラが 220.05Hz、<c>o5</c> が 440.1Hz ちょうどになる。
///
/// 音長は原典の値×3が実際の長さ（原典 cs:0x1382）。4分音符＝原典の16。
/// テンポ t なら1目盛 = 60/(t×48) 秒。
///
/// 【設計方針・全体に適用】音色（FMのパラメータ29バイト）はまだ使わず、
/// 倍音を少し足した単純な波で代用する。**音程・長さ・強弱・テンポ・繰り返しは原典どおり**で、
/// 音色だけ後から差し替えられるように、ここだけを触れば済む形にしてある。
/// </summary>
public static class SoundSynth
{
    /// <summary>原典の音源のクロック（PC-98 の 3.9936MHz）。</summary>
    public const int FmClock = 3993600;

    /// <summary>作る波の標本化周波数。</summary>
    public const int SampleRate = 22050;

    static int[] _fnum;

    /// <summary>半音（ド＝0）ごとの F-Number。Resources/SoundPitch.txt から読む。</summary>
    public static int[] FNumbers
    {
        get { if (_fnum == null) _fnum = LoadPitch(); return _fnum; }
    }

    static int[] LoadPitch()
    {
        var table = new int[12];
        var asset = Resources.Load<TextAsset>("SoundPitch");
        if (asset == null) { Debug.LogError("SoundSynth: Resources/SoundPitch.txt が無い"); return table; }
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 2) continue;
            int i = Array.IndexOf(SoundBank.NoteNames, p[0].Trim());
            int v;
            if (i >= 0 && int.TryParse(p[1].Trim(), out v)) table[i] = v;
        }
        return table;
    }

    /// <summary>半音とオクターブから周波数。原典の式そのまま。</summary>
    public static double Frequency(int step, int octave)
    {
        if (step < 0 || step >= 12) return 0.0;
        int block = Mathf.Clamp(octave - 1, 0, 7);
        return FNumbers[step] * (double)FmClock / (144.0 * (1 << (20 - block)));
    }

    /// <summary>その音のテンポ。譜面のどこかに書いてあるものを全体に効かせる（原典は割り込み1つ）。</summary>
    public static int TempoOf(List<SoundBank.Channel> channels, int fallback = 120)
    {
        if (channels == null) return fallback;
        foreach (var c in channels)
            foreach (var s in c.Steps)
                if (s.Kind == "tempo") return s.Value;
        return fallback;
    }

    /// <summary>1目盛の秒数。4分音符＝原典の音長16＝48目盛。</summary>
    public static double TickSeconds(int tempo)
    {
        return 60.0 / (tempo * 48.0);
    }

    /// <summary>1チャンネルを波にする。</summary>
    public static float[] RenderChannel(SoundBank.Channel channel, int tempo)
    {
        var buf = new List<float>();
        if (channel == null) return buf.ToArray();

        int octave = 4, volume = 10, defaultLength = 16;
        SoundVoices.Voice voice = null;
        double tick = TickSeconds(tempo);
        // 音ごとに切り捨てると誤差が積もってチャンネルがずれるので、
        // **通しの目盛**から標本の境目を出す（原典も割り込みの回数で数えている）。
        long ticks = 0;

        foreach (var s in channel.Steps)
        {
            switch (s.Kind)
            {
                case "end": case "loop": return buf.ToArray();
                case "mode": continue;              // SSG のチャンネル設定。拍には入らない
                case "octave":  octave = s.Value; continue;
                case "octave+": octave++; continue;
                case "octave-": octave--; continue;
                case "volume":  volume = s.Value; continue;
                case "volume+": volume = Mathf.Min(15, volume + 3); continue;
                case "volume-": volume = Mathf.Max(0, volume - 3); continue;
                case "tempo":   tick = TickSeconds(s.Value); continue;
                case "length":  defaultLength = s.Value; continue;
                case "voice":   voice = SoundVoices.Instance.Get(s.Value); continue;
            }
            if (s.Kind != "note" && s.Kind != "rest") continue;

            int len = s.Length > 0 ? s.Length : defaultLength;
            long from = (long)(ticks * tick * SampleRate);
            ticks += 3L * len;
            long to = (long)(ticks * tick * SampleRate);
            int n = (int)(to - from);
            if (s.Kind == "rest") { for (int i = 0; i < n; i++) buf.Add(0f); continue; }

            double f = Frequency(s.Value, octave);
            double amp = Math.Pow(volume / 15.0, 1.6) * 0.5;
            int attack = (int)(0.004 * SampleRate);
            int release = (int)(0.03 * SampleRate);
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)SampleRate;
                double env = 1.0;
                if (i < attack) env = i / (double)attack;
                if (!s.Tie && n - i < release) env = (n - i) / (double)release;
                double ph = 2.0 * Math.PI * f * t;
                buf.Add((float)(amp * env * Wave(ph, voice) / 1.6));
            }
        }
        return buf.ToArray();
    }

    /// <summary>
    /// 波の形。原典の音色から **ALG（つなぎ方）と TL（大きさ）だけ**を見て寄せる。
    ///
    /// 本物の OPN は4つのオペレータを ALG のとおりに繋いで鳴らすが、ここでは
    ///   ALG 0〜3（直列が主）… 変調をかけた波（変調の深さは op1 の TL から）
    ///   ALG 4〜7（並列が主）… 倍音を足した波（大きさは各 op の TL から）
    /// という2通りに寄せる。**音程・長さ・強弱・テンポは原典どおり**で、
    /// ここだけが近似。4オペレータをちゃんと組むときはこの関数を差し替えればよい。
    /// </summary>
    static double Wave(double ph, SoundVoices.Voice v)
    {
        if (v == null) return Math.Sin(ph) + 0.4 * Math.Sin(2 * ph) + 0.2 * Math.Sin(3 * ph);
        if (v.Algorithm <= 3)
        {
            // 直列。op1 を変調に使う（MUL がその倍率）
            double index = 6.0 * SoundVoices.LevelOf(v.Ops[0].TL);
            int mul = Mathf.Max(1, v.Ops[0].MUL);
            return Math.Sin(ph + index * Math.Sin(mul * ph)) * 1.6;
        }
        double w = 0.0;
        for (int i = 0; i < 4; i++)
        {
            int mul = Mathf.Max(1, v.Ops[i].MUL);
            w += SoundVoices.LevelOf(v.Ops[i].TL) * Math.Sin(mul * ph);
        }
        return w;
    }

    /// <summary>その音（曲なら全チャンネル）を混ぜて波にする。</summary>
    public static float[] Render(int number)
    {
        var chans = SoundBank.Instance.Get(number);
        if (chans == null || chans.Count == 0) return new float[0];
        int tempo = TempoOf(chans);

        var parts = new List<float[]>();
        int longest = 0;
        foreach (var c in chans)
        {
            var p = RenderChannel(c, tempo);
            parts.Add(p);
            longest = Mathf.Max(longest, p.Length);
        }
        var mix = new float[longest];
        foreach (var p in parts)
            for (int i = 0; i < p.Length; i++) mix[i] += p[i];
        float k = 1f / Mathf.Sqrt(Mathf.Max(1, parts.Count));
        for (int i = 0; i < mix.Length; i++) mix[i] = Mathf.Clamp(mix[i] * k, -1f, 1f);
        return mix;
    }

    /// <summary>そのまま鳴らせる形にする。曲（チャンネルが2本以上）は繰り返す。</summary>
    public static AudioClip Clip(int number)
    {
        var data = Render(number);
        if (data.Length == 0) return null;
        var clip = AudioClip.Create($"Sound{number}", data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
