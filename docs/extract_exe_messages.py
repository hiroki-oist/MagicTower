#!/usr/bin/env python3
"""PC98版 MTE.EXE から公式の文言表を抜き出す。

MT.GTX（会話）と違い、実行ファイル内の文言は素の Shift-JIS で 0x00 終端。
数値を埋める箇所は前後2つの文字列に分かれて格納されているので、
ここで {0} に畳んで1本の文にする。
"""
import sys

def strings(data, lo, hi, minlen=2):
    out, cur, start = [], b"", lo
    for i in range(lo, hi):
        b = data[i]
        if b == 0:
            if len(cur) >= minlen:
                try: out.append((start, cur.decode("cp932")))
                except UnicodeDecodeError: pass
            cur, start = b"", i + 1
        else:
            if not cur: start = i
            cur += bytes([b])
    return out

def jp(s):
    return any("぀" <= c <= "鿿" or "＀" <= c <= "￯" for c in s)

if __name__ == "__main__":
    d = open(sys.argv[1], "rb").read()
    for off, s in strings(d, 79000, 84000):
        if jp(s): print(f"{off}\t{s!r}")
