#!/usr/bin/env python3
"""Java .class を最小限パースし、指定文字列を参照している箇所の周辺の即値を拾う。"""
import struct,sys,re
d=open(sys.argv[1],'rb').read()
p=10; n=struct.unpack('>H',d[8:10])[0]
cp={}; i=1
while i<n:
    t=d[p]; p+=1
    if t==1:
        L=struct.unpack('>H',d[p:p+2])[0]; p+=2
        cp[i]=('Utf8', d[p:p+L].decode('utf-8','replace')); p+=L
    elif t==8:
        cp[i]=('String', struct.unpack('>H',d[p:p+2])[0]); p+=2
    elif t==7: cp[i]=('Class',struct.unpack('>H',d[p:p+2])[0]); p+=2
    elif t in (16,19,20): p+=2
    elif t==15: p+=3
    elif t in (3,): cp[i]=('Int',struct.unpack('>i',d[p:p+4])[0]); p+=4
    elif t in (4,9,10,11,12,17,18): p+=4
    elif t in (5,6): p+=8; i+=1
    else: break
    i+=1
cpend=p
# String定数 -> そのインデックス
s2i={}
for k,v in cp.items():
    if v[0]=='String' and cp.get(v[1],('',''))[0]=='Utf8':
        s2i.setdefault(cp[v[1]][1], []).append(k)
targets=[t for t in sys.argv[2:]]
print(f"定数プール {n} エントリ / コード領域は offset {cpend} 以降\n")
for t in targets:
    idxs=s2i.get(t)
    if not idxs: 
        # 部分一致
        idxs=[k for s,ks in s2i.items() if t in s for k in ks]
    if not idxs: print(f"  「{t}」: 見つからず"); continue
    for idx in idxs:
        # ldc(0x12)+1byte または ldc_w(0x13)+2byte を探す
        hits=[]
        for o in range(cpend,len(d)-3):
            if d[o]==0x12 and d[o+1]==idx: hits.append(o)
            elif d[o]==0x13 and struct.unpack('>H',d[o+1:o+3])[0]==idx: hits.append(o)
        for h in hits:
            ctx=d[max(0,h-24):h]
            imm=[]
            j=0
            while j<len(ctx):
                b=ctx[j]
                if b==0x10 and j+1<len(ctx): imm.append(ctx[j+1]); j+=2; continue
                if b==0x11 and j+2<len(ctx): imm.append(struct.unpack('>h',ctx[j+1:j+3])[0]); j+=3; continue
                if 0x02<=b<=0x08: imm.append(b-3); j+=1; continue
                j+=1
            print(f"  「{t[:28]}」 cp#{idx} @code {h}: 直前の即値 {imm[-6:]}")
