#!/usr/bin/env python3
"""loadMapData2 のバイトコードから属性表を復元する。
パターン: getfield <table>; push <id>; aaload; push <idx>; push <val>; iastore"""
import sys,os,collections
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from jvm import parse
from jdis import disasm
cp,ms=parse(os.path.expanduser("~/Downloads/MagicTower 2/MTower.class"))
code=dict((n,c) for n,d,c in ms)
ins=disasm(cp, code["loadMapData2"])
def val(m,o):
    if m.startswith("iconst_"): return int(m.split("_")[1])
    if m in ("bipush","sipush"): return int(o)
    if m=="ldc":
        try: return int(o)
        except ValueError: return None
    return None
tables=collections.defaultdict(dict)
i=0
while i < len(ins)-6:
    a,m,o=ins[i]
    if m=="getfield" and ("objectAttribute" in o or "mapAttribute" in o):
        tbl = "object" if "objectAttribute" in o else "map"
        v1=val(*ins[i+1][1:]) ; 
        if v1 is not None and ins[i+2][1]=="aaload":
            v2=val(*ins[i+3][1:])
            v3=val(*ins[i+4][1:])
            if v2 is not None and v3 is not None and ins[i+5][1]=="iastore":
                tables[tbl].setdefault(v1,{})[v2]=v3
                i+=6; continue
    i+=1
for tbl in ("map","object"):
    t=tables[tbl]
    idxs=sorted({k for v in t.values() for k in v})
    print(f"\n=== {tbl}Attribute: {len(t)}個のID / 属性インデックス {idxs} ===")
    for tid in sorted(t)[:30]:
        row=t[tid]
        print(f"  ID{tid:>3}: " + " ".join(f"[{k}]={row[k]}" for k in sorted(row)))
