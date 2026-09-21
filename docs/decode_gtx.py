#!/usr/bin/env python3
"""PC98版 MT.GTX（公式テキスト本体）の復号器。

符号化方式（本ファイルの解析で確定、未解決コード0件）:
  0x00            レコード区切り（レコード番号がそのままメッセージID）
  0x01            会話ボックス内の改行（原典の折り返し位置そのもの）
  0x11-0x63       JIS X 0208 第4区（ひらがな）  code = 0x10 + 点
  0x66-0xBB       JIS X 0208 第5区（カタカナ）  code = 0x65 + 点
  0xC1-0xCF       JIS X 0208 第1区（記号）      code = 0xC0 + 点
  0xD0+区, 0x20+点  その他すべて（漢字・全角英数）を区点で2バイト表現
"""
import sys, os

def build_table():
    t = {}
    def jis(ku, ten):
        try: return bytes([0xA0 + ku, 0xA0 + ten]).decode("euc_jp")
        except Exception: return None
    for n in range(1, 16):  t[0xC0 + n] = jis(1, n)
    for n in range(1, 84):  t[0x10 + n] = jis(4, n)
    for n in range(1, 87):  t[0x65 + n] = jis(5, n)
    return {k: v for k, v in t.items() if v}

def decode(data):
    """バイト列 → [(レコード番号, 本文), ...]  本文は原典の改行を保持"""
    T = build_table()
    recs, cur, i = [], [], 0
    while i < len(data):
        b = data[i]
        if b >= 0xD0:
            try: cur.append(bytes([b - 0x30, data[i + 1] + 0x80]).decode("euc_jp"))
            except Exception: cur.append("�")
            i += 2
        elif b == 0x00:
            recs.append("".join(cur)); cur = []; i += 1
        elif b == 0x01:
            cur.append("\n"); i += 1
        elif b in T:
            cur.append(T[b]); i += 1
        else:
            cur.append("�"); i += 1
    recs.append("".join(cur))
    return list(enumerate(recs))

if __name__ == "__main__":
    src = sys.argv[1]
    for n, body in decode(open(src, "rb").read()):
        if body.strip(): print(f"=== {n} ===\n{body.strip()}")
