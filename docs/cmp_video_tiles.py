#!/usr/bin/env python3
"""動画から切り出したタイル（docs/original/video_tiles/）と、
P58 から起こしたタイル（Assets/Sprites/Tiles/）を見比べる。

**結論: これは検証には使えない**（63:15 に確かめた）。理由:

  ・動画側は実機画面を録ったものを切り出しているので**倍率が違う**。
    1画素ずつずらして一番合う位置を探しても、細かい絵では2〜4割が食い違ったまま
    （例: 黄色い鍵で 23%、赤い薬で 44%）。色そのものは合っていて、
    平らな所（床・マグマ・星空）なら 5〜15% まで下がる。
  ・そもそも動画側には**中身の無い切り出し**が混じっている。
    ID93 / ID133 / ID149 は壁の模様がそのまま入っていて、モンスターの絵ではない。
    （差し替え前の絵に穴があったということで、P58 から起こし直したのは正しかった）

つまり「一致しない＝復号がおかしい」ではない。P58 の復号とパレットの裏取りは
別の筋（属性表の変位が全件一致する／音程表と OPN の式が 220Hz を返す など）で取れている。
この道具は、気になったときに絵を見比べるためだけに残してある。

  使い方: python3 docs/cmp_video_tiles.py
"""
import glob, os, struct, zlib, sys

def read_png(path):
    d = open(path, 'rb').read()
    w, h = struct.unpack('>II', d[16:24])
    i, idat, bitdepth, ctype = 8, b'', 8, 6
    while i < len(d):
        ln = struct.unpack('>I', d[i:i+4])[0]
        tag = d[i+4:i+8]
        body = d[i+8:i+8+ln]
        if tag == b'IHDR':
            bitdepth, ctype = body[8], body[9]
        if tag == b'IDAT': idat += body
        i += 12 + ln
    raw = zlib.decompress(idat)
    ch = {0:1, 2:3, 3:1, 4:2, 6:4}[ctype]
    if bitdepth != 8: return None
    px, pos, prev = [], 0, bytearray(w*ch)
    for y in range(h):
        f = raw[pos]; pos += 1
        line = bytearray(raw[pos:pos+w*ch]); pos += w*ch
        for x in range(len(line)):
            a = line[x-ch] if x >= ch else 0
            b = prev[x]
            c = prev[x-ch] if x >= ch else 0
            if f == 1: line[x] = (line[x]+a) & 255
            elif f == 2: line[x] = (line[x]+b) & 255
            elif f == 3: line[x] = (line[x]+(a+b)//2) & 255
            elif f == 4:
                p = a+b-c
                pa, pb, pc = abs(p-a), abs(p-b), abs(p-c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x]+pr) & 255
        prev = line
        px.append([tuple(line[x*ch:x*ch+ch]) for x in range(w)])
    return px, ch

def rgb(p, ch):
    return p[:3] if ch >= 3 else (p[0],)*3

def main():
    same = diff = skip = 0
    worst = []
    for v in sorted(glob.glob('docs/original/video_tiles/*.png')):
        name = os.path.basename(v)
        a = f'Assets/Sprites/Tiles/{name}'
        if not os.path.exists(a): skip += 1; continue
        A, B = read_png(v), read_png(a)
        if A is None or B is None: skip += 1; continue
        pa, ca = A; pb, cb = B
        if len(pa) != len(pb) or len(pa[0]) != len(pb[0]): skip += 1; continue
        bad = 0; tot = 0
        for y in range(len(pa)):
            for x in range(len(pa[0])):
                qb = pb[y][x]
                if cb == 4 and qb[3] == 0: continue      # 透過にした所は比べない
                tot += 1
                if rgb(pa[y][x], ca) != rgb(qb, cb): bad += 1
        r = bad / max(1, tot)
        if r == 0: same += 1
        else:
            diff += 1
            worst.append((r, name, bad, tot))
    worst.sort(reverse=True)
    print(f"比べた: {same+diff}枚  ぴったり一致: {same}枚  ずれあり: {diff}枚  比較不能: {skip}枚")
    for r, n, bad, tot in worst[:12]:
        print(f"  {r*100:5.1f}%  {n}  ({bad}/{tot} 画素)")

main()
