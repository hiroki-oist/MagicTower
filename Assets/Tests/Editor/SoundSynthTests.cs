using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 譜面を波にするところ。音程は原典の音程表と OPN の式から決まる。
/// </summary>
public class SoundSynthTests
{
    [Test]
    public void 音程表は原典の値で読めている()
    {
        var f = SoundSynth.FNumbers;
        Assert.AreEqual(618, f[0], "ド");
        Assert.AreEqual(1040, f[9], "ラ");
        Assert.AreEqual(1167, f[11], "シ");
        for (int i = 1; i < 12; i++)
            Assert.Greater(f[i], f[i - 1], "半音ごとに上がる");
    }

    /// <summary>原典の式が正しいことの証拠。o4 のラがぴったり 220Hz になる。</summary>
    [Test]
    public void オクターブ4のラは220ヘルツ()
    {
        Assert.AreEqual(220.0, SoundSynth.Frequency(9, 4), 0.1);
        Assert.AreEqual(440.0, SoundSynth.Frequency(9, 5), 0.2);
        Assert.AreEqual(110.0, SoundSynth.Frequency(9, 3), 0.1);
    }

    [Test]
    public void 一目盛の長さはテンポで決まる()
    {
        // 4分音符＝原典の音長16＝48目盛。テンポ120なら0.5秒。
        Assert.AreEqual(0.5, SoundSynth.TickSeconds(120) * 48, 1e-9);
        Assert.AreEqual(60.0 / 130 , SoundSynth.TickSeconds(130) * 48, 1e-9);
    }

    [Test]
    public void 曲のテンポは譜面から拾う()
    {
        Assert.AreEqual(130, SoundSynth.TempoOf(SoundBank.Instance.Get(1)));
        Assert.AreEqual(108, SoundSynth.TempoOf(SoundBank.Instance.Get(2)));
    }

    [Test]
    public void 効果音の長さが原典の譜面どおり()
    {
        // 音6 = ド2・レ2・ミ4 → (2+2+4)*3 目盛。テンポは書いていないので既定の120。
        var w = SoundSynth.Render(6);
        double want = (2 + 2 + 4) * 3 * SoundSynth.TickSeconds(120) * SoundSynth.SampleRate;
        Assert.AreEqual(want, w.Length, 2, "音6の長さ");
        float peak = 0f;
        foreach (var v in w) peak = Mathf.Max(peak, Mathf.Abs(v));
        Assert.Greater(peak, 0.1f, "無音ではない");
    }

    [Test]
    public void 曲は4チャンネルとも同じ長さになる()
    {
        foreach (int n in new[] { 1, 2 })
        {
            var ch = SoundBank.Instance.Get(n);
            int tempo = SoundSynth.TempoOf(ch);
            int len = SoundSynth.RenderChannel(ch[0], tempo).Length;
            foreach (var c in ch)
                Assert.AreEqual(len, SoundSynth.RenderChannel(c, tempo).Length,
                                $"音{n}のチャンネル{c.Number}");
            Assert.Greater(len / (float)SoundSynth.SampleRate, 20f, $"音{n}は20秒を超える曲");
        }
    }

    [Test]
    public void 音色は原典の4つが読めている()
    {
        var v = SoundVoices.Instance;
        Assert.AreEqual(4, v.Count, "譜面が指す FM の音色は4つ");
        var lead = v.Get(0x3c4);
        Assert.IsNotNull(lead, "曲の主旋律の音色");
        Assert.AreEqual(7, lead.Feedback);
        Assert.AreEqual(0, lead.Algorithm);
        Assert.AreEqual(28, lead.Ops[0].TL, "op1 の大きさ");
        Assert.AreEqual(10, lead.Ops[0].MUL);
        Assert.AreEqual(0, lead.Ops[3].TL, "最後のオペレータは目一杯");
        foreach (var x in v.All)
        {
            Assert.AreEqual(7, x.Feedback, "4つとも FB=7");
            foreach (var o in x.Ops)
            {
                Assert.IsTrue(o.TL >= 0 && o.TL <= 127);
                Assert.IsTrue(o.AR >= 0 && o.AR <= 31);
                Assert.IsTrue(o.MUL >= 0 && o.MUL <= 15);
            }
        }
    }

    [Test]
    public void TLは大きいほど小さい音になる()
    {
        Assert.AreEqual(1.0, SoundVoices.LevelOf(0), 1e-9, "TL0 は目一杯");
        Assert.Less(SoundVoices.LevelOf(40), SoundVoices.LevelOf(28));
        Assert.Greater(SoundVoices.LevelOf(127), 0.0);
    }

    /// <summary>音色を変えても長さは変わらない（原典どおりの拍を崩さない）。</summary>
    [Test]
    public void 音色を使っても長さは変わらない()
    {
        var ch = SoundBank.Instance.Get(1);
        int tempo = SoundSynth.TempoOf(ch);
        int len = SoundSynth.RenderChannel(ch[0], tempo).Length;
        foreach (var c in ch)
            Assert.AreEqual(len, SoundSynth.RenderChannel(c, tempo).Length);
    }

    [Test]
    public void 波は範囲に収まっている()
    {
        var w = SoundSynth.Render(13);
        foreach (var v in w) Assert.LessOrEqual(Mathf.Abs(v), 1f);
    }
}
