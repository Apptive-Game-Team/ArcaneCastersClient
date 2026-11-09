Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @((Get-ChildItem "$PSHOME/ref/*.dll").FullName + ([Drawing.Bitmap].Assembly.Location) + (Get-ChildItem "$PSHOME/System.Private.Windows*.dll").FullName) -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class ChestSpriteFinalize {
 public static Bitmap Clean(string path) {
  var source=new Bitmap(path); int w=source.Width,h=source.Height; var pixels=new bool[w*h];
  for(int y=0;y<h;y++)for(int x=0;x<w;x++)pixels[y*w+x]=source.GetPixel(x,y).A>128;
  var seen=new bool[w*h]; var largest=new List<int>(); var queue=new Queue<int>();
  for(int i=0;i<pixels.Length;i++){if(!pixels[i]||seen[i])continue;var part=new List<int>();seen[i]=true;queue.Enqueue(i);
   while(queue.Count>0){int p=queue.Dequeue();part.Add(p);int x=p%w,y=p/w;int[] ns={x>0?p-1:-1,x<w-1?p+1:-1,y>0?p-w:-1,y<h-1?p+w:-1};foreach(int n in ns)if(n>=0&&pixels[n]&&!seen[n]){seen[n]=true;queue.Enqueue(n);}}
   if(part.Count>largest.Count)largest=part;
  }
  var keep=new bool[w*h];foreach(int i in largest)keep[i]=true;var result=new Bitmap(w,h,PixelFormat.Format32bppArgb);
  for(int y=2;y<h-2;y++)for(int x=2;x<w-2;x++){bool inside=true;for(int yy=y-1;yy<=y+1;yy++)for(int xx=x-1;xx<=x+1;xx++)if(!keep[yy*w+xx])inside=false;if(inside)result.SetPixel(x,y,source.GetPixel(x,y));}
  source.Dispose();return result;
 }
 public static Rectangle Bounds(Bitmap image){int left=image.Width,top=image.Height,right=-1,bottom=-1;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>24){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}return Rectangle.FromLTRB(left,top,right+1,bottom+1);}
 public static Bitmap Render(Bitmap source,int shiftX,Rectangle crop,int size){var target=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(target)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;float scale=(float)(size-8)/Math.Max(crop.Width,crop.Height);float x=(size-crop.Width*scale)/2f+(shiftX-crop.Left)*scale;float y=(size-crop.Height*scale)/2f-crop.Top*scale;g.DrawImage(source,x,y,source.Width*scale,source.Height*scale);}return target;}
 public static void Run(string directory,string assets){using(var closed=Clean(directory+"/closed-raw.png"))using(var opened=Clean(directory+"/open-raw.png")){
  int shift=50;var cb=Bounds(closed);var ob=Bounds(opened);ob.Offset(shift,0);var union=Rectangle.Union(cb,ob);
  using(var closedAnimation=Render(closed,0,union,256))using(var openAnimation=Render(opened,shift,union,256))using(var icon=Render(closed,0,cb,256)){
   closedAnimation.Save(assets+"/animation_closed.png",ImageFormat.Png);openAnimation.Save(assets+"/open.png",ImageFormat.Png);icon.Save(assets+"/closed.png",ImageFormat.Png);
   closedAnimation.Save(directory+"/closed-animation.png",ImageFormat.Png);openAnimation.Save(directory+"/open-animation.png",ImageFormat.Png);icon.Save(directory+"/closed-icon.png",ImageFormat.Png);
   Console.WriteLine("Raw closed bounds: "+cb+"; aligned open bounds: "+ob+"; shared crop: "+union);
   Console.WriteLine("Final closed animation bounds: "+Bounds(closedAnimation)+"; final open animation bounds: "+Bounds(openAnimation)+"; icon bounds: "+Bounds(icon));
  }
 }}
}
"@
[ChestSpriteFinalize]::Run((Join-Path $PSScriptRoot '.'),(Join-Path $PSScriptRoot '../../../Assets/Resources/RewardChest/Chests/default'))