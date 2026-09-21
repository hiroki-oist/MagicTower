#!/usr/bin/env python3
"""PC98版 MTE.EXE（Microsoft C 6.0 の16bit MS-DOS 実行ファイル）を読むための道具。

難読化はされていないので、既知の文字列やデータ表のアドレスを手がかりに
それを参照しているコードを逆引きし、その関数だけを逆アセンブルする。

  ファイル先頭 4608バイト = MZヘッダ。以降がロードイメージ。
  DGROUP はイメージ 0x12340（DS=0x1234）。MS-C の _NULL セグメント
  （"MS Run-Time Library - Copyright (c) 1990, Microsoft Corp"）の位置から求めた。
"""
import struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_16

HDR = 4608
DG  = 0x12340          # DGROUP のイメージ内先頭

def load(path="/private/tmp/claude-501/-Users-hirokisawada/9fcd3364-0b8e-46a3-83df-425be50eba00/scratchpad/fd/MTE.EXE"):
    d = open(path, "rb").read()
    return d, d[HDR:]

def ds_off(data, file_off):
    """ファイル位置 → DS 相対オフセット"""
    return (file_off - HDR) - DG

def find_refs(img, value, limit=16):
    """16bit即値としてイメージ中に現れる位置を返す"""
    pat = struct.pack("<H", value); out = []; i = img.find(pat)
    while i >= 0 and len(out) < limit:
        out.append(i); i = img.find(pat, i + 1)
    return out

PROLOGUE = bytes([0x55, 0x8B, 0xEC])      # push bp ; mov bp,sp

def func_start(img, off, back=2000):
    """off を含む関数の入口を、MS-C の定型プロローグを後ろ向きに探して求める"""
    lo = max(0, off - back)
    i = img.rfind(PROLOGUE, lo, off)
    return i if i >= 0 else max(0, off - 64)

def dis(img, start, n=400, stop_at_ret=True):
    md = Cs(CS_ARCH_X86, CS_MODE_16); md.detail = False
    out = []
    for ins in md.disasm(img[start:start + n], start):
        out.append(ins)
        if stop_at_ret and ins.mnemonic in ("ret", "retf"):
            break
    return out

def show(img, start, n=400, mark=None):
    for ins in dis(img, start, n):
        m = " <<<" if mark is not None and ins.address <= mark < ins.address + ins.size else ""
        print(f"  {ins.address:06x}  {ins.bytes.hex():<14s} {ins.mnemonic:<7s} {ins.op_str}{m}")

if __name__ == "__main__":
    import sys
    d, img = load()
    off = int(sys.argv[1], 0)
    s = func_start(img, off)
    print(f"関数入口 {s:#06x} （参照 {off:#06x}）")
    show(img, s, int(sys.argv[2]) if len(sys.argv) > 2 else 500, mark=off)


# ---- 注釈つきの表示 --------------------------------------------------------
def annot(img, d, start, n=600, skip_strops=True):
    """文字列参照とグローバルに注釈を付けて逆アセンブルする。
    MS-C が展開する strcpy/strcat の定型（repne scasb … rep movsw）は1行にまとめる。"""
    from exemap import strings_in_dgroup
    global _ST
    try: _ST
    except NameError: _ST = strings_in_dgroup(d)
    md = Cs(CS_ARCH_X86, CS_MODE_16); md.detail = True
    from capstone import x86_const
    out = []; buf = []
    for ins in md.disasm(img[start:start + n], start):
        if skip_strops and ins.mnemonic in ("scasb","movsw","movsb","not","xchg") or \
           (skip_strops and ins.mnemonic in ("push","pop") and ins.op_str in ("ds","es")):
            buf.append(ins); continue
        if buf:
            out.append(f"  {buf[0].address:06x}  …文字列コピーの定型 {len(buf)}命令…")
            buf = []
        note = ""
        for op in ins.operands:
            if op.type == x86_const.X86_OP_IMM and op.imm in _ST:
                note = "   ; \"" + _ST[op.imm].replace("\n", "/")[:40] + '"'
        out.append(f"  {ins.address:06x}  {ins.mnemonic:<7s} {ins.op_str}{note}")
        if ins.mnemonic in ("ret", "retf"): break
    return out
