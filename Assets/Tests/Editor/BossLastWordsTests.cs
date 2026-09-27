using NUnit.Framework;
using UnityEngine;

/// <summary>
/// ブロックのボスを倒したときの**最期のセリフ**（作者の報告「倒された時のボスのセリフ無くない？」）。
///
/// 原典 MTE.EXE はこう書いている:
/// <code>
///   0x4e5b（10階）: フラグ0x5d = 1; 50コマ待つ; セリフ106; そのあと褒美の表を1マスずつ当てる
///   0x531d（20階）: フラグ0x68 = 1; 50コマ待つ; セリフ122; そのあと褒美の表を1マスずつ当てる
/// </code>
/// 褒美より**先に**セリフが出るのが肝で、こちらは書き換えをセリフのうしろに預けている
/// （<see cref="TowerGame.UnlockStep"/>）。
///
/// 49階の封印のセリフ156 は解錠ではなく <see cref="MagicSeal"/> が出す（原典も別の場所 0x5d67）。
/// </summary>
public class BossLastWordsTests
{
    static TowerGame At(int floor, Vector2Int pos)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999,
            Floor = floor, HighestFloor = floor, Position = pos,
        });
    }

    /// <summary>その階の解錠の守護マスを全部空けて、前提の旗も立てる。</summary>
    static void BeatBoss(TowerGame g, int floor)
    {
        var f = g.FloorAt(floor);
        foreach (var u in SealedDoors.Instance.ForFloor(floor))
        {
            foreach (var guard in u.Guards) f.Clear(guard);
            if (u.RequiresFlag >= 0) g.Player.EventFlags.Add(u.RequiresFlag);
        }
    }

    static string ReadAll(TowerGame g)
    {
        var all = new System.Text.StringBuilder();
        int guard = 0;
        while (g.Current.HasValue && guard++ < 200)
        {
            all.Append(g.CurrentPage);
            g.NextPage();
        }
        return all.ToString();
    }

    [TestCase(10, 106, "よくぞこの俺")]
    [TestCase(20, 122, "よもや、この私")]
    public void ボスを倒すと最期のセリフが出る(int floor, int record, string phrase)
    {
        var g = At(floor, new Vector2Int(5, -5));
        BeatBoss(g, floor);
        g.EnterFloor(floor, new Vector2Int(5, -5));

        var text = ReadAll(g);
        StringAssert.Contains(phrase, text, $"F{floor} でセリフ{record}が出る");
    }

    [Test]
    public void 褒美はセリフを読み終えてから並ぶ()
    {
        // 原典は「50コマ待つ → セリフ → 表を当てる」の順。
        // 先に褒美が生えると、宝と階段がセリフより前に見えてしまう。
        var stair = new Vector2Int(0, -5);        // 10階の上り階段
        var fairy = new Vector2Int(0, 3);         // 10階の妖精

        var g = At(10, new Vector2Int(5, -5));
        BeatBoss(g, 10);
        g.EnterFloor(10, new Vector2Int(5, -5));

        Assert.AreEqual(".", g.Floor.At(stair), "読む前は階段がまだ出ていない");
        Assert.AreEqual(".", g.Floor.At(fairy), "読む前は妖精もまだ出ていない");
        Assert.IsFalse(g.Player.EventFlags.Contains(0x5d),
                       "当てる前に印だけ立つと、褒美が二度と出なくなる");

        ReadAll(g);

        Assert.AreNotEqual(".", g.Floor.At(stair), "読み終えると階段が出る");
        Assert.AreNotEqual(".", g.Floor.At(fairy), "読み終えると妖精が出る");
        Assert.IsTrue(g.Player.EventFlags.Contains(0x5d), "そこで印が立つ");
    }

    [Test]
    public void セリフの前にひと呼吸おく()
    {
        // 倒した直後に窓が開くと斬り合いの絵と重なる。原典は 0x32＝50コマ待つ。
        foreach (var u in SealedDoors.Instance.ForFloor(10))
            if (u.FlagId == 0x5d)
            {
                Assert.AreEqual(106, u.Record);
                Assert.AreEqual(50, u.WaitFrames);
            }
        foreach (var u in SealedDoors.Instance.ForFloor(20))
            if (u.FlagId == 0x68)
            {
                Assert.AreEqual(122, u.Record);
                Assert.AreEqual(50, u.WaitFrames);
            }
    }

    /// <summary>セリフが付いているのはブロックのボスの2件だけ。</summary>
    [Test]
    public void セリフ付きの解錠は十階と二十階だけ()
    {
        int n = 0;
        for (int floor = 1; floor <= 50; floor++)
            foreach (var u in SealedDoors.Instance.ForFloor(floor))
                if (u.Record >= 0)
                {
                    n++;
                    Assert.IsTrue(floor == 10 || floor == 20, $"F{floor} にセリフが付いている");
                }
        Assert.AreEqual(2, n);
    }
}
