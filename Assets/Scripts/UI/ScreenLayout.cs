using UnityEngine;

/// <summary>
/// 原典 PC98版の画面の組み立て。座標は原典の画面（640x400、左上が原点）。
///
/// 盤面のマスから画面位置を出す式は、原典の魔法攻撃の演出（MTE.EXE 0x1b6d〜）に
/// そのまま書かれている:
/// <code>
///   [bp-6]  = X * 4 + 0x12          // バイト単位の横位置
///   [bp-0xa] = Y * 0x20 + 0x18      // ドット単位の縦位置
///   VRAM変位 = 0x50 * [bp-0xa] + [bp-6]
/// </code>
/// 1行80バイト＝640ドットなので、
///   横 = (X * 4 + 18) * 8 = X * 32 + 144
///   縦 = Y * 32 + 24
/// つまり **盤面の左上は (144, 24)、1マスは 32x32**。
/// 11マスで 352 ドットなので左右の余白は 144 ずつ、ちょうど中央に来る。
///
/// 原典の X,Y は 0〜10。この remake の盤面は x,y が -5〜+5 なので X = x+5、Y = 5-y。
/// </summary>
public static class ScreenLayout
{
    public const int ScreenWidth = 640, ScreenHeight = 400;

    /// <summary>盤面の左上（ドット）。</summary>
    public const int MapLeft = 144, MapTop = 24;

    /// <summary>1マスの大きさ（ドット）。</summary>
    public const int TileSize = 32;

    /// <summary>盤面の1辺のマス数。</summary>
    public const int MapCells = 11;

    public const int MapWidth = MapCells * TileSize;    // 352
    public const int MapHeight = MapCells * TileSize;   // 352

    /// <summary>原典の X,Y（0〜10）から画面の左上位置を出す。</summary>
    public static Vector2Int FromOriginalXY(int X, int Y)
    {
        return new Vector2Int((X * 4 + 0x12) * 8, Y * TileSize + MapTop);
    }

    /// <summary>盤面の座標（x,y は -5〜+5）から画面の左上位置を出す。</summary>
    public static Vector2Int FromCell(Vector2Int cell)
    {
        return FromOriginalXY(cell.x + 5, 5 - cell.y);
    }

    /// <summary>
    /// 原典の文字表示（`lcall 0x107e, 0x21`）が受け取る位置を、ドットに直す。
    ///
    /// 中の描画（linear 0x10883）は <c>VRAM位置 = y × 0x50 + x</c> で書いていて、
    /// 1行は 0x50=80バイト＝640ドット。つまり **x はバイト（＝8ドット）、y はドット**。
    /// 全角1文字で x が 2バイト（16ドット）進み、空白(0x20)は1バイト、
    /// 改行(0x0a)は y に行送りを足す。
    /// </summary>
    public static Vector2Int FromTextPos(int xBytes, int yDots)
    {
        return new Vector2Int(xBytes * 8, yDots);
    }

    /// <summary>全角1文字の幅（ドット）。原典は1文字ごとに2バイト進む。</summary>
    public const int TextCharWidth = 16;

    /// <summary>原典の VRAM 変位（1行80バイト）。</summary>
    public static int VramOffset(int X, int Y)
    {
        return 0x50 * (Y * TileSize + MapTop) + (X * 4 + 0x12);
    }

    /// <summary>
    /// 盤面を 1マス = 1ワールド単位で置いたときの、原典の画面と同じ見え方になるカメラの大きさ。
    /// 原典の画面は 400 ドット＝ 400/32 = 12.5 マスぶんなので、その半分。
    /// 横は 640/32 = 20 マスぶんで、縦横比は 1.6 になる。
    ///
    /// こうすると原典の画面と remake のワールド座標がぴったり重なる:
    ///   画面 x=144（盤面の左端）→ ワールド -10 + 144/32 = -5.5 ＝ 一番左のマス(-5)の左端
    ///   画面 y=24 （盤面の上端）→ ワールド 6.25 - 24/32 = 5.5 ＝ 一番上のマス(+5)の上端
    /// </summary>
    public const float CameraSize = ScreenHeight / (float)TileSize / 2f;   // 6.25

    /// <summary>原典の画面の縦横比。</summary>
    public const float Aspect = ScreenWidth / (float)ScreenHeight;          // 1.6

    /// <summary>画面の左上をワールド座標の原点から見た位置に直す。</summary>
    public static Vector2 ScreenToWorld(Vector2 screen)
    {
        return new Vector2(screen.x / TileSize - CameraSize * Aspect,
                           CameraSize - screen.y / TileSize);
    }

    // ---- 画面の各欄（原典の塗り潰し 0xf5f:0x6e9 に渡している VRAM 変位から）----
    //   変位 = 行 * 80 + バイト列。横幅はワード単位で渡されている
    //   （盤面の 22 ワード = 352 ドットが実測と一致する）。

    /// <summary>盤面の領域。</summary>
    public static readonly RectInt MapArea = new RectInt(MapLeft, MapTop, MapWidth, MapHeight);

    /// <summary>左の帯。階と能力値の数字を出す欄（どれも 64x16、縦は24おき）。</summary>
    public static readonly RectInt FloorBox   = new RectInt(32, 48, 64, 16);
    public static readonly RectInt HpBox      = new RectInt(48, 80, 64, 16);
    public static readonly RectInt AttackBox  = new RectInt(48, 104, 64, 16);
    public static readonly RectInt DefenseBox = new RectInt(48, 128, 64, 16);
    public static readonly RectInt GoldBox    = new RectInt(48, 152, 64, 16);

    /// <summary>右の帯。装備の絵は 32x32、名前の欄は 64x16。</summary>
    public static readonly RectInt WeaponIcon = new RectInt(592, 48, 32, 32);
    public static readonly RectInt WeaponBox  = new RectInt(528, 64, 64, 16);
    public static readonly RectInt ShieldIcon = new RectInt(592, 96, 32, 32);
    public static readonly RectInt ShieldBox  = new RectInt(528, 112, 64, 16);

    /// <summary>右下の大きい欄。モンスターの情報とメッセージを兼ねる。</summary>
    public static readonly RectInt MessageBox = new RectInt(528, 224, 96, 128);

    /// <summary>
    /// 戦闘中に数を出す欄。原典の戦闘（0x62f4 / 0x639b）がここを塗って書き換える。
    /// 以前これを「鍵の本数」と読み違えていたが、鍵は数字ではなく絵で出る（下記）。
    /// </summary>
    public static readonly RectInt BattleNumberA = new RectInt(552, 296, 64, 16);
    public static readonly RectInt BattleNumberB = new RectInt(568, 296, 48, 16);

    // ---- 鍵の絵（原典 0x9ff8）----
    //
    // 鍵は数字ではなく **絵を本数ぶん並べて** 出す。0x9ff8 を読むとこうなっている:
    //   si = 0x42（バイト列66 → x=528）、[bp-6] = 0x90（144ドット）から始める
    //   1本ごとに si += 2（＝16ドット右）
    //   di が 6 になったら di=0、si を 0x42 に戻し、[bp-1] += 5（＝0x500 = 16行ぶん下）
    //   [bp-4]（それまでの行で描いた本数）+ di が 0x18 になったら打ち切る
    //   色の順は 赤(es:[0xa0]) → 紫(es:[0x9a]) → 黄(es:[0x1c]) で、
    //   **色が変わっても位置は続き**（di と行を持ち越す）、通しで 24 本まで。
    //
    // 絵は PICTURE.P58 の1〜3枚目。原典は絵の置き場の頭から**引いた**番地で指していて
    // （黄 -0x40c / 赤 -0x3cb / 紫 -0x381）、その差 0x41=65・0x4a=74 が
    // PICTURE.P58 の各絵の大きさとぴったり合う。属性表からは一度も指されない。

    /// <summary>鍵の絵の1本目の位置。</summary>
    public static readonly Vector2Int KeyIconOrigin = new Vector2Int(528, 144);

    /// <summary>鍵の絵の大きさ（ドット）。</summary>
    public const int KeyIconSize = 16;

    /// <summary>隣の鍵との間隔（横・縦とも16ドット）。</summary>
    public const int KeyIconStep = 16;

    /// <summary>1行に並べる本数。</summary>
    public const int KeyIconsPerRow = 6;

    /// <summary>出せる本数の上限（3色ぜんぶで）。</summary>
    public const int KeyIconMax = 24;

    public enum KeyColor { Red, Purple, Yellow }

    public struct KeyIcon
    {
        public KeyColor Color;
        public RectInt Rect;
    }

    /// <summary>
    /// 鍵の絵の並びを出す。赤→紫→黄の順に詰めて並べ、6本で折り返し、24本で打ち切る。
    /// </summary>
    public static System.Collections.Generic.List<KeyIcon> KeyIcons(int red, int purple, int yellow)
    {
        var list = new System.Collections.Generic.List<KeyIcon>();
        var counts = new int[] { red, purple, yellow };
        for (int c = 0; c < counts.Length; c++)
        {
            for (int i = 0; i < counts[c]; i++)
            {
                if (list.Count >= KeyIconMax) return list;
                int col = list.Count % KeyIconsPerRow, row = list.Count / KeyIconsPerRow;
                list.Add(new KeyIcon {
                    Color = (KeyColor)c,
                    Rect = new RectInt(KeyIconOrigin.x + col * KeyIconStep,
                                       KeyIconOrigin.y + row * KeyIconStep,
                                       KeyIconSize, KeyIconSize)
                });
            }
        }
        return list;
    }

    /// <summary>原典の矩形を Canvas の左上基準の位置に直す。</summary>
    public static Vector2 ToCanvas(RectInt r)
    {
        return new Vector2(r.xMin * DialogueWindow.CanvasScale,
                           -r.yMin * DialogueWindow.CanvasScale);
    }

    /// <summary>原典の矩形の大きさを Canvas の大きさに直す。</summary>
    public static Vector2 SizeOnCanvas(RectInt r)
    {
        return new Vector2(r.width * DialogueWindow.CanvasScale,
                           r.height * DialogueWindow.CanvasScale);
    }

    /// <summary>盤面が画面の中央に来ているか。</summary>
    public static bool MapIsCentered
    {
        get { return MapLeft * 2 + MapWidth == ScreenWidth; }
    }
}
