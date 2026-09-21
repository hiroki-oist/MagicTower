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
            double amp = Math.Pow(volume / 15.0, 1.6) * 0.9;
            var fm = voice != null ? new FmVoice(voice) : null;
            double keyOff = (n / (double)SampleRate) * 0.85;      // 音長の後ろ15%で離す
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)SampleRate;
                double w;
                if (fm != null)
                {
                    w = fm.Next(f, SampleRate, t, !s.Tie && t >= keyOff, keyOff);
                }
                else
                {
                    double ph = 2.0 * Math.PI * f * t;
                    double env = 1.0;
                    double rel = 0.03;
                    if (!s.Tie && (n - i) / (double)SampleRate < rel)
                        env = (n - i) / (double)SampleRate / rel;
                    w = env * (Math.Sin(ph) + 0.4 * Math.Sin(2 * ph) + 0.2 * Math.Sin(3 * ph)) / 1.6;
                }
                buf.Add((float)(amp * w));
            }
        }
        return buf.ToArray();
    }

    /// <summary>
    /// 原典の音色で4つのオペレータを繋いで鳴らす。OPN の組み方をそのまま写す。
    ///
    /// つなぎ方（ALG）は YM2203 の8通り:
    /// <code>
    ///   0: 1→2→3→4          1: (1+2)→3→4        2: (1 + 2→3)→4      3: (1→2 + 3)→4
    ///   4: 1→2, 3→4 の和     5: 1→(2,3,4) の和    6: 1→2 と 3,4 の和   7: 1+2+3+4
    /// </code>
    /// 各オペレータの音程は基本周波数×MUL（MUL=0 は 1/2）、大きさは TL（0.75dB刻み）、
    /// 包絡は AR/DR/SL/SR/RR。**包絡の速さだけは原典の表が無いので近似**で、
    /// つなぎ方・倍率・大きさ・折り返し点は原典の値そのまま。
    /// 自己帰還（FB）は op1 にだけかかる。
    /// </summary>
    public class FmVoice
    {
        readonly SoundVoices.Voice v;
        readonly double[] phase = new double[4];
        readonly double[] outv = new double[4];      // 1標本ごとに作らない（22秒×4本ぶん回る）
        double last1, last1b;

        public FmVoice(SoundVoices.Voice voice) { v = voice; }

        /// <summary>MUL の倍率。0 は 1/2 倍（OPN の決まり）。</summary>
        static double Ratio(int mul) { return mul == 0 ? 0.5 : mul; }

        /// <summary>包絡の速さ。値が大きいほど速い（原典の表の代わりの近似）。</summary>
        static double Rate(int r, double slowest)
        {
            if (r <= 0) return 0.0;                       // 0 は「動かない」
            return slowest * Math.Pow(2.0, (r - 31) / 4.0);
        }

        /// <summary>時刻 t（音が鳴り始めてからの秒）でのオペレータの出力の大きさ（0〜1）。</summary>
        static double Envelope(SoundVoices.Op o, double t, bool released, double releasedAt)
        {
            double atk = Rate(o.AR, 600.0);               // 1秒あたりに進む割合
            double dec = Rate(o.DR, 60.0);
            double sus = Rate(o.SR, 6.0);
            double rel = Rate(o.RR * 2 + 1, 120.0);
            double sl = o.SL == 15 ? 0.0 : 1.0 - o.SL / 16.0;   // 折り返し点（SL15は無音）

            double e;
            double ta = atk > 0 ? 1.0 / atk : 0.0;
            if (t < ta) e = atk > 0 ? t * atk : 1.0;      // 立ち上がり
            else
            {
                double td = t - ta;
                double d = dec > 0 ? (1.0 - td * dec) : 1.0;
                e = d > sl ? d : sl - (td - (dec > 0 ? (1.0 - sl) / dec : 0.0)) * sus;
            }
            if (released) e -= (t - releasedAt) * rel;
            return e < 0.0 ? 0.0 : (e > 1.0 ? 1.0 : e);
        }

        /// <summary>1標本ぶん進める。<paramref name="f"/> は基本周波数。</summary>
        public double Next(double f, int rate, double t, bool released, double releasedAt)
        {
            var op = v.Ops;
            double fb = v.Feedback == 0 ? 0.0 : Math.Pow(2.0, v.Feedback) / 64.0;

            for (int i = 0; i < 4; i++)
                phase[i] += 2.0 * Math.PI * f * Ratio(op[i].MUL) / rate;

            // op1 は自分の出力を戻して変調する（平均を取るのが実機に近い）
            double m1 = fb * (last1 + last1b) * 0.5;
            outv[0] = Math.Sin(phase[0] + m1)
                    * SoundVoices.LevelOf(op[0].TL) * Envelope(op[0], t, released, releasedAt);
            last1b = last1; last1 = outv[0];

            double In(int i, double mod)
            {
                return Math.Sin(phase[i] + mod * 6.0)
                     * SoundVoices.LevelOf(op[i].TL) * Envelope(op[i], t, released, releasedAt);
            }

            switch (v.Algorithm)
            {
                case 0:
                    outv[1] = In(1, outv[0]); outv[2] = In(2, outv[1]); outv[3] = In(3, outv[2]);
                    return outv[3];
                case 1:
                    outv[1] = In(1, 0); outv[2] = In(2, outv[0] + outv[1]); outv[3] = In(3, outv[2]);
                    return outv[3];
                case 2:
                    outv[1] = In(1, 0); outv[2] = In(2, outv[1]); outv[3] = In(3, outv[0] + outv[2]);
                    return outv[3];
                case 3:
                    outv[1] = In(1, outv[0]); outv[2] = In(2, 0); outv[3] = In(3, outv[1] + outv[2]);
                    return outv[3];
                case 4:
                    outv[1] = In(1, outv[0]); outv[2] = In(2, 0); outv[3] = In(3, outv[2]);
                    return (outv[1] + outv[3]) * 0.5;
                case 5:
                    outv[1] = In(1, outv[0]); outv[2] = In(2, outv[0]); outv[3] = In(3, outv[0]);
                    return (outv[1] + outv[2] + outv[3]) / 3.0;
                case 6:
                    outv[1] = In(1, outv[0]); outv[2] = In(2, 0); outv[3] = In(3, 0);
                    return (outv[1] + outv[2] + outv[3]) / 3.0;
                default:
                    outv[1] = In(1, 0); outv[2] = In(2, 0); outv[3] = In(3, 0);
                    return (outv[0] + outv[1] + outv[2] + outv[3]) * 0.25;
            }
        }
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
