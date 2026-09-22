#!/usr/bin/env python3
"""Device Simulator に**新しい端末を足す**（Unity 公式の一覧が iPhone 13 で止まっているため）。

Unity の端末データは `com.unity.device-simulator.devices` パッケージが持っていて、
**1.0.1 が最後**（iPhone 13 世代まで）。Unity 本体を新しくしても増えない。
Device Simulator は **Assets の下の `*.device`（JSON）も読む**ので、ここで作る。

公式の `Apple iPhone 12.device` を雛形にして、画面の大きさ・dpi・安全領域・
機種名だけ差し替えたもの。端末の枠の絵（overlay）は持っていないので外してある。

  使い方: python3 docs/make_devices.py
"""
import os, json, copy

ROOT = os.path.expanduser("~/Code/MagicTower")
OUT = f"{ROOT}/Assets/DeviceSimulator"
CACHE = f"{ROOT}/Library/PackageCache/com.unity.device-simulator.devices@1.0.1/Editor/Devices"
BASE = f"{CACHE}/Apple iPhone 12.device"

# (名前, 横, 縦, dpi, 上の余白pt, 下の余白pt, 機種名, OS)
#   余白は Apple の「安全領域」の値（ダイナミックアイランドは59pt、ノッチは47/48pt、
#   下のホームバーは34pt）。実機に当てるときの目安。
DEVICES = [
    ("Apple iPhone 14",          1170, 2532, 460, 47, 34, "iPhone14,7",  "iOS 16.0"),
    ("Apple iPhone 14 Pro",      1179, 2556, 460, 59, 34, "iPhone15,2",  "iOS 16.0"),
    ("Apple iPhone 14 Pro Max",  1290, 2796, 460, 59, 34, "iPhone15,3",  "iOS 16.0"),
    ("Apple iPhone 15",          1179, 2556, 460, 59, 34, "iPhone15,4",  "iOS 17.0"),
    ("Apple iPhone 15 Pro Max",  1290, 2796, 460, 59, 34, "iPhone16,2",  "iOS 17.0"),
    ("Apple iPhone 16",          1179, 2556, 460, 59, 34, "iPhone17,3",  "iOS 18.0"),
    ("Apple iPhone 16 Pro",      1206, 2622, 460, 62, 34, "iPhone17,1",  "iOS 18.0"),
    ("Apple iPhone 16 Pro Max",  1320, 2868, 460, 62, 34, "iPhone17,2",  "iOS 18.0"),
    ("Apple iPhone SE (3rd gen)", 750, 1334, 326, 20,  0, "iPhone14,6",  "iOS 17.0"),
    # 参考に Android も1台（縦長のいちばん多い形）
    ("Google Pixel 8",           1080, 2400, 428, 24, 16, "Pixel 8",     "Android 14"),
]


def scale_of(w):
    return 3.0 if w >= 1100 else (2.0 if w >= 700 else 1.0)


def make(base, name, w, h, dpi, top_pt, bottom_pt, model, osname):
    d = copy.deepcopy(base)
    d["friendlyName"] = name
    s = d["screens"][0]
    s["width"], s["height"], s["dpi"] = w, h, float(dpi)
    k = scale_of(w)
    top, bottom = top_pt * k, bottom_pt * k
    for o in s["orientations"]:
        if o["orientation"] in (1, 2):                 # 縦（正・逆）
            o["safeArea"] = {"serializedVersion": "2", "x": 0.0, "y": float(bottom),
                             "width": float(w), "height": float(h - top - bottom)}
        else:                                          # 横
            o["safeArea"] = {"serializedVersion": "2", "x": float(top), "y": float(bottom),
                             "width": float(h - top * 2), "height": float(w - bottom)}
    s.pop("presentation", None)                        # 端末の枠の絵は持っていない
    d["systemInfo"]["deviceModel"] = model
    d["systemInfo"]["operatingSystem"] = osname
    return d


def main():
    if not os.path.exists(BASE):
        raise SystemExit(f"雛形が無い: {BASE}\n"
                         "（Package Manager で Device Simulator Devices を入れてから）")
    base = json.load(open(BASE, encoding="utf-8"))
    os.makedirs(OUT, exist_ok=True)
    for args in DEVICES:
        d = make(base, *args)
        path = f"{OUT}/{args[0]}.device"
        json.dump(d, open(path, "w", encoding="utf-8"), indent=4, ensure_ascii=False)
    print(f"{len(DEVICES)} 台を書き出した → {OUT}")
    for a in DEVICES:
        print(f"  {a[0]:<28} {a[1]}x{a[2]}  {a[3]}dpi")


if __name__ == "__main__":
    main()
