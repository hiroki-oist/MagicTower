using UnityEngine;

/// <summary>
/// **課金でゴールドを買う**（作者の指定。原典にも Java版にも無い）。
///
/// 作者の頼み（2026-09-24）:
/// 「商店は課金用のボタンとして残します。ゴールドの購入課金で
///   『一度購入すれば、次回プレイからは無料で同じゴールドを購入できる』って書いといて。
///   それで400ゴールドを160円、1000ゴールドを320円、4000ゴールドを640円にして」
///
/// 「一度買えば次からは無料」は**端末に覚えておく**（`PlayerPrefs`）。
/// セーブデータではなく端末に付くので、最初から遊び直しても買い直しにならない。
///
/// ⚠ **実際の支払いはまだ繋いでいない**（触ると買えたことにする見本）。
/// App Store に出すときは、Apple の審査指針に合わせて次の3つが要る:
///   1. **In-App Purchase を通す**（3.1.1）。ゲーム内の物をアプリの外の支払いで
///      売ることはできない。品目は「買い切りで何度でも受け取れる」ので
///      **非消耗型（Non-Consumable）**として登録する。
///   2. **「購入を復元する」を置く**（3.1.1）。非消耗型は復元できることが必須。
///      いまは端末の覚え書きを読み直すだけなので、StoreKit の
///      `AppStore.sync()` / `Transaction.currentEntitlements` に差し替える。
///   3. **値段は App Store から受け取ったものを出す**。ここの
///      <see cref="PhoneScreen.ShopYen"/> は見本で、実機では
///      `Product.displayPrice`（現地通貨・現地表記）をそのまま出すこと。
///      国によって金額も通貨も変わるので、円を焼き込んではいけない。
/// </summary>
public static class GoldShop
{
    /// <summary>売り物の数。</summary>
    public static int Count { get { return PhoneScreen.ShopGold.Length; } }

    public static int GoldOf(int i) { return PhoneScreen.ShopGold[i]; }
    public static int YenOf(int i)  { return PhoneScreen.ShopYen[i]; }

    static string KeyOf(int i) { return "GoldShop_" + PhoneScreen.ShopGold[i]; }

    /// <summary>もう買ってあるか（買ってあれば次からは無料）。</summary>
    public static bool Bought(int i)
    {
        return i >= 0 && i < Count && PlayerPrefs.GetInt(KeyOf(i), 0) != 0;
    }

    /// <summary>買ったことを端末に覚えさせる。</summary>
    public static void MarkBought(int i)
    {
        if (i < 0 || i >= Count) return;
        PlayerPrefs.SetInt(KeyOf(i), 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// その品目の値段の字。
    /// ⚠ 実機では **App Store から受け取った現地通貨の表示**（`Product.displayPrice`）を
    /// 返すこと。ここの円表記は見本。買ってあるものは「無料」。
    /// </summary>
    public static string PriceText(int i)
    {
        if (Bought(i)) return PhoneScreen.ShopFree;
        return "　" + Wide(YenOf(i)) + "円";
    }

    /// <summary>選ばせる行の数（品目＋復元＋やめる）。</summary>
    public static int ChoiceCount { get { return Count + 2; } }

    /// <summary>「購入を復元する」の行の番号。</summary>
    public static int RestoreIndex { get { return Count; } }

    /// <summary>窓に出す文章。選ばせる行は全角空白2つで字下げする（会話の窓の決まり）。</summary>
    public static string Text()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var line in PhoneScreen.ShopIntro) sb.Append(line).Append('\n');
        sb.Append('\n');
        for (int i = 0; i < Count; i++)
        {
            sb.Append("　　").Append(Wide(GoldOf(i))).Append("ＧＯＬＤ");
            sb.Append(PriceText(i));
            sb.Append('\n');
        }
        sb.Append("　　").Append(PhoneScreen.ShopRestore).Append('\n');
        sb.Append("　　やめる");
        return sb.ToString();
    }

    /// <summary>
    /// 購入を復元する。⚠ いまは端末の覚え書きを数え直すだけ。
    /// 実機では StoreKit に問い合わせて、そこから覚え書きを作り直すこと。
    /// </summary>
    public static string Restore()
    {
        int n = 0;
        for (int i = 0; i < Count; i++) if (Bought(i)) n++;
        return n > 0 ? string.Format(PhoneScreen.ShopRestored, n)
                     : PhoneScreen.ShopNothingToRestore;
    }

    /// <summary>買う。買えたら手に入れた言葉を返す。</summary>
    public static string Buy(PlayerState player, int i)
    {
        if (player == null || i < 0 || i >= Count) return null;
        MarkBought(i);
        player.Gold += GoldOf(i);
        player.ClampStats();
        return string.Format(PhoneScreen.ShopBought, Wide(GoldOf(i)));
    }

    /// <summary>数字を全角にする（原典の窓は全角で数を書く）。</summary>
    public static string Wide(int n)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in n.ToString())
            sb.Append(c >= '0' && c <= '9' ? (char)('０' + (c - '0')) : c);
        return sb.ToString();
    }
}
