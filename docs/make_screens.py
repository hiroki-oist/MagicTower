#!/usr/bin/env python3
"""画面いっぱいの1枚絵を原典の *.P58 から書き出す。Unity不要。

`make_prologue.py`（序章の4枚）と `make_ending.py`（終わりの言葉の5枚）が
受け持たない**残りの3枚**をここで作る。作り直しの手順は `docs/rebuild_assets.sh`。

  Frame.png        … LCG.P58 の1枚目（640x400）＝ 画面の枠。
                     中央 (144,24)-(496,376) は盤面が入る穴なので**透明に抜く**。
  Ending_Tower2.png… MTOWER2.P58（終幕の塔。濃いほう）
  Ending_Art.png   … MTFIN.P58（終幕の1枚絵）
"""
import sys, os, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode_all, to_indices
from p58_to_tiles import write_rgba, TILE_META

ROOT = os.path.expanduser("~/Code/MagicTower")
DISK = f"{ROOT}/docs/original/disk"
OUT = f"{ROOT}/Assets/Sprites/Screens"

# 盤面が入る穴（原典の枠の真ん中）。ScreenLayout.MapOrigin と同じ。
HOLE = (144, 24, 496, 376)


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
    made.append(("Frame.png", px, mask))

    # 終幕の塔（濃いほう）と、終幕の1枚絵
    made.append(("Ending_Tower2.png", to_indices(*images("MTOWER2.P58")[0]), None))
    made.append(("Ending_Art.png", to_indices(*images("MTFIN.P58")[0]), None))

    for name, px, mask in made:
        path = os.path.join(OUT, name)
        write_rgba(path, px, mask)
        if not os.path.exists(path + ".meta"):
            guid = hashlib.md5(f"MagicTower/screen/{name}".encode()).hexdigest()
            open(path + ".meta", "w").write(TILE_META.format(guid=guid))
    print(f"{len(made)} 枚を書き出した → {OUT}")


if __name__ == "__main__":
    main()
