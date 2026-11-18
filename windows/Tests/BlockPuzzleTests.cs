using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.UI;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static readonly Point[] PuzzleDot = { new Point(0, 0) };
        private static readonly Point[] PuzzleSquare = { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) };
        private static int PuzzleCount(BlockPuzzle game) { return Enumerable.Range(0, 64).Count(i => game.CellAt(i % 8, i / 8) > 0); }
        private static void RunBlockPuzzleTests(string directory)
        {
            var g = new BlockPuzzle(42);
            Check(g.Score == 0 && g.Moves == 0 && !g.Finished && PuzzleCount(g) == 0 && Enumerable.Range(0, 3).All(i => g.CanPlay(i)), "Placement puzzle starts with an empty eight-by-eight board and three playable pieces");
            var original = g.PieceAt(0).Cells; original[0] = new Point(999, 999);
            Check(g.PieceAt(0).Cells.All(p => p.X < 5 && p.Y < 5), "Puzzle pieces expose defensive cell copies");
            g.SetHandForTest(PuzzleSquare, PuzzleDot, PuzzleDot);
            Check(!g.TryPlace(-1, 0, 0) && !g.TryPlace(3, 0, 0) && !g.TryPlace(0, -1, 0) && !g.TryPlace(0, 7, 7) && g.Score == 0 && PuzzleCount(g) == 0, "Out-of-bounds placements never partially write cells, consume a piece or score");
            Check(g.TryPlace(0, 2, 2) && g.PieceAt(0) == null && g.Score == 4 && PuzzleCount(g) == 4, "A valid puzzle placement consumes exactly its slot and scores its occupied cells");
            Check(!g.TryPlace(0, 0, 0) && !g.TryPlace(1, 2, 2) && g.PieceAt(1) != null && g.Moves == 1 && g.Score == 4, "Used slots and overlap are rejected atomically");
            Check(g.TryPlace(1, 6, 0) && g.PieceAt(0) == null && g.TryPlace(2, 0, 7) && Enumerable.Range(0, 3).All(i => g.PieceAt(i) != null) && g.Moves == 3, "The tray refills only after all three pieces have been used");

            g.Start(); g.SetHandForTest(PuzzleDot, PuzzleDot, PuzzleDot);
            for (int x = 0; x < 7; x++) g.SetCellForTest(x, 7, 2);
            g.SetCellForTest(3, 1, 4);
            Check(g.TryPlace(0, 7, 7) && g.Lines == 1 && g.Score == 11 && PuzzleCount(g) == 1 && g.CellAt(3, 1) == 4 && g.CellAt(3, 2) == 0, "A full row clears with a ten-point bonus and floating blocks never fall");
            g.Start(); g.SetHandForTest(PuzzleDot, PuzzleDot, PuzzleDot);
            for (int y = 0; y < 8; y++) if (y != 3) g.SetCellForTest(2, y, 1);
            for (int x = 0; x < 8; x++) if (x != 2) g.SetCellForTest(x, 3, 1);
            Check(g.TryPlace(0, 2, 3) && g.Lines == 2 && g.ClearedCells.Length == 15 && g.Score == 91 && g.PerfectClear && PuzzleCount(g) == 0, "Crossing row and column clear simultaneously, deduplicating their intersection and awarding a perfect clear");
            g.Start(); g.SetHandForTest(PuzzleSquare, PuzzleDot, PuzzleDot);
            for (int y = 6; y < 8; y++) for (int x = 0; x < 6; x++) g.SetCellForTest(x, y, 3);
            Check(g.TryPlace(0, 6, 6) && g.Lines == 2 && g.ClearedCells.Length == 16 && g.Score == 94, "A square can clear two parallel rows without missing either row");
            g.Start(); g.SetHandForTest(PuzzleDot, PuzzleDot, PuzzleDot);
            for (int x = 0; x < 7; x++) g.SetCellForTest(x, 7, 2); g.TryPlace(0, 7, 7);
            for (int x = 0; x < 7; x++) g.SetCellForTest(x, 7, 2); g.TryPlace(1, 7, 7);
            Check(g.Combo == 2 && g.LastGain == 71, "Consecutive clearing moves multiply the line bonus");
            g.TryPlace(2, 0, 0); Check(g.Combo == 0 && g.LastGain == 1, "A move without a line resets the combo but preserves the accumulated score");

            g.Start(); g.SetHandForTest(PuzzleDot, PuzzleSquare, PuzzleSquare);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if ((x + y) % 2 == 0) g.SetCellForTest(x, y, 1);
            Check(g.TryPlace(0, 1, 0) && g.Finished && PuzzleCount(g) < 64 && !g.CanPlay(1) && !g.TryPlace(1, 0, 0), "The puzzle ends when no remaining piece fits, even with empty cells on the board");
            g.Start(); Check(!g.Finished && g.Score == 0 && g.Lines == 0 && g.Moves == 0 && g.ClearedCells.Length == 0 && Enumerable.Range(0, 3).All(i => g.CanPlay(i)), "Restart resets the complete puzzle state and deals a new hand");
            var a = new BlockPuzzle(123); var b = new BlockPuzzle(123);
            Check(Enumerable.Range(0, 3).All(i => a.PieceAt(i).Cells.SequenceEqual(b.PieceAt(i).Cells) && a.PieceAt(i).Color == b.PieceAt(i).Color), "Seeded puzzle hands are reproducible");
            bool valid = true; var rng = new MiniGameRandom(99);
            for (int turn = 0; turn < 1800; turn++)
            {
                if (g.Finished) g.Start(); var legal = new System.Collections.Generic.List<Point>();
                for (int slot = 0; slot < 3; slot++) for (int cell = 0; cell < 64; cell++) if (g.CanPlace(g.PieceAt(slot), cell % 8, cell / 8)) legal.Add(new Point(slot, cell));
                if (legal.Count == 0) { valid = false; break; }
                var pick = legal[rng.Next(legal.Count)]; int before = g.Score;
                valid &= g.TryPlace(pick.X, pick.Y % 8, pick.Y / 8) && g.Score >= before && g.Score <= 1000000;
                valid &= Enumerable.Range(0, 64).All(i => g.CellAt(i % 8, i / 8) >= 0 && g.CellAt(i % 8, i / 8) <= 6);
                valid &= Enumerable.Range(0, 8).All(i => Enumerable.Range(0, 8).Any(c => g.CellAt(c, i) == 0) && Enumerable.Range(0, 8).Any(r => g.CellAt(i, r) == 0));
                valid &= g.Finished == !Enumerable.Range(0, 3).Any(i => g.FitsAnywhere(g.PieceAt(i)));
            }
            Check(valid, "Eighteen hundred seeded placements preserve score, board bounds, line clears and exact game-over detection");
            string root = Path.Combine(directory, "puzzle-records"); var old = new GameRecords(Path.Combine(root, "scores.json")); old.Record("blocks", 500);
            byte[] bytes = File.ReadAllBytes(Path.Combine(root, "scores.json")); var puzzle = old.ForBlockPuzzle(); puzzle.Record("block-puzzle", 420); puzzle.Record("block-puzzle", 20);
            var restored = old.ForBlockPuzzle();
            Check(restored.Best("block-puzzle") == 420 && restored.Plays("block-puzzle") == 2 && bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(root, "scores.json"))) && old.Best("blocks") == 500, "Puzzle scores persist in a separate file and leave the older Tetris and six-game records byte-for-byte unchanged");
        }

        private static void PuzzleWindowImage(Form form, string path)
        {
            Application.DoEvents(); using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(path); }
        }
        public static void BlockPuzzlePreview(string directory)
        {
            Directory.CreateDirectory(directory); var records = new GameRecords();
            using (var w = new BlockPuzzleWindow(records, 281))
            {
                w.Show(); Application.DoEvents(); w.VerifyLayoutForTest();
                w.Game.SetHandForTest(PuzzleSquare, PuzzleDot, PuzzleDot);
                w.HandleKey(Keys.D1); w.HandleKey(Keys.Right); w.HandleKey(Keys.Down); w.HandleKey(Keys.Enter);
                if (w.Game.Moves != 1 || w.Game.CellAt(1, 1) == 0 || w.Selected != -1) throw new Exception("Puzzle keyboard placement failed");
                w.PointerDown(new PointF(480, 537)); w.PointerMove(new PointF(200, 300)); w.PointerUp(new PointF(200, 300));
                if (w.Game.Moves != 1 || w.Game.PieceAt(1) == null) throw new Exception("Puzzle invalid drag consumed a piece");
                w.PointerDown(new PointF(480, 537)); w.PointerMove(new PointF(300 + 6 * 48 + 24, 82 + 6 * 48 + 24)); w.PointerUp(new PointF(300 + 6 * 48 + 24, 82 + 6 * 48 + 24));
                if (w.Game.Moves != 2 || w.Game.CellAt(6, 6) == 0) throw new Exception("Puzzle drag-and-drop failed");
                w.PointerDown(new PointF(620, 537)); w.PointerUp(new PointF(620, 537)); w.PointerDown(new PointF(301, 83));
                if (w.Game.Moves != 3 || w.Game.CellAt(0, 0) == 0) throw new Exception("Puzzle click-select/click-place failed");
                w.StartRound();
                var three = new[] { new Point(0, 0), new Point(1, 0), new Point(2, 0) };
                var corner = new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1) };
                w.Game.SetHandForTest(three, corner, PuzzleSquare);
                for (int x = 0; x < 5; x++) w.Game.SetCellForTest(x, 7, x < 3 ? 1 : 2);
                for (int x = 0; x < 4; x++) w.Game.SetCellForTest(x, 6, 3);
                w.Game.SetCellForTest(0, 5, 3); w.Game.SetCellForTest(1, 5, 3); w.Game.SetCellForTest(6, 5, 4); w.Game.SetCellForTest(7, 5, 4); w.Game.SetCellForTest(7, 4, 4);
                PuzzleWindowImage(w, Path.Combine(directory, "block-puzzle-window.png")); w.SavePreview(Path.Combine(directory, "block-puzzle-board.png"));
                w.PointerDown(new PointF(350, 530)); w.PointerMove(new PointF(300 + 6 * 48 + 24, 82 + 7 * 48 + 24));
                PuzzleWindowImage(w, Path.Combine(directory, "block-puzzle-drag.png"));
                w.PointerUp(new PointF(300 + 6 * 48 + 24, 82 + 7 * 48 + 24));
                if (w.Game.Lines != 1 || w.Game.Score != 13) throw new Exception("Puzzle native line-clear or score failed");
                PuzzleWindowImage(w, Path.Combine(directory, "block-puzzle-clear.png"));
                w.ClientSize = Design.Size(760, 650); Application.DoEvents(); w.VerifyLayoutForTest();
                PuzzleWindowImage(w, Path.Combine(directory, "block-puzzle-compact.png"));
                w.StartRound(); w.Game.SetHandForTest(PuzzleDot, PuzzleSquare, PuzzleSquare);
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if ((x + y) % 2 == 0) w.Game.SetCellForTest(x, y, 1);
                w.HandleKey(Keys.D1); w.HandleKey(Keys.Right); w.HandleKey(Keys.Enter);
                if (!w.Game.Finished || records.Plays("block-puzzle") != 1 || records.Best("block-puzzle") != 1) throw new Exception("Puzzle game over did not record exactly once");
                w.HandleKey(Keys.Enter); w.HandleKey(Keys.D2);
                if (records.Plays("block-puzzle") != 1) throw new Exception("Puzzle finished round saved twice");
                PuzzleWindowImage(w, Path.Combine(directory, "block-puzzle-finished.png")); w.Close();
            }
            using (var hub = new GameHubWindow(new GameRecords()))
            {
                hub.Show(); Application.DoEvents(); hub.VerifyCardsForTest();
                PuzzleWindowImage(hub, Path.Combine(directory, "block-puzzle-playroom.png"));
                hub.ClientSize = Design.Size(740, 600); Application.DoEvents(); hub.VerifyCardsForTest();
                hub.OpenGame(6, false, hub, form => { if (!(form is BlockPuzzleWindow)) throw new Exception("Seventh hub card opens wrong game"); form.Close(); });
                hub.Close();
            }
            File.WriteAllText(Path.Combine(directory, "block-puzzle-ui-result.txt"), "PASS: native keyboard placement, drag-and-drop, invalid-drop preservation, click placement, row clear, scoring, small-window layout, one result save, seven-game menu routing. All records are isolated test data.");
        }
    }
}
