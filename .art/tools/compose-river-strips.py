#!/usr/bin/env python3
"""Assemble RiverWaterStrip.png and RiverBridgeStrip.png (512 x 2560, 256 px per unit).

Run in a folder holding client-river-water-gen1.png (generated river segment) and
client-river-bridge-keyed.png (generated bridge, magenta keyed out with
key-out-background.py). Also writes client-river-composite.png: the strip with the
logic cells (columns 8 and 9, rows 0 to 9) drawn over it, for the alignment check.
"""
from PIL import Image, ImageDraw
import numpy as np, sys
P=256  # pixels per unit
# --- water
src=Image.open('client-river-water-gen1.png').convert('RGB'); a=np.array(src).astype(int)
row=a[600]  # a row; sand = high R,G and lowish B vs grass (G>R) vs water (B>R)
sand=(row[:,0]>200)&(row[:,1]>180)&(row[:,2]<190)
xs=np.where(sand)[0]; left=xs[xs<627]; right=xs[xs>627]
x0,x1=left.min(),right.max()+1
print('sand outer edges',x0,x1,x1-x0)
w=x1-x0
tile=src.crop((x0,0,x1,w)).resize((2*P,2*P),Image.LANCZOS)
strip=Image.new('RGB',(2*P,10*P))
for k in range(5):
    t=tile if k%2==0 else tile.transpose(Image.FLIP_TOP_BOTTOM)
    strip.paste(t,(0,k*2*P))
strip.save('client-river-water-strip.png')
# --- bridges overlay
br=Image.open('client-river-bridge-keyed.png'); al=np.array(br)[:,:,3]
ys,xs=np.where(al>128); box=(xs.min(),ys.min(),xs.max()+1,ys.max()+1)
deck=br.crop(box).resize((2*P,3*P),Image.LANCZOS)
ov=Image.new('RGBA',(2*P,10*P),(0,0,0,0))
for r0 in (1,6):
    y=(10-(r0+3))*P
    ov.paste(deck,(0,y),deck)
ov.save('client-river-bridge-strip.png')
# --- composite with logic grid
comp=strip.convert('RGBA'); comp.alpha_composite(ov)
d=ImageDraw.Draw(comp,'RGBA')
bridge={1,2,3,6,7,8}
for r in range(10):
    for c in range(2):
        x,y=c*P,(9-r)*P
        col=(255,0,0,255) if r in bridge else (255,255,0,255)
        d.rectangle([x,y,x+P-1,y+P-1],outline=col,width=3)
        d.text((x+8,y+8),f"{8+c},{r} {'bridge' if r in bridge else 'water'}",fill=col)
comp.save('client-river-composite.png')
