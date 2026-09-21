#!/usr/bin/env python3
"""会話の窓の指定（行数・上端・セリフ番号）を逆アセンブルから全部拾って表にする。

原典の会話表示は2通りの呼ばれ方をする:
  遠呼び出し  lcall 0x68b, 0x2c0a  （= 線形 0x94ba）
  近呼び出し  call 0x94ba / 0x95d8 （直前に push cs があるぶん引数の位置がずれる）
どちらも引数は (行数, 上端, x, y, セリフ番号)。
"""
import os, re, sys

ROOT = os.path.expanduser("~/Code/MagicTower")
DIS = f"{ROOT}/docs/MTE_disasm.txt"
MI = re.compile(r'^\s+([0-9a-f]{6})\s+\S+\s+(\w+)\s+(.*?)(\s+;.*)?$')


def rows():
    out = []
    for l in open(DIS, encoding="utf-8"):
        m = MI.match(l)
        if m:
            out.append(m)
    return out


def scan():
    rs = rows()
    found = []
    for k, m in enumerate(rs):
        op, args = m.group(2), m.group(3)
        far = op == 'lcall' and '0x68b, 0x2c0a' in args
        near = op == 'call' and re.search(r'\b0x(94ba|95d8)\b', args)
        if not (far or near):
            continue
        regs, pushes = {}, []
        for m2 in rs[max(0, k - 24):k]:
            if m2.group(2) == 'mov':
                r = re.match(r'^(\w\w), (0x[0-9a-f]+|\d+)$', m2.group(3).strip())
                if r:
                    regs[r.group(1)] = int(r.group(2), 0)
            elif m2.group(2) == 'push':
                pushes.append(regs.get(m2.group(3).strip()))
        # 近呼び出しは直前に push cs が入るので1つずれる
        off = 0 if far else 1
        if len(pushes) < 5 + off:
            continue
        found.append((int(m.group(1), 16),
                      pushes[-1 - off], pushes[-2 - off], pushes[-5 - off]))
    return found


def dialogue_lines():
    out = {}
    for l in open(f"{ROOT}/Assets/Resources/Dialogue.txt", encoding="utf-8"):
        m = re.match(r'^(\d+) \| (.*)$', l.rstrip('\n'))
        if m:
            out[int(m.group(1))] = len(m.group(2).split('\\n'))
    return out


if __name__ == "__main__":
    lines = dialogue_lines()
    rec = scan()
    out = [
        "# 会話の窓 — 原典 MTE.EXE の会話表示の呼び出しを全部拾ったもの",
        "#   （docs/make_dialogue_windows.py が作る。手で書かない）",
        "#",
        "# 引数は (行数, 上端, x, y, セリフ番号)。",
        "#   行数 … その窓に出す行の数。**ふつうはセリフの行数と同じ**だが、",
        "#          短い一行のセリフだけ1行多い窓で出すことがある（下の「＋1」印）",
        "#   上端 … 窓の上端のドット位置。ふつうは120、姿の見えない声などは50や90",
        "#",
        "# 形式: 呼び出し位置 | 行数 | 上端 | セリフ | 実際の行数",
    ]
    same = plus = unknown = 0
    for a, ln, top, r in sorted(rec):
        n = lines.get(r)
        if n is None:
            unknown += 1
            mark = "-"
        elif n == ln:
            same += 1
            mark = str(n)
        else:
            plus += 1
            mark = f"{n} （＋{ln - n}）"
        out.append(f"0x{a:06x} | {ln if ln is not None else '-':>2} | "
                   f"{top if top is not None else '-':>3} | "
                   f"{r if r is not None else '-':>4} | {mark}")
    out.append(f"#")
    out.append(f"# 呼び出し {len(rec)}件: セリフ番号が定数のもの {same + plus}件"
               f"（行数が一致 {same} / 1行多い {plus}）、番号が変わるもの {unknown}件")
    open(f"{ROOT}/docs/original/dialogue_windows.txt", "w",
         encoding="utf-8").write("\n".join(out) + "\n")
    print(f"{len(rec)}件 書き出し（一致 {same} / ＋1 {plus} / 不明 {unknown}）")
