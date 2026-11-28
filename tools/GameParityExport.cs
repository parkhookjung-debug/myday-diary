// Exports real Windows engines for replay by shared/playroom/test-engine.js.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using MyDay.Windows.Core;
public static class GameParityExport {
    static string Json(object value){if(value==null)return "null";if(value is string)return "\""+((string)value).Replace("\\","\\\\").Replace("\"","\\\"")+"\"";if(value is bool)return (bool)value?"true":"false";var dictionary=value as Dictionary<string,object>;if(dictionary!=null)return "{"+string.Join(",",dictionary.Select(p=>Json(p.Key)+":"+Json(p.Value)))+"}";var list=value as System.Collections.IEnumerable;if(list!=null){var items=new List<string>();foreach(var item in list)items.Add(Json(item));return "["+string.Join(",",items)+"]";}return Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);}
    static object[] Cells(System.Drawing.Point[] cells){return cells.Select(p=>(object)new[]{p.X,p.Y}).ToArray();}
    static double[][] Board(Func<int,int,int> cell,int width,int height){return Enumerable.Range(0,height).Select(y=>Enumerable.Range(0,width).Select(x=>(double)cell(x,y)).ToArray()).ToArray();}
    public static int Main(string[] args){
      var rows=new List<Dictionary<string,object>>();
      foreach(int seed in new[]{1,397,20261128,-1,0}) {
        var snack=new SnackGame(seed);snack.Start();var jump=new JumpGame(seed);jump.Start();var fish=new FishingGame(seed);fish.Start();var sling=new SlingshotGame();sling.Start();var bake=new CookingGame();bake.Start();var blocks=new BlockGame(seed);blocks.Start();var puzzle=new BlockPuzzle(seed);
        for(int i=0;i<3600;i++){
          double axis=i%240<80?-1:i%240<160?1:0;
          if(i%90==0)jump.Jump();if(i%37==0)fish.Action();
          if(i%180==0){sling.BeginAim(225,450);sling.Aim(110,510);sling.Launch();}
          if(i%120==0)bake.BeginHeat();if(i%120==90)bake.ReleaseHeat();
          if(i%17==0)blocks.Move(i%2==0?-1:1);if(i%31==0)blocks.Rotate(1);if(i%53==0)blocks.SoftDrop();if(i%101==0)blocks.HardDrop();
          if(i%60==0&&!puzzle.Finished){bool placed=false;for(int slot=0;slot<3&&!placed;slot++)for(int y=0;y<8&&!placed;y++)for(int x=0;x<8&&!placed;x++)if(puzzle.CanPlace(puzzle.PieceAt(slot),x,y)){puzzle.TryPlace(slot,x,y);placed=true;}}
          snack.Step(1.0/60,axis);jump.Step(1.0/60);fish.Step(1.0/60);sling.Step(1.0/60);bake.Step(1.0/60);blocks.Step(1.0/60);
          if(i%120!=0&&i!=3599)continue;
          rows.Add(new Dictionary<string,object>{{"seed",seed},{"frame",i},{"snack",new object[]{snack.Score,snack.Lives,snack.PlayerX,snack.Eaten,snack.Combo,snack.State.ToString().ToLowerInvariant(),snack.Drops.Select(d=>(object)new object[]{(int)d.Kind,d.X,d.Y,d.Speed}).ToArray()}},{"jump",new object[]{jump.Score,jump.Lives,jump.Height,jump.Eaten,jump.Cleared,jump.State.ToString().ToLowerInvariant()}},{"fish",new object[]{fish.Score,fish.Catches,fish.Perfects,fish.Zone,fish.Indicator,fish.Phase.ToString().ToLowerInvariant()}},{"sling",new object[]{sling.Score,sling.Shots,sling.Goals,sling.X,sling.Y,sling.Flying}},{"bake",new object[]{bake.Score,bake.Completed,bake.Cooked,bake.Perfects,bake.Heat,bake.OrderIndex}},{"blocks",new object[]{blocks.Score,blocks.Lines,blocks.Pieces,blocks.X,blocks.Y,blocks.Rotation,(int)blocks.Kind,blocks.GhostY,Board(blocks.CellAt,10,20),blocks.Next.Select(k=>(int)k).ToArray()}},{"puzzle",new object[]{puzzle.Score,puzzle.Lines,puzzle.Moves,puzzle.Combo,puzzle.Finished,Board(puzzle.CellAt,8,8),Enumerable.Range(0,3).Select(slot=>{var p=puzzle.PieceAt(slot);return p==null?null:new object[]{p.Color,Cells(p.Cells)};}).ToArray()}}});
        }
      }
      File.WriteAllText(args[0],Json(rows),new System.Text.UTF8Encoding(false));
      Console.WriteLine("Exported "+rows.Count+" snapshots from all seven Windows engines.");return 0;
    }
}
