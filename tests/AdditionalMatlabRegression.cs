using System;
using Isidore.Matlab;

internal static class AdditionalMatlabRegression
{
    public static void Run()
    {
        FourDimensionalTransfers();
        LogicalArrayTransfers();
        EmptyAndNonvectorReads();
        ExtractionWithoutMemberPath();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void FourDimensionalTransfers()
    {
        var matlab = new MLApp.MLApp();
        int[,,,] cube = new int[2, 3, 2, 2];
        for (int t = 0; t < 2; t++)
            for (int z = 0; z < 2; z++)
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 2; x++)
                        cube[x, y, z, t] = x + 10 * y + 100 * z + 1000 * t;
        MatLab.Put(matlab, "cube", cube);
        Check(matlab.Commands.Contains("cube=zeros(3,2,2,2);"),
            "4D transfer must swap X/Y dimensions as documented and as the 2D/3D overloads do.");
        Check(matlab.Transfers.Count == 4, "4D transfer must send all Z/time slices.");
        for (int index = 0; index < 4; index++)
        {
            var frame = (double[,])matlab.Transfers[index].Item2;
            Check(frame.GetLength(0) == 3 && frame.GetLength(1) == 2,
                "Each MATLAB 4D slice must have transposed dimensions.");
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 2; x++)
                    Check(frame[y, x] == cube[x, y, index % 2, index / 2],
                        "4D transfer must preserve values in every transposed slice.");
        }
    }

    private static void EmptyAndNonvectorReads()
    {
        var matlab = new MLApp.MLApp();
        matlab.Workspace["emptyRow"] = new double[1, 0];
        matlab.Workspace["emptyColumn"] = new double[0, 1];
        Check(MatLab.Get(matlab, "emptyRow", "base").Length == 0,
            "An empty row vector must return an empty array.");
        Check(MatLab.Get(matlab, "emptyColumn", "base").Length == 0,
            "An empty column vector must return an empty array.");
        matlab.Workspace["row"] = new double[,] { { 1, 2, 3 } };
        matlab.Workspace["column"] = new double[,] { { 4 }, { 5 }, { 6 } };
        Check(MatLab.Get(matlab, "row", "base")[2] == 3 &&
            MatLab.Get(matlab, "column", "base")[2] == 6, "Both vector orientations must preserve values.");
        matlab.Workspace["matrix"] = new double[,] { { 1, 2 }, { 3, 4 } };
        try { MatLab.Get(matlab, "matrix", "base"); }
        catch (ArgumentException) { return; }
        throw new Exception("Reading a matrix as a vector must reject silent truncation.");
    }

    private static void LogicalArrayTransfers()
    {
        var matlab = new MLApp.MLApp();
        bool[,,] frames = new bool[1, 2, 1];
        frames[0, 1, 0] = true;
        MatLab.Put(matlab, "logicalFrames", frames);
        var frame = (double[,])matlab.Transfers[0].Item2;
        Check(frame[0, 0] == 0 && frame[1, 0] == 1,
            "Logical 3D frames must transfer as numeric 0/1 slices.");
        bool[,,,] cube = new bool[2, 1, 1, 1];
        cube[1, 0, 0, 0] = true;
        MatLab.Put(matlab, "logicalCube", cube);
        frame = (double[,])matlab.Transfers[1].Item2;
        Check(frame[0, 0] == 0 && frame[0, 1] == 1,
            "Logical 4D frames must transfer as numeric 0/1 slices.");
    }

    private static void ExtractionWithoutMemberPath()
    {
        var scalars = Net.GetValue<int, int>(new[] { 4, 5 });
        Check(scalars.GetLength(0) == 2 && scalars.GetLength(1) == 1 && scalars[1, 0] == 5,
            "The default extraction path must extract the values themselves.");
        var vectors = Net.GetValue<int[], int>(new[] { new[] { 1, 2 }, new[] { 3 } });
        Check(vectors.GetLength(1) == 2 && vectors[0, 1] == 2 && vectors[1, 0] == 3 && vectors[1, 1] == 0,
            "Default extraction must support uneven vectors and pad absent components.");
        Check(Net.GetValue<int, int>(9, new string[0])[0] == 9,
            "An empty member path must retain a scalar value.");
        int[,] input = { { 1, 2 }, { 3, 4 } };
        var matrix = Net.GetValue<int, int>(input);
        Check(matrix.GetLength(2) == 1 && matrix[1, 1, 0] == 4,
            "Default extraction from 2D arrays must retain every input value.");
    }
}
