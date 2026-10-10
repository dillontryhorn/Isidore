using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Isidore.ImgProcess;
using Isidore.Load;
using Isidore.Maths;
using Isidore.Models;
using Isidore.Render;
using Point = Isidore.Maths.Point;

internal static class AdditionalLoadModelsRegression
{
    public static void Run()
    {
        GrayscaleByteRangeAndBitmapOpacity();
        ContoursAtImageBorders();
        TecplotUnseparatedDataAndCounts();
        ConcurrentTecplotReads();
        TurbulenceTimeAndDisplacement();
        CannyOrientationAndHysteresis();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void ExpectException<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception(message);
    }

    private static void GrayscaleByteRangeAndBitmapOpacity()
    {
        Color[,] colors = new Color[,] {
            { Color.White, Color.Red, Color.FromArgb(128, 128, 128) }
        };
        byte[,] grayscale = ConvertImg.toGrayScale<byte>(colors);
        Assert(grayscale[0, 0] == 255 && grayscale[0, 1] == 85 && grayscale[0, 2] == 128,
            "RGB grayscale conversion must average channels and remain in the byte range.");
        using (Bitmap bitmap = ConvertImg.toBitmap(new double[,] { { 4, 4 }, { 4, 4 } }))
            Assert(bitmap.GetPixel(0, 0).A == 255 && bitmap.GetPixel(0, 0).R == 0,
                "A constant numeric image must produce opaque black, rather than transparent pixels.");
    }

    private static void ContoursAtImageBorders()
    {
        int[,] square = new int[,] { { 1, 1 }, { 1, 1 } };
        Contour<int> contour = new Contour<int>(square, 1, 1, 1);
        Assert(contour.x.Length > 1 && contour.xMin == 0 && contour.yMin == 0 &&
            contour.xMax == 1 && contour.yMax == 1,
            "A solid 2x2 border contour must not collapse to its seed pixel.");
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                Assert(contour.Inside(x, y), "Every pixel in the solid square lies on or inside its contour.");

        int[,] strip = new int[1, 5];
        for (int y = 0; y < 5; y++) strip[0, y] = 1;
        Contour<int> thinContour = new Contour<int>(strip, 1, 0, 2);
        Assert(thinContour.yMin == 0 && thinContour.yMax == 4,
            "A one-pixel-wide contour must traverse both ends of the strip.");
        for (int y = 0; y < 5; y++)
            Assert(thinContour.Inside(0, y), "A thin contour must retain every boundary pixel.");

        Contour<int> singleton = new Contour<int>(new int[,] { { 0 } }, 1, 0, 0);
        Assert(singleton.Inside(0, 0) && !singleton.Inside(1, 0),
            "An isolated below-threshold contour must retain its single pixel.");
    }

    private static void TecplotUnseparatedDataAndCounts()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dat");
        CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            string header = "\nTITLE = \"Block data\"\n\nVARIABLES = \"X\"\n  \"Y\"\n" +
                " ZONE I = 2, J = 2, K = 3, DATAPACKING = BLOCK\n";
            string values = string.Join("\t", Enumerable.Range(0, 24).Select(index =>
                (index + 1.25).ToString(CultureInfo.InvariantCulture)));
            File.WriteAllText(path, header + values);
            Data.Tecplot data = Isidore.Load.Load.Tecplot(path);
            Assert(data.Variables.Length == 2 && data.Data[0][0, 0, 0] == 1.25 &&
                data.Data[0][1, 1, 2] == 12.25 && data.Data[1][0, 0, 0] == 13.25 &&
                data.Data[1][1, 1, 2] == 24.25,
                "Tecplot must preserve unseparated tab-delimited BLOCK data and its first-index-fastest order.");

            string oneDimensional = "VARIABLES = \"X\"\nZONE I=2, DATAPACKING=BLOCK\n";
            File.WriteAllText(path, oneDimensional + "1.5 2.5");
            data = Isidore.Load.Load.Tecplot(path);
            Assert(data.Data[0].GetLength(1) == 1 && data.Data[0].GetLength(2) == 1 &&
                data.Data[0][1, 0, 0] == 2.5,
                "Omitted ordered Tecplot dimensions must default to one.");
            File.WriteAllText(path, oneDimensional + "1.5");
            ExpectException<FormatException>(() => Isidore.Load.Load.Tecplot(path),
                "Truncated Tecplot data must be rejected rather than filled with zeros.");
            File.WriteAllText(path, oneDimensional + "1.5 2.5 3.5");
            ExpectException<FormatException>(() => Isidore.Load.Load.Tecplot(path),
                "Extra Tecplot data must be rejected with a format error.");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void ConcurrentTecplotReads()
    {
        string[] paths = new string[] {
            Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dat"),
            Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dat")
        };
        try
        {
            for (int index = 0; index < paths.Length; index++)
                File.WriteAllText(paths[index], "TITLE=\"File " + index + "\"\n" +
                    "VARIABLES=\"X\"\nZONE I=2, DATAPACKING=BLOCK\n" +
                    (index + 10) + " " + (index + 20));
            Parallel.For(0, 32, iteration =>
            {
                int index = iteration % paths.Length;
                Data.Tecplot data = Isidore.Load.Load.Tecplot(paths[index]);
                Assert(data.Title == "File " + index && data.Data[0][1, 0, 0] == index + 20,
                    "Concurrent Tecplot reads must not mix another file's metadata or values.");
            });
        }
        finally
        {
            foreach (string path in paths)
                if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void TurbulenceTimeAndDisplacement()
    {
        TurbulencePointWFS point = new TurbulencePointWFS(
            noiseMagnitude: new Noise(multiplier: 0, offset: 1),
            noiseSpeed: new Noise(multiplier: 0, offset: 0),
            noiseDirection: new Noise(multiplier: 0, offset: 0),
            noiseTransform: Transform.Translate(10, 20, 30), timestep: 1);
        point.GetVal(new Point(), 3);
        Assert(point.LastTime == 3, "Extending a turbulence timeline to an exact key must stop at that key.");
        foreach (Point position in point.NoisePosition.Values)
            Assert(position.Comp.All(component => component == 0),
                "Translation of the noise frame must not create motion when turbulence speed is zero.");
        foreach (double invalidTime in new double[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            ExpectException<ArgumentOutOfRangeException>(() => point.GetVal(new Point(), invalidTime),
                "Nonfinite turbulence evaluation times must be rejected before extending the timeline.");

        point.NoisePosition.AddKeys(new Point(), 1e20);
        ExpectException<InvalidOperationException>(() => point.GetVal(new Point(), 1e20 + 1e6),
            "A time step that cannot advance the floating-point timestamp must fail rather than loop forever.");
    }

    private static void CannyOrientationAndHysteresis()
    {
        double[,] horizontal = new double[9, 9];
        double[,] vertical = new double[9, 9];
        double[,] weakContinuation = new double[9, 9];
        for (int x = 0; x < 9; x++)
            for (int y = 0; y < 9; y++)
            {
                horizontal[x, y] = x >= 4 ? 1 : 0;
                vertical[x, y] = y >= 4 ? 1 : 0;
                weakContinuation[x, y] = x >= 4 ? (y <= 3 ? 1 : 0.3) : 0;
            }
        var first = Canny.Process(horizontal, 0.8, 0.2);
        var second = Canny.Process(vertical, 0.8, 0.2);
        Assert(first.Item1[4, 4] && first.Item6[4, 4] == 1 &&
            second.Item1[4, 4] && second.Item6[4, 4] == 2,
            "Canny must thin the two axis-aligned step edges in their corresponding gradient directions.");
        var connected = Canny.Process(weakContinuation, 0.8, 0.2);
        var weakOnly = Canny.Process(weakContinuation, 1.1, 0.2);
        Assert(connected.Item1[4, 6] && !weakOnly.Item1[4, 6],
            "Canny hysteresis must retain a weak continuation connected to a strong edge.");
    }
}
