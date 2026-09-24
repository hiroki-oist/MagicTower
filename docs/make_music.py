# -*- coding: utf-8 -*-
"""Java版の MIDI から Unity 用の曲（Assets/Resources/Music/*.wav）を作る。

原典 PC98版には曲が2つしか無いので、ここで鳴らす曲は**作者の指定**で
Java版（MTower.class）に同梱されている MIDI から選んでいる。

  階のかたまり … Block1〜5（1〜10 / 11〜20 / 21〜30 / 31〜40 / 41〜50階）
  ボス戦       … Boss10 / Boss20 / Boss40 / Boss49
  終幕         … Ending（作者の指定で A_050。50階に戦いは無いのでボス戦の欄は使わない）
                 （どれも作者が全17本を聴いて選んだもの）

作り方:
  1. docs/tools/MidiRender.swift を建てて MIDI を WAV に書き出す
     （macOS 内蔵の音源。外部の音色ファイルは要らない）
  2. 頭の無音を落とし、MIDI の長さで切る（繰り返し再生の継ぎ目を短くするため）
  3. モノラル 22050Hz の 16bit WAV にして Assets/Resources/Music へ置く
     （.meta は Unity 側の取り込み設定。guid は既にあるものを引き継ぐ）

  python3 docs/make_music.py
"""
import os, re, struct, subprocess, sys, tempfile, wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MIDI = os.path.join(ROOT, "Assets/Images/MagicTower/midi")
OUT  = os.path.join(ROOT, "Assets/Resources/Music")
SRC  = os.path.join(ROOT, "docs/tools/MidiRender.swift")

# 曲名 → Java版の MIDI（AudioPlay の番号つき）
TRACKS = [
    ("Block1", "B_067", 27),   # 1〜10階
    ("Block2", "B_058", 26),   # 11〜20階
    ("Block3", "B_110", 28),   # 21〜30階
    ("Block4", "A_118", 23),   # 31〜40階
    ("Block5", "A_019", 16),   # 41〜50階
    ("Boss10", "B_117", 29),   # 10階（金の骸骨騎士）
    ("Boss20", "A_008", 15),   # 20階（ヴァンパイア）
    ("Boss40", "A_005", 13),   # 32階・40階（金騎士）
    ("Boss49", "A_023", 17),   # 49階（大魔導師の口上〜）
    ("Ending",  "A_050", 21),   # 終幕（作者の指定）
]

RATE = 22050          # Unity 側の取り込みもこの周波数
SILENCE = 300         # これ以下の振幅は無音とみなす（16bit）


def midi_seconds(path):
    """MIDI そのものの長さ（秒）。テンポと分解能から求める。"""
    d = open(path, "rb").read()
    hlen = struct.unpack(">I", d[4:8])[0]
    _, _, div = struct.unpack(">HHH", d[8:14])
    pos, us, maxtick = 8 + hlen, 500000, 0
    while pos < len(d) and d[pos:pos + 4] == b"MTrk":
        ln = struct.unpack(">I", d[pos + 4:pos + 8])[0]
        tr, pos = d[pos + 8:pos + 8 + ln], pos + 8 + ln
        i, tick, run = 0, 0, None
        while i < len(tr):
            v = 0
            while i < len(tr):
                b = tr[i]; i += 1; v = (v << 7) | (b & 0x7f)
                if not b & 0x80: break
            tick += v
            if i >= len(tr): break
            st = tr[i]
            if st < 0x80: st = run
            else:
                i += 1
                if st < 0xf0: run = st
            if st is None: break
            if st == 0xff:
                mt = tr[i]; i += 1; l = 0
                while i < len(tr):
                    b = tr[i]; i += 1; l = (l << 7) | (b & 0x7f)
                    if not b & 0x80: break
                if mt == 0x51 and l == 3 and us == 500000:
                    us = struct.unpack(">I", b"\0" + tr[i:i + 3])[0]
                i += l
            elif st in (0xf0, 0xf7):
                l = 0
                while i < len(tr):
                    b = tr[i]; i += 1; l = (l << 7) | (b & 0x7f)
                    if not b & 0x80: break
                i += l
            else:
                i += 1 if (st & 0xf0) in (0xc0, 0xd0) else 2
        maxtick = max(maxtick, tick)
    return maxtick / float(div) * (us / 1e6)


def to_mono_22k(path):
    """WAV を読んで、モノラル 22050Hz の 16bit の並びにする。"""
    w = wave.open(path, "rb")
    n, ch, rate = w.getnframes(), w.getnchannels(), w.getframerate()
    raw = w.readframes(n); w.close()
    s = struct.unpack("<%dh" % (len(raw) // 2), raw)
    mono = [(s[i * ch] + s[i * ch + 1]) // 2 for i in range(n)] if ch == 2 else list(s)
    step = max(1, int(round(rate / float(RATE))))
    return mono[::step]


def head_of_sound(samples):
    """鳴り始めの位置（無音の頭を落とすため）。"""
    for i, v in enumerate(samples):
        if abs(v) > SILENCE:
            return max(0, i - RATE // 50)     # 少しだけ前から
    return 0


def write_wav(path, samples):
    w = wave.open(path, "wb")
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
    w.writeframes(struct.pack("<%dh" % len(samples), *samples))
    w.close()


META = """fileFormatVersion: 2
guid: %s
AudioImporter:
  externalObjects: {}
  serializedVersion: 6
  defaultSettings:
    serializedVersion: 2
    loadType: 1
    sampleRateSetting: 0
    sampleRateOverride: 22050
    compressionFormat: 1
    quality: 0.5
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {}
  forceToMono: 1
  normalize: 0
  preloadAudioData: 0
  loadInBackground: 1
  ambisonic: 0
  3D: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def guid_for(meta_path, name):
    """.meta が既にあれば guid を引き継ぐ（無ければ名前から決める）。"""
    if os.path.exists(meta_path):
        m = re.search(r"^guid: ([0-9a-f]{32})$", open(meta_path).read(), re.M)
        if m: return m.group(1)
    import hashlib
    return hashlib.md5(("MagicTower/Music/" + name).encode()).hexdigest()


def main():
    render = os.path.join(tempfile.gettempdir(), "mt_midirender")
    if not os.path.exists(render) or os.path.getmtime(SRC) > os.path.getmtime(render):
        subprocess.check_call(["swiftc", "-O", SRC, "-o", render],
                              stderr=subprocess.DEVNULL)
    if not os.path.isdir(OUT): os.makedirs(OUT)
    for name, midi, num in TRACKS:
        src = os.path.join(MIDI, midi + "XGW.MID")
        sec = midi_seconds(src)
        tmp = os.path.join(tempfile.gettempdir(), "mt_%s.wav" % name)
        subprocess.check_call([render, src, tmp, "%.2f" % (sec + 2)],
                              stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        s = to_mono_22k(tmp)
        head = head_of_sound(s)
        s = s[head:head + int(sec * RATE)]
        dst = os.path.join(OUT, name + ".wav")
        write_wav(dst, s)
        meta = dst + ".meta"
        open(meta, "w").write(META % guid_for(meta, name))
        print("%-7s %s (AudioPlay %2d)  %5.1f秒  %d バイト"
              % (name, midi, num, len(s) / float(RATE), os.path.getsize(dst)))


if __name__ == "__main__":
    main()
