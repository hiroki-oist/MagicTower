using UnityEngine;

/// <summary>
/// 戦闘の演出。原典 PC98版の戦闘処理（MTE.EXE 0x6082）の後半から読み取ったもの。
///
/// <code>
///   もし1撃のダメージが0なら「相手の防御力が高すぎて攻撃できない。」で終わり
///   そうでなければ:
///     プレーヤーの X か Y を敵の方へ1つ動かす（向き 1〜4 の分岐、0x61ef〜0x6232）
///     その位置でプレーヤーを描き直す                （0x68b:0x44c8）
///     0xa(10) 待つ                                   （0xf5f:0x3f2）
///     音を鳴らす（番号9）                            （0x10da:0x20b）
///     当たった位置に effect の絵を貼る               （0xf5f:0x147 → 0xf5f:0xc66）
///     0x5(5) 待つ
///     ダメージの数を (568,296) の欄に書き換えながら繰り返す
/// </code>
///
/// 位置の出し方は盤面と同じ `横 = X*4+0x12`（バイト）、`縦 = Y*0x20+0x18`（ドット）。
/// </summary>
public static class BattlePresentation
{
    /// <summary>踏み込んだあとの待ち（原典 0xa）。</summary>
    public const int StepWait = 10;

    /// <summary>当たりの絵を出したあとの待ち（原典 0x5）。</summary>
    public const int HitWait = 5;

    /// <summary>斬る音の番号（原典 0x10da:0x20b に渡す 9）。</summary>
    public const int SlashSound = 9;

    /// <summary>
    /// 戦っている間、相手の**残りの命**を書き換える欄（原典 0x639b は幅4で x=552）。
    /// 以前ここを「ダメージの数」と読んでいたが、0x639b が書いているのは
    /// `[bp-0x1a] -= [bp-0xa]`（初めの命から受けた分を引いたもの）＝ 残りの命。
    /// </summary>
    public static RectInt MonsterHpBox { get { return ScreenLayout.MonsterHpBox; } }

    /// <summary>相手の反撃の音（原典 0x10da:0x20b に渡す 10）。</summary>
    public const int CounterSound = 10;

    /// <summary>当たりの絵を出すまでの待ち（原典 0xa）。斬るときも反撃のときも同じ。</summary>
    public const int HitEffectWait = 10;

    /// <summary>演出のひとこま。</summary>
    public struct Step
    {
        /// <summary>"踏み込む" / "待つ" / "音" / "斬る" / "反撃" / "相手が消える" / "終わり"</summary>
        public string Kind;
        /// <summary>待ち時間・音番号など。</summary>
        public int Value;
        /// <summary>絵を出すマス。</summary>
        public Vector2Int At;
    }

    /// <summary>
    /// 戦闘の演出をひとこまずつ並べる。原典 0x6082 の流れそのまま。
    ///
    /// <code>
    ///   踏み込む（相手の方へ1マス。以後プレーヤーはそのマスに立つ）
    ///   くり返し:
    ///     相手を描く → 10待つ → 音9（自分の攻撃）→ 斬りの絵 → 5待つ
    ///     相手がまだ生きていれば 10待つ → 音10（相手の反撃）→ 当たりの絵 → 5待つ
    ///   相手が消えて、そのマスが床になる
    /// </code>
    /// 何回くり返すかは <see cref="BattleRules.Predict"/> の PlayerHits と同じ。
    /// </summary>
    public static System.Collections.Generic.List<Step> Steps(
        Vector2Int from, Vector2Int to, int playerHits)
    {
        return Steps(from, to, playerHits, false);
    }

    /// <summary>
    /// <paramref name="died"/> が真なら**倒れた側**の並び。最後の一撃で相手は死なず、
    /// 相手の反撃で終わる（原典 0x64cd は一撃ごとに生命力を見る）。
    /// 相手はそのまま残るので「相手が消える」も付けない。
    /// </summary>
    public static System.Collections.Generic.List<Step> Steps(
        Vector2Int from, Vector2Int to, int playerHits, bool died)
    {
        var list = new System.Collections.Generic.List<Step>();
        if (playerHits <= 0) return list;
        list.Add(new Step { Kind = "踏み込む", At = to });
        for (int i = 0; i < playerHits; i++)
        {
            list.Add(new Step { Kind = "待つ", Value = StepWait, At = to });
            list.Add(new Step { Kind = "音",   Value = SlashSound, At = to });
            list.Add(new Step { Kind = "斬る", Value = HitWait, At = to });
            if (!died && i == playerHits - 1) break;     // 最後の一撃で倒すので反撃は無い
            list.Add(new Step { Kind = "待つ", Value = HitEffectWait, At = to });
            list.Add(new Step { Kind = "音",   Value = CounterSound, At = to });
            list.Add(new Step { Kind = "反撃", Value = HitWait, At = to });
        }
        if (!died) list.Add(new Step { Kind = "相手が消える", At = to });
        return list;
    }

    /// <summary>
    /// 待ちの単位。原典 `lcall 0xf5f, 0x3f2` は垂直帰線の回数を待つので、1回 = 1/60秒。
    /// </summary>
    public const float FrameSeconds = 1f / 60f;

    /// <summary>待ちの目盛を秒に直す。</summary>
    public static float WaitSeconds(int frames) { return frames * FrameSeconds; }

    /// <summary>ひとこまの並び全部にかかる時間（秒）。</summary>
    public static float Duration(System.Collections.Generic.List<Step> steps)
    {
        if (steps == null) return 0f;
        int frames = 0;
        foreach (var s in steps)
            if (s.Kind == "待つ" || s.Kind == "斬る" || s.Kind == "反撃") frames += s.Value;
        return WaitSeconds(frames);
    }

    /// <summary>敵へ踏み込んだときのプレーヤーの位置。盤面の座標で返す。</summary>
    public static Vector2Int StepInto(Vector2Int from, Vector2Int dir)
    {
        return from + dir;
    }

    /// <summary>その位置の画面上の左上（原典と同じ式）。</summary>
    public static Vector2Int ScreenOf(Vector2Int cell)
    {
        return ScreenLayout.FromCell(cell);
    }
}
