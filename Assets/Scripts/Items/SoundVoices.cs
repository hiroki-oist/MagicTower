using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 原典の音色（FM音源のパラメータ）。データは Resources/SoundVoices.txt
/// （`docs/decode_sound.py` が MTE.EXE の音源ドライバから取り出す）。
///
/// 譜面の `i0x???` が指す先の **29バイト**で、並びは OPN のレジスタの順:
/// DT/MUL ×4 → TL ×4 → KS/AR ×4 → AM/DR ×4 → SR ×4 → SL/RR ×4 → SSG-EG ×4 → FB/ALG。
/// ドライバも 0x30 番地から4つ飛びに28バイト書き、最後に 0xb0 番地へ FB/ALG を書く。
///
/// 【設計方針・全体に適用】ここでは**数をそのまま持つだけ**。
/// 本物の4オペレータ合成はまだ作らず、<see cref="SoundSynth"/> が
/// ALG と TL だけを見て音の明るさを寄せる（近似であることを明記しておく）。
/// </summary>
public class SoundVoices
{
    public struct Op
    {
        public int DT, MUL, TL, KS, AR, AM, DR, SR, SL, RR, SSG;
    }

    public class Voice
    {
        /// <summary>原典のドライバ内の番地（譜面の `i` が指す値）。</summary>
        public int Address;
        public int Feedback, Algorithm;
        public Op[] Ops = new Op[4];
    }

    readonly Dictionary<int, Voice> voices = new Dictionary<int, Voice>();

    static SoundVoices _instance;
    public static SoundVoices Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static SoundVoices LoadDefault()
    {
        var v = new SoundVoices();
        var asset = Resources.Load<TextAsset>("SoundVoices");
        if (asset == null) Debug.LogError("SoundVoices: Resources/SoundVoices.txt が見つかりません");
        else v.Parse(asset.text);
        return v;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 15) continue;
            int addr = ParseHex(p[0].Trim());
            if (addr < 0) continue;

            Voice v;
            if (!voices.TryGetValue(addr, out v))
            {
                v = new Voice { Address = addr };
                voices[addr] = v;
            }
            v.Feedback = int.Parse(p[1].Trim());
            v.Algorithm = int.Parse(p[2].Trim());
            int op = int.Parse(p[3].Trim()) - 1;
            if (op < 0 || op > 3) continue;
            v.Ops[op] = new Op
            {
                DT = int.Parse(p[4].Trim()),  MUL = int.Parse(p[5].Trim()),
                TL = int.Parse(p[6].Trim()),  KS = int.Parse(p[7].Trim()),
                AR = int.Parse(p[8].Trim()),  AM = int.Parse(p[9].Trim()),
                DR = int.Parse(p[10].Trim()), SR = int.Parse(p[11].Trim()),
                SL = int.Parse(p[12].Trim()), RR = int.Parse(p[13].Trim()),
                SSG = int.Parse(p[14].Trim()),
            };
        }
    }

    static int ParseHex(string s)
    {
        if (!s.StartsWith("0x")) return -1;
        int v;
        return int.TryParse(s.Substring(2), System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out v) ? v : -1;
    }

    public int Count { get { return voices.Count; } }
    public IEnumerable<Voice> All { get { return voices.Values; } }

    public Voice Get(int address)
    {
        Voice v;
        return voices.TryGetValue(address, out v) ? v : null;
    }

    /// <summary>TL（0〜127、0.75dB刻み）を 0〜1 の大きさに直す。</summary>
    public static double LevelOf(int tl)
    {
        return System.Math.Pow(10.0, -tl * 0.75 / 20.0);
    }
}
