using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 題名と終幕の画面が、物語の流れのどこで出るか。
/// 原典も題名（0x6db2）と最後の画面（0xd45c）は「絵と文字を出してキー待ち」だけ。
/// </summary>
public class ScreenFlowTests
{
    static TowerGame Game(int floor, Vector2Int at)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            Floor = floor, Position = at,
        });
    }

    /// <summary>序章のゼノの場面で、セリフ111のあとに題名のページが来る。</summary>
    [Test]
    public void 序章の途中に題名のページが入る()
    {
        var g = Game(3, new Vector2Int(0, 0));
        Assert.IsTrue(g.PlayScene("ゼノとの初対面"));

        var kinds = new System.Collections.Generic.List<TowerGame.PageKind>();
        for (int i = 0; i < 40 && g.CurrentKind.HasValue; i++)
        {
            kinds.Add(g.CurrentKind.Value);
            g.NextPage();
        }
        CollectionAssert.Contains(kinds, TowerGame.PageKind.Title, "題名のページがある");
    }

    /// <summary>終幕の最後は1枚絵のページで終わる。</summary>
    [Test]
    public void 終幕の最後は一枚絵のページ()
    {
        var g = Game(50, new Vector2Int(0, -1));
        Assert.IsTrue(g.PlayScene("終幕"));

        TowerGame.PageKind last = TowerGame.PageKind.Dialogue;
        for (int i = 0; i < 60 && g.CurrentKind.HasValue; i++)
        {
            last = g.CurrentKind.Value;
            g.NextPage();
        }
        Assert.AreEqual(TowerGame.PageKind.FinalScreen, last, "最後は1枚絵");
    }

    [Test]
    public void 一枚絵のページには終幕の文字が載る()
    {
        var g = Game(50, new Vector2Int(0, -1));
        g.PlayScene("終幕");
        string text = null;
        for (int i = 0; i < 60 && g.CurrentKind.HasValue; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.FinalScreen) { text = g.CurrentPage; break; }
            g.NextPage();
        }
        Assert.IsNotNull(text);
        StringAssert.Contains(Ending.TitleLine, text);
        StringAssert.Contains(Ending.AnyKeyLine, text);
    }

    /// <summary>大魔導師の口上はセリフ155の1枚。</summary>
    [Test]
    public void 大魔導師の口上はセリフ155()
    {
        var g = Game(49, new Vector2Int(0, 0));
        Assert.IsTrue(g.PlayScene("大魔導師の口上"));
        Assert.AreEqual(TowerGame.PageKind.Dialogue, g.CurrentKind);
        var line = GameText.Instance.Dialogue(Ending.ArchmageRecord);
        Assert.IsTrue(line.HasValue);
        StringAssert.StartsWith(g.CurrentPage.Substring(0, 6), line.Value.Body.Substring(0, 6));
    }

    /// <summary>終幕の頭で塔を駆け上がり、50階の (0,-1) に立つ（原典 0x2386〜0x23cd）。</summary>
    [Test]
    public void 終幕は塔を駆け上がってから始まる()
    {
        var g = Game(24, new Vector2Int(0, -1));
        Assert.AreEqual(24, g.Player.Floor);
        g.PlayScene("終幕");
        Assert.AreEqual(Ending.LastFloor, g.Player.Floor, "50階まで上がる");
        Assert.AreEqual(Ending.LastPosition, g.Player.Position, "立ち位置は (0,-1)");
        Assert.AreEqual(50, g.Player.HighestFloor);
    }

    /// <summary>
    /// 駆け上がりは**階の数字だけ**が 25→50 と上がる（原典 0x2386〜0x23a6）。
    /// 地図は変わらないので、出る数字の並びと時間だけが演出の中身になる。
    /// </summary>
    [Test]
    public void 駆け上がりは階の数字が二十五から五十まで上がる()
    {
        var floors = new System.Collections.Generic.List<int>(Ending.ClimbFloors());
        Assert.AreEqual(26, floors.Count, "25階から50階まで");
        Assert.AreEqual(25, floors[0]);
        Assert.AreEqual(50, floors[floors.Count - 1]);
        Assert.AreEqual(5, Ending.ClimbWait, "1階ぶん5回ぶんの待ち（原典 0x2391）");
        Assert.AreEqual(26 * 5 / 60f, Ending.ClimbSeconds, 1e-5f);

        var g = Game(24, new Vector2Int(0, -1));
        Assert.IsNull(g.LastClimb);
        g.PlayScene("終幕");
        CollectionAssert.AreEqual(floors, g.LastClimb, "本線が並びを残す");
    }

    /// <summary>組み立てたシーンで、2つの画面がそれぞれのページに結ばれている。</summary>
    [Test]
    public void 場面の画面が本線に結ばれている()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            int wired = 0;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var v in go.GetComponentsInChildren<FullScreenView>(true))
                {
                    var so = new UnityEditor.SerializedObject(v);
                    Assert.IsNotNull(so.FindProperty("controller").objectReferenceValue,
                                     v.name + " に本線が繋がっていない");
                    wired++;
                }
            Assert.AreEqual(6, wired,
                "題名・序章の絵・塔の絵・終わりの言葉・ゲームオーバー・最後の画面の6つ");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
