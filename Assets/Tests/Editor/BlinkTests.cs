using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 場面でものが現れるときの点滅。原典 PC98版 MTE.EXE の 3階の場面（0x6ae0）。
/// ゼノは50回、魔導師は各15回。点滅のあとの待ちはそれぞれ 0x1e(30) と 0xa(10)。
/// </summary>
public class BlinkTests
{
    [Test]
    public void 点滅の書き方を読める()
    {
        Cutscenes.MapStep m;
        Assert.IsTrue(Cutscenes.TryParseMap("地図 (-1,-1)=ID91 点滅50,30", out m));
        Assert.AreEqual(new Vector2Int(-1, -1), m.Pos);
        Assert.AreEqual("ID91", m.Kind);
        Assert.AreEqual(50, m.Blinks);
        Assert.AreEqual(30, m.Wait);
    }

    [Test]
    public void 点滅が無い書き方も今までどおり読める()
    {
        Cutscenes.MapStep m;
        Assert.IsTrue(Cutscenes.TryParseMap("地図 (+0,-3)=床", out m));
        Assert.AreEqual(new Vector2Int(0, -3), m.Pos);
        Assert.AreEqual("床", m.Kind);
        Assert.AreEqual(0, m.Blinks);
        Assert.AreEqual(0, m.Wait);
    }

    [Test]
    public void 三階の場面は原典の回数で点滅する()
    {
        var steps = Cutscenes.Instance.Steps("ゼノとの初対面");
        int zeno = 0, mages = 0;
        foreach (var s in steps)
        {
            Cutscenes.MapStep m;
            if (!Cutscenes.TryParseMap(s, out m) || m.Blinks == 0) continue;
            if (m.Kind == "ID91")
            {
                zeno++;
                Assert.AreEqual(50, m.Blinks, "ゼノは 0x32(50) 回");
                Assert.AreEqual(30, m.Wait, "そのあと 0x1e(30) 待つ");
            }
            else
            {
                mages++;
                Assert.AreEqual("ID93", m.Kind);
                Assert.AreEqual(15, m.Blinks, "魔導師は 0xf(15) 回");
                Assert.AreEqual(10, m.Wait, "1マスごとに 0xa(10) 待つ");
            }
        }
        Assert.AreEqual(1, zeno);
        Assert.AreEqual(4, mages);
    }

    [Test]
    public void 片付けは点滅させない()
    {
        // 原典 0x6ebd の片付けは床を書いて描くだけで、入れ替えはしない
        foreach (var s in Cutscenes.Instance.Steps("ゼノとの初対面"))
        {
            Cutscenes.MapStep m;
            if (Cutscenes.TryParseMap(s, out m) && m.Kind == "床")
                Assert.AreEqual(0, m.Blinks, s);
        }
    }

    [Test]
    public void 本線は点滅を積むだけで描かない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerGame.cs");
        StringAssert.Contains("ConsumeBlinks", src);
        var view = System.IO.File.ReadAllText("Assets/Scripts/UI/BlinkView.cs");
        // 速さは原典と同じにできないことを明記してある
        StringAssert.Contains("速さは原典と同じにできない", view);
        StringAssert.Contains("MonsterAnimation.FrameSeconds", view);
        // 会話の窓は点滅が終わってから出す（原典も点滅→待ち→0x94ba）
        var dw = System.IO.File.ReadAllText("Assets/Scripts/UI/DialogueWindowView.cs");
        StringAssert.Contains("blink.IsPlaying", dw);
        StringAssert.Contains("while (Waiting()) yield return null;", dw);
    }
}
