using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 起動時の題名画面（作者の指定。原典にこの画面は無い）。
/// 触ると**必ず最初から**始まるので、続きから始める入口は持たない。
/// </summary>
public class TitleViewTests
{
    [Test]
    public void 案ごとに出す絵が変わる()
    {
        // 看板の案＝石積みの地に「魔法の塔」の看板
        Assert.IsTrue(TitleArt.UsesBrick(TitleArt.Style.Signboard));
        Assert.IsTrue(TitleArt.UsesLogo(TitleArt.Style.Signboard));
        Assert.IsFalse(TitleArt.UsesPicture(TitleArt.Style.Signboard));

        // 題名絵の案＝MTMOJI をそのまま1枚絵として敷く
        Assert.IsTrue(TitleArt.UsesPicture(TitleArt.Style.Picture));
        Assert.IsFalse(TitleArt.UsesLogo(TitleArt.Style.Picture));

        // 塔の案＝終幕の塔の上に看板
        Assert.IsTrue(TitleArt.UsesTower(TitleArt.Style.Tower));
        Assert.IsTrue(TitleArt.UsesLogo(TitleArt.Style.Tower));

        // 原典の案＝真っ黒に2行だけ
        Assert.IsTrue(TitleArt.UsesLines(TitleArt.Style.Original));
        Assert.IsFalse(TitleArt.UsesLogo(TitleArt.Style.Original));
        Assert.IsFalse(TitleArt.UsesPicture(TitleArt.Style.Original));
    }

    [Test]
    public void 点滅する言葉は作者の指定どおり()
    {
        Assert.AreEqual("Touch to start game", TitleArt.StartLine);
        Assert.Greater(TitleArt.BlinkSeconds, 0f);
    }

    [Test]
    public void 使う絵は全部そろっている()
    {
        foreach (var path in new[] { TitleArt.LogoPath, TitleArt.PicturePath,
                                     TitleArt.TallPath, TitleArt.TowerPath,
                                     TitleArt.GroundPath, TitleArt.BrickPath })
            Assert.IsTrue(System.IO.File.Exists(path), path);
    }

    /// <summary>
    /// 作者が選んだのは案2（題名絵）。画面が縦長なので、横長の 640x400 ではなく
    /// **縦に伸ばしたほう**を使う。どんな縦横比の端末でも下まで埋まる高さが要る。
    /// </summary>
    [Test]
    public void 縦長の題名絵はどの端末でも画面を埋める()
    {
        Assert.AreEqual(TitleArt.Style.Picture, TitleArt.Choice);
        float aspect = TitleArt.TallSize.x / (float)TitleArt.TallSize.y;
        // iPhone 17 の縦画面（1206x2622 ＝ 0.46）より縦長であること
        Assert.Less(aspect, 1206f / 2622f, "いちばん縦長の端末より縦長");
        // 点滅は部屋の中（題名より下、絵の下端より上）
        Assert.Greater(TitleArt.TallStartY, 250);
        Assert.Less(TitleArt.TallStartY, TitleArt.TallSize.y - 120);
        // いちばん縦横比の低い端末（9:16）でも画面の中に入る
        float visible = TitleArt.TallSize.x * 16f / 9f;
        Assert.Less(TitleArt.TallStartY, visible, "9:16 の画面でも見える");
    }

    [Test]
    public void どの案でも点滅は絵の中に収まる()
    {
        foreach (TitleArt.Style s in System.Enum.GetValues(typeof(TitleArt.Style)))
        {
            var at = TitleArt.StartAt(s);
            Assert.Greater(at.y, 0);
            Assert.Less(at.y, ScreenLayout.ScreenHeight, $"{s}");
            Assert.AreEqual(ScreenLayout.ScreenWidth / 2, at.x, $"{s} は真ん中");
        }
    }

    /// <summary>看板は「魔法の塔」の字の外接矩形（原典 MTMOJI）を含む大きさ。</summary>
    [Test]
    public void 看板の切り出しは字を全部含む()
    {
        Assert.AreEqual(new Vector2Int(300, 104), TitleArt.LogoSize);
        // docs/make_title.py の切り出し（166,84）〜（466,188）に字（173..460 / 91..183）が入る
        var at = TitleArt.LogoAt(TitleArt.Style.Signboard);
        Assert.LessOrEqual(at.x - TitleArt.LogoSize.x / 2, 173);
        Assert.GreaterOrEqual(at.x + TitleArt.LogoSize.x / 2, 460);
    }
}
