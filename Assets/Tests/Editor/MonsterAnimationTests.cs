using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 敵と人の2コマの動き（原典 0x92a4）。
/// </summary>
public class MonsterAnimationTests
{
    [Test]
    public void 数は原典どおり()
    {
        Assert.AreEqual(121, MonsterAnimation.Cells, "11×11、原典は 0x79 で戻す");
        Assert.AreEqual(5, MonsterAnimation.CellsPerTick, "1回で5マス（[bp-6]=5）");
        Assert.AreEqual(16, MonsterAnimation.TicksPerStep, "1歩で16コマ（0xa276 は1コマごとに1回呼ぶ）");
        Assert.AreEqual(8, MonsterAnimation.FastTicksPerStep, "高速モードなら8コマ");
    }

    [Test]
    public void マスの番号と座標が行優先で対応する()
    {
        Assert.AreEqual(new Vector2Int(-5, 5), MonsterAnimation.CellOf(0));
        Assert.AreEqual(new Vector2Int(5, 5), MonsterAnimation.CellOf(10));
        Assert.AreEqual(new Vector2Int(-5, 4), MonsterAnimation.CellOf(11));
        Assert.AreEqual(new Vector2Int(5, -5), MonsterAnimation.CellOf(120));
        for (int i = 0; i < MonsterAnimation.Cells; i++)
            Assert.AreEqual(i, MonsterAnimation.IndexOf(MonsterAnimation.CellOf(i)), $"{i}");
    }

    [Test]
    public void ひと押しで五マス進む()
    {
        var a = new MonsterAnimation();
        var t = a.Tick();
        Assert.AreEqual(5, t.Count);
        Assert.AreEqual(5, a.Cursor);
    }

    [Test]
    public void 百二十一マスなめ終わるとコマが入れ替わる()
    {
        var a = new MonsterAnimation();
        Assert.IsTrue(a.FirstFrame);
        // 121マスを越えるまで押す
        int ticks = 0;
        while (a.FirstFrame && ticks < 100) { a.Tick(); ticks++; }
        Assert.AreEqual(25, ticks, "5マスずつなので25回目で121を越える");
        Assert.IsFalse(a.FirstFrame);
    }

    [Test]
    public void 二歩に一度くらい入れ替わる()
    {
        var a = new MonsterAnimation();
        int steps = 0;
        while (a.FirstFrame && steps < 20) { a.Step(); steps++; }
        Assert.AreEqual(2, steps, "1歩で16×5＝80マスなので2歩目で121を越える");
    }

    [Test]
    public void 高速モードでは半分しか進まない()
    {
        var a = new MonsterAnimation();
        a.Step(true);
        Assert.AreEqual(MonsterAnimation.FastTicksPerStep * MonsterAnimation.CellsPerTick,
                        a.Cursor, "8回ぶんだけ進む");
    }

    [Test]
    public void 切り替えは一斉ではなく舐めるように進む()
    {
        var a = new MonsterAnimation();
        // 121を越えて2コマ目に入った直後は、まだ1コマ目のままのマスが残っている
        for (int i = 0; i < 25; i++) a.Tick();
        Assert.IsFalse(a.FirstFrame);
        var first = MonsterAnimation.CellOf(0);
        var last = MonsterAnimation.CellOf(120);
        Assert.IsFalse(a.FrameAt(first), "なめ直したところは2コマ目");
        Assert.IsTrue(a.FrameAt(last), "まだ来ていないところは1コマ目のまま");
    }

    [Test]
    public void 二コマ目の絵が書き出してある()
    {
        foreach (var n in new[] { "062_Enemy_SlimeG_b", "020_Merchant_b" })
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/Sprites/Tiles/{n}.png");
            Assert.IsNotNull(s, $"{n} が無い");
        }
    }
}

/// <summary>
/// 2コマの絵がシーンに繋がっているか、絵そのものが読めるか。
/// </summary>
public class MonsterAnimatorTests
{
    [Test]
    public void シーンに二コマの役がいて絵が揃っている()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Tower.unity");
        var anim = Object.FindObjectOfType<MonsterAnimator>();
        Assert.IsNotNull(anim, "MonsterAnimator が無い");
        var so = new UnityEditor.SerializedObject(anim);
        foreach (var n in new[] { "controller", "loader" })
            Assert.IsNotNull(so.FindProperty(n).objectReferenceValue, $"{n} が未設定");

        var keys = so.FindProperty("keys");
        var first = so.FindProperty("firstFrames");
        var second = so.FindProperty("secondFrames");
        Assert.AreEqual(36, keys.arraySize, "2コマで動くもの");
        Assert.AreEqual(keys.arraySize, first.arraySize);
        Assert.AreEqual(keys.arraySize, second.arraySize);
        for (int i = 0; i < keys.arraySize; i++)
        {
            Assert.IsNotNull(first.GetArrayElementAtIndex(i).objectReferenceValue,
                             $"{keys.GetArrayElementAtIndex(i).stringValue} の1コマ目");
            Assert.IsNotNull(second.GetArrayElementAtIndex(i).objectReferenceValue,
                             $"{keys.GetArrayElementAtIndex(i).stringValue} の2コマ目");
        }
    }

    [Test]
    public void 三マス四方の絵も読める()
    {
        // 2の冪でない大きさは nPOTScale を 0 にしておかないと取り込みに失敗する
        foreach (var n in new[] { "900_Boss_Dragon", "901_Boss_GiantOctopus", "902_Shop_Altar" })
        {
            var path = $"Assets/Sprites/Tiles/{n}.png";
            Assert.IsNotNull(UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path),
                             $"{n} が Sprite として読めない");
            var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
            Assert.AreEqual(UnityEditor.TextureImporterNPOTScale.None, imp.npotScale, n);
        }
    }

    [Test]
    public void 盤面に置くボスの絵が繋がっている()
    {
        var reg = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        foreach (var key in new[] { "Boss_Dragon", "Boss_GiantOctopus" })
        {
            var prefab = reg.Get(key);
            Assert.IsNotNull(prefab, $"{key} のプレハブが無い");
            var sr = prefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(sr, $"{key} に絵の役が無い");
            Assert.IsNotNull(sr.sprite, $"{key} の絵が外れている");
        }
    }
}

/// <summary>
/// 0階（原典 INITIAL.DAT の1階の1つ手前）。行けないが正典のデータ。
/// </summary>
public class Floor0Tests
{
    [Test]
    public void 零階は床だけの部屋で真ん中に幸運の金貨がある()
    {
        var d = FloorData.Load(0);
        Assert.IsNotNull(d, "0階のデータが無い");
        Assert.AreEqual(1, d.Objects.Count, "置いてあるのは1つだけ");
        Assert.AreEqual("Coin_Lucky", d.Objects[0].Prefab);
        Assert.AreEqual(new Vector2Int(0, 0), d.Objects[0].Pos, "盤面の中央");
    }

    [Test]
    public void 零階には上り階段だけがある()
    {
        var d = FloorData.Load(0);
        int up = 0, down = 0, other = 0;
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var t = d.At(x, y);
                if (t == FloorData.Terrain.StairsUp) up++;
                else if (t == FloorData.Terrain.StairsDown) down++;
                else if (t != FloorData.Terrain.Floor) other++;
            }
        Assert.AreEqual(1, up, "上り階段は1つ");
        Assert.AreEqual(0, down, "下り階段は無い");
        Assert.AreEqual(0, other, "ほかの地形は無い");
        Assert.AreEqual(FloorData.Terrain.StairsUp, d.At(-5, 5), "上り階段は左上");
    }

    [Test]
    public void 一階に下り階段が無いので零階へは行けない()
    {
        var d = FloorData.Load(1);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                Assert.AreNotEqual(FloorData.Terrain.StairsDown, d.At(x, y),
                                   $"1階に下り階段 ({x},{y})");
        // 始まりは1階（INITIAL.DAT の先頭 …,1,1,5,10 の「現在階=1」）
        Assert.AreEqual(1, GameStart.Prologue.Floor);
    }
}

/// <summary>
/// 1マスぶんの滑らかな移動（原典 0xa276）。
/// </summary>
public class SlideTests
{
    [Test]
    public void コマ数と長さが原典の数から出ている()
    {
        Assert.AreEqual(16, MonsterAnimation.TicksPerStep);
        Assert.AreEqual(8, MonsterAnimation.FastTicksPerStep);
        Assert.AreEqual(16 * MonsterAnimation.FrameSeconds,
                        MonsterAnimation.StepSeconds(false), 1e-6f);
        Assert.AreEqual(8 * MonsterAnimation.FrameSeconds,
                        MonsterAnimation.StepSeconds(true), 1e-6f);
        Assert.Less(MonsterAnimation.StepSeconds(true), MonsterAnimation.StepSeconds(false),
                    "高速モードのほうが短い");
    }

    [Test]
    public void 滑らせるのは隣のマスへ一歩動いたときだけ()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        // 階が変わったとき・2マス以上飛んだときは通さない
        StringAssert.Contains("Game.Player.Floor == floorBefore", src);
        StringAssert.Contains("== 1", src);
        StringAssert.Contains("sliding", src);
    }

    [Test]
    public void 演出の間は入力を受けない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("if (sliding || drawing) return;", src);
    }
}

/// <summary>
/// 階に入ったときの描き出し（原典 0xafa6 の渦巻き）。
/// </summary>
public class FloorDrawOrderTests
{
    [Test]
    public void 百二十一マスを一度ずつ渦巻きの順に描く()
    {
        var cells = FloorDrawOrder.Instance.Cells;
        Assert.AreEqual(121, cells.Count);
        Assert.AreEqual(121, new System.Collections.Generic.HashSet<Vector2Int>(cells).Count,
                        "同じマスが2度出ている");
        Assert.AreEqual(new Vector2Int(-5, 5), cells[0], "左上から始まる");
        Assert.AreEqual(new Vector2Int(0, 0), cells[120], "中央で終わる");
        for (int i = 0; i + 1 < cells.Count; i++)
        {
            var d = cells[i + 1] - cells[i];
            Assert.AreEqual(1, Mathf.Abs(d.x) + Mathf.Abs(d.y), $"{i}→{i + 1} が隣ではない");
        }
    }

    [Test]
    public void 待ちの間隔は原典どおり()
    {
        Assert.AreEqual(3, FloorDrawOrder.CellsPerWait);
        Assert.AreEqual(10, FloorDrawOrder.FastCellsPerWait);
        // 121マスなら ふつう40回 ≒ 0.67秒、高速12回 ≒ 0.2秒
        Assert.AreEqual(40 * MonsterAnimation.FrameSeconds,
                        FloorDrawOrder.Seconds(121, false), 1e-5f);
        Assert.AreEqual(12 * MonsterAnimation.FrameSeconds,
                        FloorDrawOrder.Seconds(121, true), 1e-5f);
    }

    [Test]
    public void 描き出しの間は入力を受けない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("IEnumerator DrawFloor()", src);
        StringAssert.Contains("if (sliding || drawing) return;", src);
        StringAssert.Contains("FloorDrawOrder.Instance.Cells", src);
    }
}
