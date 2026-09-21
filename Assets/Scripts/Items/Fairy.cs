using UnityEngine;

/// <summary>
/// 妖精の採点。原典 MTE.EXE の妖精処理（0x74f6）と MTE.DOC（マニュアル）より。
///
/// 妖精がいるのは **10 / 20 / 25 / 40 / 49 階**。
/// マニュアルは「〜10階／〜20階／〜40階／〜50階」の4本を載せており、
/// それぞれ 10 / 20 / 40 / 49 階に対応する。**25階の式だけマニュアルに無い**。
///
/// 原典コード上は定数項（60 と各基準値）が畳み込まれていて、たとえば10階は
/// <c>(攻撃力 + 0x5535) × 3</c> と書かれている。0x5535 は
/// コンパイラが3の法逆元 43691 を使って −97 を掛け算に畳み込んだもので、
/// 展開すると <c>攻撃力×3 − 97</c>、すなわち <c>60 − 29×3 − 35×2</c> と一致する。
/// 割り算は符号つきで 0 方向に丸める（<c>cdq ; sub ax,dx ; sar</c> の形）。
/// </summary>
public static class Fairy
{
    /// <summary>妖精がいる階。</summary>
    public static readonly int[] Floors = { 10, 20, 25, 40, 49 };

    public static bool IsFairyFloor(int floor)
    {
        foreach (var f in Floors) if (f == floor) return true;
        return false;
    }

    /// <summary>称号（6段階）。原典 DS:0x36c1 付近の文字列表の順。</summary>
    public static readonly string[] Ranks = { "無称号", "戦士", "勇者", "英雄", "剣帝", "闘神" };

    /// <summary>称号がもらえる点数。</summary>
    public const int RankThreshold = 80;
    /// <summary>「おめでとう！　満点です。」になる点数。</summary>
    public const int PerfectScore = 100;

    /// <summary>符号つきで 0 方向に丸める割り算（原典の idiv / sar と同じ）。</summary>
    static int Div(int a, int b)
    {
        int q = Mathf.Abs(a) / Mathf.Abs(b);
        return ((a < 0) != (b < 0)) ? -q : q;
    }

    /// <summary>
    /// 採点。その階に妖精がいなければ int.MinValue を返す。
    /// どの式も末尾に <c>＋鍵の個数 ＋紫の鍵の個数×２</c> が付く（マニュアル記載どおり）。
    /// </summary>
    public static int Score(int floor, int hp, int attack, int defense,
                            int yellowKeys, int purpleKeys)
    {
        int baseScore;
        switch (floor)
        {
            case 10: baseScore = 60 + Div(hp - 400, 50)
                                    + (attack - 29) * 3 + (defense - 35) * 2; break;
            case 20: baseScore = 60 + Div(hp - 800, 100)
                                    + (attack - 74) * 2 + (defense - 80); break;
            // マニュアルに無い5本目。原典コードから復元した
            case 25: baseScore = 60 + Div(hp - 3000, 400)
                                    + Div(attack - 350, 2) + Div(defense - 460, 4); break;
            case 40: baseScore = 60 + Div(hp - 1600, 200)
                                    + (attack - 152) + Div(defense - 198, 2); break;
            case 49: baseScore = 60 + Div(hp - 3000, 400)
                                    + Div(attack - 240, 2) + Div(defense - 300, 3); break;
            default: return int.MinValue;
        }
        return baseScore + yellowKeys + purpleKeys * 2;
    }

    /// <summary>点数から出すメッセージのキー（Messages.txt）。</summary>
    public static string EvaluationKey(int score)
    {
        if (score >= PerfectScore) return "fairy_perfect";
        if (score >= RankThreshold) return "fairy_title";
        if (score >= 60) return "fairy_eval4";   // なかなかのものです
        if (score >= 40) return "fairy_eval3";   // まあまあですね
        if (score >= 20) return "fairy_eval2";   // いまひとつですね
        return "fairy_eval1";                    // うーん、…かなり無理があるでしょう
    }
}
