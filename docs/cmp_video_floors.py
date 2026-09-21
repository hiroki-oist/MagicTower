#!/usr/bin/env python3
"""動画の1コマと、こちらの階データから組み立てた絵をマスごとに突き合わせる。

`docs/original/video_tiles/` の切り出しは**マスの位置がずれているもの**があって
色の判断に使えなかったが、**動画の1コマをそのまま切って11x11に割る**なら確実。

やること:
  1. `Floor_N.txt` の地形と物から、原典の絵（`Assets/Sprites/Tiles/`の元になる
     P58の色番号）で 352x352 の絵を組み立てる
  2. 動画の1コマの盤面（(256,44) から 635x633）を 11x11 のマスに割る
  3. マスごとに**真ん中16x16の平均色**を比べ、離れているマスを数える

動画は圧縮で色がにじむので、**どのマスが合っているか**を見るための道具で、
1画素単位の一致は見ない。階を見分けるには十分（別の階とは20マス以上ずれる）。

  使い方:
    python3 docs/cmp_video_floors.py <コマのPNG> <階>
    python3 docs/cmp_video_floors.py <コマのPNG> --best    # 50階から一番近いものを探す
"""
import os, re, sys, zlib, struct

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "docs"))
from p58 import GAME_PALETTE                                    # noqa: E402
import p58_to_tiles as T                                        # noqa: E402

MAP_X, MAP_Y, MAP_W, MAP_H = 256, 44, 635, 633                  # 動画の中の盤面

SYMBOL_ID = {".": 6, "#": 8, "%": 9, "~": 13, ">": 11, "<": 12, "*": 14,
             "y": 1, "b": 2, "r": 3, "s": 4, "x": 5}


def object_ids():
    out = {}
    for line in open(f"{ROOT}/Assets/Resources/ObjectIds.txt", encoding="utf-8"):
        if line.startswith("#") or "|" not in line:
            continue
        name, num = line.split("|")
        out.setdefault(name.strip(), int(num.strip()))
    return out


def floor_cells(n):
    """階の (x,y) → 物体ID。物があればそちら、無ければ地形。"""
    ids = object_ids()
    text = open(f"{ROOT}/Assets/Resources/Floors/Floor_{n}.txt", encoding="utf-8").read()
    cells = {}
    for line in text.split("\n"):
        m = re.match(r"\s*([+-]\d)\s*\|\s*(\S{11})\s*$", line)
        if m:
            y = int(m.group(1))
            for i, c in enumerate(m.group(2)):
                cells[(i - 5, y)] = SYMBOL_ID.get(c, 6)
    for line in text.split("\n"):
        m = re.match(r"\s*([+-]\d)\s+([+-]\d)\s+(\S+)\s+ID(\d+)", line)
        if m:
            cells[(int(m.group(1)), int(m.group(2)))] = int(m.group(4))
    return cells


def render(n, sp):
    """階を 352x352 の色番号の配列にする。物のマスは床を敷いてから重ねる。"""
    cells = floor_cells(n)
    img = [[0] * 352 for _ in range(352)]
    floor = sp[6]
    for (x, y), oid in cells.items():
        col, row = x + 5, 5 - y
        art = sp.get(oid)
        bg = T.transparent_index(T.kinds().get(oid))
        for yy in range(32):
            for xx in range(32):
                v = floor[yy][xx]
                if art is not None:
                    a = art[yy][xx]
                    # 背景の色が分かっているものだけ、そこを床にする
                    v = v if (bg is not None and a == bg) else a
                img[row * 32 + yy][col * 32 + xx] = v
    return img


def read_png(path):
    d = open(path, "rb").read()
    i, idat, pal = 8, b"", None
    while i < len(d):
        ln = struct.unpack(">I", d[i:i + 4])[0]
        k, b = d[i + 4:i + 8], d[i + 8:i + 8 + ln]
        i += 12 + ln
        if k == b"IHDR":
            w, h, bd, ct = struct.unpack(">IIBB", b[:10])
        elif k == b"PLTE":
            pal = b
        elif k == b"IDAT":
            idat += b
    raw = zlib.decompress(idat)
    bpp = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[ct]
    stride, out, prev, p = w * bpp, [], bytearray(w * bpp), 0
    for y in range(h):
        f = raw[p]; p += 1
        line = bytearray(raw[p:p + stride]); p += stride
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            bb = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if f == 1: line[x] = (line[x] + a) & 0xff
            elif f == 2: line[x] = (line[x] + bb) & 0xff
            elif f == 3: line[x] = (line[x] + (a + bb) // 2) & 0xff
            elif f == 4:
                pp = a + bb - c
                pa, pb, pc = abs(pp - a), abs(pp - bb), abs(pp - c)
                pr = a if (pa <= pb and pa <= pc) else (bb if pb <= pc else c)
                line[x] = (line[x] + pr) & 0xff
        out.append(bytes(line)); prev = line
    px = []
    for y in range(h):
        row = []
        for x in range(w):
            if ct == 3:
                v = out[y][x]; row.append(tuple(pal[v * 3:v * 3 + 3]))
            elif ct == 6:
                row.append(tuple(out[y][x * 4:x * 4 + 3]))
            else:
                row.append(tuple(out[y][x * 3:x * 3 + 3]))
        px.append(row)
    return w, h, px


# マスは**4x4に割って**それぞれの平均色を採る（1マスにつき16個）。
# 真ん中1点の平均だけだと、床と星空ばかりの階どうし（21〜25階など）が
# 見分けられなかった。4x4にすると全50階が互いに15マス以上ちがうようになる。
SUB = 4


def cell_means_frame(px, w, h):
    """コマの盤面を11x11に割り、さらに各マスを4x4に割って平均色を出す。"""
    out = {}
    for row in range(11):
        for col in range(11):
            sig = []
            for by in range(SUB):
                for bx in range(SUB):
                    y0 = MAP_Y + int((row + by / SUB) * MAP_H / 11)
                    y1 = MAP_Y + int((row + (by + 1) / SUB) * MAP_H / 11)
                    x0 = MAP_X + int((col + bx / SUB) * MAP_W / 11)
                    x1 = MAP_X + int((col + (bx + 1) / SUB) * MAP_W / 11)
                    r = g = b = n = 0
                    for y in range(y0, y1):
                        for x in range(x0, x1):
                            if 0 <= y < h and 0 <= x < w:
                                p = px[y][x]; r += p[0]; g += p[1]; b += p[2]; n += 1
                    n = max(n, 1)
                    sig.append((r // n, g // n, b // n))
            out[(col - 5, 5 - row)] = sig
    return out


def cell_means_render(img):
    out = {}
    step = 32 // SUB
    for row in range(11):
        for col in range(11):
            sig = []
            for by in range(SUB):
                for bx in range(SUB):
                    r = g = b = 0
                    for dy in range(step):
                        for dx in range(step):
                            p = GAME_PALETTE[img[row * 32 + by * step + dy]
                                                [col * 32 + bx * step + dx]]
                            r += p[0]; g += p[1]; b += p[2]
                    n = step * step
                    sig.append((r // n, g // n, b // n))
            out[(col - 5, 5 - row)] = sig
    return out


def compare(frame_means, n, sp, limit=60):
    mine = cell_means_render(render(n, sp))
    bad = []
    for key, got in frame_means.items():
        want = mine[key]
        d = max(max(abs(p[0] - q[0]), abs(p[1] - q[1]), abs(p[2] - q[2]))
                for p, q in zip(got, want))
        if d > limit:
            bad.append((key, d))
    return bad


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return
    path, which = sys.argv[1], sys.argv[2]
    w, h, px = read_png(path)
    fm = cell_means_frame(px, w, h)
    sp = T.sprites()
    if which == "--best":
        scores = sorted((len(compare(fm, n, sp)), n) for n in range(1, 51))
        for cnt, n in scores[:5]:
            print(f"  {n:2d}階 … 離れたマス {cnt}/121")
        return
    n = int(which)
    bad = compare(fm, n, sp)
    print(f"{n}階: 離れたマス {len(bad)}/121")
    for (x, y), d in sorted(bad)[:20]:
        print(f"   ({x:+d},{y:+d}) 差 {d}")


if __name__ == "__main__":
    main()
