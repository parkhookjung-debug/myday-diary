using System;
using System.Collections.Generic;
using System.Drawing;
namespace MyDay.Windows.Core {
 public enum TetrisState { Ready,Playing,Paused,GameOver }
 public sealed class TetrisPiece { public int Kind;public Point[] Cells;public int X,Y; }
 public sealed class TetrisGame {
  public const int Columns=10,Rows=20;
  private static readonly Point[][] Shapes={
   new[]{new Point(0,1),new Point(1,1),new Point(2,1),new Point(3,1)},
   new[]{new Point(1,0),new Point(2,0),new Point(1,1),new Point(2,1)},
   new[]{new Point(1,0),new Point(0,1),new Point(1,1),new Point(2,1)},
   new[]{new Point(1,0),new Point(2,0),new Point(0,1),new Point(1,1)},
   new[]{new Point(0,0),new Point(1,0),new Point(1,1),new Point(2,1)},
   new[]{new Point(0,0),new Point(0,1),new Point(1,1),new Point(2,1)},
   new[]{new Point(2,0),new Point(0,1),new Point(1,1),new Point(2,1)}
  };
  public static readonly Color[] Colors={Color.FromArgb(107,177,190),Color.FromArgb(224,184,102),Color.FromArgb(174,145,190),Color.FromArgb(111,169,135),Color.FromArgb(211,133,130),Color.FromArgb(118,146,183),Color.FromArgb(216,157,103)};
  private readonly int[,] board=new int[Rows,Columns];private readonly Random random;private readonly List<int> bag=new List<int>();
  public TetrisPiece Current {get;private set;} public int NextKind {get;private set;} public int Score {get;private set;} public int Lines {get;private set;} public int Level {get{return Lines/10+1;}} public TetrisState State {get;private set;}
  public TetrisGame(int seed){random=new Random(seed);State=TetrisState.Ready;NextKind=DrawKind();}
  public int Cell(int x,int y){return board[y,x];}
  public void Start(){Array.Clear(board,0,board.Length);Score=Lines=0;bag.Clear();State=TetrisState.Playing;Spawn();}
  public void Pause(){if(State==TetrisState.Playing)State=TetrisState.Paused;}
  public void Resume(){if(State==TetrisState.Paused)State=TetrisState.Playing;}
  public bool Move(int dx){if(State!=TetrisState.Playing||!Fits(Current,Current.X+dx,Current.Y,Current.Cells))return false;Current.X+=dx;return true;}
  public bool Rotate(){if(State!=TetrisState.Playing)return false;var cells=new Point[4];for(int i=0;i<4;i++)cells[i]=new Point(3-Current.Cells[i].Y,Current.Cells[i].X);foreach(int kick in new[]{0,-1,1,-2,2})if(Fits(Current,Current.X+kick,Current.Y,cells)){Current.X+=kick;Current.Cells=cells;return true;}return false;}
  public bool SoftDrop(){if(State!=TetrisState.Playing)return false;if(Fits(Current,Current.X,Current.Y+1,Current.Cells)){Current.Y++;Score++;return true;}Lock();return false;}
  public void HardDrop(){if(State!=TetrisState.Playing)return;int moved=0;while(Fits(Current,Current.X,Current.Y+1,Current.Cells)){Current.Y++;moved++;}Score+=moved*2;Lock();}
  public void Tick(){if(State!=TetrisState.Playing)return;if(Fits(Current,Current.X,Current.Y+1,Current.Cells))Current.Y++;else Lock();}
  private bool Fits(TetrisPiece piece,int x,int y,Point[] cells){foreach(var p in cells){int bx=x+p.X,by=y+p.Y;if(bx<0||bx>=Columns||by>=Rows)return false;if(by>=0&&board[by,bx]!=0)return false;}return true;}
  private int DrawKind(){if(bag.Count==0){for(int i=0;i<7;i++)bag.Add(i);for(int i=bag.Count-1;i>0;i--){int j=random.Next(i+1);int t=bag[i];bag[i]=bag[j];bag[j]=t;}}int kind=bag[bag.Count-1];bag.RemoveAt(bag.Count-1);return kind;}
  private void Spawn(){Current=new TetrisPiece{Kind=NextKind,Cells=(Point[])Shapes[NextKind].Clone(),X=3,Y=0};NextKind=DrawKind();if(!Fits(Current,Current.X,Current.Y,Current.Cells))State=TetrisState.GameOver;}
  private void Lock(){foreach(var p in Current.Cells){int x=Current.X+p.X,y=Current.Y+p.Y;if(y>=0)board[y,x]=Current.Kind+1;}int cleared=0;for(int y=Rows-1;y>=0;y--){bool full=true;for(int x=0;x<Columns;x++)if(board[y,x]==0){full=false;break;}if(!full)continue;cleared++;for(int move=y;move>0;move--)for(int x=0;x<Columns;x++)board[move,x]=board[move-1,x];for(int x=0;x<Columns;x++)board[0,x]=0;y++;}if(cleared>0){int[] points={0,100,300,500,800};Score+=points[cleared]*Level;Lines+=cleared;}Spawn();}
 }
}
