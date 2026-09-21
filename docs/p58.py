#!/usr/bin/env python3
"""PC98版の画像ファイル *.P58 を復号する。

原典の展開ルーチンは グラフィックライブラリ側の 0xf737（= lcall 0xf5f:0x147）。
  ヘッダ = マーカー1バイト分を含む word（"XX"）／行バイト数 word（<=80）／高さ word（<=400）
  そのあと **4面ぶん**を続けて展開する。書き込み先は VRAM なので
  1バイト書くたび di += 0x4f、すなわち **列方向（上から下）に進み、
  1列終わると次の列へ**という並びになっている。

  マーカー（ファイル先頭の 2バイト目 = 0x58）が出たら次の1バイトが命令:
    0x00        次の1バイト = 個数、0x00 を並べる
    0x01        次の2バイト = 個数、0x00 を並べる
    0x02        次の1バイト = 個数、0xff を並べる
    0x03        次の2バイト = 個数、0xff を並べる
    0x04〜0x27  n = 命令>>1。奇数なら個数は2バイト、偶数なら1バイト。
                続く n バイトを模様として読み、個数ぶん繰り返して並べる
    0x28〜0x40  別の面の同じ位置から写す。命令で面と「そのまま/反転」と個数の幅が決まる
                  0x28〜0x32 面0 そのまま(1バイト個数) / 0x33 面0 そのまま(2バイト個数)
                  0x34      面0 反転(1バイト個数)     / 0x35 面0 反転(2バイト個数)
                  0x36,0x37,0x38,0x39 面1 の同じ4通り
                  0x3a,0x3b,0x3c,0x3d 面2 の同じ4通り
    0x63        0x58 そのもの（マーカーの逃がし）
    それ以外    その命令バイトをそのまま1つ並べる
  マーカー以外のバイトはそのまま1つ並べる。
"""
import struct, sys, os, glob


def decode(data, start=0):
    """P58 の1枚を (面4枚, 行バイト数, 高さ, 次の位置) にする。面は [高さ][行バイト数]。"""
    marker = data[start + 1]
    cols, rows = struct.unpack('<HH', data[start + 2:start + 6])
    if not (1 <= cols <= 80 and 1 <= rows <= 400):
        raise ValueError(f"大きさが変: {cols}x{rows}")

    planes = []
    i = start + 6
    for _ in range(4):
        buf = [[0] * cols for _ in range(rows)]
        c = r = 0

        def emit(v):
            nonlocal c, r
            if c >= cols:
                return False
            buf[r][c] = v
            r += 1
            if r >= rows:
                r = 0
                c += 1
            return c < cols

        while c < cols and i < len(data):
            b = data[i]; i += 1
            if b != marker:
                emit(b); continue
            op = data[i]; i += 1

            if op in (0x00, 0x01, 0x02, 0x03):
                if op & 1:
                    n = struct.unpack('<H', data[i:i + 2])[0]; i += 2
                else:
                    n = data[i]; i += 1
                v = 0x00 if op < 2 else 0xff
                for _ in range(n):
                    if not emit(v): break

            elif 0x04 <= op <= 0x27:
                m = op >> 1
                if op & 1:
                    n = struct.unpack('<H', data[i:i + 2])[0]; i += 2
                else:
                    n = data[i]; i += 1
                pat = data[i:i + m]; i += m
                for k in range(n):
                    if not emit(pat[k % m]): break

            elif 0x28 <= op <= 0x40:
                src = 0 if op <= 0x35 else (1 if op <= 0x39 else 2)
                base = 0x28 if src == 0 else (0x36 if src == 1 else 0x3a)
                # base..base+? : そのまま / 反転、個数の幅
                if src == 0:
                    invert = op >= 0x34
                    wide = op in (0x33, 0x35)
                else:
                    invert = op in (base + 2, base + 3)
                    wide = op in (base + 1, base + 3)
                if wide:
                    n = struct.unpack('<H', data[i:i + 2])[0]; i += 2
                else:
                    n = data[i]; i += 1
                for _ in range(n):
                    v = planes[src][r][c]
                    if invert: v ^= 0xff
                    if not emit(v): break

            elif op == 0x63:
                emit(marker)
            else:
                emit(op)

        planes.append(buf)
    return planes, cols, rows, i


def to_indices(planes, cols, rows):
    """4面を重ねて 0〜15 の色番号にする。"""
    px = [[0] * (cols * 8) for _ in range(rows)]
    for p in range(4):
        for y in range(rows):
            row = planes[p][y]
            for bx in range(cols):
                b = row[bx]
                for bit in range(8):
                    if b >> (7 - bit) & 1:
                        px[y][bx * 8 + bit] |= 1 << p
    return px


def decode_all(data):
    """ファイル内の絵を全部復号する。1枚ごとに6バイトのヘッダが付いている。"""
    out = []
    i = 0
    while i + 6 <= len(data) and data[i] == data[i + 1]:
        planes, cols, rows, i = decode(data, i)
        out.append((planes, cols, rows))
    return out


# ---- PNG 出力（外部ライブラリ無し） -------------------------------------

# 原典のパレット。MTE.EXE の DGROUP 0x908 にある16語で、1語 = (索引<<12)|(緑<<8)|(赤<<4)|青。
# 原典は `out 0xa8`=索引 / `out 0xaa`=緑 / `out 0xac`=赤 / `out 0xae`=青 の順に書いている
# （展開は 0xfa2c）。0x8e8 は真っ暗（開始時のフェード用）、0x928 と 0x948 は演出用の別版。
# 動画から取ったタイル色の最頻値 #444444 と #da6464 が、この表の索引0と索引6に一致する。
GAME_PALETTE = [
    (0x44, 0x44, 0x44), (0x77, 0x44, 0x22), (0x99, 0x66, 0x44), (0xbb, 0x88, 0x66),
    (0xdd, 0xaa, 0x88), (0xff, 0xcc, 0xaa), (0xdd, 0x66, 0x66), (0xff, 0x88, 0x88),
    (0xaa, 0xcc, 0xaa), (0xcc, 0xee, 0xcc), (0x88, 0x88, 0xbb), (0xaa, 0xaa, 0xdd),
    (0xcc, 0xcc, 0xff), (0x66, 0x66, 0x66), (0xcc, 0xcc, 0xcc), (0xff, 0xff, 0xff),
]
DEFAULT_PALETTE = GAME_PALETTE


def read_palette(exe_path, ds_offset=0x908):
    """MTE.EXE から16色パレットを読む。既定は本編用の 0x908。"""
    d = open(exe_path, 'rb').read()
    base = 4608 + 0x12340 + ds_offset
    out = []
    for i in range(16):
        w = struct.unpack('<H', d[base + i * 2:base + i * 2 + 2])[0]
        g, r, b = (w >> 8) & 0xf, (w >> 4) & 0xf, w & 0xf
        out.append((r * 17, g * 17, b * 17))
    return out


def write_png(path, px, palette=None):
    import zlib
    pal = palette or DEFAULT_PALETTE
    h = len(px); w = len(px[0])
    raw = b''.join(b'\x00' + bytes(row) for row in px)

    def chunk(kind, body):
        import struct as _s
        return (_s.pack('>I', len(body)) + kind + body
                + _s.pack('>I', zlib.crc32(kind + body) & 0xffffffff))

    import struct as _s
    out = b'\x89PNG\r\n\x1a\n'
    out += chunk(b'IHDR', _s.pack('>IIBBBBB', w, h, 8, 3, 0, 0, 0))
    out += chunk(b'PLTE', b''.join(bytes(c) for c in pal))
    out += chunk(b'IDAT', zlib.compress(raw, 9))
    out += chunk(b'IEND', b'')
    open(path, 'wb').write(out)


if __name__ == "__main__":
    root = sys.argv[1] if len(sys.argv) > 1 else "docs/original/disk"
    for f in sorted(glob.glob(os.path.join(root, "*.P58"))):
        d = open(f, 'rb').read()
        try:
            imgs = decode_all(d)
            w, h = imgs[0][1] * 8, imgs[0][2]
            print(f"{os.path.basename(f):14s} {w}x{h} × {len(imgs):3d}枚")
        except Exception as e:
            print(f"{os.path.basename(f):14s} 失敗: {e}")
