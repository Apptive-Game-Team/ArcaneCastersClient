#!/usr/bin/env python3
"""Measure and normalise player sprites. Subcommands:
  measure <png>                      -> bbox, eye centroid, eye side
  fit <cutout.png> <pose> <out.png>  -> place into 981x1245 canvas matching default pose by eye position and bottom
"""
import sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi
W='/home/yunseong/dev/worktrees/client-bot-appearance-art/Assets/Resources/PlayerAppearances/default/%s.png'

def cy_top(sl,top,H):
    return (sl[0].start+sl[0].stop)/2-top < 0.6*H

def eyes(im):
    a=np.array(im.convert('RGBA')).astype(float)
    alpha=a[:,:,3]>128
    rgb=a[:,:,:3]
    lum=0.299*rgb[:,:,0]+0.587*rgb[:,:,1]+0.114*rgb[:,:,2]
    mx=rgb.max(2); mn=rgb.min(2); sat=(mx-mn)/np.maximum(mx,1)
    m=alpha&(lum<95)&(sat<0.85)
    lab,n=ndi.label(m)
    ys,xs=np.where(alpha); H=ys.max()-ys.min()+1
    cands=[]
    for i,sl in enumerate(ndi.find_objects(lab),1):
        area=(lab[sl]==i).sum(); h=sl[0].stop-sl[0].start; w=sl[1].stop-sl[1].start
        if 0.0003<area/H**2<0.012 and 0.9<h/w<2.8 and cy_top(sl,ys.min(),H):
            comp=(lab==i)[sl]; filled=ndi.binary_fill_holes(comp); hole=filled&~comp
            if hole.sum()<4 or (lum[sl][hole]>150).mean()<0.5: continue
            cy,cx=ndi.center_of_mass(lab==i); cands.append((cx,cy,area))
    best=None
    for i in range(len(cands)):
        for j in range(i+1,len(cands)):
            p,q=cands[i],cands[j]
            if abs(p[1]-q[1])<0.25*abs(p[0]-q[0]) and 0.5<p[2]/q[2]<2 and 0.5*H*0.02<abs(p[0]-q[0])<0.2*H:
                sc=-min(p[2],q[2])
                if best is None or sc<best[0]: best=(sc,p,q)
    if best is None: return None
    p,q=best[1],best[2]
    return ((p[0]+q[0])/2,(p[1]+q[1])/2)

def info(im):
    al=np.array(im.convert('RGBA'))[:,:,3]
    ys,xs=np.where(al>8)
    bb=(xs.min(),ys.min(),xs.max(),ys.max())
    e=eyes(im)
    return bb,e

def side(im):
    bb,e=info(im)
    if e is None: return None
    cx=(bb[0]+bb[2])/2
    return e[0]-cx

if sys.argv[1]=='measure':
    im=Image.open(sys.argv[2]); bb,e=info(im)
    print(sys.argv[2].split('/')[-1],im.size,'bbox',bb,'h',bb[3]-bb[1]+1,'eyes',None if e is None else (round(e[0]),round(e[1])),'eye_minus_bbox_centre',None if e is None else round(e[0]-(bb[0]+bb[2])/2))
elif sys.argv[1]=='fit':
    im=Image.open(sys.argv[2]).convert('RGBA'); pose=sys.argv[3]
    ref=Image.open(W%pose); rb,re=info(ref)
    cb,ce=info(im)
    assert ce is not None,'eyes not found'
    s=(rb[3]-re[1])/(cb[3]-ce[1])
    im2=im.resize((round(im.width*s),round(im.height*s)),Image.LANCZOS)
    ex,ey=ce[0]*s,ce[1]*s
    ox=round(re[0]-ex); oy=round(re[1]-ey)
    canvas=Image.new('RGBA',(981,1245),(0,0,0,0))
    canvas.alpha_composite(im2,(ox,oy)) if ox>=0 and oy>=0 else canvas.paste(im2,(ox,oy))
    a=canvas.getchannel('A').point(lambda v:0 if v<8 else v); canvas.putalpha(a)
    canvas.save(sys.argv[4])
    print('scale',round(s,4),'offset',ox,oy)
