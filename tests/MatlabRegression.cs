using System;
using System.IO;
using Isidore.Matlab;

internal static class MatlabRegression
{
    public sealed class Record
    {
        public Record Child;
        public double[] Values;
    }

    public static void Run()
    {
        var records = new[] { new Record(), new Record { Child = new Record { Values = new[] { 4.0, 5.0 } } } };
        var values = Net.GetValue<Record, double>(records, "Child.Values");
        Check(values.GetLength(0) == 2 && values.GetLength(1) == 2 && values[0, 0] == 0 && values[1, 1] == 5,
            "Null nested members must leave an empty/default row.");
        Check(Net.GetValue<Record, double>(new Record(), new[] { "Values" }).Length == 0,
            "Null final members must return an empty vector.");
        var empty = Net.GetValue<Record, double>(new Record[0], "Values");
        Check(empty.GetLength(0) == 0 && empty.GetLength(1) == 0, "Empty record arrays must produce an empty matrix.");

        var matlab = new MLApp.MLApp();
        double[,] frame = { { 1, 2, 3 }, { 4, 5, 6 } };
        MatLab.Append(matlab, "frames", frame);
        CheckFrame((double[,])matlab.Workspace["frames"]);
        MatLab.Append(matlab, "frames", frame);
        CheckFrame((double[,])matlab.Workspace["framesTmp"]);

        MLApp.MLApp.NextReadValue = frame;
        var read = MatLab.Read("sample's data.mat", "values");
        var session = MLApp.MLApp.LastCreated;
        string expected = "load('" + Path.GetFullPath("sample's data.mat").Replace("'", "''") + "','values');";
        Check(session.Commands.Contains(expected) && ReferenceEquals(frame, read), "Read must load the requested MAT file and variable.");
        Check(session.QuitCalled, "Read must close its MATLAB session after success.");
        MLApp.MLApp.ThrowOnRead = true;
        try
        {
            MatLab.Read("sample.mat", "values");
            throw new Exception("A simulated COM read failure should propagate.");
        }
        catch (InvalidOperationException)
        {
            Check(MLApp.MLApp.LastCreated.QuitCalled, "Read must close its MATLAB session after failure.");
        }
        finally { MLApp.MLApp.ThrowOnRead = false; }
    }

    private static void CheckFrame(double[,] frame)
    {
        Check(frame.GetLength(0) == 3 && frame.GetLength(1) == 2 && frame[2, 1] == 6,
            "Append must transpose both new and appended rectangular frames.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
