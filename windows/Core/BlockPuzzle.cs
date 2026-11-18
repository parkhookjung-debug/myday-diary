using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MyDay.Windows.Core
{
    public sealed class PuzzlePiece
    {
        private readonly Point[] cells;
        public Point[] Cells { get { return (Point[])cells.Clone(); } }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Color { get; private set; }
        public int Count { get { return cells.Length; } }
        internal PuzzlePiece(Point[] shape, int color)
        {
            cells = (Point[])shape.Clone(); Color = color;
            Width = cells.Max(p => p.X) + 1; Height = cells.Max(p => p.Y) + 1;
        }
    }

    // A stationary placement puzzle: full rows AND columns disappear together, without gravity.
    public sealed class BlockPuzzle
    {
        public const int Size = 8;
        private readonly int[,] board = new int[Size, Size];
        private readonly PuzzlePiece[] hand = new PuzzlePiece[3];
        private readonly MiniGameRandom random;
        private static readonly Point[][] shapes = MakeShapes();
        public int Score { get; private set; }
        public int Lines { get; private set; }
        public int Moves { get; private set; }
        public int Combo { get; private set; }
        public int LastGain { get; private set; }
        public int LastLines { get; private set; }
        public bool PerfectClear { get; private set; }
        public bool Finished { get; private set; }
        public Point[] ClearedCells { get; private set; }
        public BlockPuzzle(int seed) { random = new MiniGameRandom(seed); Start(); }
        public int CellAt(int x, int y) { return x < 0 || y < 0 || x >= Size || y >= Size ? -1 : board[x, y]; }
        public PuzzlePiece PieceAt(int slot) { return slot < 0 || slot >= 3 ? null : hand[slot]; }
        public void Start()
        {
            Array.Clear(board, 0, board.Length); Score = Lines = Moves = Combo = LastGain = LastLines = 0;
            PerfectClear = Finished = false; ClearedCells = new Point[0]; Deal();
        }
        public bool CanPlace(PuzzlePiece piece, int x, int y)
        {
            return piece != null && piece.Cells.All(p => CellAt(x + p.X, y + p.Y) == 0);
        }
        public bool FitsAnywhere(PuzzlePiece piece)
        {
            if (piece == null) return false;
            for (int y = 0; y <= Size - piece.Height; y++)
                for (int x = 0; x <= Size - piece.Width; x++) if (CanPlace(piece, x, y)) return true;
            return false;
        }
        public bool CanPlay(int slot) { return !Finished && FitsAnywhere(PieceAt(slot)); }
        public bool TryPlace(int slot, int x, int y)
        {
            var piece = PieceAt(slot);
            if (Finished || !CanPlace(piece, x, y)) return false;
            foreach (var p in piece.Cells) board[x + p.X, y + p.Y] = piece.Color + 1;
            hand[slot] = null; Moves++;
            var rows = new bool[Size]; var cols = new bool[Size];
            // Take both sets before erasing, so a crossing line is never missed.
            for (int i = 0; i < Size; i++)
            {
                rows[i] = Enumerable.Range(0, Size).All(c => board[c, i] != 0);
                cols[i] = Enumerable.Range(0, Size).All(r => board[i, r] != 0);
            }
            LastLines = rows.Count(v => v) + cols.Count(v => v); Lines += LastLines;
            Combo = LastLines > 0 ? Combo + 1 : 0;
            var cleared = new List<Point>();
            for (int r = 0; r < Size; r++) for (int c = 0; c < Size; c++)
                if (rows[r] || cols[c]) { board[c, r] = 0; cleared.Add(new Point(c, r)); }
            ClearedCells = cleared.ToArray();
            PerfectClear = LastLines > 0 && board.Cast<int>().All(v => v == 0);
            long gain = piece.Count + 10L * LastLines * LastLines * Combo + (PerfectClear ? 50 : 0);
            LastGain = (int)Math.Min(1000000 - Score, gain); Score += LastGain;
            if (hand.All(p => p == null)) Deal();
            Finished = !hand.Any(FitsAnywhere);
            return true;
        }
        private void Deal()
        {
            // Offer at least one playable piece when the board has a legal space.
            // The other two still need planning; there is no hidden board rearrangement.
            var fitting = Enumerable.Range(0, shapes.Length)
                .Where(i => FitsAnywhere(new PuzzlePiece(shapes[i], 0))).ToArray();
            for (int i = 0; i < 3; i++)
            {
                int shape = i == 0 && fitting.Length > 0 ? fitting[random.Next(fitting.Length)] : random.Next(shapes.Length);
                hand[i] = new PuzzlePiece(shapes[shape], random.Next(6));
            }
        }
        private static Point[][] MakeShapes()
        {
            var result = new List<Point[]>();
            for (int length = 1; length <= 5; length++)
            {
                result.Add(Enumerable.Range(0, length).Select(i => new Point(i, 0)).ToArray());
                if (length > 1) result.Add(Enumerable.Range(0, length).Select(i => new Point(0, i)).ToArray());
            }
            foreach (var size in new[] { new Size(2, 2), new Size(3, 3), new Size(3, 2), new Size(2, 3) })
                result.Add(Enumerable.Range(0, size.Width * size.Height).Select(i => new Point(i % size.Width, i / size.Width)).ToArray());
            string[] patterns = { "10/11", "01/11", "11/10", "11/01", "100/111", "001/111", "111/100", "111/001", "010/111", "111/010", "10/11/10", "01/11/01", "011/110", "110/011" };
            foreach (string pattern in patterns)
            {
                var points = new List<Point>(); var lines = pattern.Split('/');
                for (int y = 0; y < lines.Length; y++) for (int x = 0; x < lines[y].Length; x++)
                    if (lines[y][x] == '1') points.Add(new Point(x, y));
                result.Add(points.ToArray());
            }
            return result.ToArray();
        }
        internal void SetCellForTest(int x, int y, int value) { board[x, y] = value; }
        internal void SetHandForTest(params Point[][] pieces)
        {
            for (int i = 0; i < 3; i++) hand[i] = i < pieces.Length && pieces[i] != null ? new PuzzlePiece(pieces[i], i) : null;
            Finished = false;
        }
    }
}
