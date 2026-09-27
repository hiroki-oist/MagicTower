using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 原典データが正しく読み込めるかの回帰テスト。
/// 期待値はすべて PC98版の復号結果（docs/REMAKE_PLAN.md に根拠あり）。
/// </summary>
public class OriginalDataTests
{
    [Test]
    public void 全50階のフロアデータが読める()
    {
        for (int n = 1; n <= 50; n++)
        {
            var d = FloorData.Load(n);
            Assert.IsNotNull(d, $"{n}階のデータが読めない");
            Assert.AreEqual(n, d.FloorNumber);
        }
    }

    [Test]
    public void F35はドラゴンと隠し壁を持つ()
    {
        var d = FloorData.Load(35);
        int dragon = 0, secret = 0;
        foreach (var o in d.Objects) if (o.Prefab == "Boss_Dragon") dragon++;
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                if (d.At(x, y) == FloorData.Terrain.SecretWall) secret++;
        Assert.AreEqual(9, dragon, "ドラゴンは3x3の9マスを占有する");
        Assert.Greater(secret, 0, "盗賊が掘った抜け穴（隠し壁）があるはず");
    }

    [Test]
    public void F13はマグマ床()
    {
        var d = FloorData.Load(13);
        int magma = 0;
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                if (d.At(x, y) == FloorData.Terrain.Magma) magma++;
        Assert.AreEqual(80, magma, "F13のマグマは80マス");
    }

    [Test]
    public void 階段の接続表が全51階分ある()
    {
        var c = FloorConnections.Instance;
        for (int n = 0; n <= 50; n++)
        {
            FloorConnections.Info info;
            Assert.IsTrue(c.TryGet(n, out info), $"{n}階の接続情報が無い");
        }
    }

    [Test]
    public void F44とF50は通常の階段で入れない()
    {
        var c = FloorConnections.Instance;
        Assert.IsTrue(c.Get(44).IsUnreachableByStairs, "F44は幻の階");
        Assert.IsTrue(c.Get(50).IsUnreachableByStairs, "F50は階段を持たない");
        Assert.IsFalse(c.Get(43).IsUnreachableByStairs);
    }

    [Test]
    public void 階段移動はF44を飛ばす()
    {
        var up = FloorTransition.GoUp(43);
        Assert.IsTrue(up.Ok);
        Assert.AreEqual(45, up.Floor, "F43から上るとF44を飛ばしてF45へ");

        var down = FloorTransition.GoDown(45);
        Assert.IsTrue(down.Ok);
        Assert.AreEqual(43, down.Floor, "F45から降りるとF44を飛ばしてF43へ");
    }

    [Test]
    public void 昇華の翼なら幻の階に入れる()
    {
        var w = FloorTransition.WingUp(43);
        Assert.IsTrue(w.Ok);
        Assert.AreEqual(44, w.Floor, "昇華の翼ならF44に入れる");
    }

    [Test]
    public void F49から上には行けない()
    {
        Assert.IsFalse(FloorTransition.GoUp(49).Ok, "実際には49階までしか行けない");
    }

    [Test]
    public void 回復薬の効果量は階で変わる()
    {
        var t = ItemPowerTable.Instance;
        Assert.AreEqual(50,   t.ForFloor(5).PotionRed);
        Assert.AreEqual(200,  t.ForFloor(5).PotionBlue);
        Assert.AreEqual(100,  t.ForFloor(15).PotionRed);
        Assert.AreEqual(400,  t.ForFloor(15).PotionBlue);
        Assert.AreEqual(200,  t.ForFloor(33).PotionRed);
        Assert.AreEqual(800,  t.ForFloor(33).PotionBlue);
        Assert.AreEqual(250,  t.ForFloor(45).PotionRed);
        Assert.AreEqual(1000, t.ForFloor(45).PotionBlue);
    }

    [Test]
    public void クリスタルの効果量は階で変わる()
    {
        var t = ItemPowerTable.Instance;
        Assert.AreEqual(1, t.ForFloor(5).CrystalAttack);
        Assert.AreEqual(2, t.ForFloor(15).CrystalAttack);
        Assert.AreEqual(4, t.ForFloor(33).CrystalAttack);
        Assert.AreEqual(5, t.ForFloor(45).CrystalDefense);
    }

    [Test]
    public void セリフが原典の145本読める()
    {
        // 原典 MT.GTX は 302レコード。うち本文があるのは145本。
        Assert.AreEqual(145, GameText.Instance.DialogueCount);
    }

    [Test]
    public void セリフは原典の改行位置を保持している()
    {
        // レコード12（商人から聞ける最初のヒント）は原典で6行に割られている。
        var line = GameText.Instance.Dialogue(12);
        Assert.IsTrue(line.HasValue);
        var lines = line.Value.Body.Split('\n');
        Assert.AreEqual(6, lines.Length);
        Assert.AreEqual("「この塔は全部で５０階建てで、１０階", lines[0]);
        Assert.AreEqual("はできないだろう。」", lines[5]);
        // 改行が入っているので、折り返しは働かず原典どおりに出る
        Assert.AreEqual(6, DialogueBox.Wrap(line.Value.Body).Count);
    }

    [Test]
    public void メッセージはPC98版の文言()
    {
        // MTE.EXE 内の文字列そのまま。「ＵＰ！」は全角、数値も全角で描画される。
        Assert.AreEqual("回復薬を手に入れた！　生命力　２００ポイントＵＰ！",
                        GameText.Instance.Message("potion", 200));
        Assert.AreEqual("パワークリスタルを手に入れた！　攻撃力　４ポイントＵＰ！",
                        GameText.Instance.Message("crystal_atk", 4));
        // ディフェンスクリスタルだけ「！」の後に空白が無い（原典のまま）
        Assert.AreEqual("ディフェンスクリスタルを手に入れた！防御力　５ポイントＵＰ！",
                        GameText.Instance.Message("crystal_def", 5));
        Assert.AreEqual("相手の防御力が高すぎて攻撃できない。",
                        GameText.Instance.Message("cannot_attack"));
    }

    [Test]
    public void 改行の無い本文は全角18文字で折り返す()
    {
        // {0} を埋めて長さが変わる Messages.txt 用の経路。
        const string body = "「このブロックのボスの金騎士は弱いくせに態度だけはでかいから、この塔の副将格の魔導師ゼノには嫌われているらしい。」";
        var lines = DialogueBox.Wrap(body);
        Assert.AreEqual(4, lines.Count);
        Assert.AreEqual("「このブロックのボスの金騎士は弱いく", lines[0]);
        Assert.AreEqual("せに態度だけはでかいから、この塔の副", lines[1]);
        Assert.AreEqual("将格の魔導師ゼノには嫌われているらし", lines[2]);
        Assert.AreEqual("い。」", lines[3]);
    }

    [Test]
    public void 戦闘計算が原典と一致する()
    {
        // Ｒスライム HP45 攻20 防2 / プレーヤー 攻20 防10
        // 動画で「命27」＝1撃後の残りHP が観測されている（45 - 18 = 27）
        Assert.AreEqual(18, BattleRules.DamagePerHit(20, 2));
        Assert.AreEqual(3,  BattleRules.HitsToDefeat(45, 18));

        var p = BattleRules.Predict(20, 10, 45, 20, 2, 2,
                                    BattleRules.EnemyKind.Normal, BattleRules.Charms.None);
        Assert.IsTrue(p.CanWin);
        Assert.AreEqual(3, p.PlayerHits);
        Assert.AreEqual(20, p.DamageTaken, "最後の一撃には反撃が無い");
    }

    [Test]
    public void 攻撃力が防御力以下なら倒せない()
    {
        Assert.AreEqual(0, BattleRules.DamagePerHit(10, 12));
        Assert.IsFalse(BattleRules.CanDefeat(10, 12,
                       BattleRules.EnemyKind.Normal, BattleRules.Charms.None));
        // 魔導師ゼノ 防1000 は攻撃力1000超が必要
        Assert.IsFalse(BattleRules.CanDefeat(1000, 1000,
                       BattleRules.EnemyKind.Normal, BattleRules.Charms.None));
        Assert.IsTrue(BattleRules.CanDefeat(1001, 1000,
                      BattleRules.EnemyKind.Normal, BattleRules.Charms.None));
    }

    [Test]
    public void 十字架はゾンビへの攻撃力を2倍にする()
    {
        Assert.AreEqual(100, BattleRules.EffectiveAttack(100,
                        BattleRules.EnemyKind.Zombie, BattleRules.Charms.None));
        Assert.AreEqual(200, BattleRules.EffectiveAttack(100,
                        BattleRules.EnemyKind.Zombie, BattleRules.Charms.Cross));
        Assert.AreEqual(100, BattleRules.EffectiveAttack(100,
                        BattleRules.EnemyKind.Normal, BattleRules.Charms.Cross));
    }

    // ------------------------------------------------------------------
    // 以下は原典 MTE.EXE の逆アセンブルから確定した規則（2026-09-21 追加）
    // ------------------------------------------------------------------

    [Test]
    public void ドラゴンスレイヤーはドラゴンにのみ2倍()
    {
        Assert.AreEqual(200, BattleRules.EffectiveAttack(100,
                        BattleRules.EnemyKind.Dragon, BattleRules.Charms.DragonSlayer));
        Assert.AreEqual(100, BattleRules.EffectiveAttack(100,
                        BattleRules.EnemyKind.Zombie, BattleRules.Charms.DragonSlayer));
    }

    [Test]
    public void 十字架の対象は原典が名指しする3体だけ()
    {
        // 原典 0x6101 は ID85 ゾンビ / ID87 ゾンビナイト / ID95 バンパイア を名指ししている
        Assert.AreEqual(BattleRules.EnemyKind.Zombie,  BattleRules.KindFromTag("Enemy_Zombie"));
        Assert.AreEqual(BattleRules.EnemyKind.Zombie,  BattleRules.KindFromTag("Enemy_ZombieWorrior"));
        Assert.AreEqual(BattleRules.EnemyKind.Vampire, BattleRules.KindFromTag("Enemy_ID95"));
        // ID139「兵士」と ID141「死霊兵士」は対象外。名前に Zombie が入るが原典では効かない
        Assert.AreEqual(BattleRules.EnemyKind.Normal,  BattleRules.KindFromTag("Enemy_GreatZombie"));
        Assert.AreEqual(BattleRules.EnemyKind.Normal,  BattleRules.KindFromTag("Enemy_GreatZombieSoldier"));
    }

    [Test]
    public void 祭壇の寄進額と効果が原典の式どおり()
    {
        // 寄進額 = min(10×(C×(C+1)+2), 9990)
        Assert.AreEqual(20,   Altar.Cost(0));
        Assert.AreEqual(40,   Altar.Cost(1));
        Assert.AreEqual(80,   Altar.Cost(2));
        Assert.AreEqual(9990, Altar.Cost(99), "上限は9990");
        // 回復量 = (C+1)×100
        Assert.AreEqual(100, Altar.HealAmount(0));
        Assert.AreEqual(500, Altar.HealAmount(4));
        // 攻撃+2×ブロック / 防御+4×ブロック
        Assert.AreEqual(2, Altar.AttackGain(1));
        Assert.AreEqual(8, Altar.AttackGain(40));
        Assert.AreEqual(16, Altar.DefenseGain(40), "防御は攻撃の2倍伸びる");
    }

    [Test]
    public void 妖精の採点が基準値でちょうど60点()
    {
        // マニュアルの4本＋原典コードから復元した25階の式。定数項は60
        Assert.AreEqual(60, Fairy.Score(10,  400,  29,  35, 0, 0));
        Assert.AreEqual(60, Fairy.Score(20,  800,  74,  80, 0, 0));
        Assert.AreEqual(60, Fairy.Score(25, 3000, 350, 460, 0, 0));
        Assert.AreEqual(60, Fairy.Score(40, 1600, 152, 198, 0, 0));
        Assert.AreEqual(60, Fairy.Score(49, 3000, 240, 300, 0, 0));
        // 鍵も点数に入る
        Assert.AreEqual(63, Fairy.Score(10, 400, 29, 35, 1, 1), "鍵1本＋紫1本で+3");
        Assert.IsFalse(Fairy.IsFairyFloor(30), "30階に妖精はいない");
    }

    [Test]
    public void 魔法攻撃は神盾で無効になる()
    {
        var pincer = new MagicAttack.Neighbors
        { Left = MagicAttack.SorcererId, Right = MagicAttack.SorcererId };
        string key;
        Assert.AreEqual(500, MagicAttack.Apply(1000, 0, pincer, out key), "挟まれると半分");
        Assert.AreEqual("mage_halve", key);
        Assert.AreEqual(1000, MagicAttack.Apply(1000, 5, pincer, out key), "神盾なら無効");
        Assert.IsNull(key);

        var next = new MagicAttack.Neighbors { Up = MagicAttack.WizardAId };
        Assert.AreEqual(800, MagicAttack.Apply(1000, 0, next, out key), "魔術士Ａは200");
        Assert.AreEqual("wizard_200", key);
    }

    /// <summary>
    /// ★**隣にいるものからは全部受ける**（作者の報告で直した）。
    /// 原典は 0x1b75（魔導師）→ 0x1c48〜(Ａの4マス)→ 0x1e29〜(Ｂの4マス) と
    /// 続けて書いてあり、途中で抜けない。ただし同じ種類が何体いても1回ぶん
    /// （Ａの4つの比較はどれも同じ「200引く所」へ飛ぶ）。
    /// </summary>
    [Test]
    public void 魔法攻撃は隣にいるぶんだけ重なる()
    {
        var keys = new System.Collections.Generic.List<string>();

        // 魔導師に挟まれ、さらに魔術士ＡとＢにも隣接
        var all = new MagicAttack.Neighbors
        {
            Left = MagicAttack.SorcererId, Right = MagicAttack.SorcererId,
            Up = MagicAttack.WizardAId, Down = MagicAttack.WizardBId,
        };
        Assert.AreEqual(1000 / 2 - 200 - 100, MagicAttack.Apply(1000, 0, all, keys),
                        "半減してから200と100");
        CollectionAssert.AreEqual(new[] { "mage_halve", "wizard_200", "wizard_100" }, keys);

        // ＡとＢが同時に隣接していれば両方
        var both = new MagicAttack.Neighbors
        { Up = MagicAttack.WizardAId, Left = MagicAttack.WizardBId };
        Assert.AreEqual(700, MagicAttack.Apply(1000, 0, both, keys), "200と100の両方");
        Assert.AreEqual(2, keys.Count);

        // 同じ種類が2体でも1回ぶん（原典はどの比較も同じ枝へ飛ぶ）
        var twoA = new MagicAttack.Neighbors
        { Up = MagicAttack.WizardAId, Down = MagicAttack.WizardAId };
        Assert.AreEqual(800, MagicAttack.Apply(1000, 0, twoA, keys), "Ａ2体でも200");
        Assert.AreEqual(1, keys.Count);

        // 神盾があればどれも受けない
        Assert.AreEqual(1000, MagicAttack.Apply(1000, 5, all, keys));
        Assert.AreEqual(0, keys.Count);

        // 生命力1のときは半分にしない（原典 0x1bfe の cmp 1 → je）
        var pinch = new MagicAttack.Neighbors
        { Left = MagicAttack.SorcererId, Right = MagicAttack.SorcererId };
        Assert.AreEqual(1, MagicAttack.Apply(1, 0, pinch, keys), "1のときは減らない");
    }

    [Test]
    public void 特殊アイテムの効果が原典どおり()
    {
        // エリクサー = 5×(攻撃×2 + 防御)
        Assert.AreEqual(1290, SpecialItemEffects.ElixirHeal(80, 98));
        // 翼は44階を飛ばさない。これが幻の階へ行く唯一の方法
        Assert.AreEqual(44, SpecialItemEffects.WingFloor(43, true));
        Assert.AreEqual(44, SpecialItemEffects.WingFloor(45, false));
        Assert.AreEqual(800, SpecialItemEffects.TeleportHpCost, "空間転移は生命力を払う");
        // 使い切りかどうか
        Assert.IsTrue(SpecialItemEffects.IsConsumedOnUse(SpecialItems.Kind.Matock));
        Assert.IsFalse(SpecialItemEffects.IsConsumedOnUse(SpecialItems.Kind.Cross));
    }

    [Test]
    public void 門は守護マスが全部床になったら開く()
    {
        var sd = SealedDoors.Instance;
        Assert.AreEqual(30, sd.Count,
                        "原典の判定関数の28件＋40階の金騎士の褒美＋39階の時計盤");

        // F17 の左の門は斜め下2マスの敵を倒すと開く
        SealedDoors.Unlock left = default(SealedDoors.Unlock);
        bool found = false;
        foreach (var u in sd.ForFloor(17))
            if (u.FlagId == 0x62) { left = u; found = true; }
        Assert.IsTrue(found, "F17 のフラグ0x62の解錠がある");
        Assert.AreEqual(2, left.Guards.Count);

        var cleared = new System.Collections.Generic.HashSet<Vector2Int>();
        System.Func<Vector2Int, bool> isCleared = p => cleared.Contains(p);
        Assert.IsFalse(SealedDoors.IsSatisfied(left, isCleared), "倒す前は開かない");
        cleared.Add(left.Guards[0]);
        Assert.IsFalse(SealedDoors.IsSatisfied(left, isCleared), "片方だけでは開かない");
        cleared.Add(left.Guards[1]);
        Assert.IsTrue(SealedDoors.IsSatisfied(left, isCleared), "両方倒すと開く");
    }

    /// <summary>
    /// 前提フラグつきの解錠（原典 0x5d←0x5c / 0x68←0x67 / 0x6a←0x69 / 0x84←0x83）。
    /// 守護マスが最初から床でも、そのできごとが起きるまでは発火しない。
    /// </summary>
    [Test]
    public void 前提フラグのある解錠はそれが立つまで開かない()
    {
        SealedDoors.Unlock u = default(SealedDoors.Unlock);
        bool found = false;
        foreach (var x in SealedDoors.Instance.ForFloor(20))
            if (x.FlagId == 0x68) { u = x; found = true; }
        Assert.IsTrue(found, "20階のフラグ0x68の解錠がある");
        Assert.AreEqual(0x67, u.RequiresFlag, "ボス登場(0x67)が前提");

        System.Func<Vector2Int, bool> isCleared = p => true;   // 守護マスは床
        Assert.IsFalse(SealedDoors.IsSatisfied(u, isCleared, f => false),
                       "ボスが出る前は開かない");
        Assert.IsTrue(SealedDoors.IsSatisfied(u, isCleared, f => f == 0x67),
                      "ボスが出たあとなら開く");

        int withFlag = 0;
        for (int f = 1; f <= 50; f++)
            foreach (var x in SealedDoors.Instance.ForFloor(f))
                if (x.RequiresFlag >= 0) withFlag++;
        Assert.AreEqual(5, withFlag, "前提フラグつきは原典の4件＋40階の金騎士");
    }

    [Test]
    public void 商店の値段が原典どおり()
    {
        // 12階の鍵屋は 1本 = 20 × 到達最高階
        Assert.AreEqual(240, Shop.VariableKeyPrice(12));
        Assert.AreEqual(980, Shop.VariableKeyPrice(49));
        Assert.AreEqual(12, Shop.Instance.Count, "階ごとの品揃え12件");
    }

    [Test]
    public void 原典の字下げが保たれている()
    {
        // 原典はタイトルを全角スペースで中央寄せしている。
        // string.Trim() は全角スペース(U+3000)も削るので、消えていないか確かめる
        var title = GameText.Instance.Dialogue(1);
        Assert.IsTrue(title.HasValue);
        StringAssert.StartsWith("　　　迷宮ＲＰＧ", title.Value.Body);

        // 装備名は6文字幅に右寄せされている
        Assert.AreEqual("　長剣", GameText.Instance.Message("panel_sword_1"));
        Assert.AreEqual("　神盾", GameText.Instance.Message("panel_shield_5"));
        Assert.AreEqual("騎士の剣", GameText.Instance.Message("panel_sword_3"));
    }
}
