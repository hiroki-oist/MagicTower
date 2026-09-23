#!/usr/bin/env python3
"""題名の看板を原典の MTMOJI.P58 から切り出す。Unity不要。

`Title_Text.png`（MTMOJI 全体）は石積みの枠つきの1枚絵で、原典のどこから
呼ばれているかは分かっていない（使われないまま残っている絵）。そこから
**「魔法の塔」の看板の部分だけ**を抜いて、起動時の題名画面に使う。

  Title_Logo.png … 「魔法の塔」と「Tower of the Sorcerer (C) 1995 by N.W」
                    茶色の字の外接矩形（x 173..460 / y 91..183）に余白を足した範囲。

  python3 docs/make_title.py
"""
import sys, os, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode_all, to_indices
from p58_to_tiles import write_rgba, TILE_META

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DISK = os.path.join(ROOT, "docs/original/disk")
OUT = os.path.join(ROOT, "Assets/Sprites/Screens")

# 切り出す範囲（左, 上, 幅, 高さ）。茶色の字の外接矩形に少し余白を足したもの。
CROP = (166, 84, 300, 104)


def main():
    px = to_indices(*decode_all(open(os.path.join(DISK, "MTMOJI.P58"), "rb").read())[0])
    x, y, w, h = CROP
    crop = [row[x:x + w] for row in px[y:y + h]]
    # 地の色（枠の中の濃い灰）は抜いて、字だけが残るようにする
    bg = crop[0][0]
    mask = [[c != bg for c in row] for row in crop]
    path = os.path.join(OUT, "Title_Logo.png")
    write_rgba(path, crop, mask)
    if not os.path.exists(path + ".meta"):
        guid = hashlib.md5(b"MagicTower/screen/Title_Logo.png").hexdigest()
        open(path + ".meta", "w").write(TILE_META.format(guid=guid))
    print(f"Title_Logo.png ({w}x{h}) を書き出した → {OUT}")


if __name__ == "__main__":
    main()
