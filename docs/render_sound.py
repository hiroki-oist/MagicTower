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

# --- 4オペレータの合成（Unity 側の SoundSynth.FmVoice と同じ作り）------------

ALG_OUT = {0:(3,),1:(3,),2:(3,),3:(3,),4:(1,3),5:(1,2,3),6:(1,2,3),7:(0,1,2,3)}

def ratio(mul):
    return 0.5 if mul == 0 else mul

def rate(r, slowest):
    return 0.0 if r <= 0 else slowest * 2.0 ** ((r - 31) / 4.0)

def envelope(o, t, released, released_at):
    atk = rate(o["AR"], 600.0); dec = rate(o["DR"], 60.0)
    sus = rate(o["SR"], 6.0);   rel = rate(o["RR"] * 2 + 1, 120.0)
    sl = 0.0 if o["SL"] == 15 else 1.0 - o["SL"] / 16.0
    ta = 1.0 / atk if atk > 0 else 0.0
    if t < ta:
        e = t * atk if atk > 0 else 1.0
    else:
        td = t - ta
        d = 1.0 - td * dec if dec > 0 else 1.0
        e = d if d > sl else sl - (td - ((1.0 - sl) / dec if dec > 0 else 0.0)) * sus
    if released:
        e -= (t - released_at) * rel
    return max(0.0, min(1.0, e))

class Fm:
    def __init__(self, v):
        self.v = v; self.ph = [0.0] * 4; self.l1 = 0.0; self.l1b = 0.0

    def next(self, f, rate_hz, t, released, released_at):
        v = self.v; ops = v["ops"]
        fb = 0.0 if v["FB"] == 0 else 2.0 ** v["FB"] / 64.0
        for i in range(4):
            self.ph[i] += 2 * math.pi * f * ratio(ops[i]["MUL"]) / rate_hz
        m1 = fb * (self.l1 + self.l1b) * 0.5
        out = [0.0] * 4
        out[0] = (math.sin(self.ph[0] + m1) * level(ops[0]["TL"])
                  * envelope(ops[0], t, released, released_at))
        self.l1b, self.l1 = self.l1, out[0]

        def op(i, mod):
            return (math.sin(self.ph[i] + mod * 6.0) * level(ops[i]["TL"])
                    * envelope(ops[i], t, released, released_at))

        a = v["ALG"]
        if a == 0:
            out[1] = op(1, out[0]); out[2] = op(2, out[1]); out[3] = op(3, out[2])
        elif a == 1:
            out[1] = op(1, 0); out[2] = op(2, out[0] + out[1]); out[3] = op(3, out[2])
        elif a == 2:
            out[1] = op(1, 0); out[2] = op(2, out[1]); out[3] = op(3, out[0] + out[2])
        elif a == 3:
            out[1] = op(1, out[0]); out[2] = op(2, 0); out[3] = op(3, out[1] + out[2])
        elif a == 4:
            out[1] = op(1, out[0]); out[2] = op(2, 0); out[3] = op(3, out[2])
        elif a == 5:
            out[1] = op(1, out[0]); out[2] = op(2, out[0]); out[3] = op(3, out[0])
        elif a == 6:
            out[1] = op(1, out[0]); out[2] = op(2, 0); out[3] = op(3, 0)
        else:
            out[1] = op(1, 0); out[2] = op(2, 0); out[3] = op(3, 0)
        idx = ALG_OUT[a]
        return sum(out[i] for i in idx) / len(idx)

def fm_voices():
    """原典の音色（29バイト）。並びは OPN のレジスタの順。"""
    img = open(EXE, "rb").read()[HDR:]
    base = SEG * 16
    out = {}
    for a in (0x3c4, 0x3e1, 0x3fe, 0x41b):
        b = img[base + a:base + a + 29]
        out[a] = {
            "FB": (b[28] >> 3) & 7, "ALG": b[28] & 7,
            "ops": [{"DT": (b[i] >> 4) & 7, "MUL": b[i] & 15,
                     "TL": b[4 + i] & 0x7f,
                     "KS": (b[8 + i] >> 6) & 3, "AR": b[8 + i] & 0x1f,
                     "AM": (b[12 + i] >> 7) & 1, "DR": b[12 + i] & 0x1f,
                     "SR": b[16 + i] & 0x1f,
                     "SL": (b[20 + i] >> 4) & 15, "RR": b[20 + i] & 15,
                     "SSG": b[24 + i] & 15} for i in range(4)],
        }
    return out

VOICES = fm_voices()

def level(tl):
    return 10.0 ** (-tl * 0.75 / 20.0)

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
        amp = (volume / 15.0) ** 1.6 * 0.9
        fm = Fm(voice) if voice else None
        key_off = (n / float(rate)) * 0.85
        for i in range(n):
            t = i / float(rate)
            if fm is not None:
                w = fm.next(f, rate, t, (not tie) and t >= key_off, key_off)
            else:
                ph = 2 * math.pi * f * t
                env = 1.0
                rel = 0.03
                if not tie and (n - i) / float(rate) < rel:
                    env = (n - i) / float(rate) / rel
                w = env * (math.sin(ph) + 0.4 * math.sin(2 * ph) + 0.2 * math.sin(3 * ph)) / 1.6
            out.append(amp * w)
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
