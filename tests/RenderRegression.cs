using System;
using System.Collections.Generic;
using Isidore.Maths;
using Isidore.Render;
using RenderSphere = Isidore.Render.Sphere;
using RenderPlane = Isidore.Render.Plane;

public static class RenderRegression
{
    public static void Run()
    {
        ProjectorsAreCentered();
        ParallelSlabBoundariesAreHits();
        AlphaControlsShapesAndMaterialLayers();
        TextureUpdatesTakeEffect();
        RayTreesResetAndReopen();
        IntersectionLookupsAndClones();
        ClonesDispatchThroughBaseReferences();
        ProceduralMaterialClonesOwnTheirPointLists();
        RayGraphClonesPreserveLinks();
        OptionalMeshAttributesAreOptional();
        FrequencySamplingRejectsNonProgressingInputs();
        VoxelPreservesCloserHit();
        SphereRadiusRefreshesGeometry();
        InverseRayTransformsAreHonored();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Render regression: " + message);
    }

    private static void Close(double actual, double expected, string message)
    {
        Assert(!double.IsNaN(actual) && Math.Abs(actual - expected) < 1e-10, message);
    }

    private static void ProjectorsAreCentered()
    {
        var single = new RectangleProjector(1, 1, 2, 3, 0.2, 0.3);
        Close(single.Pos0[0], 0, "single pixel X center");
        Close(single.Pos1[0], 0, "single pixel Y center");
        Close(single.Ang0[0], 0, "single pixel X angle");
        Close(single.Ang1[0], 0, "single pixel Y angle");
        var evenOdd = new RectangleProjector(4, 3, 2, 3, 0.2, 0.3);
        Close(evenOdd.Pos0[0] + evenOdd.Pos0[3], 0, "even pixel axis is symmetric");
        Close(evenOdd.Pos1[1], 0, "odd pixel axis includes center");
    }

    private static void ParallelSlabBoundariesAreHits()
    {
        var box = new AABB(Point.Zero(), new[] { 2.0, 2.0, 2.0 });
        var oriented = new OBB(Point.Zero(), null, new[] { 2.0, 2.0, 2.0 });
        var voxels = new Voxel(Point.Zero(), new[] { 1, 1, 1 }, new[] { 2.0, 2.0, 2.0 });
        foreach (double x in new[] { -1.0, 1.0 })
        {
            var ray = new Ray(new Point(x, 0, -2), new Vector(0, 0, 1));
            var hit = box.Intersect(ray);
            Assert(hit.Item1, "AABB ray on parallel slab boundary");
            Close(hit.Item2[0], 1, "AABB near travel on boundary");
            Assert(oriented.Intersect(ray).Item1, "OBB ray on parallel slab boundary");
            Assert(voxels.Intersect(ray, Point.Zero(), new[] { 1.0, 1.0, 1.0 }).Item1,
                "voxel ray on parallel slab boundary");
        }
        var outside = new Ray(new Point(2, 0, -2), new Vector(0, 0, 1));
        Assert(!box.Intersect(outside).Item1, "parallel ray outside slab must miss");
        Assert(!oriented.Intersect(outside).Item1, "parallel ray outside oriented slab must miss");
    }

    private static void AlphaControlsShapesAndMaterialLayers()
    {
        var plane = new RenderPlane();
        plane.Alpha = new MapTexture(new double[,] { { 0 } });
        plane.AdvanceToTime(0);
        var ray = new RenderRay(new Point(0, 0, -1), new Vector(0, 0, 1));
        Assert(!plane.Intersect(ref ray) && !ray.IntersectData.Hit, "zero plane alpha must reject hit");
        plane.UseAlpha = false;
        Assert(plane.Intersect(ref ray), "disabled shape alpha must permit hit");

        var first = new PropertyValue(new Scalar(10));
        first.Alpha = new MapTexture(new double[,] { { 0 } });
        var second = new PropertyValue(new Scalar(20));
        second.Alpha = new MapTexture(new double[,] { { 1 } });
        var third = new PropertyValue(new Scalar(30));
        var stack = new MaterialStack { first, second, third };
        stack.Apply(ref ray);
        Assert(ray.IntersectData.Properties.Count == 1, "only the first covered material layer applies");
        Close(((Scalar)ray.IntersectData.Properties[0]).Value, 20, "positive material alpha permits layer");
    }

    private static void TextureUpdatesTakeEffect()
    {
        var texture = new MapTexture(new double[,] { { 2, -3, 4 }, { 5, 6, 7 } });
        texture.ScaleMap(3);
        Close(texture.map[0, 1], -9, "texture scaling changes stored map");
        texture.reset();
        Assert(texture.map.GetLength(0) == 1 && texture.map.GetLength(1) == 1,
            "texture reset produces documented single zero pixel");
        Close(texture.map[0, 0], 0, "texture reset value");
    }

    private static void RayTreesResetAndReopen()
    {
        var tree = new RayTree();
        Assert(tree.Open && tree.NextOpenRay() != null, "default ray tree has valid root ray");
        tree.Reset();
        Assert(tree.Rays.Count == 1, "reset of root-only tree is valid");
        tree.Add(new RenderRay(Rank: 1));
        tree.Add(new RenderRay(Rank: 2));
        tree.Rays[0].IntersectData.Hit = true;
        foreach (var ray in tree.Rays)
            ray.Status = RayStatus.Closed;
        Assert(!tree.Open, "all closed rays close tree");
        tree.Reset();
        Assert(tree.Rays.Count == 1 && tree.Open && !tree.Rays[0].IntersectData.Hit,
            "reset removes all descendants and restarts root");
        tree.Rays[0].Status = RayStatus.Closed;
        Assert(!tree.Open, "tree closes after reset root closes");
        tree.Add(new RenderRay(Rank: 1));
        Assert(tree.Open, "adding an open ray reopens closed tree");
    }

    private static void IntersectionLookupsAndClones()
    {
        var miss = new IntersectData();
        Assert(miss.GetValue<int>("Body.ID") == 0, "missing hit body returns default nested value");
        Assert(miss.GetValue<int>("Body") == 0, "null terminal member returns default value");
        var data = new IntersectData(true, 2, new Point(0, 0, 2), null,
            new ShapeSpecificData(new Normal(0, 0, -1), 1, 0.2, 0.3));
        var copy = data.Clone();
        var shapeCopy = copy.BodySpecificData as ShapeSpecificData;
        Assert(shapeCopy != null, "intersection clone preserves shape-specific type");
        Close(shapeCopy.U, 0.2, "intersection clone preserves UV");
        shapeCopy.SurfaceNormal.Comp[2] = 1;
        Close(((ShapeSpecificData)data.BodySpecificData).SurfaceNormal.Comp[2], -1,
            "intersection clone normal is independent");
        BodySpecificData volume = new VolumeSpecificData(new[] { 1 }, new[] { 2 });
        var volumeCopy = (VolumeSpecificData)volume.Clone();
        volumeCopy.IntersectIndex[0] = 9;
        Assert(((VolumeSpecificData)volume).IntersectIndex[0] == 2, "volume clone dispatches through base");
    }

    private static void ClonesDispatchThroughBaseReferences()
    {
        Shape sphere = new RenderSphere(new Point(1, 2, 3), 2);
        var sphereCopy = (RenderSphere)sphere.Clone();
        sphereCopy.radius.Values[0] = 9;
        Close(((RenderSphere)sphere).radius.Values[0], 2, "unadvanced sphere base clone owns radius");

        Texture texture = new MapTexture(new double[,] { { 2 } });
        ((MapTexture)texture.Clone()).map[0, 0] = 9;
        Close(((MapTexture)texture).map[0, 0], 2, "texture base clone owns map");

        Material material = new PropertyValue(new PowerSpectrum(new[] { 1.0 }, new[] { 2.0 }));
        var materialCopy = (PropertyValue)((ICloneable)material).Clone();
        ((PowerSpectrum)materialCopy.Property).Power[0] = 9;
        Close(((PowerSpectrum)((PropertyValue)material).Property).Power[0], 2,
            "material interface clone owns nested spectrum");

        Noise noise = new FrequencyNoise(new PerlinNoiseFunction(7), 1, 4, 2);
        var noiseCopy = (FrequencyNoise)noise.Clone();
        noiseCopy.Frequency[0] = 9;
        ((PerlinNoiseFunction)noiseCopy.noiseFunc).LUT[0] = 999;
        Close(((FrequencyNoise)noise).Frequency[0], 1, "noise base clone owns frequencies");
        Assert(((PerlinNoiseFunction)noise.noiseFunc).LUT[0] != 999, "noise clone owns function lookup table");

        var polyshape = new Polyshape(new RenderSphere());
        polyshape.Shapes[0].TransformTimeLine =
            new KeyFrameTrans(Transform.Translate(1, 0, 0));
        polyshape.AdvanceToTime(0);
        var originalTimeline = polyshape.Shapes[0].TransformTimeLine;
        var polyCopy = (Polyshape)((Item)polyshape).Clone();
        Assert(!ReferenceEquals(polyCopy.Shapes[0], polyshape.Shapes[0]), "polyshape clone owns components");
        Assert(ReferenceEquals(polyshape.Shapes[0].TransformTimeLine, originalTimeline),
            "cloning polyshape does not mutate original component timeline");
        Close(((RenderSphere)polyCopy.Shapes[0]).Center.Comp[0], 1,
            "polyshape clone preserves independent child transform");
    }

    private static void ProceduralMaterialClonesOwnTheirPointLists()
    {
        var point = new ProceduralPoint(new Point(1, 2, 3), new Vector(4, 5, 6));
        var points = new List<ProceduralPoint> { point };
        Material source = new ProceduralValue(new Noise(), points);
        var copy = (ProceduralValue)source.Clone();
        Assert(points.Count == 1 && copy.ProceduralPoints.Count == 1,
            "procedural material clone does not grow source point list");
        copy.ProceduralPoints[0].Comp[0] = 9;
        copy.ProceduralPoints[0].ProcNoiseParams.velocity.Comp[0] = 9;
        Close(point.Comp[0], 1, "procedural material clone owns point coordinates");
        Close(point.ProcNoiseParams.velocity.Comp[0], 4,
            "procedural material clone owns point velocity parameters");
        copy.ProceduralPoints.Clear();
        Assert(points.Count == 1, "procedural material clone owns point collection");

        Material mixing = new ProceduralMixingValue(new Noise(), points, new Noise(),
            perturbVel: new Vector(1, 2, 3));
        var mixedCopy = (ProceduralMixingValue)mixing.Clone();
        mixedCopy.perturbVel.Comp[0] = 9;
        mixedCopy.perturbNoise.shift.Comp[0] = 9;
        Assert(!ReferenceEquals(mixedCopy.perturbPoly, ((ProceduralMixingValue)mixing).perturbPoly),
            "mixing clone owns perturbation polynomial");
        Close(((ProceduralMixingValue)mixing).perturbVel.Comp[0], 1,
            "mixing clone owns perturbation velocity");
        Close(((ProceduralMixingValue)mixing).perturbNoise.shift.Comp[0], 1000.1,
            "mixing clone owns perturbation noise");
    }

    private static void RayGraphClonesPreserveLinks()
    {
        var parent = new RenderRay();
        var child = new RenderRay(Rank: 1) { ParentRay = parent };
        parent.IntersectData.CastedRays.Add(child);
        var tree = new RayTree(parent);
        tree.Add(child);
        var copy = tree.Clone();
        Assert(!ReferenceEquals(copy.Rays[0], parent) && !ReferenceEquals(copy.Rays[1], child),
            "ray graph clone copies nodes");
        Assert(ReferenceEquals(copy.Rays[1].ParentRay, copy.Rays[0]), "cloned child links to cloned parent");
        Assert(ReferenceEquals(copy.Rays[0].IntersectData.CastedRays[0], copy.Rays[1]),
            "cloned tree and casted ray lists share the same copied child");

        var projector = new RectangleProjector(1, 1);
        projector.AdvanceToTime(0);
        projector.Rays[0] = tree;
        var projectorCopy = (RectangleProjector)((Item)projector).Clone();
        projectorCopy.Pos0[0] = 9;
        projectorCopy.LocalRays[0].Origin.Comp[0] = 9;
        Assert(projectorCopy.Rays[0].Rays.Count == 2, "projector clone preserves traced history");
        Close(projector.Pos0[0], 0, "projector clone owns pixel axes");
        Close(projector.LocalRays[0].Origin.Comp[0], 0, "projector clone owns local rays");
    }

    private static void OptionalMeshAttributesAreOptional()
    {
        var mesh = new Mesh(new int[,] { { 0, 1, 2 } },
            new double[,] { { 0, 0, 1 }, { 1, 0, 1 }, { 0, 1, 1 } });
        var copy = (Mesh)((Shape)mesh).Clone();
        copy.LocalVertices[0].Position.Comp[0] = 9;
        Close(mesh.LocalVertices[0].Position.Comp[0], 0, "unadvanced mesh clone owns vertices");
        mesh.AdvanceToTime(0);
    }

    private static void FrequencySamplingRejectsNonProgressingInputs()
    {
        foreach (double bad in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity })
        {
            ThrowsArgument(delegate { new FrequencyNoise(minFreq: bad); }, "invalid minimum frequency");
            ThrowsArgument(delegate { new FrequencyNoise(maxFreq: bad); }, "invalid maximum frequency");
        }
        foreach (double bad in new[] { 0.0, 1.0, double.NaN, double.PositiveInfinity })
            ThrowsArgument(delegate { new FrequencyNoise(lacunarity: bad); }, "invalid lacunarity");
        var valid = new FrequencyNoise(minFreq: 1, maxFreq: 4, lacunarity: 2);
        Assert(valid.Frequency.Length == 3, "valid geometric frequency sampling");
        ThrowsArgument(delegate { valid.Lacunarity = 1; }, "invalid lacunarity setter");
    }

    private static void ThrowsArgument(Action action, string message)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception("Render regression: expected ArgumentException for " + message);
    }

    private static void VoxelPreservesCloserHit()
    {
        var voxels = new Voxel(Point.Zero(), new[] { 1, 1, 1 }, new[] { 2.0, 2.0, 2.0 });
        var ray = new RenderRay(Point.Zero(), new Vector(0, 0, 1));
        var closer = new IntersectData(true, 0.5);
        ray.IntersectData = closer;
        Assert(!voxels.Intersect(ref ray) && ReferenceEquals(ray.IntersectData, closer),
            "voxel exit must not overwrite closer shape hit");
    }

    private static void SphereRadiusRefreshesGeometry()
    {
        var sphere = new RenderSphere(new Point(0, 0, 5), 1);
        sphere.AdvanceToTime(0);
        sphere.Radius = 2;
        var ray = new RenderRay(Point.Zero(), new Vector(0, 0, 1));
        Assert(sphere.Intersect(ref ray), "sphere intersects after radius change");
        Close(ray.IntersectData.Travel, 3, "sphere intersection uses updated radius immediately");
    }

    private static void InverseRayTransformsAreHonored()
    {
        var ray = new RenderRay(new Point(1, 2, 3), new Vector(0, 0, 1));
        var copy = ray.CopyTransform(Transform.Translate(4, 5, 6), true);
        Close(copy.Origin.Comp[0], -3, "inverse ray translation X");
        Close(copy.Origin.Comp[1], -3, "inverse ray translation Y");
        Close(copy.Origin.Comp[2], -3, "inverse ray translation Z");
        Close(ray.Origin.Comp[0], 1, "copy transform leaves source ray unchanged");
    }
}
