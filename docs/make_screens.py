#!/usr/bin/env python3
"""画面いっぱいの1枚絵を原典の *.P58 から書き出す。Unity不要。

`make_prologue.py`（序章の4枚）と `make_ending.py`（終わりの言葉の5枚）が
受け持たない**残りの3枚**をここで作る。作り直しの手順は `docs/rebuild_assets.sh`。

  Frame.png        … LCG.P58 の1枚目（640x400）＝ 画面の枠。
                     中央 (144,24)-(496,376) は盤面が入る穴なので**透明に抜く**。
  Hud/Icon_*.png   … その枠の左の欄に描いてある小さな絵（心臓・剣・盾・金貨）の切り出し。
                     スマホ縦画面の上の帯で使う（作者の指定でUIを組み直したときに要る）。
  Hud/Brick.png    … 同じ枠の左の欄の**石積みの地**。上下の帯の背景に敷く。
  Ending_Tower1.png… MTOWER1.P58（終幕の**塔**。320x400。索引0の周りは透かす）
  Ending_Tower2.png… MTOWER2.P58（終幕の**荒野**。640x400。索引13の空は透かす）
  Ending_Art.png   … MTFIN.P58（終幕の1枚絵）

終幕の2枚だけは**終幕のパレット**（原典 0xc4d8 がスタックに積む表の最後の1本
`0002 1004 2226 3448 466a 588c 6fff 7fff …`）で色を付ける。本編のパレット（0x908）
で塗ると茶色い荒野になってしまうが、**原典の終幕は青い夜の絵**（動画 Part2 t≈1790）。

塔は荒野の**後ろ**を下へ滑っていく（塔が沈んで消える＝「虚栄の塔は崩れさる」）ので、
　　荒野の空（索引13）を透かして前に重ね、塔の周り（索引0）も透かす。
"""
import sys, os, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode_all, to_indices
from p58_to_tiles import write_rgba, TILE_META

ROOT = os.path.expanduser("~/Code/MagicTower")
DISK = f"{ROOT}/docs/original/disk"
OUT = f"{ROOT}/Assets/Sprites/Screens"
HUD = f"{ROOT}/Assets/Sprites/Hud"

# 枠の左の欄から切り出すもの（原典の枠の中の位置。20x20 と 32x32）。
# 心臓・剣・盾・金貨は縦に 24 ドット間隔で並んでいる。
HUD_ICONS = [("Icon_Hp.png", 14, 78, 20, 20),
             ("Icon_Attack.png", 14, 102, 20, 20),
             ("Icon_Defense.png", 14, 126, 20, 20),
             ("Icon_Gold.png", 14, 150, 20, 20),
             ("Brick.png", 0, 356, 32, 32)]

# 盤面が入る穴（原典の枠の真ん中）。ScreenLayout.MapOrigin と同じ。
HOLE = (144, 24, 496, 376)

# 終幕のパレット（原典 0xc4d8 の表の最後の1本＝ふだんの色）。1語は NGRB。
ENDING_PALETTE_WORDS = [0x0002, 0x1004, 0x2226, 0x3448, 0x466a, 0x588c, 0x6fff, 0x7fff,
                        0x8fff, 0x9fff, 0xafff, 0xbfff, 0xcfff, 0xdfff, 0xefff, 0xffff]
ENDING_PALETTE = [(((w >> 4) & 0xf) * 17, ((w >> 8) & 0xf) * 17, (w & 0xf) * 17)
                  for w in ENDING_PALETTE_WORDS]


def hide(px, index):
    """その索引のマスだけ透かす当て紙を作る。"""
    return [[v == index for v in row] for row in px]


def images(name):
    return decode_all(open(f"{DISK}/{name}", "rb").read())


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []

    # 枠。中央の穴を抜く
    px = to_indices(*images("LCG.P58")[0])
    h, w = len(px), len(px[0])
    x0, y0, x1, y1 = HOLE
    mask = [[x0 <= x < x1 and y0 <= y < y1 for x in range(w)] for y in range(h)]
    made.append(("Frame.png", px, mask, None))

    # 終幕の塔と荒野。透かす索引と終幕のパレットは上の説明のとおり
    tower = to_indices(*images("MTOWER1.P58")[0])
    ground = to_indices(*images("MTOWER2.P58")[0])
    made.append(("Ending_Tower1.png", tower, hide(tower, 0), ENDING_PALETTE))
    made.append(("Ending_Tower2.png", ground, hide(ground, 13), ENDING_PALETTE))
    made.append(("Ending_Art.png", to_indices(*images("MTFIN.P58")[0]), None, None))

    # 上下の帯で使う切り出し（枠と同じ絵から取るので、原典から作り直せる）
    os.makedirs(HUD, exist_ok=True)
    for name, x, y, w2, h2 in HUD_ICONS:
        crop = [row[x:x + w2] for row in px[y:y + h2]]
        path = os.path.join(HUD, name)
        write_rgba(path, crop, None)
        if not os.path.exists(path + ".meta"):
            guid = hashlib.md5(f"MagicTower/hud/{name}".encode()).hexdigest()
            meta = TILE_META.format(guid=guid)
            # 石積みは Image の「敷き詰め」で使うので、切り抜きを四角のまま取り込む
            # （Tight のままだと敷き詰めに使えないと Unity に叱られる）。
            if name == "Brick.png":
                meta = meta.replace("spriteMeshType: 1", "spriteMeshType: 0")
            open(path + ".meta", "w").write(meta)

    for name, px, mask, pal in made:
        path = os.path.join(OUT, name)
        write_rgba(path, px, mask, pal)
        if not os.path.exists(path + ".meta"):
            guid = hashlib.md5(f"MagicTower/screen/{name}".encode()).hexdigest()
            open(path + ".meta", "w").write(TILE_META.format(guid=guid))
    print(f"{len(made)} 枚を書き出した → {OUT}")
    print(f"{len(HUD_ICONS)} 枚を書き出した → {HUD}")


if __name__ == "__main__":
    main()
