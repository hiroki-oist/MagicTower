#!/usr/bin/env python3
"""GIF を読む（87a/89a の1枚目だけ）。外部の道具は使わない。

Java版（2001年の公式移植）の終幕の絵 `data2/edata01..18.gif` を取り込むために要る。
Unity は .gif をそのままスプライトにできないので、ここで索引と色板に開いて
PNG へ書き直す（`docs/make_ending_java.py`）。
"""
import struct


def read(path):
    """(幅, 高さ, 色板[(r,g,b)...], 索引の2次元配列, 透かす索引 or None)"""
    d = open(path, 'rb').read()
    if d[:3] != b'GIF':
        raise ValueError(f'{path}: GIF ではない')
    w, h, flags, _bg, _ar = struct.unpack('<HHBBB', d[6:13])
    p = 13
    gct = []
    if flags & 0x80:
        n = 2 << (flags & 7)
        gct = [tuple(d[p + i * 3:p + i * 3 + 3]) for i in range(n)]
        p += n * 3
    transparent = None
    while p < len(d):
        b = d[p]
        if b == 0x21:                      # 拡張ブロック
            label = d[p + 1]
            p += 2
            if label == 0xf9:              # 図形制御（透明色）
                size = d[p]
                block = d[p + 1:p + 1 + size]
                if block[0] & 1:
                    transparent = block[3]
            while d[p]:
                p += 1 + d[p]
            p += 1
            continue
        if b == 0x2c:                      # 画像ブロック
            left, top, iw, ih, iflags = struct.unpack('<HHHHB', d[p + 1:p + 10])
            p += 10
            table = gct
            if iflags & 0x80:
                n = 2 << (iflags & 7)
                table = [tuple(d[p + i * 3:p + i * 3 + 3]) for i in range(n)]
                p += n * 3
            interlaced = bool(iflags & 0x40)
            min_code = d[p]
            p += 1
            data = bytearray()
            while d[p]:
                size = d[p]
                data += d[p + 1:p + 1 + size]
                p += 1 + size
            p += 1
            px = _lzw(bytes(data), min_code, iw * ih)
            rows = [px[y * iw:(y + 1) * iw] for y in range(ih)]
            if interlaced:
                rows = _deinterlace(rows, ih)
            # 画面いっぱいでない絵は左上に置く（終幕の絵はどれも全画面）
            if (left, top, iw, ih) != (0, 0, w, h):
                full = [[0] * w for _ in range(h)]
                for y, row in enumerate(rows):
                    full[top + y][left:left + iw] = row
                rows = full
            return w, h, table, rows, transparent
        if b == 0x3b:
            break
        raise ValueError(f'{path}: 知らないブロック 0x{b:02x}')
    raise ValueError(f'{path}: 画像ブロックが無い')


def _deinterlace(rows, h):
    out = [None] * h
    src = iter(rows)
    for start, step in ((0, 8), (4, 8), (2, 4), (1, 2)):
        for y in range(start, h, step):
            out[y] = next(src)
    return out


def _lzw(data, min_code, count):
    clear = 1 << min_code
    end = clear + 1
    out = []
    dic = None
    code_size = min_code + 1
    prev = None
    bits = 0
    acc = 0
    pos = 0
    while len(out) < count and pos <= len(data):
        while bits < code_size and pos < len(data):
            acc |= data[pos] << bits
            bits += 8
            pos += 1
        if bits < code_size:
            break
        code = acc & ((1 << code_size) - 1)
        acc >>= code_size
        bits -= code_size
        if code == clear:
            dic = [bytes([i]) for i in range(clear)] + [b'', b'']
            code_size = min_code + 1
            prev = None
            continue
        if code == end:
            break
        if dic is None:
            dic = [bytes([i]) for i in range(clear)] + [b'', b'']
        if code < len(dic):
            entry = dic[code]
        elif prev is not None:
            entry = prev + prev[:1]
        else:
            break
        out.extend(entry)
        if prev is not None:
            dic.append(prev + entry[:1])
            if len(dic) == (1 << code_size) and code_size < 12:
                code_size += 1
        prev = entry
    out = out[:count] + [0] * max(0, count - len(out))
    return out
