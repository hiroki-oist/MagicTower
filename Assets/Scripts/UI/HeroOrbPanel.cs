using UnityEngine;

/// <summary>
/// 勇者のオーブの一覧の見せ方。
///
/// **作者の指定**（<see cref="AuthorStyle"/> と同じ扱い）で、原典 PC98版の再現ではない。
/// 作者から中国語版スマホゲームの画面を見せてもらい、その並べ方に合わせたもの:
///
/// <code>
///   [絵] 初級魔術士      5ターン        ダメージ 88
///        5 金       生命 60  攻撃 32  防御 8
/// </code>
///
/// 中身（どの相手を何体まで出すか・数値の出し方）は原典 0x8c2a のまま
/// （<see cref="HeroOrb.Rows"/>）で、変えているのは並べ方だけ。
/// </summary>
public static class HeroOrbPanel
{
    /// <summary>窓の余白（画面の幅・高さに対する割合）。</summary>
    public const float MarginX = 0.06f, MarginY = 0.08f;

    /// <summary>1行の高さ（窓の高さに対する割合）。原典と同じ9行ぶん。</summary>
    public const float RowHeight = 1f / HeroOrb.MaxRows;

    /// <summary>行の中の左端（絵）と文字の位置（行の幅に対する割合）。</summary>
    public const float IconLeft = 0.02f, NameLeft = 0.14f,
                       MiddleLeft = 0.42f, RightEdge = 0.98f;

    /// <summary>文字の色。ダメージは赤、金は黄（見せてもらった画面に合わせた）。</summary>
    public static readonly Color Text   = new Color32(0xff, 0xff, 0xff, 0xff);
    public static readonly Color Gold   = new Color32(0xff, 0xcc, 0x44, 0xff);
    public static readonly Color Damage = new Color32(0xff, 0x55, 0x55, 0xff);

    /// <summary>金の欄。</summary>
    public static string GoldLine(int gold) { return GameText.Num(gold) + " 金"; }

    /// <summary>手数の欄。</summary>
    public static string TurnLine(int turns) { return GameText.Num(turns) + " ターン"; }

    /// <summary>能力の欄。原典の見出し（命・攻・防）を言葉にしたもの。</summary>
    public static string StatLine(int hp, int attack, int defense)
    {
        return "生命 " + GameText.Num(hp)
             + "　攻撃 " + GameText.Num(attack)
             + "　防御 " + GameText.Num(defense);
    }

    /// <summary>ダメージの欄。攻撃できない相手は原典と同じ文言を出す。</summary>
    public static string DamageLine(bool canAttack, int damage)
    {
        return canAttack
             ? GameText.Instance.Message("orb_damage") + " " + GameText.Num(damage)
             : GameText.Instance.Message("orb_no_attack").Trim();
    }
}
