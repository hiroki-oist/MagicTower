#!/usr/bin/env python3
"""終わりの言葉（MTENDMJ）を、段落が1つずつ現れる5枚に書き出す。

MTENDMJ は **白（索引15）と黒（索引0）の2色だけ**の全画面の絵で、
中身は4つの段落（黒い行のかたまりは7つ）:

    段落1  行 59〜 82   ながい戦いは終わりを告げ、虚栄の塔は崩れさる。
    段落2  行109〜164   神々は天へと帰り、／戦士は大地へと戻った。
    段落3  行192〜246   現在は過去へと移り変わり、／この旅も過去のものになろうとしている。
    段落4  行272〜326   あなたは、少しだけ振り返ると、／新たな旅へ向けて足を踏み出すのであった。

動画（Part2 t≈1810〜1825）では**段落ごとに現れる**ので、
その段までを残して下を白で消した5枚を書き出す。

  使い方: python3 docs/make_ending.py
"""
import os, sys, zlib, struct

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "docs"))
from p58 import decode_all, to_indices, GAME_PALETTE            # noqa: E402

# 段落の終わりの行（ここまでを残す）
ENDS = [0, 83, 165, 247, 400]


def write(path, px, keep_rows):
    rows = []
    for y, row in enumerate(px):
        line = bytearray()
        for v in row:
            r, g, b = GAME_PALETTE[15 if y >= keep_rows else v]
            line += bytes((r, g, b))
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
    data = open(f"{ROOT}/docs/original/disk/MTENDMJ.P58", "rb").read()
    px = to_indices(*decode_all(data)[0])
    for i, keep in enumerate(ENDS):
        out = f"{ROOT}/Assets/Sprites/Screens/Ending_Text_{i}.png"
        write(out, px, keep)
        print(f"書き出した {os.path.basename(out)}")


if __name__ == "__main__":
    main()
