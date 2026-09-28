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
/// ★作者の頼み（2026-09-28。録画中に見つかった不具合）:
/// 「一回買うと、無限に増やせてしまう。一度増やしたら二回目以上は使えないようにし
///   （使おうとしたら『交換済みの引換券です』と出す）、新たなゲームを始めた際に
///   再度押せば一回だけ手に入るようにしてください」
/// → **引換券は1回の冒険につき1枚**。引き換えたことは<b>遊び手の側</b>に覚える
///   （<see cref="PlayerState.TicketsUsed"/>）ので、セーブにも入り、最初から
///   遊び直せばまた1枚引き換えられる。買った事実（端末・App Store の権利）とは別もの。
///
/// **支払いは Apple In-App Purchase（StoreKit 2）を通す**（<see cref="AppleStore"/>、
/// 実体は `Assets/Plugins/iOS/MagicTowerStore.swift`）。Apple の決まりで:
///   1. **3.1.1** ゲーム内の物をアプリの外の支払いで売ることはできない。品目は
///      「買い切りで何度でも受け取れる」ので **非消耗型（Non-Consumable）**で登録する。
///   2. **3.1.1** 非消耗型は復元できることが必須 → 「購入を復元する」の行。
///   3. **HIG** 値段は App Store から受け取った現地表記（`Product.displayPrice`）を出す。
///      <see cref="PhoneScreen.ShopYen"/> の円は**エディタで見るときの見本**で、
///      実機ではそちらを使わない。
///   4. **HIG** 支払いができない端末では商店を出さない（`AppStore.canMakePayments`）。
///   5. **HIG** 確認のシートは作らない。Apple のシートに任せる。
///
/// エディタとシミュレータでは StoreKit を呼べないので、端末の覚え書きだけで動く見本になる。
/// </summary>
public static class GoldShop
{
    /// <summary>売り物の数。</summary>
    public static int Count { get { return PhoneScreen.ShopGold.Length; } }

    public static int GoldOf(int i) { return PhoneScreen.ShopGold[i]; }
    public static int YenOf(int i)  { return PhoneScreen.ShopYen[i]; }

    static string KeyOf(int i) { return "GoldShop_" + PhoneScreen.ShopGold[i]; }

    /// <summary>
    /// もう買ってあるか（買ってあれば次からは無料）。
    /// 実機では **App Store の権利**（`Transaction.currentEntitlements`）が正で、
    /// 端末の覚え書きはその写し。機種を変えても復元すれば戻る。
    /// </summary>
    public static bool Bought(int i)
    {
        if (i < 0 || i >= Count) return false;
        if (AppleStore.Live) return AppleStore.Owned(i);
        return PlayerPrefs.GetInt(KeyOf(i), 0) != 0;
    }

    /// <summary>買ったことを端末に覚えさせる（エディタで見るときの見本の道）。</summary>
    public static void MarkBought(int i)
    {
        if (i < 0 || i >= Count) return;
        PlayerPrefs.SetInt(KeyOf(i), 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 商店を出せるか。**支払いができない端末では出さない**（HIG）。
    /// ペアレンタルコントロールなどで `canMakePayments` が false になる。
    /// </summary>
    public static bool Available
    {
        get { return !AppleStore.Live || AppleStore.CanPay; }
    }

    /// <summary>支払いができない端末で「商店」を触ったときの知らせ。</summary>
    public const string Unavailable = "この端末では購入ができない設定になっています。";

    /// <summary>
    /// その品目の値段の字。
    /// ⚠ 実機では **App Store から受け取った現地通貨の表示**（`Product.displayPrice`）を
    /// 返すこと。ここの円表記は見本。買ってあるものは「無料」。
    /// </summary>
    public static string PriceText(int i)
    {
        if (Bought(i)) return PhoneScreen.ShopFree;
        if (AppleStore.Live)
        {
            var price = AppleStore.PriceOf(i);
            // まだ App Store から読めていないうちは値段を出さない（嘘の値段を出さないため）
            return string.IsNullOrEmpty(price) ? "…" : price;
        }
        return Wide(YenOf(i)) + "円";            // エディタで見るときの見本
    }

    /// <summary>選ばせる行の数（品目＋復元＋やめる）。</summary>
    public static int ChoiceCount { get { return Count + 2; } }

    /// <summary>「購入を復元する」の行の番号。</summary>
    public static int RestoreIndex { get { return Count; } }

    /// <summary>窓に出す文章。選ばせる行は全角空白2つで字下げする（会話の窓の決まり）。</summary>
    public static string Text()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(DialogueBox.WideMark);          // この窓は広く出す
        foreach (var line in PhoneScreen.ShopIntro) sb.Append(line).Append('\n');
        sb.Append('\n');
        // 名前はいちばん長いものに合わせて全角空白で伸ばし、値段の頭をそろえる。
        // （飾りの札で列を指すやり方は TMP が解釈してくれなかった）
        int wide = 0;
        for (int i = 0; i < Count; i++)
        {
            int n = string.Format(PhoneScreen.ShopItemName, Wide(GoldOf(i))).Length;
            if (n > wide) wide = n;
        }
        for (int i = 0; i < Count; i++)
        {
            // 「[金貨]４００ゴールドの引換券　　　$0.99」
            // 頭の全角2つは金貨の絵を重ねる場所（絵は DialogueWindowView が置く）
            sb.Append(PhoneScreen.ShopIndent);
            var name = string.Format(PhoneScreen.ShopItemName, Wide(GoldOf(i)));
            sb.Append(name);
            sb.Append('　', wide - name.Length + PhoneScreen.ShopPriceGap);
            sb.Append(PriceText(i));
            sb.Append('\n');
            // 品目のあいだを空ける（枠はこの空行まで含めて ShopRowLines 行ぶんの高さになる）
            for (int k = 1; k < PhoneScreen.ShopRowLines; k++) sb.Append('\n');
        }
        // この2行だけ少し小さく（大きいままだと1行ぶんの枠に収まらない）
        sb.Append("　　").Append(PhoneScreen.ShopSmall)
          .Append(PhoneScreen.ShopRestore).Append(PhoneScreen.ShopSmallEnd);
        for (int k = 1; k < PhoneScreen.ShopSmallRowLines; k++) sb.Append('\n');
        sb.Append('\n');
        sb.Append("　　").Append(PhoneScreen.ShopSmall)
          .Append("やめる").Append(PhoneScreen.ShopSmallEnd);
        // 最後の行のうしろは空けない。枠の下半分は窓の余白（上下16ドット）に収まる。
        return sb.ToString();
    }

    /// <summary>
    /// 購入を復元する（審査指針 3.1.1 で非消耗型には必須）。
    /// 実機は `AppStore.sync()` に投げ、結果は次の <see cref="AppleStore.Poll"/> で入る。
    /// </summary>
    public static string Restore()
    {
        if (AppleStore.Live) { AppleStore.Restore(); return PhoneScreen.ShopRestoring; }
        int n = 0;
        for (int i = 0; i < Count; i++) if (Bought(i)) n++;
        return n > 0 ? string.Format(PhoneScreen.ShopRestored, n)
                     : PhoneScreen.ShopNothingToRestore;
    }

    /// <summary>
    /// 選ばれた品目を処理する。
    ///   もう買ってある … その場でゴールドを渡す（何度でも受け取れる）
    ///   まだ買っていない … **Apple の購入のシートを出す**。ここでは何も渡さない
    ///                      （渡すのは、買えたことが分かってから <see cref="Claim"/> で）
    /// 返すのは知らせる一言。何も出さないなら null。
    /// </summary>
    public static string Pick(PlayerState player, int i)
    {
        bool got;
        return Pick(player, i, out got);
    }

    /// <param name="received">実際にゴールドを渡したか（音を鳴らすかの判断に使う）。</param>
    public static string Pick(PlayerState player, int i, out bool received)
    {
        received = false;
        if (player == null || i < 0 || i >= Count) return null;
        if (Bought(i)) return Claim(player, i, out received);
        if (AppleStore.Live) { AppleStore.Buy(i); return null; }
        // エディタで見るときの見本。実機ではここを通らない
        MarkBought(i);
        return Claim(player, i, out received);
    }

    /// <summary>
    /// その冒険でもうこの引換券を使ったか（作者の指定）。
    /// 覚えているのは遊び手なので、最初から遊び直せば消える。
    /// </summary>
    public static bool Used(PlayerState player, int i)
    {
        if (player == null || i < 0 || i >= Count) return false;
        return player.TicketsUsed.Contains(GoldOf(i));
    }

    /// <summary>買ってある品目のゴールドを受け取る。</summary>
    public static string Claim(PlayerState player, int i)
    {
        bool got;
        return Claim(player, i, out got);
    }

    /// <summary>
    /// 買ってある品目のゴールドを受け取る。
    /// **1回の冒険につき1枚**しか引き換えられない（作者の指定）。
    /// 二度目は何も渡さずに「交換済みの引換券です。」と知らせる。
    /// </summary>
    /// <param name="received">実際にゴールドを渡したか（音を鳴らすかの判断に使う）。</param>
    public static string Claim(PlayerState player, int i, out bool received)
    {
        received = false;
        if (player == null || i < 0 || i >= Count) return null;
        if (Used(player, i)) return PhoneScreen.ShopUsed;
        player.TicketsUsed.Add(GoldOf(i));
        player.Gold += GoldOf(i);
        player.ClampStats();
        received = true;
        return string.Format(PhoneScreen.ShopBought, Wide(GoldOf(i)));
    }

    /// <summary>昔の名前（試験が使う）。</summary>
    public static string Buy(PlayerState player, int i) { return Pick(player, i); }

    /// <summary>数字を全角にする（原典の窓は全角で数を書く）。</summary>
    public static string Wide(int n)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in n.ToString())
            sb.Append(c >= '0' && c <= '9' ? (char)('０' + (c - '0')) : c);
        return sb.ToString();
    }
}
