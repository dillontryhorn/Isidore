using System;
using Isidore.Maths;
using Isidore.Render;

internal static class RegressionTests
{
    private static int Main(string[] args)
    {
        if (Array.IndexOf(args, "--cpu") >= 0 &&
            (Array.IndexOf(args, "--benchmark") >= 0 || Array.IndexOf(args, "--require-gpu") >= 0))
        {
            Console.Error.WriteLine("CPU-only execution cannot require or benchmark a GPU.");
            return 1;
        }
        if (Array.IndexOf(args, "--cpu") >= 0)
            GpuAcceleration.Mode = GpuMode.Disabled;
        if (Array.IndexOf(args, "--require-gpu") >= 0 && !GpuAcceleration.IsAvailable)
        {
            Console.Error.WriteLine("A compatible GPU is required: " + GpuAcceleration.LastError);
            return 1;
        }
        int failed = 0;
        bool cpuOnly = Array.IndexOf(args, "--cpu") >= 0;
        if (!cpuOnly)
            Run("GPU runtime and safe fallback", GpuRuntimeRegression.Run, ref failed);
        if (!cpuOnly)
            Run("Pooled GPU download equivalence", PoolRegression.Run, ref failed);
        Run("Maths", MathsRegression.Run, ref failed);
        Run("SIMD primitive array equivalence", SimdRegression.Run, ref failed);
        Run("Bitmap pixel equivalence", BitmapPerformanceRegression.Run, ref failed);
        Run("Additional maths edge cases", AdditionalMathsRegression.Run, ref failed);
        Run("Render", RenderRegression.Run, ref failed);
        Run("Additional rendering edge cases", AdditionalRenderRegression.Run, ref failed);
        Run("Load, models and image processing", LoadModelsRegression.Run, ref failed);
        Run("Additional loader, model and image edge cases", AdditionalLoadModelsRegression.Run, ref failed);
        Run("MATLAB wrapper (test double) and data extraction", MatlabRegression.Run, ref failed);
        Run("Additional MATLAB edge cases", AdditionalMatlabRegression.Run, ref failed);
        Run("Scene and embedded asset smoke checks", SmokeChecks, ref failed);
        if (!cpuOnly)
        {
            Run("GPU maths equivalence", GpuMathsRegression.Run, ref failed);
            Run("GPU image equivalence", GpuImageRegression.Run, ref failed);
            Run("GPU noise equivalence", GpuNoiseRegression.Run, ref failed);
            Run("GPU rendering equivalence", GpuRenderRegression.Run, ref failed);
        }
        if (failed == 0 && Array.IndexOf(args, "--benchmark") >= 0)
        {
            GpuMathsRegression.Benchmark();
            Console.WriteLine(GpuImageRegression.Benchmark());
            Console.WriteLine(GpuNoiseRegression.Benchmark());
            Console.WriteLine(GpuRenderRegression.Benchmark());
        }
        Console.WriteLine(failed == 0 ? "All regression groups passed." : failed + " regression group(s) failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action action, ref int failed)
    {
        try
        {
            action();
            Console.WriteLine("PASS: " + name);
        }
        catch (Exception error)
        {
            failed++;
            Console.Error.WriteLine("FAIL: " + name + "\n" + error);
        }
    }

    private static void SmokeChecks()
    {
        var projector = new RectangleProjector(3, 3, 0.1, 0.1);
        projector.TransformTimeLine = new KeyFrameTrans(Transform.Translate(0, 0, -3));
        var scene = new Scene();
        scene.UseMultiCores = false;
        scene.Projectors.Add(projector);
        scene.Bodies.Add(new Isidore.Render.Sphere(new Point(0, 0, 0), 1));
        scene.AdvanceToTime(0);
        var ray = projector.Ray(1, 1).Rays[0];
        if (!ray.IntersectData.Hit || Math.Abs(ray.IntersectData.Travel - 2) > 1e-12)
            throw new Exception("The central scene ray should hit the sphere at distance 2.");
        scene.UseMultiCores = true;
        scene.AdvanceToTime(0, true);
        if (Math.Abs(projector.Ray(1, 1).Rays[0].IntersectData.Travel - 2) > 1e-12)
            throw new Exception("Multicore rendering should produce the same central intersection.");
        if (Isidore.Library.Models.Cube() == null)
            throw new Exception("The embedded cube asset should load.");
        if (Isidore.Library.Images.R() == null)
            throw new Exception("The embedded image asset should load.");
    }
}
