using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using Isidore.ImgProcess;
using Isidore.Load;
using Isidore.Maths;
using Isidore.Models;
using Isidore.Render;
using Point = Isidore.Maths.Point;

internal static class LoadModelsRegression
{
    public static void Run()
    {
        TextLineEndings();
        BitmapLoaderReleasesFile();
        TecplotReaderReleasesFile();
        NastranFormatsAndContinuations();
        ObjIndicesAndObjects();
        TurbulenceDefaultsAndClones();
        TurbulenceVarianceNormalization();
        ReferenceInterpolationDistances();
        CannyZeroImage();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static void TextLineEndings()
    {
        string[] lines = Text.Parse("first\n\rsecond\r\nthird\rfourth\nfifth");
        Assert(lines.Length == 5 && lines[1] == "second" && lines[3] == "fourth",
            "Text parsing must consume paired line endings before single-character ones.");
    }

    private static void BitmapLoaderReleasesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bmp");
        try
        {
            using (Bitmap bitmap = new Bitmap(1, 1))
            {
                bitmap.SetPixel(0, 0, Color.Red);
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            Color[,] pixels = Isidore.Load.Load.Bitmap(path);
            Assert(pixels[0, 0].R == 255, "Bitmap loading must preserve pixel colors.");
            using (FileStream file = File.Open(path, FileMode.Open,
                FileAccess.ReadWrite, FileShare.None))
                Assert(file.Length > 0, "The bitmap loader must release its input file.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void NastranFormatsAndContinuations()
    {
        Assert(FileRef.NAS.Format.WhichFormat("GRID,123,,1,2,3") ==
            FileRef.NAS.Format.fType.Free, "Free-format GRID fields were misidentified.");
        Assert(FileRef.NAS.Format.Separate("+,7").Length == 2,
            "Short free-format continuation lines must parse.");
        Assert(FileRef.NAS.Format.Separate("GRID    ,123,,1,2,3").Length == 6,
            "Padded free-format mnemonics must parse.");
        Data.NAS nas = Isidore.Load.Load.NAS(new string[] {
            "GRID,123,,1.5+2,-2.5-1,3D+1",
            "CTETRA,12,7,1,2,3,4,5,6,+A",
            "+A,7,8,+B",
            "+B,9,10"
        });
        Assert(nas.Grid.Position[0, 0] == 150 && nas.Grid.Position[0, 1] == -0.25 &&
            nas.Grid.Position[0, 2] == 30, "NASTRAN signed and D exponents were not parsed.");
        Assert(nas.Node.Vertices[0, 9] == 10,
            "NASTRAN must follow all continuation lines rather than only the first.");

        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert(FileRef.NAS.GRID.parseAsDouble("1.25+2") == 125,
                "NASTRAN numeric data must parse independently of the current culture.");
        }
        finally { Thread.CurrentThread.CurrentCulture = originalCulture; }
    }

    private static void TecplotReaderReleasesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dat");
        string header = "TITLE=\"Example\"\nVARIABLES=\"X\"\n" +
            "ZONE I=2, J=1, K=1, DATAPACKING=BLOCK\nDT=(DOUBLE)\n";
        try
        {
            File.WriteAllText(path, header + "1 2\n");
            Data.Tecplot data = Isidore.Load.Load.Tecplot(path);
            Assert(data.Title == "Example" && data.Data[0][1, 0, 0] == 2,
                "Tecplot BLOCK values must load into the declared dimensions.");
            using (FileStream file = File.Open(path, FileMode.Open,
                FileAccess.ReadWrite, FileShare.None))
                Assert(file.Length > 0, "A successful Tecplot read must release its file.");

            File.WriteAllText(path, header + "1 invalid\n");
            bool rejected = false;
            try { Isidore.Load.Load.Tecplot(path); }
            catch (FormatException) { rejected = true; }
            Assert(rejected, "Malformed Tecplot numeric data must be rejected.");
            using (FileStream file = File.Open(path, FileMode.Open,
                FileAccess.ReadWrite, FileShare.None))
                Assert(file.Length > 0, "A failed Tecplot read must release its file.");

            File.WriteAllText(path, "TITLE=\"Header only\"\nVARIABLES=\"X\"\n" +
                "ZONE I=2, J=1, K=1, DATAPACKING=BLOCK");
            var metadata = Isidore.Load.Load.TecplotHeader(path);
            Assert(metadata.Item1 == "Header only" && metadata.Item4[0] == 2,
                "Reading Tecplot metadata must handle end-of-file without a null dereference.");
            using (FileStream file = File.Open(path, FileMode.Open,
                FileAccess.ReadWrite, FileShare.None))
                Assert(file.Length > 0, "Tecplot header inspection must release its file.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void ObjIndicesAndObjects()
    {
        Polyshape shape = OBJ.Read(new string[] {
            "  o first mesh", "v 0 0 0", "v 1 0 0", "v 1 1 0", "v 0 1 0",
            "vt 0.2 0.3", "vt 0.8 0.9", "vn 0 0 1", "vn 0 0 -1",
            "  f\t1/2/2  2/1/1 3/2/2 4/1/1 # quad",
            "# comments and materials must not discard the coordinate tables",
            "usemtl ignored", "f 1/1/1 3/1/1 4/1/1", "o second",
            "f -4//-1 -3//-1 \\", "-2//-1", ""
        });
        Assert(shape.Shapes.Count == 2, "OBJ must produce meshes only for objects with faces.");
        Mesh first = (Mesh)shape.Shapes[0];
        Mesh second = (Mesh)shape.Shapes[1];
        Assert(first.Name == "first mesh" && first.Facets.Count == 3,
            "OBJ quad triangulation or face continuation after metadata was incorrect.");
        Vertex corner = first.LocalVertices[first.Facets[0][0]];
        Assert(corner.UV[0] == 0.8 && corner.Normal.Comp[2] == -1,
            "OBJ face texture and normal indices must be independent of position indices.");
        Assert(second.Facets.Count == 1 && second.LocalVertices[0].Position.Comp[0] == 0,
            "OBJ negative indices must refer to the file-wide table across objects.");
        Assert(OBJ.Read(new string[] { "# no faces" }).Shapes.Count == 0,
            "An OBJ without faces must not produce an empty mesh.");

        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Polyshape fractional = OBJ.Read(new string[] {
                "v 0.5 0 0", "v 1 0 0", "v 0 1 0", "f 1 2 3"
            });
            Assert(((Mesh)fractional.Shapes[0]).LocalVertices[0].Position.Comp[0] == 0.5,
                "OBJ decimal numbers must use invariant culture.");
        }
        finally { Thread.CurrentThread.CurrentCulture = originalCulture; }
    }

    private static void TurbulenceDefaultsAndClones()
    {
        TurbulentNoise turbulent = new TurbulentNoise();
        Assert(((PerlinNoiseFunction)turbulent.noiseFunc).StandardNormal,
            "The default turbulent noise function must be initialized and standardized.");
        TurbulencePointWFS point = new TurbulencePointWFS();
        Assert(point.Comp.Length == 3 && point.LastTime == 0,
            "A default turbulence point must initialize at the three-dimensional origin.");
        TurbulencePointWFS clone = point.Clone();
        clone.Comp[0] = 5;
        clone.NoisePosition.Values[0].Comp[0] = 10;
        clone.ReferencePoints.Add(new Point());
        Assert(point.Comp[0] == 0 && point.NoisePosition.Values[0].Comp[0] == 0 &&
            point.ReferencePoints.Count == 0,
            "Cloned turbulence points must have independent coordinates, timeline values, and lists.");
        bool rejected = false;
        try { point.TimeStep = 0; }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Assert(rejected, "A zero time step would prevent timeline extension from terminating.");
    }

    private static void ReferenceInterpolationDistances()
    {
        TurbulencePointWFS turbulence0 = new TurbulencePointWFS(new Point(0.0, 0, 0),
            noiseMagnitude: new Noise(multiplier: 0, offset: 2));
        TurbulencePointWFS turbulence1 = new TurbulencePointWFS(new Point(2.0, 0, 0),
            noiseMagnitude: new Noise(multiplier: 0, offset: 4));
        ReferencePoint reference0 = new ReferencePoint(new double[] { 0, 0, 0 },
            new List<Point> { turbulence0 });
        ReferencePoint reference1 = new ReferencePoint(new double[] { 2, 0, 0 },
            new List<Point> { turbulence1 });
        ReferencePoint middle = new ReferencePoint(new double[] { 1, 0, 0 },
            new List<Point> { reference0, reference1 }, ReferencePoint.Category.Interpolation);
        ReferencePointTurbulence field = new ReferencePointTurbulence(
            new List<ReferencePoint> { middle });
        RefPtTurbStruct result = field.GetStruct(new Point(1.0, 0, 0), 0);
        Assert(Math.Abs(result.Value - 3) < 1e-12, "Reference midpoint interpolation was incorrect.");
        Assert(result.TurbulencePointDistance != null && result.TurbulencePointDistance.Length == 2 &&
            result.TurbulencePointDistance[0] == 1 && result.TurbulencePointDistance[1] == 1,
            "Interpolation must return distances from both reference point groups.");
    }

    private sealed class IndependentOctaveNoise : TurbulentNoise
    {
        public IndependentOctaveNoise() : base(minFreq: 1, maxFreq: 4,
            Hurst: 1, mean: 5, std: 3) { }

        public override double[] GetComponents(Point coordinate)
        {
            int sample = (int)coordinate.Comp[0];
            // Across all eight samples these independent sign sequences each
            // have zero mean and unit variance, with zero cross covariance.
            return new double[] {
                (sample & 1) == 0 ? -1 : 1,
                (sample & 2) == 0 ? -1 : 1,
                (sample & 4) == 0 ? -1 : 1
            };
        }
    }

    private static void TurbulenceVarianceNormalization()
    {
        IndependentOctaveNoise noise = new IndependentOctaveNoise();
        double sum = 0;
        double squareSum = 0;
        for (int sample = 0; sample < 8; sample++)
        {
            double value = noise.GetBaseVal(new Point((double)sample, 0, 0));
            sum += value;
            squareSum += (value - 5) * (value - 5);
        }
        Assert(Math.Abs(sum / 8 - 5) < 1e-12 && Math.Abs(squareSum / 8 - 9) < 1e-12,
            "Weighted unit-variance turbulence octaves must preserve the requested mean and variance.");
    }

    private static void CannyZeroImage()
    {
        var result = Canny.Process(new double[5, 5], 0.5, 0.2);
        foreach (double magnitude in result.Item4)
            Assert(magnitude == 0, "Canny must return finite zero magnitudes for an all-zero image.");
        foreach (bool edge in result.Item1)
            Assert(!edge, "Canny must not detect edges in an all-zero image.");
    }
}
