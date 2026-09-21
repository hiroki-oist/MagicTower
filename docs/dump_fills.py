#!/usr/bin/env python3
"""原典が塗り直す画面の欄をぜんぶ数え上げる。

塗り潰しは `lcall 0xf5f, 0x6e9` で、引数は C の並び（push は逆順）:

    塗り潰し(VRAM変位, 幅ワード, 高さ行, 色)

VRAM は1行80バイト＝640ドットなので、変位 → (x=列*8, y=行)、幅は ワード*16 ドット。
**画面のどこを書き換えるか**が全部ここに出るので、
リメイク側の欄（`Assets/Scripts/UI/ScreenLayout.cs`）の取りこぼしを見つけられる。

出力: docs/original/screen_fills.txt
"""
import re, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
lines = open(f"{ROOT}/docs/MTE_disasm.txt", encoding="utf-8").read().split("\n")

found = {}
for i, l in enumerate(lines):
    if "lcall   0xf5f, 0x6e9" not in l:
        continue
    vals, j, ok = [], i - 1, True
    while j > 0 and len(vals) < 4:
        t = lines[j]
        if "push" in t:
            reg = t.split("push")[1].strip()
            m, k = None, j - 1
            while k > j - 4 and k > 0:      # push の直前の mov を拾う
                m = re.search(rf"mov\s+({reg}|{reg[0]}l),\s*(0x[0-9a-f]+|\d+)", lines[k])
                if m:
                    break
                k -= 1
            if not m:                      # 定数でない（変数から来る）ものは飛ばす
                ok = False
                break
            v = m.group(2)
            vals.append(int(v, 16) if v.startswith("0x") else int(v))
        elif "lcall" in t or "=====" in t:
            ok = False
            break
        j -= 1
    if not ok or len(vals) < 4:
        continue
    off, words, rows, color = vals
    y, x = divmod(off, 80)
    found.setdefault((x * 8, y, words * 16, rows), []).append(lines[i].split()[0])

out = [
    "# 原典が塗り直す画面の欄（docs/dump_fills.py が生成）",
    "# 塗り潰し lcall 0xf5f,0x6e9(変位, 幅ワード, 高さ行, 色) の引数が定数のものを全部。",
    "#",
    "#    x |    y |  幅 |  高 | 呼ぶ所",
]
for (x, y, w, h), addrs in sorted(found.items()):
    out.append(f"{x:5d} | {y:4d} | {w:3d} | {h:3d} | " + ",".join(addrs))
open(f"{ROOT}/docs/original/screen_fills.txt", "w", encoding="utf-8").write("\n".join(out) + "\n")
print("\n".join(out))
