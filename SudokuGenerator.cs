using System;
using System.Collections.Generic;
using System.Numerics;

namespace PeacefulSudoku;

public enum Difficulty { Easy, Medium, Hard, Expert }

public static class SudokuGenerator
{
    private static readonly Random _rng = new();

    // Each mask holds 9 bits — bit N set means number N+1 is still available.
    // All 9 numbers free = 0b111111111 = 511
    private const int AllFree = 0b111111111;

    public static (int[,] puzzle, int[,] solution) Generate(Difficulty difficulty)
    {
        int clues = difficulty switch
        {
            Difficulty.Easy   => 36,
            Difficulty.Medium => 30,
            Difficulty.Hard   => 26,
            Difficulty.Expert => 22,
            _                 => 30
        };

        // Keep trying until we get a puzzle with enough clues.
        // (Rarely needs more than one attempt.)
        while (true)
        {
            var board    = new int[9, 9];
            var rowMask  = new int[9];
            var colMask  = new int[9];
            var boxMask  = new int[9];
            InitMasks(rowMask, colMask, boxMask);

            // Step 1 — fill a complete valid board
            Fill(board, rowMask, colMask, boxMask);

            // Step 2 — save solution before we punch holes
            var solution = (int[,])board.Clone();

            // Step 3 — remove cells while preserving unique solution
            int removed = TryRemoveCells(board, solution, 81 - clues);

            if (81 - removed >= clues)
                return (board, solution);
        }
    }

    private static void InitMasks(int[] rowMask, int[] colMask, int[] boxMask)
    {
        for (int i = 0; i < 9; i++)
            rowMask[i] = colMask[i] = boxMask[i] = AllFree;
    }

    private static int BoxIndex(int row, int col) => (row / 3) * 3 + (col / 3);

    // Available candidates for a cell = intersection of its row, col, box masks
    private static int Available(int[] rowMask, int[] colMask, int[] boxMask, int row, int col)
        => rowMask[row] & colMask[col] & boxMask[BoxIndex(row, col)];

    private static void Place(int[] rowMask, int[] colMask, int[] boxMask, int row, int col, int bit)
    {
        rowMask[row]             &= ~bit;
        colMask[col]             &= ~bit;
        boxMask[BoxIndex(row, col)] &= ~bit;
    }

    private static void Unplace(int[] rowMask, int[] colMask, int[] boxMask, int row, int col, int bit)
    {
        rowMask[row]             |= bit;
        colMask[col]             |= bit;
        boxMask[BoxIndex(row, col)] |= bit;
    }

    // Rebuild masks from a partially filled board (used before solving a copy)
    private static (int[] row, int[] col, int[] box) BuildMasks(int[,] board)
    {
        var rowMask = new int[9];
        var colMask = new int[9];
        var boxMask = new int[9];
        InitMasks(rowMask, colMask, boxMask);

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (board[r, c] != 0)
                {
                    int bit = 1 << (board[r, c] - 1);
                    Place(rowMask, colMask, boxMask, r, c, bit);
                }

        return (rowMask, colMask, boxMask);
    }

    // 

    private static bool Fill(int[,] board, int[] rowMask, int[] colMask, int[] boxMask)
    {
        // Find cell with fewest candidates (MRV), random tiebreak
        var tied      = new List<(int row, int col)>();
        int bestCount = 10;

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c] != 0) continue;

                int avail = Available(rowMask, colMask, boxMask, r, c);
                int count = BitOperations.PopCount((uint)avail);

                if (count == 0) return false; // dead end

                if (count < bestCount)
                {
                    bestCount = count;
                    tied.Clear();
                    tied.Add((r, c));
                    if (count == 1) goto foundBest; // can't beat 1
                }
                else if (count == bestCount)
                {
                    tied.Add((r, c));
                }
            }

        if (tied.Count == 0) return true; // no empty cells — complete!

        foundBest:
        var (bestRow, bestCol) = tied[_rng.Next(tied.Count)];

        // Try each candidate in random order
        int candidates = Available(rowMask, colMask, boxMask, bestRow, bestCol);
        foreach (int num in ShuffleBits(candidates))
        {
            int bit = 1 << (num - 1);
            board[bestRow, bestCol] = num;
            Place(rowMask, colMask, boxMask, bestRow, bestCol, bit);

            if (Fill(board, rowMask, colMask, boxMask)) return true;

            // Backtrack
            board[bestRow, bestCol] = 0;
            Unplace(rowMask, colMask, boxMask, bestRow, bestCol, bit);
        }

        return false;
    }

    private static int TryRemoveCells(int[,] board, int[,] solution, int target)
    {
        // Shuffle cell indices so removal order is random
        var indices = new int[81];
        for (int i = 0; i < 81; i++) indices[i] = i;
        Shuffle(indices);

        int removed = 0;

        foreach (int idx in indices)
        {
            if (removed == target) break;

            int row = idx / 9;
            int col = idx % 9;
            int saved = board[row, col];

            board[row, col] = 0;

            // Check uniqueness on a copy so we don't corrupt the masks
            var copy = (int[,])board.Clone();
            var (rm, cm, bm) = BuildMasks(copy);

            if (CountSolutions(copy, rm, cm, bm, 2) == 1)
                removed++;
            else
                board[row, col] = saved; // put it back
        }

        return removed;
    }

    private static int CountSolutions(int[,] board, int[] rowMask, int[] colMask, int[] boxMask, int limit)
    {
        int bestRow = -1, bestCol = -1, bestCount = 10;

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c] != 0) continue;

                int avail = Available(rowMask, colMask, boxMask, r, c);
                int count = BitOperations.PopCount((uint)avail);

                if (count == 0) return 0; // dead end

                if (count < bestCount)
                {
                    bestCount = count;
                    bestRow   = r;
                    bestCol   = c;
                    if (count == 1) goto foundBest;
                }
            }

        if (bestRow == -1) return 1; // no empty cells — valid solution found

        foundBest:
        int solutions  = 0;
        int candidates = Available(rowMask, colMask, boxMask, bestRow, bestCol);

        // Iterate over set bits without shuffling (order doesn't matter for counting)
        while (candidates != 0)
        {
            int bit = candidates & -candidates; // lowest set bit
            candidates &= candidates - 1;       // clear lowest set bit
            int num = BitOperations.TrailingZeroCount(bit) + 1;

            board[bestRow, bestCol] = num;
            Place(rowMask, colMask, boxMask, bestRow, bestCol, bit);

            solutions += CountSolutions(board, rowMask, colMask, boxMask, limit - solutions);

            board[bestRow, bestCol] = 0;
            Unplace(rowMask, colMask, boxMask, bestRow, bestCol, bit);

            if (solutions >= limit) return solutions; // early exit
        }

        return solutions;
    }

    // Utils
    private static IEnumerable<int> ShuffleBits(int mask)
    {
        var nums = new List<int>();
        while (mask != 0)
        {
            int bit = mask & -mask;
            nums.Add(BitOperations.TrailingZeroCount(bit) + 1);
            mask &= mask - 1;
        }
        Shuffle(nums);
        return nums;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]); // tuple swap
        }
    }
}