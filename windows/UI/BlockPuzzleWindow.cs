using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class BlockPuzzleWindow : Form
    {
        private readonly BlockPuzzle game;
        private readonly GameRecords records;
        private readonly MiniSceneCanvas canvas = new MiniSceneCanvas();
        private readonly Button restart = Design.Button("새 게임", true), export = Design.Button("결과 카드 저장");
        private readonly Label status = Design.Label("", 9);
        private readonly Timer timer = new Timer { Interval = 33 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private const int BoardLeft = 300, BoardTop = 82, Cell = 48;
        private int selected = -1, cursorX, cursorY;
        private bool dragging, moved, saved;
        private PointF pressed;
        private double flashUntil;
        private string message = "아래 블록을 골라 빈칸에 놓아보세요.";
        private MiniGameResult result;
        internal BlockPuzzle Game { get { return game; } }
        internal int Selected { get { return selected; } }
        internal static readonly Color[] Colors = {
            Color.FromArgb(130, 184, 164), Color.FromArgb(222, 155, 132), Color.FromArgb(216, 185, 104),
            Color.FromArgb(169, 151, 196), Color.FromArgb(124, 168, 199), Color.FromArgb(201, 145, 168) };
        public BlockPuzzleWindow(GameRecords records = null) : this(records, Environment.TickCount) { }
        internal BlockPuzzleWindow(GameRecords records, int seed)
        {
            this.records = records ?? new GameRecords(); game = new BlockPuzzle(seed);
            Text = "MyDay · 상몬 블록 퍼즐"; BackColor = Design.Paper; Font = Design.Font(10);
            StartPosition = FormStartPosition.CenterScreen; ClientSize = Design.Size(1040, 770); MinimumSize = Design.Size(760, 650); KeyPreview = true;
            var header = new Panel { Dock = DockStyle.Top, Height = Design.P(76) };
            var title = Design.Label("상몬 블록 퍼즐", 21, true); title.Location = Design.Point(26, 9);
            var sub = Design.Label("작은 블록을 모아, 오늘도 한 줄의 여유.", 10); sub.Location = Design.Point(28, 48); sub.ForeColor = Design.Muted;
            header.Controls.AddRange(new Control[] { title, sub });
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = Design.P(62), Padding = Design.Pad(22, 12, 12, 8), WrapContents = false };
            restart.Size = Design.Size(105, 35); export.Size = Design.Size(136, 35); status.Margin = Design.Pad(10, 8, 0, 0);
            footer.Controls.AddRange(new Control[] { restart, export, status });
            canvas.Dock = DockStyle.Fill; canvas.AccessibleName = "상몬 블록 퍼즐. 1, 2, 3으로 블록 선택, 방향키로 위치 이동, Enter로 놓기, Esc로 선택 취소.";
            canvas.Render = DrawScene; canvas.Press = PointerDown; canvas.Drag = PointerMove; canvas.Release = PointerUp; canvas.Cancel = CancelDrag;
            Controls.Add(canvas); Controls.Add(header); Controls.Add(footer);
            restart.Click += delegate { RequestRestart(); }; export.Click += delegate { MiniGameArt.Export(this, result); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (HandleKey(e.KeyCode)) { e.Handled = true; e.SuppressKeyPress = true; } };
            Deactivate += delegate { CancelDrag(); }; Resize += delegate { CancelDrag(); };
            timer.Tick += delegate { if (Visible && WindowState != FormWindowState.Minimized) canvas.Invalidate(); };
            Shown += delegate { timer.Start(); canvas.Focus(); }; UpdateUi();
        }
        private static RectangleF Tray(int slot) { return new RectangleF(BoardLeft + slot * 132, 489, 120, 96); }
        internal bool PointerDown(PointF point)
        {
            if (game.Finished) return false;
            for (int slot = 0; slot < 3; slot++) if (Tray(slot).Contains(point) && game.PieceAt(slot) != null)
            {
                selected = slot; pressed = point; dragging = true; moved = false; message = "드래그해서 놓거나, 빈칸을 클릭하세요.";
                canvas.Invalidate(); return true;
            }
            if (selected >= 0 && InBoard(point)) { cursorX = (int)((point.X - BoardLeft) / Cell); cursorY = (int)((point.Y - BoardTop) / Cell); Place(); }
            return false;
        }
        private static bool InBoard(PointF p) { return p.X >= BoardLeft && p.Y >= BoardTop && p.X < BoardLeft + 8 * Cell && p.Y < BoardTop + 8 * Cell; }
        internal void PointerMove(PointF point)
        {
            if (!dragging || selected < 0) return;
            moved |= Math.Abs(point.X - pressed.X) + Math.Abs(point.Y - pressed.Y) > 7;
            var piece = game.PieceAt(selected); if (piece == null) return;
            cursorX = (int)Math.Floor((point.X - BoardLeft) / Cell) - piece.Width / 2;
            cursorY = (int)Math.Floor((point.Y - BoardTop) / Cell) - piece.Height / 2;
            canvas.Invalidate();
        }
        internal void PointerUp(PointF point)
        {
            if (!dragging) return;
            PointerMove(point); bool commit = moved && InBoard(point); dragging = false;
            if (commit) Place(); else { cursorX = cursorY = 0; canvas.Invalidate(); }
        }
        private void CancelDrag() { dragging = false; moved = false; canvas.Capture = false; canvas.Invalidate(); }
        private void RequestRestart()
        {
            if (game.Moves > 0 && !game.Finished && MessageBox.Show(this, "이번 판을 끝내고 새로 시작할까요?", "새 게임", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            StartRound();
        }
        internal void StartRound()
        {
            CancelDrag(); game.Start(); selected = -1; saved = false; result = null; flashUntil = 0;
            message = "새 블록 셋이 도착했어요. 천천히 시작해요."; UpdateUi(); canvas.Focus(); canvas.Invalidate();
        }
        internal bool HandleKey(Keys key)
        {
            if (key == Keys.Escape) { CancelDrag(); selected = -1; canvas.Invalidate(); return true; }
            int slot = key >= Keys.D1 && key <= Keys.D3 ? key - Keys.D1 : key >= Keys.NumPad1 && key <= Keys.NumPad3 ? key - Keys.NumPad1 : -1;
            if (slot >= 0) { if (!game.Finished && game.PieceAt(slot) != null) { selected = slot; cursorX = cursorY = 0; } canvas.Invalidate(); return true; }
            if (selected < 0 || game.Finished) return false;
            if (key == Keys.Left) cursorX = Math.Max(0, cursorX - 1);
            else if (key == Keys.Right) cursorX = Math.Min(7, cursorX + 1);
            else if (key == Keys.Up) cursorY = Math.Max(0, cursorY - 1);
            else if (key == Keys.Down) cursorY = Math.Min(7, cursorY + 1);
            else if (key == Keys.Enter || key == Keys.Space) Place();
            else return false;
            canvas.Invalidate(); return true;
        }
        private void Place()
        {
            if (!game.TryPlace(selected, cursorX, cursorY)) { message = "여기엔 놓을 수 없어요. 다른 빈칸을 찾아볼까요?"; UpdateUi(); canvas.Invalidate(); return; }
            selected = -1; flashUntil = clock.Elapsed.TotalSeconds + .55;
            message = game.PerfectClear ? "퍼펙트 클리어! 보드가 말끔해졌어요. +50" : game.LastLines > 0 ? game.LastLines + "줄 완성! +" + game.LastGain + "점" : "+" + game.LastGain + "점 · 차곡차곡, 잘하고 있어요.";
            UpdateUi(); canvas.Focus(); canvas.Invalidate();
        }
        private void UpdateUi()
        {
            if (game.Finished && !saved)
            {
                saved = true; result = new MiniGameResult { Name = "상몬 블록 퍼즐", Day = DateTime.Today, Score = game.Score, Details = game.Lines + "줄 완성 · " + game.Moves + "개 블록 배치" };
                records.Record("block-puzzle", game.Score);
            }
            export.Enabled = result != null; restart.Text = game.Finished ? "한 번 더" : "새 게임";
            status.Text = records.Problem ?? message;
        }
        internal void DrawScene(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(246, 245, 239));
            MiniGameArt.Text(g, "PLAY AT YOUR OWN PACE", 9, Design.Muted, 27, 25, true);
            MiniGameArt.Text(g, "작은 블록,", 23, Design.Ink, 26, 59, true);
            MiniGameArt.Text(g, "커다란 쉼표.", 23, Design.Ink, 26, 97, true);
            MiniGameArt.Text(g, "시간 제한 없이, 한 칸씩.", 11, Design.Muted, 28, 145);
            MiniGameArt.Card(g, new RectangleF(24, 200, 238, 133), Color.FromArgb(255, 254, 249));
            MiniGameArt.Text(g, "이번 판 점수", 10, Design.Muted, 43, 216, true);
            MiniGameArt.Text(g, game.Score.ToString("N0"), 39, Design.Ink, 40, 241, true);
            MiniGameArt.Text(g, "BEST  " + Math.Max(game.Score, records.Best("block-puzzle")).ToString("N0"), 11, Design.Olive, 44, 305, true);
            MiniGameArt.Card(g, new RectangleF(24, 347, 238, 104), Color.FromArgb(232, 238, 224));
            MiniGameArt.Text(g, "완성한 줄", 10, Design.Olive, 43, 362, true);
            MiniGameArt.Text(g, game.Lines.ToString("00"), 25, Design.Ink, 42, 386, true);
            MiniGameArt.Text(g, game.Combo > 1 ? game.Combo + " COMBO!" : "차근차근 모으는 중", 10, Design.Olive, 110, 401, true);
            MiniGameArt.Text(g, "나만의 속도로 즐겨요.\n세 블록을 모두 놓으면\n새 블록이 찾아와요.", 11, Design.Muted, 29, 492);
            MiniGameArt.Text(g, "8 × 8", 12, Design.Ink, BoardLeft, 27, true);
            MiniGameArt.Text(g, "가로도, 세로도. 가득 채우면 사르르!", 10, Design.Muted, BoardLeft + 72, 29);
            MiniGameArt.Card(g, new RectangleF(BoardLeft - 10, BoardTop - 10, 404, 404), Color.FromArgb(223, 226, 215));
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            {
                using (var hole = Design.Rounded(new RectangleF(BoardLeft + x * Cell + 2, BoardTop + y * Cell + 2, Cell - 4, Cell - 4), 9))
                using (var paper = new SolidBrush(Color.FromArgb(239, 239, 230))) g.FillPath(paper, hole);
                int color = game.CellAt(x, y); if (color > 0) Tile(g, BoardLeft + x * Cell, BoardTop + y * Cell, Cell, color - 1, 255, false);
            }
            if (clock.Elapsed.TotalSeconds < flashUntil && game.LastLines > 0)
                foreach (var p in game.ClearedCells) Tile(g, BoardLeft + p.X * Cell, BoardTop + p.Y * Cell, Cell, 2, (int)(170 * Math.Max(0, (flashUntil - clock.Elapsed.TotalSeconds) / .55)), false);
            if (selected >= 0 && (!dragging || moved))
            {
                var piece = game.PieceAt(selected); bool valid = game.CanPlace(piece, cursorX, cursorY);
                if (piece != null) foreach (var p in piece.Cells)
                {
                    int x = cursorX + p.X, y = cursorY + p.Y;
                    if (x >= 0 && x < 8 && y >= 0 && y < 8) Tile(g, BoardLeft + x * Cell, BoardTop + y * Cell, Cell, valid ? piece.Color : 1, valid ? 125 : 170, !valid);
                }
            }
            for (int slot = 0; slot < 3; slot++)
            {
                var tray = Tray(slot); var piece = game.PieceAt(slot);
                MiniGameArt.Card(g, tray, selected == slot ? Color.FromArgb(226, 236, 215) : Color.FromArgb(255, 254, 249));
                if (selected == slot) using (var outline = new Pen(Design.Olive, 2)) using (var path = Design.Rounded(RectangleF.Inflate(tray, -1, -1), 19)) g.DrawPath(outline, path);
                MiniGameArt.Text(g, (slot + 1).ToString("00"), 8, Design.Muted, tray.X + 9, tray.Y + 6, true);
                if (piece == null) { MiniGameArt.Text(g, "놓았어요", 10, Design.Muted, tray.X + 30, tray.Y + 40); continue; }
                float size = Math.Min(25, Math.Min(94f / piece.Width, 66f / piece.Height));
                float ox = tray.X + (tray.Width - piece.Width * size) / 2, oy = tray.Y + 22 + (68 - piece.Height * size) / 2;
                bool fits = game.CanPlay(slot);
                foreach (var p in piece.Cells) Tile(g, ox + p.X * size, oy + p.Y * size, size, piece.Color, fits ? 255 : 80, false);
                if (!fits) MiniGameArt.Text(g, "공간이 필요해요", 7, Design.Muted, tray.X + 24, tray.Bottom - 15);
            }
            MiniGameArt.Card(g, new RectangleF(723, 82, 212, 226), Color.FromArgb(255, 254, 249));
            MiniGameArt.Text(g, "이렇게 놀아요", 15, Design.Ink, 741, 100, true);
            MiniGameArt.Text(g, "01  블록을 골라요\n\n02  빈칸에 놓아요\n\n03  한 줄을 채워요", 11, Design.Olive, 742, 144, true);
            MiniGameArt.Text(g, "회전 없이 그대로 놓아요.\n가로·세로를 함께 지우면\n더 큰 보너스가 기다려요.", 10, Design.Muted, 728, 333);
            MiniGameArt.Pet(g, 831, 471, 174, clock.Elapsed.TotalSeconds, 0, -3, game.LastLines > 0 && clock.Elapsed.TotalSeconds < flashUntil);
            MiniGameArt.Text(g, "한 줄의 작은 성취!", 11, Design.Olive, 754, 545, true);

            if (game.Finished)
            {
                using (var veil = new SolidBrush(Color.FromArgb(190, 246, 245, 239))) g.FillRectangle(veil, BoardLeft - 12, BoardTop - 12, 409, 519);
                MiniGameArt.Card(g, new RectangleF(309, 192, 368, 218), Color.FromArgb(255, 254, 249));
                MiniGameArt.Text(g, "이번 판도 잘했어요!", 23, Design.Ink, 335, 215, true);
                MiniGameArt.Text(g, game.Score.ToString("N0") + "점", 31, Design.Olive, 335, 260, true);
                MiniGameArt.Text(g, "놓을 수 있는 블록이 없어요.\n아래 ‘한 번 더’로 다시 시작해요.", 11, Design.Muted, 337, 330);
            }
        }
        internal static void Tile(Graphics g, float x, float y, float size, int color, int alpha, bool invalid)
        {
            var fill = Color.FromArgb(Math.Max(0, Math.Min(255, alpha)), Colors[color]);
            using (var path = Design.Rounded(new RectangleF(x + 2, y + 2, size - 4, size - 4), Math.Max(3, size * .17f)))
            using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
            using (var highlight = new Pen(Color.FromArgb(alpha * 70 / 255, Color.White), Math.Max(1, size / 22))) g.DrawLine(highlight, x + size * .24f, y + size * .2f, x + size * .74f, y + size * .2f);
            if (alpha == 255 && size >= 27)
                using (var eyes = new SolidBrush(Color.FromArgb(105, Design.Ink)))
                { float eye = Math.Max(2, size * .055f); g.FillEllipse(eyes, x + size * .34f, y + size * .57f, eye, eye); g.FillEllipse(eyes, x + size * .60f, y + size * .57f, eye, eye); }
            if (invalid) using (var pen = new Pen(Color.FromArgb(180, 153, 65, 57), 2)) g.DrawLine(pen, x + size * .35f, y + size * .65f, x + size * .65f, y + size * .35f);
        }
        internal void VerifyLayoutForTest()
        {
            foreach (var button in new[] { restart, export }) if (!button.Parent.ClientRectangle.Contains(button.Bounds)) throw new InvalidOperationException("Puzzle footer is clipped");
        }
        internal void SavePreview(string path) { using (var image = new Bitmap(960, 620)) using (var g = Graphics.FromImage(image)) { DrawScene(g); image.Save(path); } }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }
}
