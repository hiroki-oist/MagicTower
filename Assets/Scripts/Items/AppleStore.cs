using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Apple In-App Purchase（StoreKit 2）の橋渡し。
/// 実体は <c>Assets/Plugins/iOS/MagicTowerStore.swift</c>。
///
/// 呼び返しは使わず、毎コマ <see cref="Poll"/> を読む形にしてある
/// （UnitySendMessage を使うと橋渡しのヘッダが要り、組み上げが脆くなるため）。
///
/// 実機の iOS 以外（エディタ・シミュレータの見本）では、
/// 端末に覚えた「買った印」だけで動く見本になる（<see cref="GoldShop"/>）。
/// </summary>
public static class AppleStore
{
    /// <summary>
    /// App Store Connect に登録する品目の Product ID。**非消耗型（Non-Consumable）**。
    /// 「一度買えば何度でも同じゴールドを受け取れる」ので、消耗型ではなくこちら。
    /// 並びは <see cref="PhoneScreen.ShopGold"/> と同じ（安い順）。
    ///
    /// ⚠ **App Store Connect に入れた字と1文字でも違うと品目を読めない**。
    /// Product ID は作ったあと変えられず、消しても同じ字は二度と使えないので、
    /// 向こうで作ったものに**こちらを合わせる**（作者が 2026-09-24 に作ったもの）。
    /// </summary>
    public static readonly string[] ProductIds =
    {
        "magicTowerGoldFourHundred",
        "magicTowerGoldOneThousand",
        "magicTowerGoldFourThousand",
    };

    /// <summary>いま読めている品目の状態。</summary>
    public struct Item
    {
        public string Id;
        /// <summary>App Store から受け取った**現地表記の値段**（「¥160」「$1.99」など）。</summary>
        public string Price;
        /// <summary>もう買ってあるか（非消耗型なので、買ってあれば何度でも受け取れる）。</summary>
        public bool Owned;
    }

    public static bool Ready { get; private set; }
    /// <summary>支払いができる端末か（ペアレンタルコントロール等で false になる）。</summary>
    public static bool CanPay { get; private set; }
    public static bool Busy { get; private set; }
    public static string Message { get; private set; }
    public static Item[] Items = new Item[0];

    /// <summary>
    /// **写しを撮るとき**だけ、実機と同じ道を通らせる（`TowerScreenshot`）。
    /// 値段の字は <see cref="Parse"/> で入れる。ふだんは false。
    /// </summary>
    public static bool PreviewLive;

    /// <summary>実機の iOS で動いているか（見本ではなく本物の StoreKit を通すか）。</summary>
    public static bool Live
    {
        get
        {
            if (PreviewLive) return true;
#if UNITY_IOS && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void MTStoreStart(string csv);
    [DllImport("__Internal")] static extern System.IntPtr MTStorePoll();
    [DllImport("__Internal")] static extern void MTStoreBuy(string id);
    [DllImport("__Internal")] static extern void MTStoreRestore();
    [DllImport("__Internal")] static extern void MTStoreReload();
#endif

    static bool started;

    /// <summary>品目を読み始める。何度呼んでも一度きり。</summary>
    public static void Start()
    {
        if (started || !Live) return;
        started = true;
#if UNITY_IOS && !UNITY_EDITOR
        MTStoreStart(string.Join(",", ProductIds));
#endif
    }

    /// <summary>いまの状態を読み直す（毎コマ呼んでよい）。</summary>
    public static void Poll()
    {
        if (!Live) return;
#if UNITY_IOS && !UNITY_EDITOR
        var p = MTStorePoll();
        if (p == System.IntPtr.Zero) return;
        Parse(Marshal.PtrToStringUTF8(p));
#endif
    }

    public static void Buy(int index)
    {
        if (!Live || index < 0 || index >= ProductIds.Length) return;
#if UNITY_IOS && !UNITY_EDITOR
        MTStoreBuy(ProductIds[index]);
#endif
    }

    /// <summary>
    /// 品目を読み直す。**窓を開けるときと、アプリが前に戻ったとき**に呼ぶ。
    ///
    /// 値段の字は Apple から受け取ったものをそのまま出しているので、
    /// 読んだあとに**店の国が変わる**と、並んでいる値段と実際に請求される額が
    /// 食い違って見える（サンドボックスで試すときは、あとからテスト用の
    /// アカウントに入るので必ずこうなる。作者の報告「窓は $0.99 なのに
    /// 購入画面では150円」）。Swift の側も Storefront.updates を見張っている。
    /// </summary>
    public static void Reload()
    {
        if (!Live) return;
#if UNITY_IOS && !UNITY_EDITOR
        MTStoreReload();
#endif
    }

    public static void Restore()
    {
        if (!Live) return;
#if UNITY_IOS && !UNITY_EDITOR
        MTStoreRestore();
#endif
    }

    /// <summary>その番号の品目を持っているか。</summary>
    public static bool Owned(int index)
    {
        if (index < 0 || index >= ProductIds.Length) return false;
        foreach (var it in Items) if (it.Id == ProductIds[index]) return it.Owned;
        return false;
    }

    /// <summary>その番号の値段の字（読めていなければ空）。</summary>
    public static string PriceOf(int index)
    {
        if (index < 0 || index >= ProductIds.Length) return "";
        foreach (var it in Items) if (it.Id == ProductIds[index]) return it.Price;
        return "";
    }

    /// <summary>
    /// Swift が返す小さな JSON を読む。形は
    /// <c>{"ready":b,"canPay":b,"busy":b,"msg":"…","items":[{"id":"…","price":"…","owned":b}]}</c>。
    /// 外の道具を足したくないので、要る所だけを手で拾う。
    /// </summary>
    public static void Parse(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        Ready = Flag(json, "\"ready\":");
        CanPay = Flag(json, "\"canPay\":");
        Busy = Flag(json, "\"busy\":");
        Message = Str(json, "\"msg\":");

        var list = new System.Collections.Generic.List<Item>();
        int at = json.IndexOf("\"items\":");
        while (at >= 0)
        {
            int open = json.IndexOf('{', at);
            if (open < 0) break;
            int close = json.IndexOf('}', open);
            if (close < 0) break;
            var one = json.Substring(open, close - open + 1);
            list.Add(new Item
            {
                Id = Str(one, "\"id\":"),
                Price = Str(one, "\"price\":"),
                Owned = Flag(one, "\"owned\":"),
            });
            at = close;
        }
        Items = list.ToArray();
    }

    static bool Flag(string s, string key)
    {
        int at = s.IndexOf(key);
        return at >= 0 && s.Substring(at + key.Length).TrimStart().StartsWith("true");
    }

    static string Str(string s, string key)
    {
        int at = s.IndexOf(key);
        if (at < 0) return "";
        int q = s.IndexOf('"', at + key.Length);
        if (q < 0) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = q + 1; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length) { sb.Append(s[++i]); continue; }
            if (s[i] == '"') break;
            sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
