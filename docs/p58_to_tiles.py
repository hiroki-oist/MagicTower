#!/usr/bin/env python3
"""原典の *.P58 を Unity の Assets/Sprites/Tiles/*.png へ差し替える。

ファイル名の先頭3桁が物体IDなので、docs/original/p58_map.txt の対応で絵を選ぶ。
.meta（GUID）はそのままなので、プレハブの参照を書き換える必要はない。
差し替え前の動画切り出しは docs/original/video_tiles/ に控えてある。

背景は透過にする。原典は物体の絵のまわりを「索引1（茶）」または「索引8（淡緑）」で
埋めていて、動画ではそこに下地が見えている（索引8を茶にすると一致度が上がることで確認）。
縁からつながっている同じ索引のところだけを透過にするので、絵の中の同色は残る。
"""
import sys, os, glob, zlib, struct
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58 import decode_all, to_indices, GAME_PALETTE

ROOT = os.path.expanduser("~/Code/MagicTower")

def sprites():
    """物体ID → 32x32 の色番号配列。対応は原典の属性表から作った p58_map.txt に従う。"""
    imgs = {}
    for name in ("GROUND", "OBJECT", "ITEM", "WEAPON", "MONSTER"):
        imgs[name] = [to_indices(*im)
                      for im in decode_all(open(f"{ROOT}/docs/original/disk/{name}.P58", 'rb').read())]
    out = {}
    for line in open(f"{ROOT}/docs/original/p58_map.txt", encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        oid, kind, where = [c.strip() for c in line.split("|")]
        if where == "-":
            continue
        f, k = where.rsplit("_", 1)
        out[int(oid)] = imgs[f][int(k)]
    return out


def kinds():
    """物体ID → 原典の種別。8/10 が敵の1コマ目、9/11 が2コマ目。"""
    out = {}
    for line in open(f"{ROOT}/docs/original/p58_map.txt", encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        oid, kind, _ = [c.strip() for c in line.split("|")]
        out[int(oid)] = int(kind)
    return out


def transparent_border(px):
    """縁からつながっている背景（索引1か8）を透過にする。"""
    h, w = len(px), len(px[0])
    bg = px[0][0]
    if bg not in (1, 8):
        return None
    n = sum(1 for r in px for v in r if v == bg)
    if n < h * w * 0.2:
        return None
    seen = [[False] * w for _ in range(h)]
    stack = [(0, x) for x in range(w)] + [(h - 1, x) for x in range(w)] \
          + [(y, 0) for y in range(h)] + [(y, w - 1) for y in range(h)]
    while stack:
        y, x = stack.pop()
        if not (0 <= y < h and 0 <= x < w) or seen[y][x] or px[y][x] != bg:
            continue
        seen[y][x] = True
        stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    return seen


def write_rgba(path, px, mask):
    rows = []
    for y, row in enumerate(px):
        line = bytearray()
        for x, v in enumerate(row):
            r, g, b = GAME_PALETTE[v]
            a = 0 if (mask and mask[y][x]) else 255
            line += bytes((r, g, b, a))
        rows.append(bytes(line))
    raw = b''.join(b'\x00' + r for r in rows)

    def chunk(kind, body):
        return (struct.pack('>I', len(body)) + kind + body
                + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff))

    out = b'\x89PNG\r\n\x1a\n'
    out += chunk(b'IHDR', struct.pack('>IIBBBBB', len(px[0]), len(px), 8, 6, 0, 0, 0))
    out += chunk(b'IDAT', zlib.compress(raw, 9))
    out += chunk(b'IEND', b'')
    open(path, 'wb').write(out)


COMPOSITE = {900: (115, 3, 3), 901: (97, 3, 3), 902: (15, 3, 1)}


def composite(sp, base, cols, rows):
    """3x3 のボスなどを1枚に組み立てる。IDは左上から横に並んでいる。"""
    out = [[0] * (32 * cols) for _ in range(32 * rows)]
    for r in range(rows):
        for c in range(cols):
            src = sp[base + r * cols + c]
            for y in range(32):
                for x in range(32):
                    out[r * 32 + y][c * 32 + x] = src[y][x]
    return out


def main():
    sp = sprites()
    done = skipped = 0
    for p in sorted(glob.glob(f"{ROOT}/Assets/Sprites/Tiles/*.png")):
        name = os.path.basename(p)[:-4]
        oid = int(name[:3])
        if oid in COMPOSITE:
            base, cols, rows = COMPOSITE[oid]
            px = composite(sp, base, cols, rows)
        elif oid in sp:
            px = sp[oid]
        else:
            skipped += 1
            continue
        write_rgba(p, px, transparent_border(px))
        done += 1
    # 2コマ目（敵と人の偶数ID）も書き出す。ただし**原典のこのビルドは一度も描かない**ので
    # Assets ではなく docs/original/frame2/ に置く（絵の位置を引く箇所14件を全部調べ、
    # IDに1を足したり属性表の次のレコードを引いたりする経路が無いことを確認済み）。
    ks = kinds()
    frames = 0
    for p in sorted(glob.glob(f"{ROOT}/Assets/Sprites/Tiles/*.png")):
        name = os.path.basename(p)[:-4]
        if name.endswith("_b"):
            continue
        oid = int(name[:3])
        if ks.get(oid) not in (7, 8, 10):
            continue
        nxt = oid + 1
        if ks.get(nxt) not in (7, 9, 11) or nxt not in sp:
            continue
        px = sp[nxt]
        out = f"{ROOT}/docs/original/frame2/{nxt:03d}_{name[4:]}_b.png"
        write_rgba(out, px, transparent_border(px))
        frames += 1
    print(f"差し替え {done} 枚 / 対応なし {skipped} 枚 / 2コマ目 {frames} 枚")


if __name__ == "__main__":
    main()
