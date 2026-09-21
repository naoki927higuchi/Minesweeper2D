using System;
using System.Linq;
using Minesweeper;

static class Program
{
    static int checks;
    static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }

    static MineBoard Started(int seed = 42)
    { var b = new MineBoard(16, 16, 40, seed); b.Reveal(0); return b; }

    static void Main()
    {
        PackagingTests.Run();
        foreach (var size in new[] { (9, 9, 10), (16, 16, 40), (30, 16, 99) })
        for (int seed = 0; seed < 100; seed++)
        {
            var b = new MineBoard(size.Item1, size.Item2, size.Item3, seed);
            int first = seed % (b.Width * b.Height);
            b.Reveal(first);
            Check(!b.IsMine(first) && b.Adjacent(first) == 0, "First click neighborhood is safe");
            Check(Enumerable.Range(0, b.Width * b.Height).Count(b.IsMine) == b.MineCount, "Exact mine count");
            for (int i = 0; i < b.Width * b.Height; i++)
            {
                Check(b.Adjacent(i) == b.Neighbors(i).Count(b.IsMine), "Correct neighbor count");
                Check(!b.IsOpen(i) || !b.IsMine(i), "Flood does not reveal mines");
                if (b.IsOpen(i) && b.Adjacent(i) == 0)
                    Check(b.Neighbors(i).All(b.IsOpen), "Empty region is fully expanded");
            }
            for (int i = 0; i < b.Width * b.Height; i++) if (!b.IsMine(i)) b.Reveal(i);
            Check(b.State == GameState.Won && b.Flags == b.MineCount, "All safe cells win and mark mines");
            int flags = b.Flags; b.ToggleFlag(0); b.Reveal(0);
            Check(b.Flags == flags && b.State == GameState.Won, "Won board is locked");
        }

        var flagged = new MineBoard(9, 9, 10, 1);
        flagged.ToggleFlag(0); flagged.Reveal(0);
        Check(flagged.State == GameState.Ready && flagged.Revealed == 0, "Flag prevents initial reveal");
        flagged.ToggleFlag(0); flagged.Reveal(0);
        Check(flagged.State == GameState.Playing, "Unflagged cell starts game");
        flagged.ToggleFlag(0);
        Check(flagged.Flags == 0, "Cannot flag open cell");

        var capped = new MineBoard(9, 9, 10, 2);
        for (int i = 0; i < 11; i++) capped.ToggleFlag(i);
        Check(capped.Flags == 10 && !capped.IsFlagged(10), "Flag count is capped");
        capped.ToggleFlag(0); capped.ToggleFlag(10);
        Check(capped.Flags == 10 && capped.IsFlagged(10), "Flags can be relocated");

        var lost = Started();
        int mine = Enumerable.Range(0, 256).First(lost.IsMine);
        lost.Reveal(mine);
        Check(lost.State == GameState.Lost && lost.ExplodedIndex == mine, "Mine triggers loss");
        int before = lost.Revealed; lost.Reveal(255); lost.ToggleFlag(255);
        Check(lost.Revealed == before && lost.Flags == 0, "Lost board is locked");

        var chord = Started();
        int target = Enumerable.Range(0, 256).First(i => chord.IsOpen(i) && chord.Adjacent(i) > 0 && chord.Neighbors(i).Any(n => !chord.IsOpen(n) && !chord.IsMine(n)));
        before = chord.Revealed; chord.Chord(target);
        Check(chord.Revealed == before, "Chord needs matching flags");
        foreach (int n in chord.Neighbors(target).Where(chord.IsMine)) chord.ToggleFlag(n);
        chord.Chord(target);
        Check(chord.Revealed > before && chord.State != GameState.Lost, "Correct flags enable chord");

        var wrong = Started();
        target = Enumerable.Range(0, 256).First(i => wrong.IsOpen(i) && wrong.Adjacent(i) > 0 && wrong.Neighbors(i).Any(n => !wrong.IsOpen(n) && !wrong.IsMine(n)));
        var nearbyMines = wrong.Neighbors(target).Where(wrong.IsMine).ToArray();
        foreach (int n in nearbyMines.Skip(1)) wrong.ToggleFlag(n);
        wrong.ToggleFlag(wrong.Neighbors(target).First(n => !wrong.IsOpen(n) && !wrong.IsMine(n)));
        wrong.Chord(target);
        Check(wrong.State == GameState.Lost, "Incorrect chord flags trigger loss");

        var protectedFlag = new MineBoard(9, 9, 10, 7);
        protectedFlag.ToggleFlag(1); protectedFlag.Reveal(0);
        Check(!protectedFlag.IsOpen(1), "Flood respects flags");
        protectedFlag.ToggleFlag(1); protectedFlag.Reveal(1);
        Check(protectedFlag.IsOpen(1), "Safe flagged cell can be opened after removal");

        bool invalid = false;
        try { new MineBoard(3, 3, 1, 0); } catch (ArgumentOutOfRangeException) { invalid = true; }
        Check(invalid, "Reject impossible safe neighborhood");
        protectedFlag.Reveal(-1); protectedFlag.ToggleFlag(9999); protectedFlag.Chord(-1);
        Console.WriteLine("PASS: " + checks + " assertions; 300 seeded boards, first-click safety, adjacency, flood, flags, chord, win/loss and invalid input.");
    }
}
