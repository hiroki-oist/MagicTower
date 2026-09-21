#!/usr/bin/env python3
"""原典の譜面（Assets/Resources/Sounds.txt）を耳で確かめられる WAV にする。

音程は**原典の音程表そのもの**を使う。ドライバは YM2203 の F-Number 表を
cs:0x96f + 音符値 に持っていて（半音ごとに2バイト）、オクターブは
`o n` が block = n-1 として F-Number の上位に入る。周波数は OPN の式

    f = fnum × 3993600 / (144 × 2^(20 - block))

で、`o4` の「ラ」は 220Hz ちょうどになる（式が正しいことの確認）。

音長は原典の値×3が実際の長さ（cs:0x1382）。4分音符＝原典の 16＝48。
テンポ t なら 1目盛 = 60 / (t × 48) 秒。

音色（FM のパラメータ29バイト）は原典から取り出して控えてあるが、
ここでは**倍音を少し足した単純な波**で代用する。音程・長さ・強弱・
テンポ・繰り返しは原典どおり。

  使い方: python3 docs/render_sound.py [出力先ディレクトリ]
"""
import math, os, struct, sys, wave

HERE  = os.path.dirname(__file__)
EXE   = os.path.join(HERE, "original/disk/MTE.EXE")
DATA  = os.path.join(HERE, "../Assets/Resources/Sounds.txt")
HDR, SEG = 4608, 0x10da
FM_CLOCK = 3993600
RATE = 22050

NOTES = ["c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b"]

def fnum_table():
    """原典の音程表（半音0〜11）。"""
    img = open(EXE, "rb").read()[HDR:]
    base = SEG * 16 + 0x96f
    out = []
    for i in range(12):
        v = 0x42 + 2 * i
        lo, hi = img[base + v], img[base + v + 1]
        out.append(lo | ((hi & 7) << 8))
    return out

FNUM = fnum_table()

def freq(step, octave):
    """半音とオクターブから周波数。原典の式そのまま。"""
    block = max(0, min(7, octave - 1))
    return FNUM[step] * FM_CLOCK / (144.0 * (1 << (20 - block)))

def load_scores():
    scores = {}
    for line in open(DATA, encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#") or "|" not in line: continue
        p = [x.strip() for x in line.split("|")]
        scores.setdefault(int(p[0]), []).append(p[2].split())
    return scores

def fm_voices():
    """原典の音色。ALG と TL だけ使う（本物の4オペレータ合成はまだ作っていない）。"""
    img = open(EXE, "rb").read()[HDR:]
    base = SEG * 16
    out = {}
    for a in (0x3c4, 0x3e1, 0x3fe, 0x41b):
        b = img[base + a:base + a + 29]
        out[a] = {"ALG": b[28] & 7,
                  "TL": [b[4 + i] & 0x7f for i in range(4)],
                  "MUL": [b[i] & 15 for i in range(4)]}
    return out

VOICES = fm_voices()

def level(tl):
    return 10.0 ** (-tl * 0.75 / 20.0)

def fm_wave(ph, v):
    if v is None:
        return math.sin(ph) + 0.4 * math.sin(2 * ph) + 0.2 * math.sin(3 * ph)
    if v["ALG"] <= 3:
        return math.sin(ph + 6.0 * level(v["TL"][0]) * math.sin(max(1, v["MUL"][0]) * ph)) * 1.6
    return sum(level(v["TL"][i]) * math.sin(max(1, v["MUL"][i]) * ph) for i in range(4))

def render_channel(tokens, rate=RATE, tempo=120):
    """1チャンネルを鳴らして float の並びにする。"""
    octave, volume, tick, out = 4, 10, 60.0 / (tempo * 48), []
    voice = None
    default_len = 16
    ticks = 0        # 通しの目盛。音ごとに切り捨てると誤差が積もってチャンネルがずれる
    for tok in tokens:
        head, _, tail = tok.partition(":")
        length = int(tail) if tail else None
        tie = head.endswith("~")
        if tie: head = head[:-1]

        if head == "end" or head.startswith("loop"): break
        if head == "o+": octave += 1; continue
        if head == "o-": octave -= 1; continue
        if head == "v+": volume = min(15, volume + 3); continue
        if head == "v-": volume = max(0, volume - 3); continue
        if head and head[0] == "o" and head[1:].isdigit(): octave = int(head[1:]); continue
        if head and head[0] == "v" and head[1:].isdigit(): volume = int(head[1:]); continue
        if head and head[0] == "t" and head[1:].isdigit():
            tempo = int(head[1:]); tick = 60.0 / (tempo * 48); continue
        if head and head[0] == "l" and head[1:].isdigit(): default_len = int(head[1:]); continue
        if head and head[0] == "m" and head[1:].isdigit():
            continue        # SSG のチャンネル設定（原典の命令 0x59）。音符でも拍でもない
        if head and head[0] == "i":
            voice = VOICES.get(int(head[1:], 0)); continue      # 音色（ALG と TL だけ使う）

        L = length if length is not None else default_len
        frm = int(ticks * tick * rate); ticks += 3 * L
        n = int(ticks * tick * rate) - frm
        if head == "r":
            out.extend([0.0] * n); continue
        if head not in NOTES:
            continue
        f = freq(NOTES.index(head), octave)
        amp = (volume / 15.0) ** 1.6 * 0.5
        for i in range(n):
            t = i / float(rate)
            env = 1.0
            a = int(0.004 * rate)
            if i < a: env = i / float(a)                        # 立ち上がり
            rel = int(0.03 * rate)
            if not tie and n - i < rel: env = (n - i) / float(rel)
            ph = 2 * math.pi * f * t
            out.append(amp * env * fm_wave(ph, voice) / 1.6)
    return out

def render_sound(channels, tempo=120):
    # テンポは原典では**1つの割り込み**で全チャンネル共通。
    # 譜面はどれか1本にしか書いていないので、見つけたものを全体に効かせる。
    for c in channels:
        for tok in c:
            if tok and tok[0] == "t" and tok[1:].isdigit():
                tempo = int(tok[1:]); break
    parts = [render_channel(c, tempo=tempo) for c in channels]
    n = max((len(p) for p in parts), default=0)
    mix = [0.0] * n
    for p in parts:
        for i, v in enumerate(p): mix[i] += v
    if parts: 
        k = 1.0 / max(1, len(parts)) ** 0.5
        mix = [v * k for v in mix]
    return mix

def write_wav(path, samples, rate=RATE):
    with wave.open(path, "w") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes(b"".join(
            struct.pack("<h", max(-32000, min(32000, int(v * 32000)))) for v in samples))

def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "original/sound_wav")
    os.makedirs(outdir, exist_ok=True)
    scores = load_scores()
    for num in sorted(scores):
        mix = render_sound(scores[num])
        path = os.path.join(outdir, f"sound_{num:02d}.wav")
        write_wav(path, mix)
        print(f"音{num:2d}: {len(scores[num])}チャンネル  {len(mix)/RATE:6.2f}秒  {path}")

if __name__ == "__main__":
    main()
