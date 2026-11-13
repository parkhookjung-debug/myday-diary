using System;
using System.Collections.Generic;
using System.Drawing;
namespace MyDay.Windows.Core
{
    public enum BlockKind { I,J,L,O,S,T,Z }
    public sealed class BlockGame
    {
        private static readonly Point[][] shapes={
            new[]{new Point(0,1),new Point(1,1),new Point(2,1),new Point(3,1)},
            new[]{new Point(0,0),new Point(0,1),new Point(1,1),new Point(2,1)},
            new[]{new Point(2,0),new Point(0,1),new Point(1,1),new Point(2,1)},
            new[]{new Point(1,0),new Point(2,0),new Point(1,1),new Point(2,1)},
            new[]{new Point(1,0),new Point(2,0),new Point(0,1),new Point(1,1)},
            new[]{new Point(1,0),new Point(0,1),new Point(1,1),new Point(2,1)},
            new[]{new Point(0,0),new Point(1,0),new Point(1,1),new Point(2,1)}};
        private static readonly Point[] kicks={new Point(0,0),new Point(-1,0),new Point(1,0),new Point(-2,0),new Point(2,0),new Point(0,-1),new Point(-1,-1),new Point(1,-1),new Point(0,-2)};
        private readonly int[,] board=new int[MiniGameRules.BlocksRows,MiniGameRules.BlocksColumns];
        private readonly Queue<BlockKind> next=new Queue<BlockKind>();
        private readonly int seed;
        private MiniGameRandom random;
        private double remainder,fallClock,lockClock;
        private int lockResets;
        private int[] clearedRows=new int[0];
        public SnackRoundState State {get;private set;}
        public BlockKind Kind {get;private set;}
        public int X {get;private set;}
        public int Y {get;private set;}
        public int Rotation {get;private set;}
        public int Score {get;private set;}
        public int Lines {get;private set;}
        public int Pieces {get;private set;}
        public int LastLinePoints {get;private set;}
        public double Elapsed {get;private set;}
        public double Flash {get;private set;}
        public string Message {get;private set;}
        public int Level {get{return Math.Min(20,1+Lines/MiniGameRules.BlocksLinesPerLevel);}}
        public double FallSeconds {get{return Math.Max(MiniGameRules.BlocksMinFallSeconds,MiniGameRules.BlocksFallSeconds*Math.Pow(.84,Level-1));}}
        public int CellAt(int x,int y){return board[y,x];}
        public BlockKind[] Next {get{return next.ToArray();}}
        public int[] ClearedRows {get{return (int[])clearedRows.Clone();}}
        public Point[] Cells {get{return CellsFor(Kind,Rotation);}}
        public int GhostY {get{int y=Y;while(Fits(Kind,Rotation,X,y+1))y++;return y;}}
        public BlockGame(int seed){this.seed=seed;Message="상몬 네 마리를 모아 한 줄을 채워요.";}
        public static Point[] CellsFor(BlockKind kind,int rotation){var cells=(Point[])shapes[(int)kind].Clone();if(kind==BlockKind.O)return cells;int count=(rotation%4+4)%4,size=kind==BlockKind.I?4:3;for(int turn=0;turn<count;turn++)for(int i=0;i<cells.Length;i++)cells[i]=new Point(size-1-cells[i].Y,cells[i].X);return cells;}
        public void Start(){Array.Clear(board,0,board.Length);next.Clear();random=new MiniGameRandom(seed);Score=Lines=Pieces=LastLinePoints=0;Elapsed=Flash=remainder=0;clearedRows=new int[0];State=SnackRoundState.Playing;Message="한 줄을 채우면 상몬들이 사르르!";Spawn();}
        private void FillBag(){var bag=new[]{BlockKind.I,BlockKind.J,BlockKind.L,BlockKind.O,BlockKind.S,BlockKind.T,BlockKind.Z};for(int i=bag.Length-1;i>0;i--){int j=random.Next(i+1);var saved=bag[i];bag[i]=bag[j];bag[j]=saved;}foreach(var kind in bag)next.Enqueue(kind);}
        private void Spawn(){if(next.Count==0)FillBag();Kind=next.Dequeue();if(next.Count<3)FillBag();X=MiniGameRules.BlocksColumns/2-2;Y=-1;Rotation=0;fallClock=lockClock=0;lockResets=0;if(!Fits(Kind,Rotation,X,Y))Finish();}
        private bool Fits(BlockKind kind,int rotation,int x,int y){foreach(var cell in CellsFor(kind,rotation)){int px=x+cell.X,py=y+cell.Y;if(px<0||px>=MiniGameRules.BlocksColumns||py>=MiniGameRules.BlocksRows||py< -4||(py>=0&&board[py,px]!=0))return false;}return true;}
        private void ResetLock(bool grounded){if(grounded&&lockResets<15){lockClock=0;lockResets++;}}
        public bool Move(int direction){if(State!=SnackRoundState.Playing||(direction!=-1&&direction!=1)||!Fits(Kind,Rotation,X+direction,Y))return false;bool grounded=!Fits(Kind,Rotation,X,Y+1);X+=direction;ResetLock(grounded);return true;}
        public bool Rotate(int direction){if(State!=SnackRoundState.Playing||Kind==BlockKind.O||(direction!=-1&&direction!=1))return false;int rotation=(Rotation+direction+4)%4;bool grounded=!Fits(Kind,Rotation,X,Y+1);foreach(var kick in kicks)if(Fits(Kind,rotation,X+kick.X,Y+kick.Y)){Rotation=rotation;X+=kick.X;Y+=kick.Y;ResetLock(grounded);return true;}return false;}
        private void AddScore(int amount){Score=(int)Math.Min(1000000L,(long)Score+amount);}
        public bool SoftDrop(){if(State!=SnackRoundState.Playing||!Fits(Kind,Rotation,X,Y+1))return false;Y++;AddScore(1);fallClock=0;return true;}
        public bool HardDrop(){if(State!=SnackRoundState.Playing)return false;int landing=GhostY;AddScore((landing-Y)*2);Y=landing;LockPiece();return true;}
        private void LockPiece(){foreach(var cell in Cells)if(Y+cell.Y<0){Finish();return;}foreach(var cell in Cells)board[Y+cell.Y,X+cell.X]=(int)Kind+1;Pieces++;var rows=new List<int>();int write=MiniGameRules.BlocksRows-1;for(int read=write;read>=0;read--){bool full=true;for(int x=0;x<MiniGameRules.BlocksColumns;x++)if(board[read,x]==0){full=false;break;}if(full){rows.Add(read);continue;}for(int x=0;x<MiniGameRules.BlocksColumns;x++)board[write,x]=board[read,x];write--;}while(write>=0){for(int x=0;x<MiniGameRules.BlocksColumns;x++)board[write,x]=0;write--;}
            LastLinePoints=0;if(rows.Count>0){LastLinePoints=new[]{0,100,300,500,800}[rows.Count]*Level;AddScore(LastLinePoints);Lines+=rows.Count;Flash=.45;clearedRows=rows.ToArray();Message=rows.Count+"줄 완성! +"+LastLinePoints+"점";}Spawn();}
        private void Finish(){State=SnackRoundState.Finished;Message="상몬들이 꼭대기까지 모였어요!";}
        public void Pause(){if(State==SnackRoundState.Playing)State=SnackRoundState.Paused;remainder=0;}
        public void Resume(){if(State==SnackRoundState.Paused)State=SnackRoundState.Playing;}
        public void Step(double seconds){if(State!=SnackRoundState.Playing||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;remainder+=Math.Min(.25,seconds);const double h=1.0/120;while(remainder+1e-10>=h&&State==SnackRoundState.Playing){remainder-=h;Elapsed+=h;Flash=Math.Max(0,Flash-h);fallClock+=h;while(fallClock>=FallSeconds){fallClock-=FallSeconds;if(Fits(Kind,Rotation,X,Y+1))Y++;}if(Fits(Kind,Rotation,X,Y+1))lockClock=0;else{lockClock+=h;if(lockClock>=MiniGameRules.BlocksLockSeconds-1e-8)LockPiece();}}}
        internal void SetCellForTest(int x,int y,int value){board[y,x]=value;}
        internal void ClearBoardForTest(){Array.Clear(board,0,board.Length);}
        internal void SetPieceForTest(BlockKind kind,int rotation,int x,int y){Kind=kind;Rotation=rotation;X=x;Y=y;fallClock=lockClock=0;lockResets=0;}
    }
}
