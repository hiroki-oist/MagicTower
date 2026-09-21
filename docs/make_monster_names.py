#!/usr/bin/env python3
"""モンスターの名前を MTE.EXE から取り出して Battlers の .asset に書き戻す。

名前は相手の表（DS:0x63c から 14バイト/件）の **+10 の遠ポインタ**の先にある。
原典は名前を**そのまま**右下の欄の (528,272) に書くので、
**文字列の側に「中央に寄せるための空白」が入っている**:

    'スケルトンＣ' … 6文字ちょうど、空白なし
    '　大バット'   … 全角空白1つ（16ドット）＋4文字 ＝ 80ドット
    '　 バット'    … 全角＋半角（24ドット）＋3文字 ＝ 72ドット
    '　　僧侶'     … 全角2つ（32ドット）＋2文字 ＝ 64ドット

欄は96ドット（全角6文字）なので、どれも **(96 - 文字の幅) / 2** ぴったり。
つまり空白は中央寄せのためのもので、**名前の一部として持っておく**のが原典どおり。

  使い方: python3 docs/make_monster_names.py
"""
import os, re, glob, struct, json

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DG = 4608 + 0x12340
TABLE, TABLE_END, STRIDE = 0x63c, 0x112c, 14


def original_names():
    exe = open(f"{ROOT}/docs/original/disk/MTE.EXE", "rb").read()
    out, base = {}, TABLE
    while base + STRIDE <= TABLE_END:
        mid = struct.unpack("<H", exe[DG + base:DG + base + 2])[0]
        off = struct.unpack("<H", exe[DG + base + 10:DG + base + 12])[0]
        base += STRIDE
        if mid == 0:
            break
        if mid in out or off == 0:
            continue
        end = exe.find(b"\x00", DG + off, DG + off + 40)
        if end < 0:
            continue
        try:
            out[mid] = exe[DG + off:end].decode("cp932")
        except UnicodeDecodeError:
            pass
    return out


def object_ids():
    """Battlers の名札 → 物体ID。

    ふだんは `ObjectIds.txt` で引けるが、3x3のボスだけ盤面のプレハブ名
    （`Boss_Dragon`）と戦う相手の名札（`Enemy_Dragon`）が別なので、
    **代表のマスのID**で結び直す（大ダコ=104 / ドラゴン=122）。
    """
    ids = {}
    for line in open(f"{ROOT}/Assets/Resources/ObjectIds.txt", encoding="utf-8"):
        if line.startswith("#") or "|" not in line:
            continue
        a, b = line.split("|")
        ids.setdefault(a.strip(), int(b.strip()))
    ids["Enemy_GiantOctopus"] = 104
    ids["Enemy_Dragon"] = 122
    return ids


def escape(s):
    """Unity の YAML に入れる形。日本語は \\uXXXX で書く。"""
    return '"' + "".join(c if 0x20 <= ord(c) < 0x7f and c != '"' and c != "\\"
                         else f"\\u{ord(c):04X}" for c in s) + '"'


def main():
    names, ids = original_names(), object_ids()
    changed = 0
    for path in sorted(glob.glob(f"{ROOT}/Assets/Resources/Battlers/*.asset")):
        key = os.path.basename(path)[:-len(".asset")]
        oid = ids.get(key)
        if oid is None or oid not in names:
            continue
        text = open(path, encoding="utf-8").read()
        want = escape(names[oid])
        new = re.sub(r"\n  name: .*", lambda _m: "\n  name: " + want, text, count=1)
        if new != text:
            open(path, "w", encoding="utf-8").write(new)
            changed += 1
    print(f"名前を書き戻した {changed} 件 / 原典から取れた名前 {len(names)} 件")


if __name__ == "__main__":
    main()
