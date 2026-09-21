using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店。データは Resources/ShopOffers.txt。
/// 原典 MTE.EXE の商人処理（0x0ea6）より。
///
/// 原典では商人は**階（同じ階に2人いる場合はX座標）で品物が固定**されている。
/// 買うと商人ごとの「買った」フラグが立ち、
/// **その次のレコードのヒントが聞けるようになる**
/// （原典ヒント「商人から物を買うと、あとで重要な情報を聞く事をできるじゃろう。」）。
/// </summary>
public class Shop
{
    public struct Offer
    {
        public int Floor;
        /// <summary>同じ階に商人が2人いる場合の区別。1人だけなら int.MinValue。</summary>
        public int X;
        /// <summary>"buy" / "sell" / "buy_var" / "gift"</summary>
        public string Mode;
        /// <summary>"purple:1" のような内容。</summary>
        public string Goods;
        /// <summary>支払う（または受け取る）額。buy_var は -1。</summary>
        public int Price;
        public int OfferId;
        /// <summary>買うと聞けるヒントのレコード番号。無ければ -1。</summary>
        public int HintId;
        /// <summary>「もう買った」の覚え書きの番号。何度でも買える商人は -1。</summary>
        public int BuyFlag;
        /// <summary>「もうヒントを聞いた」の覚え書きの番号。無ければ -1。</summary>
        public int HintFlag;
    }

    /// <summary>
    /// 値段が変動する鍵屋（12階・X=9）の1本あたりの値段。
    /// 原典は <c>mov ax, 0x14 ; mul word ptr es:[0x96]</c>、
    /// すなわち **20 × 到達最高階**。いくらでも買える。
    /// </summary>
    public const int VariableKeyUnitPrice = 20;
    public static int VariableKeyPrice(int highestFloorReached)
    {
        return VariableKeyUnitPrice * Mathf.Max(0, highestFloorReached);
    }

    readonly List<Offer> offers = new List<Offer>();

    static Shop _instance;
    public static Shop Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static Shop LoadDefault()
    {
        var s = new Shop();
        var asset = Resources.Load<TextAsset>("ShopOffers");
        if (asset == null) Debug.LogError("Shop: Resources/ShopOffers.txt が見つかりません");
        else s.Parse(asset.text);
        return s;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 7) continue;
            int floor;
            if (!int.TryParse(p[0].Trim(), out floor)) continue;

            int x;
            if (!int.TryParse(p[1].Trim(), out x)) x = int.MinValue;
            int price;
            if (!int.TryParse(p[4].Trim(), out price)) price = -1;
            int offerId;
            if (!int.TryParse(p[5].Trim(), out offerId)) offerId = -1;
            int hintId;
            if (!int.TryParse(p[6].Trim(), out hintId)) hintId = -1;
            int buyFlag = -1, hintFlag = -1;
            if (p.Length > 7) int.TryParse(p[7].Trim(), out buyFlag);
            if (p.Length > 8) int.TryParse(p[8].Trim(), out hintFlag);
            if (buyFlag == 0) buyFlag = -1;
            if (hintFlag == 0) hintFlag = -1;

            offers.Add(new Offer
            {
                Floor = floor, X = x,
                Mode = p[2].Trim(), Goods = p[3].Trim(),
                Price = price, OfferId = offerId, HintId = hintId,
                BuyFlag = buyFlag, HintFlag = hintFlag
            });
        }
    }

    /// <summary>その階（必要ならX）の商人の品揃え。</summary>
    public IEnumerable<Offer> ForFloor(int floor, int x = int.MinValue)
    {
        foreach (var o in offers)
        {
            if (o.Floor != floor) continue;
            if (o.X != int.MinValue && x != int.MinValue && o.X != x) continue;
            yield return o;
        }
    }

    public int Count { get { return offers.Count; } }
}
