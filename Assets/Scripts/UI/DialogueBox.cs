using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 会話ボックスの整形。原典（PC98版）の見た目に合わせる。
///
/// 【設計方針・全体に適用】
///   原典 MT.GTX は会話ボックス内の改行位置を 0x01 としてデータに持っている。
///   復号した Dialogue.txt はその改行をそのまま保持しているので、
///   **改行を含む本文は折り返さずそのまま描画する**（原典の折り返しが正典）。
///   折り返しを行うのは、{0} を値で置換して長さが変わる Messages.txt の文言だけ。
///
/// 原典の窓幅は3種類ある（復号した145本の最大行長を数えて判明）:
///   18文字 … 会話ボックス（「」のセリフ）。約物のぶら下げで19文字になる行がある
///   20〜21文字 … システム窓（ロード/セーブ、オーブの説明）
///   32〜35文字 … 全画面のメッセージ（作者からの文章、エンディング）
///   いずれもデータ側の改行で表現されているので、ここでは幅を判定しない。
///
/// ボックスの意匠（動画より）:
///   角丸の矩形、外周に明るい枠線、内側は濃灰、白文字。上辺の中央に小さな丸い飾り。
///   話者名の別枠は無く、原典では本文の先頭に話者名を書く
///   （例: スケルトン「ううう・・・ / 　様を倒せたものだ・・・）。
/// </summary>
public static class DialogueBox
{
    /// <summary>会話ボックス1行あたりの全角文字数（動画および原典データで確認）。</summary>
    public const int CharsPerLine = 18;

    /// <summary>
    /// **原典にページ送りは無い。** 会話表示（0x94ba）の第1引数は「窓の行数」で、
    /// 窓は文章に合わせて開かれる。呼び出しは全部で44箇所あり、セリフ番号が定数の
    /// 38箇所のうち **32箇所は行数がちょうど一致**、残る6箇所は1行以上多い窓だった
    /// （「なんだっ！？　うわぁーっ！！？」のような短い一行を2行の窓で出す、
    /// 　セリフ53「今までに聞いた情報を…」を10行の窓で出して下に一覧を並べる、など）。
    /// **窓がセリフより小さいことは一度も無い**ので、分割はせず1レコード＝1つの窓で出す。
    /// </summary>
    public const int LinesPerPage = int.MaxValue;

    /// <summary>窓の上端（ドット）。原典は普通の会話でここに開く。</summary>
    public const int TopEdge = 120;

    /// <summary>
    /// 高い位置に開く窓の上端。原典で 50 が使われるのは3件だけで、
    /// どれも「姿の見えない相手の声」にあたる:
    ///   セリフ107（10階の盗賊）/ セリフ159（25階の人形の声）/ セリフ156（49階の封印）
    /// </summary>
    public const int HighTopEdge = 50;

    /// <summary>
    /// 本文の一番長い行の幅（全角いくつぶんか）。窓の横幅を本文に合わせるのに使う。
    /// 原典の文字は全角だけだが、置き換えた数字などで半角が混じることがあるので、
    /// 半角は 0.5 として数える。
    /// </summary>
    public static float MaxLineChars(string body)
    {
        float max = 0f;
        foreach (var line in Wrap(body))
        {
            float w = 0f;
            foreach (var ch in Visible(line)) w += IsHalfWidth(ch) ? 0.5f : 1f;
            if (w > max) max = w;
        }
        return max;
    }

    /// <summary>半角の文字か（ASCII と半角カナ）。</summary>
    public static bool IsHalfWidth(char c)
    {
        return c < 0x100 || (c >= 0xff61 && c <= 0xff9f);
    }

    /// <summary>そのセリフを出すのに要る窓の行数＝原典の第1引数。</summary>
    public static int WindowLines(string body)
    {
        return Wrap(body).Count;
    }

    /// <summary>
    /// 本文を行に分ける。
    /// 原典の改行を優先し、1行が charsPerLine を超える場合だけ機械的に折り返す。
    /// </summary>
    public static List<string> Wrap(string body, int charsPerLine = 0)
    {
        if (charsPerLine <= 0) charsPerLine = LimitFor(body);
        var lines = new List<string>();
        if (string.IsNullOrEmpty(body)) return lines;
        foreach (var seg in body.Replace("\r", "").Split('\n'))
        {
            if (Visible(seg).Length <= charsPerLine) { lines.Add(seg); continue; }
            for (int i = 0; i < seg.Length; i += charsPerLine)
            {
                int n = Mathf.Min(charsPerLine, seg.Length - i);
                lines.Add(seg.Substring(i, n));
            }
        }
        return lines;
    }

    /// <summary>
    /// **自分で組んだ窓**（絵を入れる商店）だけ広くする。
    /// 原典のセリフは 18文字で折り返されていて、こちらもそれに合わせている（0x94ba）が、
    /// 課金の窓は原典に無いもので、絵と名前と値段を1行に並べたいので広くする。
    /// 目印は本文の頭に置く <see cref="WideMark"/>。
    /// </summary>
    public const int WideCharsPerLine = 24;

    /// <summary>
    /// 「この窓は広く出す」の目印。TMP の `&lt;nobr&gt;`（折り返さない）を使う —
    /// 見た目には何も出ず、<see cref="Visible"/> が外すので幅にも数えない。
    /// </summary>
    public const string WideMark = "<nobr>";

    public static int LimitFor(string body)
    {
        return body != null && body.StartsWith(WideMark) ? WideCharsPerLine : CharsPerLine;
    }

    /// <summary>
    /// 飾りの札（`&lt;nobr&gt;` や `&lt;color&gt;` など）を外して、**目に見えるぶんだけ**にする。
    /// </summary>
    public static string Visible(string line)
    {
        if (string.IsNullOrEmpty(line) || line.IndexOf('<') < 0) return line ?? "";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] != '<') { sb.Append(line[i]); continue; }
            int close = line.IndexOf('>', i);
            if (close < 0) { sb.Append(line[i]); continue; }
            i = close;
        }
        return sb.ToString();
    }

    /// <summary>行をページ単位に分ける。DialogueHandler へはページごとに渡す。</summary>
    public static List<string> Paginate(string body,
                                        int charsPerLine = CharsPerLine,
                                        int linesPerPage = LinesPerPage)
    {
        var lines = Wrap(body, charsPerLine);
        var pages = new List<string>();
        for (int i = 0; i < lines.Count; i += linesPerPage)
        {
            var sb = new System.Text.StringBuilder();
            for (int j = i; j < Mathf.Min(i + linesPerPage, lines.Count); j++)
            {
                if (j > i) sb.Append('\n');
                sb.Append(lines[j]);
            }
            pages.Add(sb.ToString());
        }
        return pages;
    }

    /// <summary>セリフ1本をページの配列にする。話者名は原典どおり本文に含まれる。</summary>
    public static List<string> Pages(GameText.Line line)
    {
        return Paginate(line.Body);
    }
}
