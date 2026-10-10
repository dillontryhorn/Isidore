using System;
using System.Collections.Generic;
using Isidore.Maths;

internal static class MathsRegression
{
    public static void Run()
    {
        MeansAndPopulationStatistics();
        TaggedStatistics();
        QuadraticRoots();
        MultidimensionalIndices();
        PlaneIntersections();
        KeyFrameMutations();
        TransformKeyFrames();
        ArrayUtilitiesAndRetrieval();
        SmallAndMultidimensionalKdTrees();
    }

    private static void Near(double expected, double actual, string message)
    {
        if (double.IsNaN(actual) || Math.Abs(expected - actual) > 1e-9 * Math.Max(1, Math.Abs(expected)))
            throw new Exception(message + ": expected " + expected + ", received " + actual);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception(message);
    }

    private static void MeansAndPopulationStatistics()
    {
        Near(2, Stats.Mean(new[] { 1, 2, 3 }), "Integer vector mean");
        Near(3, Stats.Mean(new[,] { { 2, 4 } }), "Integer matrix mean");
        Near(2, Stats.Mean(new[] { 1.0, 2.0, 3.0 }), "Double vector mean");
        Near(3, Stats.Mean(new[,] { { 2.0, 4.0 } }), "Double matrix mean");
        Check(Stats.Mean<decimal>(new[] { 1m, 2m, 3m }) == 2m, "Generic vector mean");
        Check(Stats.Mean<decimal>(new[,] { { 2m, 4m } }) == 3m, "Generic matrix mean");
        Near(1, Stats.Variance(new[] { 1.0, 3.0 })[0], "Population variance");
        Near(1, Stats.STD(new[] { 1.0, 3.0 })[0], "Population standard deviation");
        Near(0, Stats.Variance(new[] { 7.0 })[0], "Singleton variance");
        Throws<ArgumentException>(() => Stats.Mean(new double[0]), "Empty mean must reject an empty data set");
        Throws<ArgumentOutOfRangeException>(() => Stats.Variance(0.0, 0.0, 0), "Empty variance must reject an empty data set");
    }

    private static void TaggedStatistics()
    {
        double[] vector = { 1, 99, 3 };
        bool[] tags = { true, false, true };
        double[,] matrix = { { 1, 99 }, { 3, 99 } };
        bool[,] matrixTags = { { true, false }, { true, false } };
        Near(1, Stats.Variance(vector, tags)[0], "Tagged double vector variance");
        Near(2, Stats.Variance(vector, tags)[1], "Tagged double vector mean");
        Near(1, Stats.STD(vector, tags)[0], "Tagged double vector standard deviation");
        Near(1, Stats.Variance(matrix, matrixTags)[0], "Tagged double matrix variance");
        Near(2, Stats.STD(matrix, matrixTags)[1], "Tagged double matrix mean");
        int[] integers = { 1, 99, 3 };
        int[,] integerMatrix = { { 1, 99 }, { 3, 99 } };
        Near(1, Stats.Variance<int>(integers, tags)[0], "Tagged generic vector variance");
        Near(2, Stats.STD<int>(integers, tags)[1], "Tagged generic vector mean");
        Near(1, Stats.Variance<int>(integerMatrix, matrixTags)[0], "Tagged generic matrix variance");
        Near(1, Stats.STD<int>(integerMatrix, matrixTags)[0], "Tagged generic matrix standard deviation");
        bool[,] legacyTags = { { true, false, true } };
        Near(1, Stats.Variance<int>(integers, legacyTags)[0], "Compatibility variance overload uses tags");
        Near(2, Stats.STD<int>(integers, legacyTags)[1], "Compatibility STD overload uses tags");
        Throws<ArgumentException>(() => Stats.Variance<int>(integers, new bool[2]), "Mismatched generic tags must be rejected");
        Throws<ArgumentOutOfRangeException>(() => Stats.STD(vector, new bool[3]), "All-false tags must reject an empty selection");
    }

    private static void QuadraticRoots()
    {
        double[] roots = Function.Quadratic(1, 0, -4);
        Near(-2, roots[0], "Zero linear coefficient lower root");
        Near(2, roots[1], "Zero linear coefficient upper root");
        roots = Function.Quadratic(1, -4, 4);
        Near(2, roots[0], "Repeated lower root");
        Near(2, roots[1], "Repeated upper root");
        roots = Function.Quadratic(0, 2, -6);
        Near(3, roots[0], "Degenerate linear root");
        roots = Function.Quadratic(1, 0, 4);
        Check(double.IsNaN(roots[0]) && double.IsNaN(roots[1]), "A negative discriminant must return no real roots");
        roots = Function.Quadratic(1, 1e8, 1);
        Near(1, roots[0] * roots[1], "Stable root product for disparate magnitudes");
    }

    private static void MultidimensionalIndices()
    {
        int[] resolution = { 2, 3, 4, 5 };
        for (int index = 0; index < 120; index++)
            Check(Function.LinearIndex(Function.SubscriptIndex(index, resolution), resolution) == index,
                "Linear and subscript indices must round trip in four dimensions");
        Check(Function.LinearIndex(new[] { 1, 2, 3 }, new[] { 2, 3, 4 }) == 23,
            "Linear index must include every previous dimension in its stride");
    }

    private static void PlaneIntersections()
    {
        Plane plane = new Plane(new Point(0, 0, 2), new Normal(0, 0, -1));
        var hit = plane.RayIntersection(new Ray(new Point(0, 0, 0), new Vector(0, 0, 1)));
        Near(2, hit.Item1, "Plane hit travel");
        Near(2, hit.Item3.Comp[2], "Plane hit point");
        Near(1, hit.Item2, "Plane incidence cosine");
        var miss = plane.RayIntersection(new Ray(new Point(0, 0, 0), new Vector(0, 0, -1)));
        Check(double.IsNaN(miss.Item1) && double.IsNaN(miss.Item3.Comp[0]), "A plane miss must return a NaN point");
        var parallel = plane.RayIntersection(new Ray(new Point(0, 0, 0), new Vector(1, 0, 0)));
        Check(double.IsPositiveInfinity(parallel.Item1), "A parallel ray must return infinite travel");
        Near(0, parallel.Item2, "Parallel incidence cosine");
        Check(double.IsPositiveInfinity(parallel.Item3.Comp[0]), "A parallel ray must return an infinite point");
        var backFace = plane.RayIntersection(new Ray(new Point(0, 0, 3), new Vector(0, 0, -1)), false);
        Check(double.IsNaN(backFace.Item1), "A disabled back face must not intersect");
        Plane plane2D = new Plane(new Point(new[] { 0.0, 2.0 }), new Normal(new[] { 0.0, -1.0 }));
        var miss2D = plane2D.RayIntersection(new Ray(new Point(new[] { 0.0, 0.0 }), new Vector(new[] { 0.0, -1.0 })));
        Check(miss2D.Item3.Comp.Length == 2 && double.IsNaN(miss2D.Item3.Comp[0]),
            "A plane miss point must retain the plane dimension");
    }

    private static void KeyFrameMutations()
    {
        KeyFrame<double> keys = new KeyFrame<double>(new[] { 0.0, 20.0 }, new[] { 0.0, 2.0 });
        Near(10, keys.InterpolateToTime(1), "Initial interpolation");
        keys.AddKeys(6, 1);
        Check(keys.Times.Length == 3 && keys.Times[1] == 1, "A middle key must be inserted in time order");
        Near(6, keys.InterpolateToTime(1), "Insertion must refresh a cached interpolation");
        keys.AddKeys(8, 1);
        Check(keys.Times.Length == 3, "Replacing a key must not add a duplicate timestamp");
        Near(8, keys.CurrentValue, "Replacement must refresh the current value");
        keys.AddKeys(-10, -1);
        keys.AddKeys(30, 3);
        Check(keys.Times[0] == -1 && keys.Times[4] == 3, "Keys may be prepended or appended");
        keys.Scale(2);
        Near(16, keys.InterpolateToTime(1), "Scale must retain its result and refresh the cache");
        keys.Offset(1);
        Near(17, keys.InterpolateToTime(1), "Offset must retain its result and refresh the cache");
        keys.RemoveKeys(2);
        Near(21, keys.InterpolateToTime(1), "Removing a key must refresh interpolation");
        keys.Animate = false;
        Near(-19, keys.InterpolateToTime(1), "Animation switch must invalidate the time cache");
        keys.Animate = true;
        Near(21, keys.InterpolateToTime(1), "Restoring animation at the same time must recompute");
        KeyFrame<double> single = new KeyFrame<double>(1, 0);
        single.RemoveKeys(0);
        single.AddKeys(5, 2);
        Near(5, single.InterpolateToTime(2), "An empty timeline must accept its first new key");
        Throws<ArgumentException>(() => new KeyFrame<double>(new[] { 1.0, 2.0 }, new[] { 0.0 }),
            "A timeline must reject mismatched keys and timestamps");
        Throws<ArgumentException>(() => new KeyFrame<double>(new[] { 1.0, 2.0 }, new[] { 1.0, 0.0 }),
            "A timeline must reject descending timestamps");
        Throws<ArgumentException>(() => new KeyFrame<double>(new[] { 1.0, 2.0 }, new[] { 1.0, 1.0 }),
            "A timeline must reject duplicate constructor timestamps");
        Throws<ArgumentException>(() => keys.AddKeys(1, double.NaN),
            "A timeline must reject NaN timestamps");
    }

    private static void TransformKeyFrames()
    {
        KeyFrameTrans keys = new KeyFrameTrans(new[] { Transform.Translate(0, 0, 0), Transform.Translate(2, 0, 0) }, new[] { 0.0, 2.0 });
        KeyFrame<Transform> baseReference = keys;
        Near(1, baseReference.InterpolateToTime(1).M[0, 3], "Base references must use transform interpolation");
        keys.AddKeys(Transform.Translate(4, 0, 0), 1);
        Near(4, keys.CurrentValue.M[0, 3], "A transform key insertion must refresh its cached value");
        keys.ApplyTransform(Transform.Translate(3, 0, 0));
        Near(7, keys.InterpolateToTime(1).M[0, 3], "Applying a transform must refresh the current frame");
        keys.MergeKeys(new KeyFrameTrans(Transform.Translate(1, 0, 0)));
        Near(8, keys.InterpolateToTime(1).M[0, 3], "Transform timelines must merge through transform interpolation");
    }

    private sealed class NestedValue
    {
        public NestedValue Child { get; set; }
        public int Number { get; set; }
    }

    private static void ArrayUtilitiesAndRetrieval()
    {
        Check(Compare.Size<int, double>(new int[2, 3], new double[2, 3]) == 0,
            "Matching array dimensions must return the documented zero status");
        Check(Compare.Size<int, double>(new int[2, 3], new double[3, 2]) == 2,
            "Differing array dimensions must return the size-mismatch status");
        Array uniform = Distribution.Uniform(new[] { 2, 3, 4 }, 7);
        Check(uniform.Rank == 3 && uniform.Length == 24, "Uniform distribution must retain all dimensions");
        foreach (int value in uniform)
            Check(value == 7, "Uniform must fill every element of a multidimensional array");
        Check(Distribution.Uniform(new[] { 2, 0, 4 }, 7).Length == 0, "Uniform must support an empty dimension");
        NestedValue missing = new NestedValue();
        Check(Retrieve<NestedValue>.Value<int>(missing, "Child.Number") == 0,
            "A missing intermediate object must return the documented default");
        NestedValue present = new NestedValue { Child = new NestedValue { Number = 12 } };
        Check(Retrieve<NestedValue>.Value<int>(present, "Child.Number") == 12,
            "Nested reflection retrieval must still find present members");
    }

    private static void SmallAndMultidimensionalKdTrees()
    {
        Point point = new Point(1, 2, 3);
        KDTree single = new KDTree(new List<Point> { point });
        Check(single.boxes.Length == 1, "A singleton K-D tree must have one leaf node");
        var nearest = single.Nearest(new Point(1, 2, 5));
        Check(nearest.Item1 == 0, "A singleton tree must return its only point");
        Near(2, nearest.Item2, "Singleton nearest distance");
        var near = single.LocateNear(point, 0);
        Check(near.Item1.Length == 1 && near.Item1[0] == 0, "Zero-radius search must include a coincident point");
        Near(0, near.Item2[0], "Coincident singleton distance");
        Check(single.LocateNear(new Point(1, 2, 5), 1).Item1.Length == 0,
            "A radius search must exclude an out-of-range singleton");
        Check(single.LocateNear(point, 1, 0).Item1.Length == 0,
            "A zero-result limit must return no points");
        KDTree pair = new KDTree(new List<Point> { new Point(0, 0, 0), new Point(4, 0, 0) });
        Check(pair.boxes.Length == 1 && pair.Nearest(new Point(3, 0, 0)).Item1 == 1,
            "A two-point tree must search both points in its root leaf");
        near = pair.LocateNear(new Point(3, 0, 0), 5, 1);
        Check(near.Item1.Length == 1 && near.Item1[0] == 1, "Limited radius search must retain the closest point");
        KDTree split = new KDTree(new List<Point> {
            new Point(new[] { 0.0 }), new Point(new[] { 1.0 }), new Point(new[] { 1.0 }) });
        near = split.LocateNear(new Point(new[] { 0.0 }), 1);
        Check(near.Item1.Length == 3, "An inclusive radius touching a split plane must search both daughters");
        near = split.LocateNear(new Point(new[] { 1.0 }), 0);
        Check(near.Item1.Length == 2, "Zero-radius search must return duplicate points from both daughters");
        foreach (double distance in near.Item2)
            Near(0, distance, "Duplicate point distance");
        near = split.LocateNear(new Point(new[] { 2.0 }), 1);
        Check(near.Item1.Length == 2, "A radius touching the split plane from the right must include both duplicates");
        Throws<ArgumentNullException>(() => new KDTree(null), "A null point list must be rejected explicitly");
        Throws<ArgumentException>(() => new KDTree(new List<Point>()), "An empty point list must be rejected explicitly");
        Throws<ArgumentException>(() => new KDTree(new List<Point> { new Point(new double[0]) }),
            "A K-D tree must reject points without coordinates");
        Throws<ArgumentException>(() => new KDTree(new List<Point> { new Point(new double[1]), new Point(new double[2]) }),
            "A K-D tree must reject mixed point dimensions");

        // Check subdivision and node sizing against an exhaustive distance search.
        for (int dimensions = 1; dimensions <= 4; dimensions++)
            for (int count = 3; count <= 40; count++)
            {
                List<Point> points = new List<Point>();
                for (int index = 0; index < count; index++)
                {
                    double[] coordinates = new double[dimensions];
                    for (int dimension = 0; dimension < dimensions; dimension++)
                        coordinates[dimension] = ((index * (dimension + 3)) % 17) + index * 0.13;
                    points.Add(new Point(coordinates));
                }
                double[] queryCoordinates = new double[dimensions];
                for (int dimension = 0; dimension < dimensions; dimension++)
                    queryCoordinates[dimension] = 6.4 + dimension * 0.7;
                Point query = new Point(queryCoordinates);
                double expectedDistance = double.PositiveInfinity;
                List<double> expectedRange = new List<double>();
                foreach (Point candidate in points)
                {
                    double distance = candidate.Distance(query);
                    expectedDistance = Math.Min(expectedDistance, distance);
                    if (distance <= 7)
                        expectedRange.Add(distance);
                }
                KDTree tree = new KDTree(points);
                nearest = tree.Nearest(query);
                Near(expectedDistance, nearest.Item2, "K-D nearest search must match exhaustive distance search");
                Near(expectedDistance, points[nearest.Item1].Distance(query), "Nearest index must identify a nearest point");
                near = tree.LocateNear(query, 7);
                expectedRange.Sort();
                Check(near.Item1.Length == expectedRange.Count, "K-D range search must return every in-range point");
                for (int index = 0; index < expectedRange.Count; index++)
                {
                    Near(expectedRange[index], near.Item2[index], "K-D range distances must be sorted and complete");
                    Near(near.Item2[index], points[near.Item1[index]].Distance(query), "Range indices must match their distances");
                }
            }
    }
}
