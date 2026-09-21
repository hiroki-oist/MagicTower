#!/usr/bin/env python3
"""Java .class を解析し、メソッド一覧とバイトコードを取り出す。"""
import struct,sys

def parse(path):
    d=open(path,'rb').read()
    assert d[:4]==b'\xca\xfe\xba\xbe'
    n=struct.unpack('>H',d[8:10])[0]; p=10; cp={}; i=1
    while i<n:
        t=d[p]; p+=1
        if t==1:
            L=struct.unpack('>H',d[p:p+2])[0]; p+=2
            cp[i]=('Utf8', d[p:p+L].decode('utf-8','replace')); p+=L
        elif t==3: cp[i]=('Int',struct.unpack('>i',d[p:p+4])[0]); p+=4
        elif t==4: cp[i]=('Float',0); p+=4
        elif t==5: cp[i]=('Long',struct.unpack('>q',d[p:p+8])[0]); p+=8; i+=1
        elif t==6: cp[i]=('Double',0); p+=8; i+=1
        elif t==7: cp[i]=('Class',struct.unpack('>H',d[p:p+2])[0]); p+=2
        elif t==8: cp[i]=('String',struct.unpack('>H',d[p:p+2])[0]); p+=2
        elif t in (9,10,11): cp[i]=('Ref',struct.unpack('>HH',d[p:p+4])); p+=4
        elif t==12: cp[i]=('NameType',struct.unpack('>HH',d[p:p+4])); p+=4
        elif t==15: p+=3
        elif t==16: p+=2
        elif t in (17,18): p+=4
        elif t in (19,20): p+=2
        else: raise ValueError(f"unknown cp tag {t} at {p-1}")
        i+=1
    # access, this, super, interfaces
    p+=6
    ni=struct.unpack('>H',d[p:p+2])[0]; p+=2+ni*2
    def skip_attrs(p):
        na=struct.unpack('>H',d[p:p+2])[0]; p+=2
        out=[]
        for _ in range(na):
            ni=struct.unpack('>H',d[p:p+2])[0]
            ln=struct.unpack('>I',d[p+2:p+6])[0]
            out.append((cp[ni][1], d[p+6:p+6+ln]))
            p+=6+ln
        return p,out
    # fields
    nf=struct.unpack('>H',d[p:p+2])[0]; p+=2
    for _ in range(nf):
        p+=6; p,_=skip_attrs(p)
    # methods
    nm=struct.unpack('>H',d[p:p+2])[0]; p+=2
    methods=[]
    for _ in range(nm):
        acc,ni,di=struct.unpack('>HHH',d[p:p+6]); p+=6
        p,attrs=skip_attrs(p)
        code=None
        for an,ab in attrs:
            if an=="Code":
                clen=struct.unpack('>I',ab[4:8])[0]
                code=ab[8:8+clen]
        methods.append((cp[ni][1], cp[di][1], code))
    return cp, methods

if __name__=="__main__":
    cp,ms=parse(sys.argv[1])
    print(f"定数プール {len(cp)} / メソッド {len(ms)}")
    ms2=[(len(c) if c else 0,n,d) for n,d,c in ms]
    ms2.sort(reverse=True)
    print("\n=== コードの大きいメソッド上位20 ===")
    for L,n,dsc in ms2[:20]:
        print(f"  {L:>6} バイト  {n}{dsc}")
