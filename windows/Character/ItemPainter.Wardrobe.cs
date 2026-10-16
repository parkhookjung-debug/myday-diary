using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using MyDay.Windows.Core;
using Shape=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    public static partial class ItemPainter
    {
        private static RectangleF Placement(string slot)
        {
            switch(slot) {
                case "head":return new RectangleF(184,350,190,180);
                case "face":return new RectangleF(174,380,200,210);
                case "neck":return new RectangleF(175,470,210,180);
                case "back":return new RectangleF(335,440,220,260);
                case "aura":return new RectangleF(120,425,420,365);
                case "ground":return new RectangleF(145,690,350,135);
                case "weapon":return new RectangleF(415,450,190,300);
                default:return new RectangleF(340,410,118,126);
            }
        }
        private static RectangleF WeaponPlacement(SangmonItem item) {return new RectangleF(408,490,145,210);}
        internal static void DrawWearables(Graphics g,MonsterPose pose,SangmonEquipment equipment,bool behind)
        {
            foreach(string slot in behind?new[] {"ground","aura","back"}:new[] {"neck","head","face"}) {
                var item=SangmonItems.Find(SangmonItems.Get(equipment,slot));
                if(item!=null)Draw(g,Placement(slot),item,pose.Phase);
            }
        }
        public static void DrawOverlay(Graphics g,Rectangle area,SangmonItem item)
        {
            var save=g.Save();float scale=Math.Min(area.Width/555f,area.Height/430f);
            g.TranslateTransform(area.X+area.Width/2f,area.Y+area.Height/2f);g.ScaleTransform(scale,scale);g.TranslateTransform(-275,-615);
            if(item.Slot=="weapon"){g.TranslateTransform(480,625);g.RotateTransform(-10);g.TranslateTransform(-480,-625);}
            Draw(g,item.Slot=="weapon"?WeaponPlacement(item):Placement(item.Slot),item,0);g.Restore(save);
        }
    }
}
