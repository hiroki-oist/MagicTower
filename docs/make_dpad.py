#!/usr/bin/env python3
"""移動の十字キーの**候補**の絵を書き出す（作者に選んでもらうためのもの）。

原典には十字キーは無い（キーボードで遊ぶ）。スマホ縦画面（作者の指定）で
下に置くボタンの見た目を選ぶために、色は**原典の枠（LCG.P58 #1）の石積みと
欄の色**から取って5案作る。

  A 石の十字      … 枠と同じ石積みの色。角を面取りした3x3の十字
  B 丸い十字      … 黒い円盤の上に4つの矢印と中央のつまみ
  C ひし形        … 4枚の三角を離して置く
  D 四角いキー    … 原典の欄（濃い灰＋白枠）と同じ作りの4つのキー
  E 円盤          … 円を4つに割って矢印を彫る
  F 輪と矢じり    … 見せてもらった画面に近い形（暗い輪＋矢じり＋中央のつまみ）

  使い方: python3 docs/make_dpad.py
"""
import os, sys, zlib, struct, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from p58_to_tiles import TILE_META
import hashlib

ROOT = os.path.expanduser("~/Code/MagicTower")
OUT = f"{ROOT}/Assets/Sprites/Hud"
N = 240                                  # 1枚の大きさ

# 原典の枠から取った色
STONE      = (168, 168, 216, 255)        # 石積みの地
STONE_HI   = (216, 216, 240, 255)        # 明るい縁
STONE_LO   = (104, 104, 152, 255)        # 暗い縁
BOX        = (68, 68, 68, 255)           # 欄の中（原典の索引0）
WHITE      = (255, 255, 255, 255)
BLACK      = (0, 0, 0, 220)
CLEAR      = (0, 0, 0, 0)


def blank():
    return [[CLEAR] * N for _ in range(N)]


def rect(px, x0, y0, x1, y1, c):
    for y in range(max(0, y0), min(N, y1)):
        for x in range(max(0, x0), min(N, x1)):
            px[y][x] = c


def bevel(px, x0, y0, x1, y1, face, hi, lo, edge=6):
    """面取りした四角。"""
    rect(px, x0, y0, x1, y1, lo)
    rect(px, x0, y0, x1 - edge, y1 - edge, hi)
    rect(px, x0 + edge, y0 + edge, x1 - edge, y1 - edge, face)


def disc(px, cx, cy, r, c, inner=0):
    for y in range(N):
        for x in range(N):
            d = (x - cx) ** 2 + (y - cy) ** 2
            if inner ** 2 <= d <= r ** 2:
                px[y][x] = c


def tri(px, pts, c):
    """三角形を塗る（座標は (x,y) の3つ）。"""
    (x1, y1), (x2, y2), (x3, y3) = pts
    def side(ax, ay, bx, by, x, y):
        return (bx - ax) * (y - ay) - (by - ay) * (x - ax)
    for y in range(N):
        for x in range(N):
            a = side(x1, y1, x2, y2, x, y)
            b = side(x2, y2, x3, y3, x, y)
            cc = side(x3, y3, x1, y1, x, y)
            if (a >= 0 and b >= 0 and cc >= 0) or (a <= 0 and b <= 0 and cc <= 0):
                px[y][x] = c


def arrow(px, cx, cy, size, dir_, c):
    """矢じり。dir_: 0=上 1=右 2=下 3=左"""
    s = size
    if dir_ == 0: pts = [(cx, cy - s), (cx - s, cy + s // 2), (cx + s, cy + s // 2)]
    elif dir_ == 1: pts = [(cx + s, cy), (cx - s // 2, cy - s), (cx - s // 2, cy + s)]
    elif dir_ == 2: pts = [(cx, cy + s), (cx - s, cy - s // 2), (cx + s, cy - s // 2)]
    else: pts = [(cx - s, cy), (cx + s // 2, cy - s), (cx + s // 2, cy + s)]
    tri(px, pts, c)


def design_a():
    """石の十字（枠と同じ石積みの色、角を面取りした十字）。"""
    px = blank()
    def plus(inset, c):
        u = N // 3
        rect(px, u + inset, inset, u * 2 - inset, N - inset, c)
        rect(px, inset, u + inset, N - inset, u * 2 - inset, c)
    plus(0, STONE_LO)
    plus(5, STONE_HI)
    plus(11, STONE)
    for d, (cx, cy) in enumerate([(N // 2, 46), (N - 46, N // 2),
                                  (N // 2, N - 46), (46, N // 2)]):
        arrow(px, cx, cy, 24, d, BOX)
    return px


def design_b():
    """丸い十字（黒い円盤＋矢印＋中央のつまみ）。"""
    px = blank()
    disc(px, N // 2, N // 2, N // 2 - 2, BLACK)
    disc(px, N // 2, N // 2, N // 2 - 2, STONE_LO, N // 2 - 8)
    for d, (cx, cy) in enumerate([(N // 2, 46), (N - 46, N // 2),
                                  (N // 2, N - 46), (46, N // 2)]):
        arrow(px, cx, cy, 26, d, STONE_HI)
    disc(px, N // 2, N // 2, 40, STONE)
    disc(px, N // 2, N // 2, 40, STONE_HI, 34)
    return px


def design_c():
    """ひし形（4枚の三角を離して置く）。"""
    px = blank()
    c = N // 2

    # 4枚を離すため、底辺の半幅は中心からの隙間と同じにする（斜めに隙間ができる）
    gap, b, r = 34, 30, c - 12

    def pts_of(d):
        if d == 0: return [(c, c - r), (c - b, c - gap), (c + b, c - gap)]
        if d == 1: return [(c + r, c), (c + gap, c - b), (c + gap, c + b)]
        if d == 2: return [(c, c + r), (c - b, c + gap), (c + b, c + gap)]
        return [(c - r, c), (c - gap, c - b), (c - gap, c + b)]

    def inset(pts, k):
        mx = sum(p[0] for p in pts) / 3.0
        my = sum(p[1] for p in pts) / 3.0
        return [(int(round(x + (mx - x) * k)), int(round(y + (my - y) * k)))
                for x, y in pts]

    for d in range(4):
        pts = pts_of(d)
        tri(px, pts, STONE_LO)            # 縁
        tri(px, inset(pts, 0.16), STONE)  # 面
        tri(px, inset(pts, 0.45), STONE_HI)
    return px


def design_d():
    """四角いキー（原典の欄と同じ、濃い灰＋白枠）。"""
    px = blank()
    u = 88
    spots = [(N // 2 - u // 2, 0), (N - u, N // 2 - u // 2),
             (N // 2 - u // 2, N - u), (0, N // 2 - u // 2)]
    for d, (x, y) in enumerate(spots):
        rect(px, x, y, x + u, y + u, WHITE)
        rect(px, x + 5, y + 5, x + u - 5, y + u - 5, BOX)
        arrow(px, x + u // 2, y + u // 2, 26, d, WHITE)
    rect(px, N // 2 - 18, N // 2 - 18, N // 2 + 18, N // 2 + 18, STONE_LO)
    rect(px, N // 2 - 13, N // 2 - 13, N // 2 + 13, N // 2 + 13, BOX)
    return px


def design_e():
    """円盤（円を4つに割って矢印を彫る）。"""
    px = blank()
    c, r = N // 2, N // 2 - 2
    for y in range(N):
        for x in range(N):
            dx, dy = x - c, y - c
            if dx * dx + dy * dy > r * r:
                continue
            if dx * dx + dy * dy > (r - 7) ** 2:
                px[y][x] = STONE_LO                     # 外の輪
            elif abs(abs(dx) - abs(dy)) <= 3:
                px[y][x] = STONE_LO                     # 4分割の筋
            else:
                px[y][x] = STONE
    for d, (cx, cy) in enumerate([(N // 2, 46), (N - 46, N // 2),
                                  (N // 2, N - 46), (46, N // 2)]):
        arrow(px, cx, cy, 24, d, BOX)
    disc(px, c, c, 28, STONE_HI)
    disc(px, c, c, 28, STONE_LO, 24)
    return px


def design_f():
    """見せてもらった画面に近い形（暗い輪＋4つの矢じり＋中央のつまみ）。"""
    px = blank()
    c = N // 2
    disc(px, c, c, c - 2, BLACK)
    disc(px, c, c, c - 2, STONE_LO, c - 10)
    for d, (cx, cy) in enumerate([(c, 52), (N - 52, c), (c, N - 52), (52, c)]):
        arrow(px, cx, cy, 28, d, STONE_HI)
    disc(px, c, c, 52, BLACK)
    disc(px, c, c, 52, STONE_LO, 46)
    return px


# ---- 実際に使う十字キー（B を4つの扇と中央のボタンに分けたもの）----
#
# 押している向きだけ色を変えたいので、**1枚の絵ではなく5枚**に分ける。
# 扇は45度のところで切り、中央のボタンとの間にも隙間をあける（作者の指定）。

RING_OUTER = N // 2 - 2      # 輪の外
RING_INNER = 64              # 輪の内（ここから内側は中央のボタンの領域）
WEDGE_GAP = 7                # 45度の切れ目と、中央との隙間（ドット）


def wedge(dir_):
    """扇ひとつ（上=0 右=1 下=2 左=3）。押されたときだけ色を変えるので1枚ずつ。"""
    px = blank()
    c = N / 2.0
    for y in range(N):
        for x in range(N):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            r = math.hypot(dx, dy)
            if r > RING_OUTER or r < RING_INNER + WEDGE_GAP:
                continue
            # 45度で切る。dy は下向きが＋（画像の座標）
            if dir_ == 0 and not (dy < 0 and abs(dx) < -dy): continue
            if dir_ == 2 and not (dy > 0 and abs(dx) < dy): continue
            if dir_ == 1 and not (dx > 0 and abs(dy) < dx): continue
            if dir_ == 3 and not (dx < 0 and abs(dy) < -dx): continue
            # 45度の切れ目に隙間をあける
            if abs(abs(dx) - abs(dy)) < WEDGE_GAP: continue
            px[y][x] = STONE if r < RING_OUTER - 6 else STONE_LO
    # 矢じりは輪の帯のまん中に置き、**扇からはみ出したぶんは消す**
    mark = blank()
    rmid = (RING_OUTER + RING_INNER) // 2
    cx, cy = {0: (N // 2, N // 2 - rmid), 1: (N // 2 + rmid, N // 2),
              2: (N // 2, N // 2 + rmid), 3: (N // 2 - rmid, N // 2)}[dir_]
    arrow(mark, cx, cy, 22, dir_, BOX)
    for y in range(N):
        for x in range(N):
            if mark[y][x][3] and px[y][x][3]: px[y][x] = BOX
    return px


def center_button():
    """真ん中の決定ボタン。扇との間に隙間をあける。"""
    px = blank()
    c = N // 2
    disc(px, c, c, RING_INNER - WEDGE_GAP, STONE_LO)
    disc(px, c, c, RING_INNER - WEDGE_GAP - 6, STONE_HI)
    return px


def png(path, px):
    raw = b""
    for row in px:
        raw += b"\x00" + b"".join(bytes(c) for c in row)
    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xffffffff))
    out = (b"\x89PNG\r\n\x1a\n"
           + chunk(b"IHDR", struct.pack(">IIBBBBB", N, N, 8, 6, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(raw, 9))
           + chunk(b"IEND", b""))
    open(path, "wb").write(out)


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []
    parts = [("Up", lambda: wedge(0)), ("Right", lambda: wedge(1)),
             ("Down", lambda: wedge(2)), ("Left", lambda: wedge(3)),
             ("Center", center_button)]
    for name, fn in [("A", design_a), ("B", design_b), ("C", design_c),
                     ("D", design_d), ("E", design_e), ("F", design_f)] + parts:
        path = f"{OUT}/DPad_{name}.png"
        png(path, fn())
        guid = hashlib.md5(f"MagicTower/dpad/{name}".encode()).hexdigest()
        meta = TILE_META.format(guid=guid).replace("filterMode: 1", "filterMode: 0")
        meta = meta.replace("spriteMeshType: 1", "spriteMeshType: 0")
        open(path + ".meta", "w").write(meta)
        made.append(os.path.basename(path))
    print(f"{len(made)} 枚を書き出した → {OUT}  {made}")


if __name__ == "__main__":
    main()
