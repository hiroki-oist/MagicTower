#!/usr/bin/env python3
"""JVM バイトコードを逆アセンブルする（必要な命令のみ実装）。"""
import struct,sys,os
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from jvm import parse

# opcode -> (ニーモニック, オペランド長, フォーマット)
OPS={
 0x00:("nop",0),0x01:("aconst_null",0),
 **{0x02+i:(f"iconst_{i-1}",0) for i in range(0,8)},
 0x10:("bipush",1),0x11:("sipush",2),0x12:("ldc",1),0x13:("ldc_w",2),0x14:("ldc2_w",2),
 0x15:("iload",1),0x19:("aload",1),
 **{0x1a+i:(f"iload_{i}",0) for i in range(4)},
 **{0x2a+i:(f"aload_{i}",0) for i in range(4)},
 0x2e:("iaload",0),0x32:("aaload",0),0x33:("baload",0),0x34:("caload",0),0x35:("saload",0),
 0x36:("istore",1),0x3a:("astore",1),
 **{0x3b+i:(f"istore_{i}",0) for i in range(4)},
 **{0x4b+i:(f"astore_{i}",0) for i in range(4)},
 0x4f:("iastore",0),0x53:("aastore",0),0x54:("bastore",0),0x55:("castore",0),0x56:("sastore",0),
 0x57:("pop",0),0x59:("dup",0),0x5a:("dup_x1",0),0x5c:("dup2",0),
 0x60:("iadd",0),0x64:("isub",0),0x68:("imul",0),0x6c:("idiv",0),0x70:("irem",0),0x74:("ineg",0),
 0x78:("ishl",0),0x7a:("ishr",0),0x7e:("iand",0),0x80:("ior",0),0x82:("ixor",0),0x84:("iinc",2),
 0x91:("i2b",0),0x92:("i2c",0),0x93:("i2s",0),
 0x99:("ifeq",2),0x9a:("ifne",2),0x9b:("iflt",2),0x9c:("ifge",2),0x9d:("ifgt",2),0x9e:("ifle",2),
 0x9f:("if_icmpeq",2),0xa0:("if_icmpne",2),0xa1:("if_icmplt",2),0xa2:("if_icmpge",2),
 0xa3:("if_icmpgt",2),0xa4:("if_icmple",2),0xa5:("if_acmpeq",2),0xa6:("if_acmpne",2),
 0xa7:("goto",2),0xac:("ireturn",0),0xb0:("areturn",0),0xb1:("return",0),
 0xb2:("getstatic",2),0xb3:("putstatic",2),0xb4:("getfield",2),0xb5:("putfield",2),
 0xb6:("invokevirtual",2),0xb7:("invokespecial",2),0xb8:("invokestatic",2),
 0xb9:("invokeinterface",4),0xbb:("new",2),0xbc:("newarray",1),0xbd:("anewarray",2),
 0xbe:("arraylength",0),0xbf:("athrow",0),0xc0:("checkcast",2),0xc1:("instanceof",2),
 0xc6:("ifnull",2),0xc7:("ifnonnull",2),
}
def cpname(cp,i):
    t=cp.get(i)
    if not t: return f"#{i}"
    k,v=t
    if k=="Utf8": return repr(v)
    if k=="String": return "str:"+repr(cp[v][1])
    if k=="Int": return str(v)
    if k=="Class":
        e=cp.get(v)
        return "cls:"+e[1] if e and e[0]=="Utf8" else f"cls#{i}"
    if k=="Ref":
        try:
            ci,nt=v
            cls=cp[cp[ci][1]][1]
            nm,ds=cp[nt][1]
            return f"{cls}.{cp[nm][1]}{cp[ds][1]}"
        except Exception: return f"ref#{i}"
    if k=="NameType":
        try: return f"{cp[v[0]][1]}{cp[v[1]][1]}"
        except Exception: return f"#{i}"
    return f"#{i}"

def disasm(cp, code, start=0, end=None):
    out=[]; p=start; end=end if end is not None else len(code)
    while p<end:
        op=code[p]
        if op==0xaa or op==0xab:   # tableswitch / lookupswitch
            out.append((p,"switch",""));
            q=(p+4)&~3
            if op==0xaa:
                dflt,lo,hi=struct.unpack('>iii',code[q:q+12]); q+=12+ (hi-lo+1)*4
            else:
                dflt,np=struct.unpack('>ii',code[q:q+8]); q+=8+np*8
            p=q; continue
        if op not in OPS: out.append((p,f"?{op:02x}","")); p+=1; continue
        mn,ln=OPS[op]
        arg=""
        if ln==1:
            v=code[p+1]
            arg=str(v-256 if (mn=="bipush" and v>127) else v)
            if mn in ("ldc",): arg=cpname(cp,v)
        elif ln==2:
            v=struct.unpack('>H',code[p+1:p+3])[0]
            if mn.startswith(("if","goto")):
                arg=f"→{p+struct.unpack('>h',code[p+1:p+3])[0]}"
            elif mn=="sipush": arg=str(struct.unpack('>h',code[p+1:p+3])[0])
            elif mn=="iinc": arg=f"local{code[p+1]} += {struct.unpack('>b',code[p+2:p+3])[0]}"
            else: arg=cpname(cp,v)
        elif ln==4:
            v=struct.unpack('>H',code[p+1:p+3])[0]; arg=cpname(cp,v)
        out.append((p,mn,arg)); p+=1+ln
    return out

if __name__=="__main__":
    cp,ms=parse(sys.argv[1])
    want=sys.argv[2]
    for n,d,c in ms:
        if n==want and c:
            s=int(sys.argv[3]) if len(sys.argv)>3 else 0
            e=int(sys.argv[4]) if len(sys.argv)>4 else len(c)
            print(f"=== {n}{d}  ({len(c)} バイト) 範囲 {s}..{e} ===")
            for a,m,o in disasm(cp,c,s,e): print(f"{a:6d}: {m:<16} {o}")
            break
