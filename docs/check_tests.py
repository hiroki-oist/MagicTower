#!/usr/bin/env python3
"""EditMode テストが参照している API が実在するかを機械的に確かめる。

型名・メンバ名のタイプミスを捕まえるための簡易検査。

⚠ **これはコンパイルの保証にはならない。** 実際、この検査が「問題0件」と言っている
状態でも、テストが asmdef を持っていたために**ゲーム側の型が1つも見えず**
333件のコンパイルエラーになっていたことがある（2026-09-21 40:00 に発覚）。
この検査は「名前が Assets/Scripts のどこかにある」ことしか見ておらず、
**アセンブリをまたいで見えるかどうかは見ていない**。
本物の検証は docs/run_tests.sh（Unity のバッチモード）で行うこと。
"""
import re, glob, os, sys

ROOT = os.path.expanduser("~/Code/MagicTower")
TEST = os.path.join(ROOT, "Assets/Tests/Editor/OriginalDataTests.cs")

def strip_comments(text):
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return "\n".join(l for l in text.splitlines() if not l.lstrip().startswith("//"))

def run():
    raw = open(TEST, encoding="utf-8").read()
    test = strip_comments(raw)
    src = "\n".join(open(p, encoding="utf-8").read()
                    for p in glob.glob(os.path.join(ROOT, "Assets/Scripts/**/*.cs"), recursive=True))

    # プロジェクト内で定義されている型
    types = set(re.findall(r"\b(?:class|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)", src))

    bad = []
    # 「行頭または記号のあと」に現れる 型.メンバ だけを見る（連鎖参照を除く）
    for m in re.finditer(r"(?<![.\w])([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)", test):
        t, mem = m.group(1), m.group(2)
        if t not in types:
            continue                      # 外部の型（Assert など）は対象外
        if not re.search(rf"\b{mem}\b", src):
            bad.append(f"{t}.{mem}")

    counts = {
        "波括弧": (test.count("{"), test.count("}")),
        "丸括弧": (test.count("("), test.count(")")),
    }
    for label, (a, b) in counts.items():
        if a != b:
            bad.append(f"{label}の数が合わない ({a} 対 {b})")
    return bad, raw.count("[Test]"), len(types)

if __name__ == "__main__":
    bad, n, ntypes = run()
    print(f"テスト {n} 本 / プロジェクトの型 {ntypes} 種 / 問題 {len(bad)} 件")
    for b in bad:
        print("  ✗", b)
    sys.exit(1 if bad else 0)
