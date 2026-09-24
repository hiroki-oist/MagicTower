#!/usr/bin/env python3
"""PC98版 MTE.EXE に埋め込まれた音楽・効果音を取り出す。

原典の音は **FM音源ドライバ**（MTE.EXE の中、セグメント 0x10da）が鳴らしている。
ゲーム側は `lcall 0x10da, 0x20b` に番号を1つ渡すだけで、ドライバの中の
振り分け表がその番号のチャンネルデータの先頭アドレスを各チャンネルに入れる。

  ・音1 と音2 … 4チャンネルの**曲**（BGM）
  ・音6〜13   … 1チャンネルの**効果音**
  ・音3/4/5   … **振り分け表にそもそも無い**。ドライバは 1,2,6〜13 の10個しか
                見ておらず（cmp bx,N が10個だけ）、3〜5 は番号ごと空いている。
                以前ここに「表にあるが呼ばれない」と書いていたが誤り。

データは MML 風のバイト列で、割り込みハンドラ（cs:0x0a49）が1バイトずつ読む。
命令の振り分けは cs:0x929 + 2*命令 の飛び先表（cs:0xbce で引いている）。

  0x00〜0x5b … 音符。値は「(半音 × 2) + 0x42」。直後のバイトが 0x41 未満なら音長。
               音長 L の実際の長さは L*3（cs:0x1382）。省略すると前の音長を使う。
  0x41       … 次のバイトを読み直す前置き（タイ・スラー）
  0x60 n     … オクターブ（ブロック）を n にする      0x61 / 0x62 … 1つ上げる / 下げる
  0x63 aa aa … 音色（その番地から29バイトのFMパラメータ）
  0x66 n     … チャンネルの状態フラグ                0x68 n … 既定の音長
  0x69       … 終わり
  0x6a aa aa … その番地へ飛ぶ（曲の繰り返し）        0x6b / 0x6c n … 繰り返しの始め／回数
  0x6e       … 休符（音符と同じく音長が続く）
  0x70 n     … テンポ                                0x71 n … 音量  0x72 / 0x73 … 音量 −3 / +3

  使い方: python3 docs/decode_sound.py > docs/original/sounds.txt
"""
import struct, sys, os

EXE  = os.path.join(os.path.dirname(__file__), "original/disk/MTE.EXE")
HDR  = 4608
SEG  = 0x10da          # ドライバのセグメント
BASE = SEG * 16        # イメージ内の先頭
DISPATCH = 0x929       # FM 側の命令の飛び先表（cs 相対）。SSG 側は 0x961
SSG_SLOTS = (0x957, 0x972, 0x98d)   # 第2音源側のチャンネルの枠

NOTE0 = 0x42           # ド
NAMES = ["ド","ド#","レ","レ#","ミ","ファ","ファ#","ソ","ソ#","ラ","ラ#","シ"]
ABC   = ["C","C#","D","D#","E","F","F#","G","G#","A","A#","B"]

# 第2音源（SSG）側は **別の命令表**（cs:0x961）を持ち、**命令の境目も 0x59**（FM は 0x5c）。
# 振り分けは cs:0x0d9c。だから同じバイトでも意味が変わる:
#   0x59 n     … チャンネルの設定（FM では同じ処理が 0x75 に居る）。**音符ではない**
#   0x63 aa aa … その番地から **6バイト**を読む（FM の29バイトの音色とは別物）
SSG_OPS = {
    0x59:("設定",1),       0x5a:("設定5a",1),     0x5b:("設定5b",1),   0x5c:("設定5c",1),
    0x5d:("設定5d",0),     0x5e:("設定5e",0),     0x5f:("設定5f",0),
    0x60:("オクターブ",1), 0x61:("オクターブ+1",0), 0x62:("オクターブ-1",0),
    0x63:("設定表",2),     0x64:("設定64",0),     0x65:("設定65",0),
    0x66:("フラグ",1),     0x67:("呼び出し",2),   0x68:("既定の音長",1),
    0x69:("終わり",0),     0x6a:("飛ぶ",2),       0x6b:("繰り返し始",0),
    0x6c:("繰り返し回数",1), 0x6d:("繰り返し終",0), 0x6e:("休符",0),
    0x6f:("設定6f",0),     0x70:("テンポ",1),     0x71:("音量",1),
    0x72:("音量-1",0),     0x73:("音量+1",0),
}

# 命令 → (名前, 引数のバイト数)
OPS = {
    0x60:("オクターブ",1), 0x61:("オクターブ+1",0), 0x62:("オクターブ-1",0),
    0x63:("音色",2),       0x66:("フラグ",1),       0x67:("呼び出し",2),
    0x68:("既定の音長",1), 0x69:("終わり",0),       0x6a:("飛ぶ",2),
    0x6b:("繰り返し始",0), 0x6c:("繰り返し回数",1), 0x6d:("繰り返し終",0),
    0x6e:("休符",0),       0x70:("テンポ",1),       0x71:("音量",1),
    0x72:("音量-3",0),     0x73:("音量+3",0),       0x75:("設定75",1),
    0x76:("設定76",1),     0x77:("設定77",1),       0x78:("設定78",1),
    0x7c:("設定7c",1),     0x7d:("設定7d",0),       0x7e:("設定7e",0),
    0x7f:("設定7f",2),
}

def load():
    return open(EXE, "rb").read()[HDR:]

def dispatch_table(img):
    """音番号 → チャンネルデータの先頭（cs 相対）。ドライバの振り分けを読む。"""
    from capstone import Cs, CS_ARCH_X86, CS_MODE_16
    md = Cs(CS_ARCH_X86, CS_MODE_16)
    cur, out = None, {}
    for ins in md.disasm(img[BASE + 0x20b: BASE + 0x20b + 0x200], 0x20b):
        if ins.mnemonic == "cmp" and ins.op_str.startswith("bx, "):
            cur = int(ins.op_str.split(", ")[1], 0); out.setdefault(cur, [])
            last = None
        elif ins.mnemonic == "mov" and ins.op_str.startswith("bx, 0x") and cur is not None:
            last = int(ins.op_str.split(", ")[1], 0)
        elif (ins.mnemonic == "mov" and cur is not None and last is not None
              and ins.op_str.startswith("word ptr [0x") and ins.op_str.endswith(", bx")):
            slot = int(ins.op_str.split("[")[1].split("]")[0], 0)
            if slot in (0x90c, 0x925, 0x93e, 0x957, 0x972):
                out[cur].append((slot, last))
            last = None
        if ins.address > 0x40b: break
    return {k: v for k, v in out.items() if v}

def parse(img, start, limit=4000, ssg=False):
    """1チャンネル分を読む。(種類, 値, 音長) の並びを返す。

    ssg=True なら第2音源側の読み方（命令の境目 0x59、命令表 cs:0x961）。
    """
    table = SSG_OPS if ssg else OPS
    edge = 0x59 if ssg else 0x5c
    p, out, seen = start, [], set()
    while len(out) < limit:
        if p in seen: out.append(("※同じ所へ戻った", p, None)); break
        seen.add(p)
        b = img[BASE + p]; p += 1
        tie = False
        if b == 0x41:                       # タイ・スラーの前置き
            tie = True; b = img[BASE + p]; p += 1
        if b < edge or b == 0x6e:           # 音符と休符は音長を取り得る
            ln = None
            if p < len(img) and img[BASE + p] < 0x41:
                ln = img[BASE + p]; p += 1
            if b == 0x6e: out.append(("休符", None, ln))
            else:
                s = (b - NOTE0)
                out.append(("タイ音符" if tie else "音符", s, ln))
            continue
        name, n = table.get(b, (f"未知0x{b:02x}", 0))
        if n == 1:
            v = img[BASE + p]; p += 1
        elif n == 2:
            v = struct.unpack("<H", img[BASE + p:BASE + p + 2])[0]; p += 2
        else:
            v = None
        out.append((name, v, None))
        if b == 0x69: break                 # 終わり
        if b == 0x6a: p = v                 # 飛ぶ（曲の繰り返し）
    return out

def note_name(step):
    """音符の値 → 音名。原典は「半音×2」で持っている。"""
    if step % 2: return f"半音{step}（＃側）"
    i = (step // 2) % 12
    oct_shift = (step // 2) // 12
    return f"{ABC[i]}{'+' if oct_shift > 0 else ''}{NAMES[i]}"

def main():
    img = load()
    tbl = dispatch_table(img)
    print("# 原典 PC98版 MTE.EXE の音 — docs/decode_sound.py が MTE.EXE から取り出したもの")
    print("#")
    print("# ゲームは `lcall 0x10da, 0x20b` に番号を渡すだけ。番号ごとのチャンネル構成は下のとおり。")
    print("# 書式の説明は docs/decode_sound.py の先頭にある。")
    for num in sorted(tbl):
        chans = tbl[num]
        kind = "曲（BGM）" if len(chans) > 1 else "効果音"
        print(f"\n=== 音{num}  {kind}  チャンネル{len(chans)}本")
        for slot, off in chans:
            ev = parse(img, off, ssg=slot in SSG_SLOTS)
            print(f"--- チャンネル cs:0x{off:03x}（枠 0x{slot:03x}）{len(ev)}個")
            line = []
            for k, v, ln in ev:
                if k in ("音符", "タイ音符"):
                    line.append(f"{note_name(v)}{'~' if k=='タイ音符' else ''}"
                                + (f":{ln}" if ln is not None else ""))
                elif k == "休符":
                    line.append("休" + (f":{ln}" if ln is not None else ""))
                elif v is None:
                    line.append(k)
                else:
                    line.append(f"{k}={v}" if v < 256 else f"{k}=0x{v:x}")
            print("    " + " ".join(line))

# ---- リメイク用のデータ書き出し -------------------------------------------

TOKEN = {"オクターブ":"o", "オクターブ+1":"o+", "オクターブ-1":"o-", "音色":"i",
         "設定":"m", "設定表":"i", "音量+1":"v+", "音量-1":"v-",
         "既定の音長":"l", "テンポ":"t", "音量":"v", "音量+3":"v+", "音量-3":"v-",
         "終わり":"end", "飛ぶ":"loop", "繰り返し始":"[", "繰り返し終":"]",
         "繰り返し回数":"]n", "フラグ":"f", "呼び出し":"call"}

def tokens(ev):
    out = []
    for k, v, ln in ev:
        if k in ("音符", "タイ音符"):
            if v % 2:                      # 原典の音符の範囲外（出だしの間合い）
                out.append("x" + (f":{ln}" if ln is not None else "")); continue
            i = (v // 2) % 12
            out.append(ABC[i].lower().replace("#", "+")
                       + ("~" if k == "タイ音符" else "")
                       + (f":{ln}" if ln is not None else ""))
        elif k == "休符":
            out.append("r" + (f":{ln}" if ln is not None else ""))
        elif k.startswith("※"):
            continue
        else:
            t = TOKEN.get(k, k)
            out.append(t if v is None else (f"{t}0x{v:x}" if v > 255 else f"{t}{v}"))
    return out

def write_pitch(path):
    """原典の音程表（F-Number）をリメイク側へ渡す。"""
    img = load()
    base = BASE + 0x96f
    L = ["# 原典の音程表 — MTE.EXE の音源ドライバ cs:0x96f + 音符値（半音ごとに2バイト）",
         "#",
         "# YM2203 の F-Number。周波数は OPN の式",
         "#     f = fnum × 3993600 / (144 × 2^(20 - block))",
         "# で、block は `o n` の n-1。o4 のラ = 220.05Hz、o5 のラ = 440.1Hz になる。",
         "#",
         "# 形式: 音名 | F-Number"]
    for i in range(12):
        v = 0x42 + 2 * i
        lo, hi = img[base + v], img[base + v + 1]
        L.append(f"{ABC[i].lower().replace('#','+')} | {lo | ((hi & 7) << 8)}")
    open(path, "w", encoding="utf-8").write("\n".join(L) + "\n")

def voices(img, addrs):
    """FMの音色（29バイト）を名前つきで読む。並びは OPN のレジスタの順そのまま。"""
    out = []
    for a in addrs:
        b = img[BASE + a:BASE + a + 29]
        v = {"addr": a, "FB": (b[28] >> 3) & 7, "ALG": b[28] & 7, "ops": []}
        for op in range(4):
            v["ops"].append({
                "DT": (b[op] >> 4) & 7, "MUL": b[op] & 15,
                "TL": b[4 + op] & 0x7f,
                "KS": (b[8 + op] >> 6) & 3, "AR": b[8 + op] & 0x1f,
                "AM": (b[12 + op] >> 7) & 1, "DR": b[12 + op] & 0x1f,
                "SR": b[16 + op] & 0x1f,
                "SL": (b[20 + op] >> 4) & 15, "RR": b[20 + op] & 15,
                "SSG": b[24 + op] & 15,
            })
        out.append(v)
    return out

def write_voices(path):
    """譜面が `i` で指す音色のうち、FM音源側の4つを書き出す。"""
    img = load()
    L = ["# 原典の音色（FM音源のパラメータ）— MTE.EXE の音源ドライバから取り出したもの",
         "#",
         "# 譜面の `i0x???` が指す先の **29バイト**。並びは OPN のレジスタの順で、",
         "#   DT/MUL ×4 → TL ×4 → KS/AR ×4 → AM/DR ×4 → SR ×4 → SL/RR ×4 → SSG-EG ×4 → FB/ALG",
         "# ドライバも 0x30 番地から4つ飛びに28バイト書いて、最後に 0xb0 番地へ FB/ALG を書く",
         "# （cs:0x0c56）ので、この並びで合っている。",
         "#",
         "# 第2音源側（SSG）のチャンネルが指す 0x562 は譜面と重なっていて音色ではない。",
         "# `i` の意味がチャンネルによって違うとみられる（未解明）。",
         "#",
         "# 形式: 音色 | FB | ALG | op番号 | DT | MUL | TL | KS | AR | AM | DR | SR | SL | RR | SSG"]
    for v in voices(img, (0x3c4, 0x3e1, 0x3fe, 0x41b)):
        for i, o in enumerate(v["ops"], 1):
            L.append(f"0x{v['addr']:03x} | {v['FB']} | {v['ALG']} | {i} | "
                     + " | ".join(str(o[k]) for k in
                                  ("DT","MUL","TL","KS","AR","AM","DR","SR","SL","RR","SSG")))
    open(path, "w", encoding="utf-8").write("\n".join(L) + "\n")

def write_data(path):
    img = load()
    tbl = dispatch_table(img)
    L = []
    L.append("# 原典の音 — docs/decode_sound.py が MTE.EXE の音源ドライバから取り出したもの")
    L.append("#")
    L.append("# ゲームは音番号を1つ渡すだけ（原典 lcall 0x10da, 0x20b、全61か所）。")
    L.append("#   音1・音2 = 4チャンネルの曲、音6〜13 = 1チャンネルの効果音。")
    L.append("#")
    L.append("# 書式: 音番号 | チャンネル | 記号の並び")
    L.append("#   o4 = オクターブ4 / o+ o- = 上下 / v10 = 音量 / v+ v- = ±3 / t130 = テンポ")
    L.append("#   i0x41b = 音色（原典ドライバ内のFMパラメータの番地） / l8 = 既定の音長")
    L.append("#   c d+ e … = 音名（+ は半音上）、`:` のあとが音長（原典の値。実長はこの3倍）")
    L.append("#   r = 休符 / ~ = タイ / end = 終わり / loop0x… = そこへ戻って繰り返す")
    L.append("#")
    L.append("#")
    L.append("# 音1・音2 のチャンネル3と4は**第2音源（SSG）側**で、命令表も境目も別物:")
    L.append("#   命令の境目は 0x59（FM は 0x5c）、表は cs:0x961（FM は cs:0x929）、")
    L.append("#   振り分けは cs:0x0d9c（FM は cs:0x0bce）。")
    L.append("#   m62 … SSG の命令 0x59（引数1バイト）。チャンネルの設定で、音符でも拍でもない")
    L.append("#   i0x562 … SSG では **6バイトの設定表**の番地（FM の29バイトの音色とは別物）")
    for num in sorted(tbl):
        for ch, (slot, off) in enumerate(tbl[num], 1):
            ev = parse(img, off, ssg=slot in SSG_SLOTS)
            L.append(f"{num} | {ch} | " + " ".join(tokens(ev)))
    open(path, "w", encoding="utf-8").write("\n".join(L) + "\n")

if __name__ == "__main__":
    if "--data" in sys.argv:
        here = os.path.dirname(__file__)
        write_data(os.path.join(here, "../Assets/Resources/Sounds.txt"))
        write_pitch(os.path.join(here, "../Assets/Resources/SoundPitch.txt"))
        write_voices(os.path.join(here, "../Assets/Resources/SoundVoices.txt"))
    else:
        main()
