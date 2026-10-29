using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using MyDay.Windows.Core;
using MyDay.Windows.UI;

namespace MyDay.Windows
{
    [DataContract] internal sealed class DesktopReport
    {
        [DataMember] public string Digest;
        [DataMember] public string[][] Templates;
        [DataMember] public int[][] Layouts;
        [DataMember] public int[] Limits,Heights;
        [DataMember] public string[] Palette;
    }
    internal static class DesktopVerification
    {
        private static string Hex(Color color) {return "#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2");}
        internal static DesktopReport Report()
        {
            var layouts=DesktopFixtures.Layouts.Select(c=> {
                var result=DiaryLayout.Drag(new Rectangle(c[1],c[2],c[3],c[4]),new Point(c[5],c[6]),c[0]==1);
                var row=new[]{result.X,result.Y,result.Width,result.Height};
                if(!row.SequenceEqual(c.Skip(7)))throw new Exception("Shared layout fixture failed");
                return row;
            }).ToArray();
            return new DesktopReport {
                Digest=DesktopContract.SourceDigest,
                Templates=DiaryTemplates.All.Select(t=>new[]{t.Id,t.Name,t.Description,t.Style,t.Category,t.Minutes.ToString(System.Globalization.CultureInfo.InvariantCulture),t.Layout,t.Reference}
                    .Concat(t.Sections.SelectMany(s=>new[]{s.Kind,s.Title,s.Prompt})).ToArray()).ToArray(),
                Layouts=layouts,
                Limits=new[]{DesktopContract.MaxDays,DesktopContract.MaxBlocks,DesktopContract.MaxTextUtf16,DesktopContract.MaxTitleUtf16,DesktopContract.MaxPromptUtf16,
                    DiaryLayout.MinWidth,DiaryLayout.MaxWidth,DiaryLayout.MinHeight,DiaryLayout.MaxHeight,DiaryLayout.MaxPosition,
                    DesktopContract.ChatProtocol,DesktopContract.ChatMaxText,DesktopContract.ChatMaxPage,DesktopContract.ChatMaxUsers,DesktopContract.ChatVisibleHistory},
                Heights=DesktopContract.BlockKinds.Select(k=>DiaryLayout.DefaultHeight(DiaryBlock.Create(k))).ToArray(),
                Palette=new[]{Design.Ink,Design.Muted,Design.Accent,Design.Paper}.Concat(Design.Backgrounds).Select(Hex).ToArray()
            };
        }
        internal static void Export(string file)
        {
            var report=Report();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file)));
            using(var stream=File.Create(file))new DataContractJsonSerializer(typeof(DesktopReport)).WriteObject(stream,report);
            Console.WriteLine("Windows shared desktop contract: passed ("+report.Templates.Length+" templates, "+report.Layouts.Length+" geometry cases).");
        }
    }
    internal static partial class Tests
    {
        private static void RunSharedDesktopTests()
        {
            var report=DesktopVerification.Report();
            Check(report.Templates.Length==DesktopContract.TemplateCount,"Windows uses the shared template catalog");
            Check(report.Layouts.Length==DesktopFixtures.Layouts.Length,"Windows matches common drag and resize fixtures");
            Check(DesktopContract.BlockKinds.All(k=>report.Heights[Array.IndexOf(DesktopContract.BlockKinds,k)]>0),"Common block types have native default heights");
        }
    }
}
