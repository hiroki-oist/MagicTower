using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 会話の窓とメニューの窓が「出る／出ない」を正しく決めているかを、
/// 実際に `LateUpdate` を叩いて確かめる。
///
/// 出す中身（文面・寸法）は別の検査で見ているので、ここは**窓の出し入れ**が主。
/// 題名と終幕の1枚絵のときに会話の窓が邪魔をしないことも、ここで押さえる。
/// </summary>
public class WindowViewTests
{
    static readonly BindingFlags Any =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static void Call(object target, string name)
    {
        var m = target.GetType().GetMethod(name, Any);
        Assert.IsNotNull(m, name);
        m.Invoke(target, null);
    }

    /// <summary>窓ひと組（板・背景・文字）を作って繋ぐ。</summary>
    static GameObject MakeWindow<T>(TowerController controller, out T view) where T : MonoBehaviour
    {
        var root = new GameObject("WindowTest", typeof(RectTransform));
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var text = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(panel.transform, false);

        view = root.AddComponent<T>();
        var so = new UnityEditor.SerializedObject(view);
        so.FindProperty("controller").objectReferenceValue = controller;
        so.FindProperty("panel").objectReferenceValue = panel.GetComponent<RectTransform>();
        so.FindProperty("background").objectReferenceValue = panel.GetComponent<Image>();
        so.FindProperty("body").objectReferenceValue = text.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    static TowerController MakeController(out GameObject host, TowerGame game)
    {
        host = new GameObject("ControllerTest");
        var c = host.AddComponent<TowerController>();
        typeof(TowerController).GetProperty("Game").SetValue(c, game);
        return c;
    }

    static TowerGame Game()
    {
        return new TowerGame(new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = 1, Position = new Vector2Int(0, 0),
        });
    }

    [Test]
    public void 会話が無いときは窓が出ない()
    {
        GameObject host;
        var g = Game();
        var c = MakeController(out host, g);
        DialogueWindowView view;
        var root = MakeWindow(c, out view);
        try
        {
            Call(view, "LateUpdate");
            Assert.IsNull(view.Showing, "何も出ていない");
            Assert.IsFalse(root.transform.GetChild(0).gameObject.activeSelf, "板は隠れている");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(host); }
    }

    [Test]
    public void 会話があると窓が出て文が入る()
    {
        GameObject host;
        var g = Game();
        var c = MakeController(out host, g);
        DialogueWindowView view;
        var root = MakeWindow(c, out view);
        try
        {
            g.Speak("ためしの文");
            Call(view, "LateUpdate");
            Assert.AreEqual("ためしの文", view.Showing);
            Assert.IsTrue(root.transform.GetChild(0).gameObject.activeSelf, "板が出る");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(host); }
    }

    /// <summary>題名や終幕の1枚絵のときは、会話の窓は出ない（1枚絵の側が出す）。</summary>
    [Test]
    public void 一枚絵のページでは会話の窓が邪魔をしない()
    {
        GameObject host;
        var g = Game();
        var c = MakeController(out host, g);
        DialogueWindowView view;
        var root = MakeWindow(c, out view);
        try
        {
            g.PlayScene("終幕");
            while (g.CurrentKind.HasValue
                   && g.CurrentKind != TowerGame.PageKind.FinalScreen) g.NextPage();
            Assert.AreEqual(TowerGame.PageKind.FinalScreen, g.CurrentKind);

            Call(view, "LateUpdate");
            Assert.IsNull(view.Showing, "会話の窓は出ない");
            Assert.IsFalse(root.transform.GetChild(0).gameObject.activeSelf);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(host); }
    }

    [Test]
    public void メニューを開くと窓が出て項目が並ぶ()
    {
        GameObject host;
        var g = Game();
        var c = MakeController(out host, g);
        MenuWindowView view;
        var root = MakeWindow(c, out view);
        try
        {
            Call(view, "LateUpdate");
            Assert.IsFalse(root.transform.GetChild(0).gameObject.activeSelf, "閉じている間は出ない");

            g.OpenMenu();
            Call(view, "LateUpdate");
            Assert.IsTrue(root.transform.GetChild(0).gameObject.activeSelf, "開くと出る");

            var body = root.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            StringAssert.Contains("メニューの選択", body.text);
            StringAssert.Contains(MenuWindowView.Cursor, body.text, "選んでいる所に印が付く");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(host); }
    }
}
