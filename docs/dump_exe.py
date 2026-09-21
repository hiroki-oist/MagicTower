#!/usr/bin/env python3
"""MTE.EXE の全コードを注釈付きで丸ごと逆アセンブルしてファイルに書き出す。

注釈:
  ・DS 相対の即値が文字列表の位置なら、その文字列を出す
  ・よく使うグローバル（生命力・攻撃力・現在階など）は名前で出す
  ・地図のマスへの参照 es:[bx + 0x1aXX] は (x,y) に直す
  ・関数の入口には目印を入れる
"""
import sys, struct, json, collections
sys.path.insert(0, "/Users/hirokisawada/Code/MagicTower/docs")
from dis16 import load, HDR, DG
from exemap import strings_in_dgroup, far_calls, lin
from capstone import Cs, CS_ARCH_X86, CS_MODE_16, x86_const

MAPBASE = 0x1aee          # 地図の index0 のマスの変位
GLOBALS = {0x98:"生命力", 0x9c:"攻撃力", 0x16:"防御力", 0xa2:"金", 0x9e:"現在階",
           0x96:"到達最高階", 0x1c:"鍵", 0x9a:"紫鍵", 0xa0:"赤鍵",
           0x21:"装備剣", 0x22:"装備盾", 0x2:"X", 0x4:"Y", 0x20:"妖精回数"}
ITEMFLAG = {0x23:"勇者オーブ",0x24:"知恵オーブ",0x25:"飛翔オーブ",0x26:"十字架",
            0x27:"エリクサー",0x28:"マトック",0x29:"破壊の玉",0x2a:"空間転移",
            0x2b:"昇華の翼",0x2c:"降臨の翼",0x2d:"ドラゴンスレイヤー",
            0x2e:"雪の結晶",0x2f:"魔法の鍵",0x30:"スーパーマトック",0x31:"幸運の金貨"}

def cellname(disp):
    o = disp - MAPBASE
    if o % 2 or not (0 <= o < 242): return None
    i = o // 2
    return f"マス({i%11-5:+d},{5-i//11:+d})"

def main(out_path):
    d, img = load()
    ST = strings_in_dgroup(d)
    ent = set(t for _, _, _, t in far_calls(img))
    i = -1
    while True:
        i = img.find(b"\x55\x8b\xec", i + 1)
        if i < 0: break
        ent.add(i)
    md = Cs(CS_ARCH_X86, CS_MODE_16); md.detail = True
    n = 0
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("# MTE.EXE 全コードの逆アセンブル（docs/dump_exe.py が生成）\n")
        f.write(f"# ヘッダ {HDR} バイト / DGROUP イメージ {DG:#07x} (DS=0x1234)\n")
        f.write(f"# 地図: セグメント [0x5552] の +{MAPBASE:#06x} から、1階 248バイト\n\n")
        for ins in md.disasm(img[0:DG], 0):
            if ins.address in ent:
                f.write(f"\n===== 関数 {ins.address:#07x} =====\n")
            note = []
            for op in ins.operands:
                if op.type == x86_const.X86_OP_IMM and op.imm in ST:
                    note.append('"' + ST[op.imm].replace("\n", "/")[:50] + '"')
                if op.type == x86_const.X86_OP_MEM:
                    dsp = op.mem.disp & 0xFFFF
                    c = cellname(dsp)
                    if c and op.mem.base != 0: note.append(c)
                    elif dsp in GLOBALS and op.mem.base == 0: note.append(GLOBALS[dsp])
                    elif dsp in ITEMFLAG and op.mem.base == 0: note.append("所持:" + ITEMFLAG[dsp])
            s = f"  {ins.address:06x}  {ins.bytes.hex():<16s} {ins.mnemonic:<7s} {ins.op_str}"
            if note: s += "   ; " + " ".join(note)
            f.write(s + "\n"); n += 1
    return n

if __name__ == "__main__":
    print("命令数", main(sys.argv[1] if len(sys.argv) > 1 else "docs/MTE_disasm.txt"))
