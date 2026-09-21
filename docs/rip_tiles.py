#!/usr/bin/env python3
"""動画のフレームから、PC98タイルID毎のスプライトを 32x32 で切り出す。
IDと絵の対応は INITIAL.DAT の配置から決まるので取り違えが起きない。"""
import struct,os,sys,subprocess,collections
SP=sys.argv[1]
V={1:os.path.expanduser("~/Documents/魔法の塔 魔塔（PC98版） Tower of the Sorcerer Part 1 Level 1 - 30.mp4"),
   2:os.path.expanduser("~/Documents/魔法の塔 魔塔（PC98版） Tower of the Sorcerer Part 2 Level 31 - 50.mp4")}
# 階 -> (動画, フレームindex)
FR={**{n:(1,i) for n,i in zip(
      [1,3,2,5,4,6,7,8,9,10,11,12,14,15,16,17,18,19,20,13,21,22,23,24,26,27,28,29,30,31],
      [7,10,25,48,54,71,79,88,92,218,352,353,368,372,378,381,469,475,481,544,624,626,627,628,629,630,633,634,640,646])},
    32:(2,4),33:(2,45),34:(2,74),25:(2,851),
    35:(2,114),36:(2,120),37:(2,130),38:(2,142),39:(2,207),40:(2,211),41:(2,259),
    42:(2,363),43:(2,273),44:(2,339),45:(2,285),46:(2,286),47:(2,294),48:(2,624),
    49:(2,637),50:(2,882)}
d=open(os.path.join(SP,"pc98","INITIAL.DAT"),'rb').read()
w=[struct.unpack('<H',d[i:i+2])[0] for i in range(0,len(d)-1,2)]
loc=collections.defaultdict(list)
for n in range(1,51):
    b=196+(n-1)*124+3
    for k in range(121):
        if n in FR: loc[w[b+k]].append((n,k%11-5,6-k//11))
OUT=os.path.join(SP,"tiles"); os.makedirs(OUT,exist_ok=True)
TW=634/11.0; TH=632/11.0
done=[]; skipped=[]
for ID in sorted(loc):
    n,gx,gy = loc[ID][0]
    tag,idx = FR[n]
    px=int(256+(gx+5)*TW); py=int(44+(6-gy)*TH)
    out=os.path.join(OUT,f"tile_{ID:03d}.png")
    r=subprocess.run(["ffmpeg","-loglevel","error","-i",V[tag],
        "-vf",f"fps=1/2,select='eq(n\\,{idx})',crop=57:57:{px}:{py},scale=32:32:flags=area",
        "-vsync","0","-frames:v","1","-y",out],capture_output=True)
    if os.path.exists(out) and os.path.getsize(out)>0: done.append((ID,n,gx,gy))
    else: skipped.append(ID)
print(f"切り出し成功 {len(done)} / 失敗 {len(skipped)}")
print("失敗ID:", skipped)
