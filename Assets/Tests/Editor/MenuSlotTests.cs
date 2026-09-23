using NUnit.Framework;
using UnityEngine;

/// <summary>
/// セーブ／ロードの**原典の姿**（MTE.EXE 0x0d60〜0x0e19）を控えておく検査。
///
/// 原典はメニューから8つのスロットを選ばせる（セリフ50＝ロード / 51＝セーブ、
/// どちらも10行の窓・上端50）。★作者の指定でスマホ版はこの選び方をやめ、
/// 下の「セーブ」「ロード」のボタンで**スロット1つ**を「はい／いいえ」で読み書きする
/// （動きは <see cref="QuickSaveTests"/>）。原典のデータ自体は残してあるので、
/// ここでそれが崩れていないかだけ見る。
/// </summary>
public class MenuSlotTests
{
    [Test]
    public void スロットは八つでセリフは五十と五十一()
    {
        Assert.AreEqual(8, TowerGame.SlotCount, "原典も選ぶ処理に 8 を渡す");
        Assert.AreEqual(8, TowerSave.SlotCount, "書き出せる数も8つのまま");
        Assert.AreEqual(50, TowerGame.LoadRecord);
        Assert.AreEqual(51, TowerGame.SaveRecord);
    }

    [Test]
    public void セリフ五十は問いかけと八つの並び()
    {
        var body = GameText.Instance.Dialogue(50).Value.Body;
        var lines = body.Split('\n');
        StringAssert.Contains("どのデータをロードしますか？", lines[0]);
        Assert.AreEqual(10, lines.Length, "問いかけ1行＋データ1〜8＋やっぱりやめる");
        Assert.AreEqual("　　やっぱりやめる", lines[9]);
    }

    [Test]
    public void セリフ五十一はセーブの問いかけ()
    {
        var body = GameText.Instance.Dialogue(51).Value.Body;
        StringAssert.Contains("セーブ", body.Split('\n')[0]);
    }

    /// <summary>8つのスロットに別々に書けること自体は変えていない。</summary>
    [Test]
    public void 八つのスロットに別々に書ける()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath,
                                         "slots_" + System.Guid.NewGuid());
        TowerSave.Directory = dir;
        try
        {
            for (int slot = 1; slot <= TowerSave.SlotCount; slot++)
                TowerSave.Save(slot, new PlayerState { Hp = 100 * slot, Floor = slot });
            for (int slot = 1; slot <= TowerSave.SlotCount; slot++)
            {
                var p = TowerSave.Load(slot);
                Assert.IsNotNull(p, $"スロット{slot}");
                Assert.AreEqual(100 * slot, p.Hp);
                Assert.AreEqual(slot, p.Floor);
            }
            Assert.IsNull(TowerSave.Load(TowerSave.SlotCount + 1), "範囲の外は空");
        }
        finally
        {
            TowerSave.Directory = null;
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }
}
