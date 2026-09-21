using System;
using System.Collections.Generic;

namespace Minesweeper
{
    public enum GameState { Ready, Playing, Won, Lost }

    // Engine-independent rules, also exercised by the standalone test runner.
    public sealed class MineBoard
    {
        public int Width { get; }
        public int Height { get; }
        public int MineCount { get; }
        public int Flags { get; private set; }
        public int Revealed { get; private set; }
        public int ExplodedIndex { get; private set; } = -1;
        public GameState State { get; private set; }
        private readonly bool[] mines, open, flags;
        private readonly int[] adjacent;
        private readonly Random random;

        public MineBoard(int width, int height, int mineCount, int seed)
        {
            if (width < 3 || height < 3 || width > 100 || height > 100 ||
                mineCount < 1 || mineCount > width * height - 9)
                throw new ArgumentOutOfRangeException(nameof(mineCount));
            Width = width; Height = height; MineCount = mineCount;
            mines = new bool[width * height]; open = new bool[mines.Length];
            flags = new bool[mines.Length]; adjacent = new int[mines.Length];
            random = new Random(seed);
        }

        public bool IsMine(int i) => mines[i];
        public bool IsOpen(int i) => open[i];
        public bool IsFlagged(int i) => flags[i];
        public int Adjacent(int i) => adjacent[i];
        public bool Finished => State == GameState.Won || State == GameState.Lost;

        public IEnumerable<int> Neighbors(int i)
        {
            int x = i % Width, y = i / Width;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && x + dx >= 0 && x + dx < Width && y + dy >= 0 && y + dy < Height)
                        yield return (y + dy) * Width + x + dx;
        }

        public void ToggleFlag(int i)
        {
            if (i < 0 || i >= mines.Length || Finished || open[i]) return;
            // Keep the remaining-mine display meaningful; remove a flag before adding more.
            if (!flags[i] && Flags >= MineCount) return;
            flags[i] = !flags[i]; Flags += flags[i] ? 1 : -1;
        }

        public void Reveal(int i)
        {
            if (i < 0 || i >= mines.Length || Finished || flags[i] || open[i]) return;
            if (State == GameState.Ready) PlaceMines(i);
            Flood(i);
            CheckWin();
        }

        public void Chord(int i)
        {
            if (i < 0 || i >= mines.Length || State != GameState.Playing || !open[i]) return;
            int count = 0;
            foreach (int n in Neighbors(i)) if (flags[n]) count++;
            if (count != adjacent[i]) return;
            foreach (int n in Neighbors(i))
            {
                if (!open[n] && !flags[n]) Flood(n);
                if (State == GameState.Lost) return;
            }
            CheckWin();
        }

        private void PlaceMines(int first)
        {
            var candidates = new List<int>();
            int fx = first % Width, fy = first / Width;
            for (int i = 0; i < mines.Length; i++)
                if (Math.Abs(i % Width - fx) > 1 || Math.Abs(i / Width - fy) > 1) candidates.Add(i);
            for (int i = 0; i < MineCount; i++)
            {
                int j = random.Next(i, candidates.Count);
                int temp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = temp;
                mines[candidates[i]] = true;
            }
            for (int i = 0; i < mines.Length; i++)
                foreach (int n in Neighbors(i)) if (mines[n]) adjacent[i]++;
            State = GameState.Playing;
        }

        private void Flood(int start)
        {
            if (open[start] || flags[start]) return;
            if (mines[start]) { ExplodedIndex = start; State = GameState.Lost; return; }
            var queue = new Queue<int>();
            open[start] = true; Revealed++; queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                if (adjacent[i] != 0) continue;
                foreach (int n in Neighbors(i))
                    if (!open[n] && !flags[n] && !mines[n])
                    {
                        open[n] = true; Revealed++; queue.Enqueue(n);
                    }
            }
        }

        private void CheckWin()
        {
            if (State != GameState.Playing || Revealed != mines.Length - MineCount) return;
            State = GameState.Won;
            for (int i = 0; i < mines.Length; i++) if (mines[i]) flags[i] = true;
            Flags = MineCount;
        }
    }
}
