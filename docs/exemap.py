#!/usr/bin/env python3
"""MTE.EXE 全体の地図を作る。

やること:
  1. 全 far call (9A off16 seg16) を集めて関数の入口を列挙する
  2. MS-C の定型プロローグ (55 8B EC) も入口候補として拾う
  3. 各関数を逆アセンブルして、呼び先・参照する DS 文字列・触るグローバルを記録する
  4. DGROUP 内の文字列表を作り、関数に名前を付ける手がかりにする
"""
import struct, collections
from capstone import Cs, CS_ARCH_X86, CS_MODE_16

HDR = 4608
DG  = 0x12340
EXE = "/private/tmp/claude-501/-Users-hirokisawada/9fcd3364-0b8e-46a3-83df-425be50eba00/scratchpad/fd/MTE.EXE"

def load(path=EXE):
    d = open(path, "rb").read()
    return d, d[HDR:HDR + 96695]

def seg_off(img_off, seg):
    return img_off - seg * 16

def lin(seg, off):
    return seg * 16 + off

def far_calls(img):
    """(呼び出し位置, 相手セグメント, 相手オフセット) を全部集める"""
    out = []
    i = -1
    while True:
        i = img.find(b"\x9a", i + 1)
        if i < 0 or i + 5 > len(img): break
        off, seg = struct.unpack("<HH", img[i+1:i+5])
        t = lin(seg, off)
        if 0 <= t < len(img):
            out.append((i, seg, off, t))
    return out

def strings_in_dgroup(d):
    """DGROUP 内の cp932 文字列を {DSオフセット: 文字列} で返す"""
    out = {}
    cur = b""; start = None
    for i in range(DG, len(d) - HDR):
        b = d[HDR + i]
        if b == 0:
            if len(cur) >= 4:
                try:
                    s = cur.decode("cp932")
                    if any("぀" <= c <= "鿿" or "！" <= c <= "ﾟ" or c.isascii() and c.isprintable() for c in s):
                        out[start - DG] = s
                except UnicodeDecodeError:
                    pass
            cur = b""; start = None
        else:
            if not cur: start = i
            cur += bytes([b])
    return out

def analyze(img, start, maxlen=4000):
    """関数を1つ逆アセンブルして (末尾, 呼び先, 即値の集合) を返す"""
    md = Cs(CS_ARCH_X86, CS_MODE_16)
    calls, imms = [], set()
    end = start
    for ins in md.disasm(img[start:start + maxlen], start):
        end = ins.address + ins.size
        if ins.id == 0: break
        b = ins.bytes
        if b[0] == 0x9A and len(b) >= 5:
            off, seg = struct.unpack("<HH", b[1:5])
            calls.append(lin(seg, off))
        for m in (b[i:i+2] for i in range(1, max(1, len(b) - 1))):
            if len(m) == 2: imms.add(struct.unpack("<H", m)[0])
        if ins.mnemonic in ("ret", "retf", "jmp"):
            if ins.mnemonic in ("ret", "retf"): break
    return end, calls, imms
