using System;
using System.Drawing;
using System.Windows.Forms;
namespace MyDay.Windows.Character
{
    public sealed partial class DesktopPet
    {
        private FetchGame fetch;
        private DesktopBallWindow toy;
        public bool FetchPlaying {get{return fetch!=null&&fetch.Active;}}
        internal FetchGame Fetch {get{return fetch;}}
        internal DesktopBallWindow Toy {get{return toy;}}
        private PointF PetCenter {get{return new PointF(dynamics.Position.X+artworkBounds.Left+artworkBounds.Width/2,dynamics.Position.Y+artworkBounds.Top+artworkBounds.Height/2);}}
        private RectangleF PetCenterRange(Rectangle area){var b=OriginBounds(area);b.Offset(artworkBounds.Left+artworkBounds.Width/2,artworkBounds.Top+artworkBounds.Height/2);return b;}
        public void StartFetch(){if(!Visible)Show();StartFetchCore(true);}
        internal void StartFetchForTest(){StartFetchCore(false);}
        private void StartFetchCore(bool show){StopFetch();CancelPointer();dynamics.Place(Location);var p=PetCenter;var area=Screen.FromPoint(Point.Round(p)).WorkingArea;fetch=new FetchGame(area,PetCenterRange(area),p);toy=new DesktopBallWindow(fetch,StopFetch){TopMost=TopMost};if(show)toy.Show(this);toy.RefreshFrame();}
        public void StopFetch(){var previous=toy;toy=null;if(fetch!=null)fetch.Stop();fetch=null;if(previous!=null)previous.Dispose();}
        private PointF StepFetch(double delta,Rectangle area){if(!FetchPlaying)return PointF.Empty;fetch.Resize(area,PetCenterRange(area));var old=fetch.Phase;fetch.Step(ContextMenuStrip.Visible||toy!=null&&toy.MenuOpen?0:delta,PetCenter);if(old!=FetchPhase.Cheering&&fetch.Phase==FetchPhase.Cheering)behavior.Hop();return fetch.Movement;}
    }
}
