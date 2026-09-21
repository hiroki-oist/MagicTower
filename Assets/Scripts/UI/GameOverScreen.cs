using UnityEngine;

/// <summary>
/// ゲームオーバーの画面。原典 PC98版 MTE.EXE の `0x097ac`（遠呼び出し 0x68b:0x2efc）。
///
/// <code>
///   画面を上から400行ぶん黒で塗る            （lcall 0xf5f,0x7e6 に 0, 0x28, 0x190, 0）
///   「ＧＡＭＥ　ＯＶＥＲ」を (0x1f, 0xbe) に書く（0x9e30、x はバイト・y はドット）
///   キー入力を待つ                            （lcall 0x107e,0x4fa）
///   INITIAL.DAT を読み直す                    （0x97ec）＝ 初めからやり直し
/// </code>
///
/// 呼ばれるのは3か所:
///   `0x006519` 戦闘で相手の一撃が生命力以上だったとき（0x64cd の比較）
///   `0x001df2` / `0x001fd0` 魔法攻撃などで生命力が尽きたとき
///
/// **生命力が0になったら必ずここへ来る**。
/// 「負ける相手には近づけない」といった作りにはなっていない
/// （攻撃できないのは *相手の防御力が自分の攻撃力以上* のときだけ）。
/// </summary>
public static class GameOverScreen
{
    /// <summary>出す文字（原典 DS:0x181e の Shift-JIS そのまま）。</summary>
    public const string Line = "ＧＡＭＥ　ＯＶＥＲ";

    /// <summary>原典が渡す位置。**x はバイト（＝8ドット）、y はドット**。</summary>
    public const int X = 0x1f, Y = 0xbe;

    /// <summary>文字の左上（ドット）。</summary>
    public static Vector2Int Dot { get { return ScreenLayout.FromTextPos(X, Y); } }

    /// <summary>キーを押したあと（原典は INITIAL.DAT を読み直す＝最初から）。</summary>
    public const string Restart = "やり直し";
}
