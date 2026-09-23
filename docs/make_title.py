#!/usr/bin/env python3
"""題名の看板を原典の MTMOJI.P58 から切り出す。Unity不要。

`Title_Text.png`（MTMOJI 全体）は石積みの枠つきの1枚絵で、原典のどこから
呼ばれているかは分かっていない（使われないまま残っている絵）。そこから
**「魔法の塔」の看板の部分だけ**を抜いて、起動時の題名画面に使う。

  Title_Logo.png … 「魔法の塔」と「Tower of the Sorcerer (C) 1995 by N.W」
                    茶色の字の外接矩形（x 173..460 / y 91..183）に余白を足した範囲。
  Title_Tall.png … 同じ絵を**縦長の画面に合わせて縦に伸ばした**もの。
                    枠は封印扉(ID4)のタイルでできていて、y=256..288 と y=288..320 が
                    **1ドットも違わず同じ**（＝縦に繰り返せる帯）。その帯を挟んで
                    部屋を下へ伸ばし、題名を上に置いたまま画面いっぱいにする。

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

# 縦に伸ばすときの継ぎ目。上は 0..288、繰り返す帯は 256..288、下は 288..400。
TALL_TOP, TALL_BAND, TALL_BOTTOM = 288, (256, 288), 288
# 繰り返す回数。640x1424（縦横比 0.449）になり、いちばん縦長の端末でも足りる
# （iPhone 17 の縦画面が 0.46）。
TALL_REPEAT = 32


def main():
    px = to_indices(*decode_all(open(os.path.join(DISK, "MTMOJI.P58"), "rb").read())[0])
    x, y, w, h = CROP
    crop = [row[x:x + w] for row in px[y:y + h]]
    # 地の色（枠の中の濃い灰）は抜いて、字だけが残るようにする
    bg = crop[0][0]
    mask = [[c != bg for c in row] for row in crop]
    save("Title_Logo.png", crop, mask)
    print(f"Title_Logo.png ({w}x{h})")

    # 縦長の画面ぶん、部屋を下へ伸ばす
    b0, b1 = TALL_BAND
    band = px[b0:b1]
    assert px[b0:b1] == px[b1:b1 + (b1 - b0)], "繰り返せる帯になっていない"
    tall = px[:TALL_TOP] + band * TALL_REPEAT + px[TALL_BOTTOM:]
    save("Title_Tall.png", tall, None)
    print(f"Title_Tall.png ({len(tall[0])}x{len(tall)})")
    print(f"→ {OUT}")


def save(name, px, mask):
    path = os.path.join(OUT, name)
    write_rgba(path, px, mask)
    if not os.path.exists(path + ".meta"):
        guid = hashlib.md5(("MagicTower/screen/" + name).encode()).hexdigest()
        open(path + ".meta", "w").write(TILE_META.format(guid=guid))


if __name__ == "__main__":
    main()
