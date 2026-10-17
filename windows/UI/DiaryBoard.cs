using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class DiaryBoard : Panel
    {
        private readonly Action changed;
        private DiaryEntry entry;
        private BlockCard dragged;
        private Rectangle original,pending;
        private Point pointerStart,scrollStart;
        private bool resizing;
        public DiaryBoard(Action changed) { this.changed=changed; DoubleBuffered=true; AutoScroll=true; }
        public int LogicalWidth { get { return Math.Max(DiaryLayout.MinWidth+28,Design.U(ClientSize.Width-SystemInformation.VerticalScrollBarWidth)); } }
        public void AddCard(BlockCard card)
        {
            card.DragHandle.Started+=p=>Begin(card,p,false);
            card.ResizeHandle.Started+=p=>Begin(card,p,true);
            card.DragHandle.Moved+=MovePlacement; card.ResizeHandle.Moved+=MovePlacement;
            card.DragHandle.Ended+=Finish; card.ResizeHandle.Ended+=Finish;
            Controls.Add(card); card.BringToFront();
        }
        public void Arrange(DiaryEntry value)
        {
            entry=value;
            if(dragged!=null) return;
            var auto=DiaryLayout.AutoArrange(entry.Blocks,LogicalWidth);
            var cards=Controls.OfType<BlockCard>().ToDictionary(c=>c.Block.Id);
            SuspendLayout();
            for(int i=0;i<entry.Blocks.Count;i++) {
                BlockCard card;
                if(!cards.TryGetValue(entry.Blocks[i].Id,out card)) continue;
                Place(card,entry.LayoutMode=="free"?DiaryLayout.Bounds(card.Block):auto[i]);
                card.BringToFront();
            }
            UpdateExtent(); ResumeLayout(false);
        }
        private void Place(BlockCard card,Rectangle units)
        {
            var offset=AutoScrollPosition;
            card.Bounds=new Rectangle(Design.P(units.X)+offset.X,Design.P(units.Y)+offset.Y,Design.P(units.Width),Design.P(units.Height));
        }
        private void UpdateExtent()
        {
            var offset=AutoScrollPosition;
            int right=0,bottom=0;
            foreach(var card in Controls.OfType<BlockCard>()) { right=Math.Max(right,card.Right-offset.X); bottom=Math.Max(bottom,card.Bottom-offset.Y); }
            AutoScrollMinSize=new Size(right+Design.P(24),bottom+Design.P(24));
        }
        private void Begin(BlockCard card,Point pointer,bool resize)
        {
            if(entry==null || entry.LayoutMode!="free") return;
            dragged=card; original=pending=DiaryLayout.Bounds(card.Block); pointerStart=pointer;
            scrollStart=new Point(-AutoScrollPosition.X,-AutoScrollPosition.Y); resizing=resize;
            card.BringToFront();
        }
        private void MovePlacement(Point pointer)
        {
            if(dragged==null) return;
            var scroll=AutoScrollPosition;
            var delta=new Point(Design.U(pointer.X-pointerStart.X-scroll.X-scrollStart.X),Design.U(pointer.Y-pointerStart.Y-scroll.Y-scrollStart.Y));
            pending=DiaryLayout.Drag(original,delta,resizing); Place(dragged,pending); UpdateExtent();
        }
        private void Finish(bool commit)
        {
            if(dragged==null) return;
            var card=dragged; dragged=null;
            if(commit && pending!=original) {
                DiaryLayout.SetBounds(card.Block,pending);
                entry.Blocks.Remove(card.Block); entry.Blocks.Add(card.Block);
                changed();
            }
            Arrange(entry);
            if(commit) ScrollControlIntoView(card);
        }
        public void CancelPlacement()
        {
            if(dragged==null) return;
            var card=dragged; card.DragHandle.Cancel(); card.ResizeHandle.Cancel();
            if(dragged!=null) Finish(false);
        }
    }

    public sealed class PlacementHandle : Panel
    {
        private bool active;
        public bool Editing;
        public event Action<Point> Started,Moved;
        public event Action<bool> Ended;
        public void Forward(Control child)
        {
            child.MouseDown+=delegate(object sender,MouseEventArgs e) { if(e.Button==MouseButtons.Left) Begin(child.PointToScreen(e.Location)); };
            child.MouseMove+=delegate(object sender,MouseEventArgs e) { if(active && Moved!=null) Moved(child.PointToScreen(e.Location)); };
            child.MouseUp+=delegate(object sender,MouseEventArgs e) { if(e.Button==MouseButtons.Left) Complete(true); };
        }
        private void Begin(Point p)
        {
            if(!Editing) return;
            active=true; Capture=true; if(Started!=null) Started(p);
        }
        private void Complete(bool commit)
        {
            if(!active) return;
            active=false; Capture=false; if(Ended!=null) Ended(commit);
        }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if(e.Button==MouseButtons.Left) Begin(PointToScreen(e.Location)); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if(active && Moved!=null) Moved(PointToScreen(e.Location)); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if(e.Button==MouseButtons.Left) Complete(true); }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if(active && !Capture) Complete(false); }
        public void Cancel() { Complete(false); }
        internal void DragForTest(Point start,Point end,bool cancel)
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,PointToClient(start).X,PointToClient(start).Y,0));
            var next=PointToClient(end); OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,next.X,next.Y,0));
            if(cancel) Cancel(); else { next=PointToClient(end); OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,next.X,next.Y,0)); }
        }
    }
}
