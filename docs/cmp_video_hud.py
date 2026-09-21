#!/usr/bin/env python3
"""動画のコマを枠の絵と見比べて、**画面のどこが描き替わるか**を洗い出す。

盤面の外（左右の帯）で `Assets/Sprites/Screens/Frame.png` と違っている所は、
ゲームがそこに何かを描いているということ。塗り潰しの数え上げ
（`docs/dump_fills.py`）は**箱ごと塗る所しか拾えない**ので、
持ち物の欄のように1マスずつ描き替える所を取りこぼしていた。こちらはその穴を塞ぐ。

  使い方: python3 docs/cmp_video_hud.py <640x400に直したコマ> ...
"""
import os, sys, zlib, struct

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "docs"))
import cmp_video_floors as C                                  # noqa: E402

BLOCK = 8
MAP = (144, 24, 352, 352)

# 分かっている欄。ここに入らない所が出たら、まだ作っていないものがある。
KNOWN = [(32, 48, 64, 16, "階"), (48, 80, 64, 16, "生命力"),
         (48, 104, 64, 16, "攻撃力"), (48, 128, 64, 16, "防御力"),
         (48, 152, 64, 16, "所持金"), (16, 192, 96, 160, "持ち物"),
         (528, 64, 64, 16, "武器の名前"), (528, 112, 64, 16, "盾の名前"),
         (592, 48, 32, 32, "武器の絵"), (592, 96, 32, 32, "盾の絵"),
         (528, 144, 96, 64, "鍵"), (528, 224, 96, 128, "情報欄")]


def read_rgba(path):
    d = open(path, "rb").read()
    i, idat = 8, b""
    while i < len(d):
        ln = struct.unpack(">I", d[i:i + 4])[0]
        k, b = d[i + 4:i + 8], d[i + 8:i + 8 + ln]
        i += 12 + ln
        if k == b"IHDR":
            w, h, bd, ct = struct.unpack(">IIBB", b[:10])
        elif k == b"IDAT":
            idat += b
    raw = zlib.decompress(idat)
    stride, out, prev, p = w * 4, [], bytearray(w * 4), 0
    for y in range(h):
        f = raw[p]; p += 1
        line = bytearray(raw[p:p + stride]); p += stride
        for x in range(stride):
            a = line[x - 4] if x >= 4 else 0
            bb = prev[x]
            c = prev[x - 4] if x >= 4 else 0
            if f == 1: line[x] = (line[x] + a) & 0xff
            elif f == 2: line[x] = (line[x] + bb) & 0xff
            elif f == 3: line[x] = (line[x] + (a + bb) // 2) & 0xff
            elif f == 4:
                pp = a + bb - c
                pa, pb, pc = abs(pp - a), abs(pp - bb), abs(pp - c)
                pr = a if (pa <= pb and pa <= pc) else (bb if pb <= pc else c)
                line[x] = (line[x] + pr) & 0xff
        out.append(bytes(line)); prev = line
    return w, h, out


def blocks(paths, limit=35):
    fw, fh, frame = read_rgba(f"{ROOT}/Assets/Sprites/Screens/Frame.png")
    found = set()
    for path in paths:
        w, h, px = C.read_png(path)
        for by in range(0, 400, BLOCK):
            for bx in range(0, 640, BLOCK):
                if MAP[0] <= bx < MAP[0] + MAP[2] and MAP[1] <= by < MAP[1] + MAP[3]:
                    continue
                s = n = 0
                for y in range(by, min(by + BLOCK, 400)):
                    for x in range(bx, min(bx + BLOCK, 640)):
                        if frame[y][x * 4 + 3] == 0:
                            continue
                        f3 = (frame[y][x * 4], frame[y][x * 4 + 1], frame[y][x * 4 + 2])
                        v3 = px[y][x]
                        s += max(abs(f3[0] - v3[0]), abs(f3[1] - v3[1]), abs(f3[2] - v3[2]))
                        n += 1
                if n and s / n > limit:
                    found.add((bx, by))
    return found


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return
    found = blocks(sys.argv[1:])
    unknown = []
    for bx, by in sorted(found):
        hit = [k[4] for k in KNOWN
               if bx < k[0] + k[2] and k[0] < bx + BLOCK
               and by < k[1] + k[3] and k[1] < by + BLOCK]
        if not hit:
            unknown.append((bx, by))
    print(f"枠の絵と違う 8x8 の塊 {len(found)}、知らない場所 {len(unknown)}")
    for bx, by in unknown[:20]:
        print(f"  ★ ({bx},{by})")


if __name__ == "__main__":
    main()
