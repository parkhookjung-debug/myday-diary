using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.UI;

namespace MyDay.Windows
{
    internal static class Program
    {
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
        [STAThread]
        private static int Main(string[] args)
        {
            if(args.Length>0 && args[0]=="--self-test")
            {
                try { Tests.Run(); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }
            SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length==2 && args[0]=="--smoke-test")
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                try { Tests.Smoke(Path.GetFullPath(args[1])); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }
            if(args.Length==2 && args[0]=="--character-preview")
            {
                try { Tests.Preview(Path.GetFullPath(args[1])); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }
            if(args.Length==2 && args[0]=="--variant-preview")
            {
                try { Tests.VariantPreview(Path.GetFullPath(args[1])); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }
            if(args.Length==2 && args[0]=="--appearance-preview")
            {
                try { Tests.VariantPreview(Path.GetFullPath(args[1]),8,48); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }
            if(args.Length==2 && args[0]=="--game-preview")
            {
                try { Tests.VariantPreview(Path.GetFullPath(args[1]),48,8,true); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }
            if(args.Length==2 && (args[0]=="--variants-first" || args[0]=="--variants-second" || args[0]=="--variants-third"))
            {
                try { Tests.VariantPreview(Path.GetFullPath(args[1]),args[0]=="--variants-first"?0:args[0]=="--variants-second"?16:32,16); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }
            bool created;
            // One writer per Windows user prevents two windows overwriting each other's diary.
            using(var mutex=new Mutex(true,"Local\\MyDay.Windows."+System.Security.Principal.WindowsIdentity.GetCurrent().User.Value,out created))
            {
                if(!created) { MessageBox.Show("마이데이가 이미 실행 중이에요. 작업 표시줄 알림 영역의 MyDay 아이콘을 더블클릭하면 일기를 열 수 있어요.","MyDay"); return 0; }
                try
                {
                    var directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MyDay","Windows");
                    var store=new DiaryStore(directory);
                    using(var session=new DiarySession(store,store.Load())) Application.Run(session);
                    return 0;
                }
                catch(Exception ex)
                {
                    MessageBox.Show("마이데이를 열지 못했어요. 기록 파일이 손상된 경우 diary.json.bak을 따로 보관하고 복구해주세요.\n기존 기록을 빈 파일로 덮어쓰지 않았습니다.\n\n"+ex.Message,"MyDay 오류",MessageBoxButtons.OK,MessageBoxIcon.Error); return 1;
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }

    internal sealed class DiarySession : ApplicationContext
    {
        private readonly DiaryWindow window;
        public DiarySession(DiaryStore store,DiaryBook book)
        {
            window=new DiaryWindow(store,book);
            window.FormClosed+=delegate { ExitThread(); };
            window.StartOnDesktop();
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing) window.Dispose();
            base.Dispose(disposing);
        }
    }
}
