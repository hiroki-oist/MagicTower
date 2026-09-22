using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 人や物に触れたときに**上に乗らない**ことを確かめる。
/// 原典の物体ディスパッチャ（MTE.EXE 0x23ee）は種別7の物体では
/// それぞれの処理を呼ぶだけで、プレーヤーの位置を動かさない。
///
/// 通しで動かしてみて見つかった取りこぼしの回帰試験。
/// 直す前は仙人や祭壇の上に乗ってしまい、マスが残るので同じ所を回り続けていた。
/// </summary>
public class NpcTouchTests
{
    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999, Gold = 9990,
            Floor = floor, Position = at
        };
        return new TowerGame(p);
    }

    [Test]
    public void 人や物の見分けが揃っている()
    {
        Assert.IsTrue(TowerNpc.IsPerson("Master"));
        Assert.IsTrue(TowerNpc.IsPerson("Merchant"));
        Assert.IsTrue(TowerNpc.IsPerson("Mikata"));
        Assert.IsTrue(TowerNpc.IsPerson("Princess"));
        Assert.IsTrue(TowerNpc.IsPerson("Shop_Altar"));
        Assert.IsTrue(TowerNpc.IsPerson("Vendor"));
        Assert.IsFalse(TowerNpc.IsPerson("Key_Yellow"), "アイテムは拾って乗る");
        Assert.IsFalse(TowerNpc.IsPerson("Enemy_SlimeG"), "敵は戦う");
    }

    [Test]
    public void 仙人に触れても上に乗らない()
    {
        // 2階の仙人は (+5,-1)
        var g = Game(2, new Vector2Int(4, -1));
        var at = g.Player.Position;
        var r = g.Step(Vector2Int.right);

        Assert.AreEqual(TowerPlayer.Outcome.Talk, r);
        Assert.AreEqual(at, g.Player.Position, "進まない");
        Assert.AreEqual("Master", g.Floor.At(new Vector2Int(5, -1)), "マスは残る");
        Assert.IsTrue(g.IsShowingPage, "話が出る");
    }

    [Test]
    public void 祭壇に触れても上に乗らない()
    {
        // 4階の祭壇（3マスで1つ）
        var g = Game(4, new Vector2Int(0, 0));
        bool found = false;
        for (int y = FloorData.MinY; y <= FloorData.MaxY && !found; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX && !found; x++)
            {
                var c = new Vector2Int(x, y);
                if (!TowerNpc.IsAltar(g.Floor.At(c))) continue;
                found = true;
                var from = c + Vector2Int.down;
                if (!g.Floor.IsCleared(from)) continue;
                g.Player.Position = from;
                var r = g.Step(Vector2Int.up);
                Assert.AreEqual(TowerPlayer.Outcome.Talk, r);
                Assert.AreEqual(from, g.Player.Position, "進まない");
            }
        Assert.IsTrue(found, "4階に祭壇がある");
    }

    [Test]
    public void 何度触れても同じ所を回り続けない()
    {
        // 直す前は「乗る→マスが残る→また乗る」で無限に回っていた
        var g = Game(2, new Vector2Int(4, -1));
        for (int i = 0; i < 5; i++)
        {
            g.Step(Vector2Int.right);
            while (g.IsShowingPage) g.NextPage();
        }
        Assert.AreEqual(new Vector2Int(4, -1), g.Player.Position);
        Assert.AreEqual("Master", g.Floor.At(new Vector2Int(5, -1)));
    }

    [Test]
    public void アイテムを拾うとそのマスへ進む()
    {
        var g = Game(1, new Vector2Int(0, -5));
        bool found = false;
        for (int y = FloorData.MinY; y <= FloorData.MaxY && !found; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX && !found; x++)
            {
                var c = new Vector2Int(x, y);
                var w = g.Floor.At(c);
                if (!w.StartsWith("Potion_") && !w.StartsWith("Gem_")) continue;
                var from = c + Vector2Int.down;
                if (from.y < FloorData.MinY || !g.Floor.IsCleared(from)) continue;
                found = true;
                g.Player.Position = from;
                var r = g.Step(Vector2Int.up);
                Assert.AreEqual(TowerPlayer.Outcome.TookItem, r);
                // 作者の指定で**拾ったらそのマスへ進む**（原典 0x00a6fb は進まない）
                Assert.AreEqual(AuthorStyle.StepOntoItem ? c : from, g.Player.Position,
                                "消す→進む→手に入れる");
                Assert.IsTrue(g.Floor.IsCleared(c), "マスは床になる");
            }
        Assert.IsTrue(found, "1階に薬か宝石がある");
    }
}
