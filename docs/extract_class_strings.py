#!/usr/bin/env python3
"""MTower.class の定数プールから全文字列を取り出す（オリジナルのセリフの正典）。
使い方: python3 extract_class_strings.py <MTower.class>"""
import sys,struct
d=open(sys.argv[1],'rb').read()
assert d[:4]==b'\xca\xfe\xba\xbe', "Java class ファイルではありません"
n=struct.unpack('>H',d[8:10])[0]; p=10; i=1
while i<n:
    t=d[p]; p+=1
    if t==1:
        L=struct.unpack('>H',d[p:p+2])[0]; p+=2
        print(d[p:p+L].decode('utf-8','replace')); p+=L
    elif t in (7,8,16,19,20): p+=2
    elif t==15: p+=3
    elif t in (3,4,9,10,11,12,17,18): p+=4
    elif t in (5,6): p+=8; i+=1
    else: break
    i+=1
