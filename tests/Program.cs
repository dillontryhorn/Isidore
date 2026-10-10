using System;
using Isidore.Maths;
using Isidore.Render;

internal static class RegressionTests
{
    private static int Main()
    {
        int failed = 0;
        Run("Maths", MathsRegression.Run, ref failed);
        Run("Additional maths edge cases", AdditionalMathsRegression.Run, ref failed);
        Run("Render", RenderRegression.Run, ref failed);
        Run("Additional rendering edge cases", AdditionalRenderRegression.Run, ref failed);
        Run("Load, models and image processing", LoadModelsRegression.Run, ref failed);
        Run("Additional loader, model and image edge cases", AdditionalLoadModelsRegression.Run, ref failed);
        Run("MATLAB wrapper (test double) and data extraction", MatlabRegression.Run, ref failed);
        Run("Additional MATLAB edge cases", AdditionalMatlabRegression.Run, ref failed);
        Run("Scene and embedded asset smoke checks", SmokeChecks, ref failed);
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
