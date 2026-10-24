// A bounded byte-mode QR v6-L encoder adapted from Project Nayuki's MIT implementation.
// The complete MIT notice is in tools/vendor/qrcodegen.py and tools/vendor/README.md.
// Fixed version and mask keep this .NET Framework build dependency-free.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
namespace MyDay.Windows.Core {
    public sealed class DeviceQr {
        public const int Size=41;
        public readonly bool[,] Modules=new bool[Size,Size];
        private readonly bool[,] function=new bool[Size,Size];
        private void Set(int x,int y,bool dark){Modules[y,x]=dark;function[y,x]=true;}
        private static int Multiply(int x,int y){int z=0;for(int i=7;i>=0;i--){z=(z<<1)^((z>>7)*0x11D);z^=((y>>i)&1)*x;}return z;}
        private static void Bits(List<int> bits,int value,int count){for(int i=count-1;i>=0;i--)bits.Add((value>>i)&1);}
        public DeviceQr(string text) {
            byte[] input=Encoding.UTF8.GetBytes(text);if(input.Length>134)throw new ArgumentException("연결 주소가 너무 길어요.");
            var bits=new List<int>();Bits(bits,4,4);Bits(bits,input.Length,8);foreach(byte b in input)Bits(bits,b,8);Bits(bits,0,Math.Min(4,1088-bits.Count));while(bits.Count%8!=0)bits.Add(0);
            var data=new List<int>();for(int i=0;i<bits.Count;i+=8){int v=0;for(int j=0;j<8;j++)v=(v<<1)|bits[i+j];data.Add(v);}int pad=0;while(data.Count<136)data.Add((pad++%2==0)?0xEC:0x11);
            int[] divisor=new int[18];divisor[17]=1;int root=1;for(int i=0;i<18;i++){for(int j=0;j<18;j++){divisor[j]=Multiply(divisor[j],root);if(j+1<18)divisor[j]^=divisor[j+1];}root=Multiply(root,2);}
            int[][] blocks=new int[2][];for(int b=0;b<2;b++){var rem=new int[18];blocks[b]=new int[86];for(int i=0;i<68;i++){int v=data[b*68+i];blocks[b][i]=v;int factor=v^rem[0];Array.Copy(rem,1,rem,0,17);rem[17]=0;for(int j=0;j<18;j++)rem[j]^=Multiply(divisor[j],factor);}Array.Copy(rem,0,blocks[b],68,18);}
            var codewords=new List<int>();for(int i=0;i<86;i++)for(int b=0;b<2;b++)codewords.Add(blocks[b][i]);
            for(int i=0;i<Size;i++){Set(6,i,i%2==0);Set(i,6,i%2==0);}Finder(3,3);Finder(Size-4,3);Finder(3,Size-4);
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)Set(34+dx,34+dy,Math.Max(Math.Abs(dx),Math.Abs(dy))!=1);
            Format();int index=0;
            for(int right=Size-1;right>=1;right-=2){if(right==6)right=5;for(int vert=0;vert<Size;vert++)for(int j=0;j<2;j++){int x=right-j;bool up=((right+1)&2)==0;int y=up?Size-1-vert:vert;if(function[y,x])continue;bool dark=index<codewords.Count*8 && ((codewords[index/8]>>(7-index%8))&1)!=0;index++;Modules[y,x]=dark^((x+y)%2==0);}}
            Format();
        }
        private void Finder(int x,int y){for(int dy=-4;dy<=4;dy++)for(int dx=-4;dx<=4;dx++){int xx=x+dx,yy=y+dy;if(xx>=0&&yy>=0&&xx<Size&&yy<Size){int d=Math.Max(Math.Abs(dx),Math.Abs(dy));Set(xx,yy,d!=2&&d!=4);}}}
        private void Format(){int data=8,rem=data;for(int i=0;i<10;i++)rem=(rem<<1)^((rem>>9)*0x537);int bits=((data<<10)|rem)^0x5412;for(int i=0;i<6;i++)Set(8,i,((bits>>i)&1)!=0);Set(8,7,((bits>>6)&1)!=0);Set(8,8,((bits>>7)&1)!=0);Set(7,8,((bits>>8)&1)!=0);for(int i=9;i<15;i++)Set(14-i,8,((bits>>i)&1)!=0);for(int i=0;i<8;i++)Set(Size-1-i,8,((bits>>i)&1)!=0);for(int i=8;i<15;i++)Set(8,Size-15+i,((bits>>i)&1)!=0);Set(8,Size-8,true);}
        public Bitmap Image(){var image=new Bitmap((Size+8)*6,(Size+8)*6);using(var g=Graphics.FromImage(image)){g.Clear(Color.White);for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)if(Modules[y,x])g.FillRectangle(Brushes.Black,(x+4)*6,(y+4)*6,6,6);}return image;}
        public string Matrix(){var text=new StringBuilder();for(int y=0;y<Size;y++){for(int x=0;x<Size;x++)text.Append(Modules[y,x]?'1':'0');text.Append('\n');}return text.ToString();}
    }
}
