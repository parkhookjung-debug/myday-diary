using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using MyDay.Windows.Core;

namespace MyDay.Windows.Character
{
    // The public painter stays shared by Windows and the Android asset exporter.
    public static partial class ItemPainter
    {
        public static Color Tone(ItemElement element) {return CozyItems.Palettes[(int)element];}
        public static void Draw(Graphics g,RectangleF area,SangmonItem item,double seconds)
        {
            if(item==null || area.Width<=0 || area.Height<=0)return;
            var saved=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
            float scale=Math.Min(area.Width/200,area.Height/240);
            g.TranslateTransform(area.X+area.Width/2,area.Y+area.Height/2);g.ScaleTransform(scale,scale);g.TranslateTransform(-100,-120);
            CozyItems.Draw(g,item);g.Restore(saved);
        }
        internal static void DrawEquipped(Graphics g,MonsterPose pose,SangmonEquipment equipment)
        {
            if(equipment==null)return;
            var held=SangmonItems.Find(equipment.WeaponId);var charm=SangmonItems.Find(equipment.CharmId);
            if(held!=null){var saved=g.Save();g.TranslateTransform(480,625);g.RotateTransform(-10+(float)Math.Sin(pose.Phase*2)*2);g.TranslateTransform(-480,-625);Draw(g,WeaponPlacement(held),held,pose.Phase);g.Restore(saved);}
            if(charm!=null)Draw(g,Placement("charm"),charm,pose.Phase);
        }
    }
}