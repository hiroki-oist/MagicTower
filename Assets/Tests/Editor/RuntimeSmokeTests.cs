using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 見た目の層の「Unity の API を実際に叩く道」を1回は通しておく検査。
///
/// ほかの検査は原典の規則やデータを見るもので、`AudioClip` を作るとか
/// `MonoBehaviour` を足すといった **Unity 側の呼び出しは一度も走っていなかった**。
/// ここだけはそれを実際に動かして、例外が出ないことと出来上がりの形を見る。
/// </summary>
public class RuntimeSmokeTests
{
    [Test]
    public void 譜面から鳴らせる形が作れる()
    {
        var clip = SoundSynth.Clip(6);              // いちばん短い効果音
        Assert.IsNotNull(clip, "音6の AudioClip");
        Assert.AreEqual(1, clip.channels);
        Assert.AreEqual(SoundSynth.SampleRate, clip.frequency);
        Assert.AreEqual(SoundSynth.Render(6).Length, clip.samples);
        Assert.Greater(clip.length, 0f);

        var data = new float[clip.samples];
        Assert.IsTrue(clip.GetData(data, 0), "中身が読み出せる");
        float peak = 0f;
        foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
        Assert.Greater(peak, 0.05f, "無音ではない");
        Object.DestroyImmediate(clip);
    }

    [Test]
    public void 曲も作れて繰り返しの長さになる()
    {
        var clip = SoundSynth.Clip(1);
        Assert.IsNotNull(clip);
        Assert.AreEqual(22.15f, clip.length, 0.05f, "音1の繰り返しは約22秒");
        Object.DestroyImmediate(clip);
    }

    [Test]
    public void 鳴らす側が音を覚えて使い回す()
    {
        var go = new GameObject("SoundPlayerTest");
        try
        {
            var p = go.AddComponent<SoundPlayer>();
            var a = p.ClipOf(6);
            var b = p.ClipOf(6);
            Assert.IsNotNull(a);
            Assert.AreSame(a, b, "2回目は作り直さない");

            p.PlayCue(SoundCues.TakeItem);          // 効果音。例外が出なければよい
            Assert.AreEqual(-1, p.PlayingMusic, "効果音では曲は変わらない");

            p.Play(SoundCues.Instance.Number(SoundCues.FieldMusic));
            Assert.AreEqual(1, p.PlayingMusic, "曲は流しっぱなしになる");
            p.StopMusic();
            Assert.AreEqual(-1, p.PlayingMusic);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void 本線が積んだ音を鳴らす側が引き取れる()
    {
        var go = new GameObject("FlushTest");
        try
        {
            var p = go.AddComponent<SoundPlayer>();
            var g = new TowerGame(new PlayerState
            {
                Hp = 1000, Attack = 100, Defense = 100,
                Floor = 1, Position = new Vector2Int(0, 0),
            });
            g.Floor.Put(new Vector2Int(0, 1), "Key_Yellow");
            g.Step(new Vector2Int(0, 1));
            Assert.Greater(g.Sounds.Count, 0, "拾って音が積まれた");
            p.Flush(g);
            Assert.AreEqual(0, g.Sounds.Count, "引き取ると空になる");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>1枚絵の出し入れ。組み立てたシーンと同じ形を手で作って動かす。</summary>
    [Test]
    public void 一枚絵の出し入れができる()
    {
        var go = new GameObject("FullScreenTest");
        try
        {
            var view = go.AddComponent<FullScreenView>();
            var so = new UnityEditor.SerializedObject(view);
            so.FindProperty("panel").objectReferenceValue = go;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(view.IsShowing, "作った直後は出ている");
            view.Hide();
            Assert.IsFalse(view.IsShowing);
            view.Show(null, TitleScreen.Title, TitleScreen.Copyright);
            Assert.IsTrue(view.IsShowing);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
