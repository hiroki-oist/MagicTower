#!/usr/bin/env python3
"""抽出JSON -> .floor テキスト形式。11x11 グリッド。"""
import json, glob, os, sys, math

SP = sys.argv[1]
X0, X1, Y0, Y1 = -5, 5, -5, 5          # 11 x 11
W = X1 - X0 + 1
H = Y1 - Y0 + 1

TERRAIN = {                              # プレハブ -> 地形記号
    "Wall_Field": "#", "Wall_Secret": "%", "Magma": "~",
    "Stairs_Up": ">", "Stairs_Down": "<",
    "Lock_Yellow": "y", "Lock_Blue": "b", "Lock_Red": "r",
    "Lock_Sealed": "s", "Lock_Special": "x",
    "road": ".",
}
SKIP = {"BaseField", "FloorBase", "FloorManager", "GameController",
        "Main Camera", "Player"}

def cells(o):
    """壁のサイズを考慮して占有セルを返す"""
    w = int(round(o.get("w") or 1)); h = int(round(o.get("h") or 1))
    # 中心座標 + サイズ -> 左下セル
    cx, cy = o["x"], o["y"]
    x0 = int(round(cx - (w - 1) / 2.0))
    y0 = int(round(cy - (h - 1) / 2.0))
    for dy in range(h):
        for dx in range(w):
            yield x0 + dx, y0 + dy

def convert(n, objs):
    grid = [["." for _ in range(W)] for _ in range(H)]
    extras, oob, drift = [], 0, 0
    for o in objs:
        if o["prefab"] in SKIP: continue
        if o["x"] is None: continue
        if abs(o["x"] - round(o["x"])) > 0.26 or abs(o["y"] - round(o["y"])) > 0.26:
            drift += 1
        t = TERRAIN.get(o["prefab"])
        if t:
            for cx, cy in cells(o):
                if X0 <= cx <= X1 and Y0 <= cy <= Y1:
                    grid[Y1 - cy][cx - X0] = t
                else:
                    oob += 1
        else:
            gx, gy = int(round(o["x"])), int(round(o["y"]))
            extras.append((gx, gy, o["prefab"], o["name"]))
            if not (X0 <= gx <= X1 and Y0 <= gy <= Y1): oob += 1

    L = [f"# Floor {n}", f"grid: {W}x{H}  x:{X0}..{X1}  y:{Y0}..{Y1}", "",
         "terrain:  . 床  # 壁  % 隠し壁  ~ マグマ  > 上り  < 下り",
         "          y/b/r 黄青赤の扉  s 封印扉  x 特殊扉", ""]
    for r, row in enumerate(grid):
        L.append(f"  {Y1-r:+d} | " + "".join(row))
    L += ["", "objects:  (x, y, prefab, instance-name)"]
    for gx, gy, pf, nm in sorted(extras, key=lambda e: (-e[1], e[0])):
        L.append(f"  {gx:+d} {gy:+d}  {pf:<26} {nm}")
    return "\n".join(L) + "\n", oob, drift, len(extras)

os.makedirs(os.path.join(SP, "floorfmt"), exist_ok=True)
tot_oob = tot_drift = 0
for f in sorted(glob.glob(os.path.join(SP, "floors", "*.json")),
                key=lambda p: int(''.join(c for c in os.path.basename(p) if c.isdigit()))):
    n = int(''.join(c for c in os.path.basename(f) if c.isdigit()))
    objs = json.load(open(f))
    txt, oob, drift, ne = convert(n, objs)
    open(os.path.join(SP, "floorfmt", f"Floor_{n}.floor"), "w").write(txt)
    tot_oob += oob; tot_drift += drift
    if n <= 34:
        print(f"F{n:<3} オブジェクト{len(objs):3d} → 非地形{ne:3d}  枠外{oob}  座標ズレ{drift}")
print(f"\n合計: 枠外 {tot_oob}  座標ズレ {tot_drift}")
