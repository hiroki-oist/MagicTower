#!/usr/bin/env python3
"""序章の1枚絵（MTOPSTR）を、行が1つずつ現れる4枚に書き出す。

原典は**絵の中に3行とも描いてある**。ただし行ごとに色の索引が違っていて、

    1行目「時は来たれり・・・」            索引1   行122〜149
    2行目「我、全能の力を用いて、…」      索引2   行164〜195
    3行目「偉大なる知恵を手に入れん・・・」索引3   行212〜243

最初はこの3つの索引を**下地と同じ色**にしておき、`lcall 0xf5f,0x461` で
パレットを差し替えて1つずつ白にする。それで行が順に現れる（動画 Part1 t≈34〜41）。

Unity で絵のパレットを差し替えるのは面倒なので、**色を変えた4枚**を書き出しておく:

    Prologue_0.png … まだ1行も出ていない
    Prologue_1.png … 1行目まで
    Prologue_2.png … 2行目まで
    Prologue_3.png … 3行とも

  使い方: python3 docs/make_prologue.py
"""
import os, sys, zlib, struct

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "docs"))
from p58 import decode_all, to_indices, GAME_PALETTE            # noqa: E402

BACK = (68, 68, 68)        # 下地（索引0）
SHOWN = (255, 255, 255)    # 出ている行の色
LINES = (1, 2, 3)          # 行ごとの色の索引


def write(path, px, palette):
    rows = []
    for row in px:
        line = bytearray()
        for v in row:
            line += bytes(palette(v))
        rows.append(bytes(line))
    raw = b"".join(b"\x00" + r for r in rows)

    def chunk(kind, body):
        return (struct.pack(">I", len(body)) + kind + body
                + struct.pack(">I", zlib.crc32(kind + body) & 0xffffffff))

    out = b"\x89PNG\r\n\x1a\n"
    out += chunk(b"IHDR", struct.pack(">IIBBBBB", len(px[0]), len(px), 8, 2, 0, 0, 0))
    out += chunk(b"IDAT", zlib.compress(raw, 9))
    out += chunk(b"IEND", b"")
    open(path, "wb").write(out)


def main():
    data = open(f"{ROOT}/docs/original/disk/MTOPSTR.P58", "rb").read()
    px = to_indices(*decode_all(data)[0])
    for shown in range(4):
        def palette(v, shown=shown):
            if v in LINES:
                return SHOWN if v <= shown else BACK
            return GAME_PALETTE[v]
        out = f"{ROOT}/Assets/Sprites/Screens/Prologue_{shown}.png"
        write(out, px, palette)
        print(f"書き出した {os.path.basename(out)}")


if __name__ == "__main__":
    main()
