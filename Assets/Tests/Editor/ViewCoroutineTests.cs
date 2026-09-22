using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 見た目の層の**コルーチン**を1回は通しておく検査。
///
/// 戦闘の演出・金騎士の突撃・塔の駆け上がりは、どれも待ちを挟みながら進む
/// コルーチンで書いてある。EditMode では自動では回らないので、
/// `MoveNext()` を手で叩いて中身を最後まで走らせ、例外が出ないことと
/// 終わったあとの形（絵が消えているか、など）を見る。
///
/// 待ち時間そのものは <see cref="BattlePresentation"/> 側の検査で見ている。
/// </summary>
public class ViewCoroutineTests
{
    static readonly BindingFlags Any =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>私的なメソッドを呼ぶ（コルーチンの中身を走らせるため）。</summary>
    static object Call(object target, string name, params object[] args)
    {
        var m = target.GetType().GetMethod(name, Any);
        Assert.IsNotNull(m, $"{target.GetType().Name}.{name} が見つからない");
        return m.Invoke(target, args);
    }

    /// <summary>コルーチンを最後まで手で進める。</summary>
    static int Drain(object routine, int limit = 5000)
    {
        var it = routine as IEnumerator;
        Assert.IsNotNull(it, "コルーチンではない");
        int steps = 0;
        while (steps < limit && it.MoveNext()) steps++;
        Assert.Less(steps, limit, "終わらない");
        return steps;
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            Floor = floor, HighestFloor = 50, Position = at,
        });
    }

    static GameObject MakeBattleView(out BattleView view)
    {
        var go = new GameObject("BattleViewTest");
        view = go.AddComponent<BattleView>();
        var so = new UnityEditor.SerializedObject(view);
        so.FindProperty("hitSprite").objectReferenceValue =
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Hit.png");
        so.ApplyModifiedPropertiesWithoutUndo();
        Call(view, "Awake");                      // 絵の入れ物を作る
        return go;
    }

    [Test]
    public void 戦闘の演出を最後まで流せる()
    {
        BattleView view;
        var go = MakeBattleView(out view);
        try
        {
            var steps = BattlePresentation.Steps(new Vector2Int(0, 0), new Vector2Int(0, 1), 3);
            int n = Drain(Call(view, "Play", steps));
            Assert.Greater(n, 0, "待ちの数だけ進む");
            Assert.IsFalse(view.IsPlaying, "終わったら止まっている");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void 閃光を出して消せる()
    {
        BattleView view;
        var go = MakeBattleView(out view);
        try
        {
            Drain(Call(view, "Flash", new Vector2Int(2, -3)));
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>金騎士の突撃。75歩ぶん動かして最後に消える。</summary>
    [Test]
    public void 金騎士の突撃を最後まで流せる()
    {
        BattleView view;
        var go = MakeBattleView(out view);
        try
        {
            var path = KnightCharge.Instance.Trace();
            int n = Drain(Call(view, "Charge", path));
            Assert.AreEqual(path.Count, n, "1歩ごとに待つ");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>塔の駆け上がり。階の数字だけが 25→50 と動く。</summary>
    [Test]
    public void 駆け上がりを最後まで流せる()
    {
        var go = new GameObject("HudViewTest");
        try
        {
            var view = go.AddComponent<TowerHudView>();
            var floors = new List<int>(Ending.ClimbFloors());
            int n = Drain(Call(view, "Climb", floors));
            Assert.AreEqual(floors.Count, n, "1階ごとに待つ");

            var f = typeof(TowerHudView).GetField("climbing", Any);
            Assert.AreEqual(0, (int)f.GetValue(view), "終わったら普段の表示に戻る");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>本線が積んだものを見た目の層が拾うところ（LateUpdate）を1回通す。</summary>
    [Test]
    public void 見た目の層が本線から拾うところを通せる()
    {
        BattleView view;
        var go = MakeBattleView(out view);
        var host = new GameObject("ControllerTest");
        try
        {
            var controller = host.AddComponent<TowerController>();
            var g = Game(1, new Vector2Int(0, 0));
            typeof(TowerController).GetProperty("Game").SetValue(controller, g);

            var so = new UnityEditor.SerializedObject(view);
            so.FindProperty("controller").objectReferenceValue = controller;
            so.ApplyModifiedPropertiesWithoutUndo();

            g.PlayEffect(new Vector2Int(1, 1));
            Call(view, "LateUpdate");                  // 例外が出なければよい
            Assert.AreEqual(0, g.ConsumeEffects().Count, "引き取られて空になる");
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(host);
        }
    }
}

/// <summary>
/// あとから足した3つの演出（1マスの移動・階の描き出し・窓の塗り広げ）も
/// 同じように手で最後まで回しておく。
/// </summary>
public class NewViewCoroutineTests
{
    static readonly BindingFlags Any =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static object Call(object target, string name, params object[] args)
    {
        var m = target.GetType().GetMethod(name, Any);
        Assert.IsNotNull(m, $"{target.GetType().Name}.{name} が見つからない");
        return m.Invoke(target, args);
    }

    static int Drain(object routine, int limit = 5000)
    {
        var it = routine as IEnumerator;
        Assert.IsNotNull(it, "コルーチンではない");
        int steps = 0;
        while (steps < limit && it.MoveNext()) steps++;
        Assert.Less(steps, limit, "終わらない");
        return steps;
    }

    static GameObject MakeController(out TowerController c, bool fast = false)
    {
        var go = new GameObject("ControllerTest");
        c = go.AddComponent<TowerController>();
        var loader = go.AddComponent<FloorLoader>();
        var reg = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        var container = new GameObject("Floor").transform;
        container.SetParent(go.transform, false);
        loader.Configure(reg, container);
        var so = new UnityEditor.SerializedObject(c);
        so.FindProperty("loader").objectReferenceValue = loader;
        so.ApplyModifiedPropertiesWithoutUndo();

        var p = new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            Floor = 1, HighestFloor = 50, Position = new Vector2Int(0, 0),
        };
        p.FastMode = fast;
        typeof(TowerController).GetProperty("Game").SetValue(c, new TowerGame(p));
        return go;
    }

    [Test]
    public void 一マスの移動を最後まで流せる()
    {
        TowerController c;
        var go = MakeController(out c);
        try
        {
            // **経過時間で進める**ようにしたので、止まる回数は画面の速さで決まる。
            // ここでは「最後まで流れて、行き先に着く」ことだけを見る。
            int n = Drain(Call(c, "Slide", new Vector2Int(0, 0), new Vector2Int(0, 1)));
            // 止まる回数は 経過時間 ÷ 1コマ ぶん。60コマ/秒なら 0.067秒 ÷ 0.017 ≒ 4回。
            Assert.Greater(n, 0, "少なくとも1回は止まる");
            Assert.Less(n, MonsterAnimation.TicksPerStep,
                        "コマ送りで待っていた頃（16回）より速い");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void 高速モードでは移動が半分の長さで終わる()
    {
        TowerController c;
        var go = MakeController(out c, fast: true);
        try
        {
            // 高速モードは**半分の長さ**で終わる（コマ数8＝原典どおり）
            Assert.AreEqual(MonsterAnimation.StepSeconds(false) / 2f,
                            MonsterAnimation.StepSeconds(true), 1e-5f);
            int n = Drain(Call(c, "Slide", new Vector2Int(0, 0), new Vector2Int(1, 0)));
            Assert.Greater(n, 0, "最後まで流れる");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void 階の描き出しを最後まで流せる()
    {
        TowerController c;
        var go = MakeController(out c);
        try
        {
            c.RebuildView();                       // 中で DrawFloor を始める（EditMode では走らない）
            int n = Drain(Call(c, "DrawFloor"));
            Assert.AreEqual(121 / FloorDrawOrder.CellsPerWait, n, "3マスごとに1回止まる");
            // 終わったら全部の絵が出ている
            foreach (Transform child in c.GetComponentInChildren<FloorLoader>().Container)
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) Assert.IsTrue(sr.enabled, $"{child.name} が消えたまま");
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void 窓の塗り広げを最後まで流せる()
    {
        var go = new GameObject("WindowTest");
        try
        {
            var view = go.AddComponent<DialogueWindowView>();
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(go.transform, false);
            var so = new UnityEditor.SerializedObject(view);
            so.FindProperty("panel").objectReferenceValue = panelGo.GetComponent<RectTransform>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var page = "あいうえお";
            int lines = DialogueBox.WindowLines(page);
            int n = Drain(Call(view, "Open", page));
            Assert.AreEqual(0x14 * lines / DialogueWindow.RowStep + 1, n, "きざみの数だけ止まる");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
