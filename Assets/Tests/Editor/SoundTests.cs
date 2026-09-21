using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 原典の音（Resources/Sounds.txt）の検査。
/// 期待値は docs/decode_sound.py が MTE.EXE の音源ドライバから取り出した譜面そのもの。
/// </summary>
public class SoundTests
{
    [Test]
    public void 音は13種類で曲は2つ()
    {
        var b = SoundBank.Instance;
        Assert.AreEqual(10, b.Count, "原典がゲームから鳴らすのは1,2,6〜13の10種類");
        Assert.IsTrue(b.IsMusic(1), "音1は曲");
        Assert.IsTrue(b.IsMusic(2), "音2は曲");
        for (int n = 6; n <= 13; n++)
        {
            Assert.IsNotNull(b.Get(n), $"音{n}がある");
            Assert.IsFalse(b.IsMusic(n), $"音{n}は効果音（1チャンネル）");
        }
    }

    [Test]
    public void 曲は4チャンネルで最後に繰り返す()
    {
        foreach (int n in new[] { 1, 2 })
        {
            var ch = SoundBank.Instance.Get(n);
            Assert.AreEqual(4, ch.Count, $"音{n}は4チャンネル");
            foreach (var c in ch)
            {
                var last = c.Steps[c.Steps.Count - 1];
                Assert.AreEqual("loop", last.Kind, $"音{n}のチャンネル{c.Number}は繰り返しで終わる");
            }
        }
    }

    [Test]
    public void 効果音は終わりで閉じている()
    {
        for (int n = 6; n <= 13; n++)
        {
            var c = SoundBank.Instance.Get(n)[0];
            Assert.AreEqual("end", c.Steps[c.Steps.Count - 1].Kind, $"音{n}は終わりで閉じる");
        }
    }

    /// <summary>いちばん多く鳴る音6は「ド・レ・ミ」の短い上昇。</summary>
    [Test]
    public void 音6はドレミの上昇()
    {
        var c = SoundBank.Instance.Get(6)[0];
        var notes = new System.Collections.Generic.List<int>();
        var lens = new System.Collections.Generic.List<int>();
        foreach (var s in c.Steps)
            if (s.Kind == "note") { notes.Add(s.Value); lens.Add(s.Length); }
        Assert.AreEqual(new[] { 0, 2, 4 }, notes.ToArray(), "ド・レ・ミ");
        Assert.AreEqual(new[] { 2, 2, 4 }, lens.ToArray(), "原典の音長");
    }

    /// <summary>音13は同じ2音を20回くり返しながら音量が20から1へ落ちていく（遠ざかる音）。</summary>
    [Test]
    public void 音13は音量が落ちていく()
    {
        var c = SoundBank.Instance.Get(13)[0];
        int pairs = 0, lastVolume = int.MaxValue, drops = 0;
        foreach (var s in c.Steps)
        {
            if (s.Kind == "rest") pairs++;
            if (s.Kind != "volume") continue;
            if (s.Value < lastVolume) drops++;
            lastVolume = s.Value;
        }
        Assert.AreEqual(20, pairs, "20回くり返す");
        Assert.AreEqual(1, lastVolume, "最後は音量1まで落ちる");
        Assert.AreEqual(10, drops, "最初の音量10を含め、10から1へ1ずつ下がっていく");
    }

    /// <summary>音12は音を鳴らさずテンポだけ戻す。</summary>
    [Test]
    public void 音12はテンポだけ()
    {
        var c = SoundBank.Instance.Get(12)[0];
        Assert.AreEqual(2, c.Steps.Count);
        Assert.AreEqual("tempo", c.Steps[0].Kind);
        Assert.AreEqual(130, c.Steps[0].Value);
        Assert.AreEqual("end", c.Steps[1].Kind);
    }

    /// <summary>曲の出だしは テンポ→音量→音色→オクターブ の順（原典どおり）。</summary>
    [Test]
    public void 曲の出だしは原典と同じ順()
    {
        var c = SoundBank.Instance.Get(1)[0];
        Assert.AreEqual("tempo", c.Steps[0].Kind);
        Assert.AreEqual(130, c.Steps[0].Value, "音1のテンポ");
        Assert.AreEqual("volume", c.Steps[1].Kind);
        Assert.AreEqual("voice", c.Steps[2].Kind);
        Assert.AreEqual("octave", c.Steps[3].Kind);
        Assert.AreEqual(4, c.Steps[3].Value);

        var c2 = SoundBank.Instance.Get(2)[0];
        Assert.AreEqual(108, c2.Steps[0].Value, "音2のテンポ");
    }
}
