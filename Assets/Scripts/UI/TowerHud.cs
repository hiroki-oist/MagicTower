using UnityEngine;

/// <summary>
/// 画面に出す文字を組み立てる。原典 PC98版の画面構成に合わせる。
///
/// <code>
///   左側   「塔　Ｎ階」／ハート＝生命力／剣＝攻撃力／盾＝防御力／コイン＝所持金
///          その下に所持している特殊アイテムのアイコン
///   右上   WEAPON 欄（武器名）／SHIELD 欄（盾名）／鍵3種のアイコン
///   右下   モンスター情報（名前・命・攻・防）とメッセージ欄を兼ねる1つの枠
/// </code>
///
/// 数字は原典どおり**全角**で出す（<see cref="GameText.Num"/>）。
/// 装備名は原典が持っている6文字幅の短縮名をそのまま使う
/// （`panel_sword_N` / `panel_shield_N`）。
/// </summary>
public static class TowerHud
{
    /// <summary>
    /// 「塔　Ｎ階」。原典は先に **「塔　　階」** を書いてから（0xbaf5）、
    /// 真ん中の2マスに階を**右詰め**で重ねる（0xb8d6、x=6バイト・枠2つ）。
    /// なので2桁の階では全角空白が消えて「塔５０階」になる。
    /// </summary>
    public const int FloorDigits = 2;

    public static string FloorLabel(int floor)
    {
        return GameText.Instance.Message("floor_label",
                                         GameText.NumRight(floor, FloorDigits));
    }

    /// <summary>情報欄の桁数と行数（96x128 ＝ 全角6文字 × 8行）。</summary>
    public const int InfoColumns = 6, InfoLines = 8;

    /// <summary>
    /// 行の間隔の調整。原典の行送りは <see cref="ScreenLayout.LineAdvance"/>＝20ドット
    /// （Canvas では 36）だが、TextMeshPro の既定はフォント任せ（実測 31.22）なので、
    /// その差を足して 36 にする。単位は文字の大きさの1%（0.288）。
    /// 実測して確かめているのは <c>TextMetricsTests</c>。
    /// </summary>
    public const float InfoLineSpacing = 16.6f;

    /// <summary>
    /// 右下の情報欄に出す形に折り返す。原典 0x9d80 の作りをそのまま写した:
    ///
    ///   ・1行は **6桁**。全角1文字＝1桁、半角の空白も1桁
    ///   ・改行(0x0a)が来たらその行はそこで終わり
    ///   ・タブ(0x09)は桁を使わずに読み飛ばす
    ///   ・行数は常に8行（足りない分は空行）
    ///
    /// 原典は欄を塗り直してから**その時のメッセージ1つだけ**を書くので、
    /// 履歴は積み上がらない。
    /// </summary>
    public static string InfoBox(string message)
    {
        var src = message ?? "";
        var sb = new System.Text.StringBuilder();
        int i = 0;
        for (int line = 0; line < InfoLines; line++)
        {
            if (line > 0) sb.Append('\n');
            for (int col = 0; col < InfoColumns && i < src.Length; )
            {
                var c = src[i];
                if (c == '\t') { i++; continue; }           // 桁を使わない
                if (c == '\n') { i++; break; }              // ここで行を切る
                sb.Append(c); i++; col++;
            }
        }
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>左側に並ぶ4つの数値。全角。</summary>
    public static string Hp(PlayerState p)      { return GameText.Num(p.Hp); }

    /// <summary>
    /// 左の欄に出す生命力。**斬り合いを見せている間はターンに合わせて減らす**
    /// （作者の指定「戦闘直後に戦闘後のHPまで生命力が下がってる。ターンと同期させて」）。
    ///
    /// 本線は踏み込んだ拍子に戦いを最後まで片付けてしまう（原典 0x6082 も
    /// その場で回り切る）ので、<see cref="PlayerState.Hp"/> をそのまま出すと
    /// 斬り合いが始まる前に最後の値まで落ちて見える。
    /// 数え方は旧版の帯（<see cref="BattleBand.At"/>）と**同じものを通す** —
    /// 別々に数えると帯と左の欄で違う数字が出る。
    /// </summary>
    /// <param name="playing">斬り合いを見せている最中か。</param>
    /// <param name="hitsDone">ここまでに入れた斬りの数。</param>
    /// <param name="countersDone">ここまでに受けた反撃の数。</param>
    public static string Hp(PlayerState p, BattleBand.Numbers n, bool playing,
                            int hitsDone, int countersDone)
    {
        if (!playing || string.IsNullOrEmpty(n.Enemy) || n.Hits <= 0) return Hp(p);
        var tick = BattleBand.At(n.PlayerHp, n.EnemyHp, n.PlayerDamage, n.EnemyDamage,
                                 hitsDone, countersDone);
        return GameText.Num(tick.PlayerHp);
    }
    public static string Attack(PlayerState p)  { return GameText.Num(p.Attack); }
    public static string Defense(PlayerState p) { return GameText.Num(p.Defense); }
    public static string Gold(PlayerState p)    { return GameText.Num(p.Gold); }

    /// <summary>WEAPON 欄に出す名前。持っていなければ空。</summary>
    public static string WeaponName(PlayerState p)
    {
        return p.SwordRank >= 1 && p.SwordRank <= 5
            ? GameText.Instance.Message("panel_sword_" + p.SwordRank) : "";
    }

    /// <summary>SHIELD 欄に出す名前。</summary>
    public static string ShieldName(PlayerState p)
    {
        return p.ShieldRank >= 1 && p.ShieldRank <= 5
            ? GameText.Instance.Message("panel_shield_" + p.ShieldRank) : "";
    }

    /// <summary>右下の枠に出すモンスター情報。原典は 名前／命／攻／防 の並び。</summary>
    public struct MonsterInfo
    {
        public bool Valid;
        public string Name;
        public string Hp, Attack, Defense;
        /// <summary>数のままの命。戦っている間に減らして書き直すのに使う。</summary>
        public int MaxHp;
        /// <summary>プレーヤーの一撃で減る命。</summary>
        public int DamagePerHit;
        /// <summary>勇者のオーブを持っているときだけ出る予想ダメージ。</summary>
        public string ExpectedDamage;
    }

    public static MonsterInfo Inspect(PlayerState player, string enemyPrefab)
    {
        var info = new MonsterInfo();
        var e = TowerBattle.LoadEnemy(enemyPrefab);
        if (e == null) return info;

        info.Valid = true;
        info.Name = e.Name;
        info.Hp = GameText.Num(e.MaxHP);
        info.Attack = GameText.Num(e.Attack);
        info.Defense = GameText.Num(e.Defense);
        info.MaxHp = e.MaxHP;
        if (player != null)
        {
            var k = BattleRules.KindFromTag(enemyPrefab);
            info.DamagePerHit = BattleRules.DamagePerHit(
                BattleRules.EffectiveAttack(player.Attack, k, TowerBattle.CharmsOf(player)),
                e.Defense);
        }

        // 勇者のオーブを持っていれば、受けるダメージまで出してくれる
        if (player != null && player.Has(SpecialItems.Kind.HeroOrb))
        {
            var kind = BattleRules.KindFromTag(enemyPrefab);
            var charms = TowerBattle.CharmsOf(player);
            var p = BattleRules.Predict(player.Attack, player.Defense,
                                        e.MaxHP, e.Attack, e.Defense, e.Coin, kind, charms);
            info.ExpectedDamage = p.CanWin
                ? GameText.Num(p.DamageTaken)
                : GameText.Instance.Message("orb_no_attack");   // 「　　　　　攻撃不可能」
        }
        return info;
    }

    /// <summary>
    /// 何撃か入れたあとの相手の残りの命。原典は一撃ごとにこの数字を書き換える（0x639b）。
    /// </summary>
    public static int MonsterHpAfter(MonsterInfo info, int hits)
    {
        if (info.DamagePerHit <= 0) return info.MaxHp;
        return Mathf.Max(0, info.MaxHp - info.DamagePerHit * hits);
    }

    /// <summary>所持している鍵の本数。原典は本数ぶんアイコンを並べる。</summary>
    public static int YellowKeys(PlayerState p) { return p.YellowKeys; }
    public static int PurpleKeys(PlayerState p) { return p.PurpleKeys; }
    public static int RedKeys(PlayerState p)    { return p.RedKeys; }
}
