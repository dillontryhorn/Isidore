using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Isidore.Maths;
using Isidore.Render;

internal static class GpuRenderRegression
{
    public static void Run()
    {
        GpuMode previous = GpuAcceleration.Mode;
        try
        {
            foreach (bool multicore in new[] { false, true })
            {
                Mesh mesh = LayerMesh(new[] { 4.0, 2.0 });
                Compare(mesh, () => PatternProjector(47), multicore, true, "nearest facets");
                Compare(mesh, LimitProjector, multicore, true, "travel limits and prior hits");
                Compare(mesh, BoundaryProjector, multicore, true, "edges, vertices and parallel rays");

                mesh.Alpha = new MapTexture(new double[,] { { 0 }, { 1 } });
                for (int v = 0; v < 3; v++) mesh.GlobalVertices[v].UV = new[] { 0.75, 0.5 };
                for (int v = 3; v < 6; v++) mesh.GlobalVertices[v].UV = new[] { 0.25, 0.5 };
                Compare(mesh, () => PatternProjector(19), multicore, true,
                    "transparent nearest facet retains farther surface");

                mesh.Alpha = null;
                mesh.IntersectBackFaces = false;
                for (int v = 0; v < 3; v++) mesh.GlobalVertices[v].Normal = new Normal(0, 0, -1);
                for (int v = 3; v < 6; v++) mesh.GlobalVertices[v].Normal = new Normal(0, 0, 1);
                Compare(mesh, () => PatternProjector(19), multicore, true,
                    "back-facing nearest facet retains farther surface");
            }

            foreach (Material material in new Material[] {
                new PropertyValue(new Scalar(12)), new TextureValue(new MapTexture(new double[,] { { 7 } })),
                new OPD(3), new Reflector(0.4), new Reflective(0.4),
                new Transparency(1.5) { CastReflectedRays = true } })
            {
                Mesh mesh = LayerMesh(new[] { 2.0 });
                mesh.Materials.Add(new MaterialStack { material });
                Compare(mesh, OpticalProjector, false, true, material.GetType().Name);
            }
            Mesh layered = LayerMesh(new[] { 2.0 });
            layered.Materials.Add(new MaterialStack {
                new PropertyValue(new Scalar(1)) { Alpha = new MapTexture(new double[,] { { 0 } }) },
                new OPD(5) { Alpha = new MapTexture(new double[,] { { 1 } }) },
                new PropertyValue(new Scalar(99)) });
            Compare(layered, () => PatternProjector(13), false, true, "material layer alpha and order");

            Mesh ties = LayerMesh(new[] { 2.0, 2.0 });
            for (int v = 0; v < 3; v++) ties.GlobalVertices[v].UV = new[] { 0.1, 0.5 };
            for (int v = 3; v < 6; v++) ties.GlobalVertices[v].UV = new[] { 0.9, 0.5 };
            Compare(ties, () => PatternProjector(11), false, true, "equal-distance facet traversal order");

            Mesh mutable = LayerMesh(new[] { 3.0, 2.0 });
            mutable.GlobalVertices[3].Position.Comp[0] += 0.125;
            Compare(mutable, () => PatternProjector(23), false, true,
                "edited public vertices use actual cached edges");
            mutable.GlobalVertices[3].Position.Comp[0] -= 0.25;
            Compare(mutable, () => PatternProjector(23), false, true, "subsequent geometry edit repacks");
            Mesh octree = LayerMesh(new[] { 2.4, 2.0, 3.0, 2.2 }, 1, 3);
            Compare(octree, () => PatternProjector(23), false, true, "subdivided octree candidates");
            Assert(MeshStat<long>(octree, "LastGpuCandidateTriangles") == 23 * 4,
                "overlapping octree leaves must deduplicate each ray's facet candidates");
            Mesh staleOctree = LayerMesh(new[] { 2.0, 10.0 }, 1, 3);
            staleOctree.GlobalVertices[3].Position.Comp[2] = 1.0;
            Compare(staleOctree, () => PatternProjector(23), false, true,
                "stale octree pruning retains the original CPU traversal result");
            Mesh retainedChildren = LayerMesh(new[] { 2.0, 4.0 }, 1, 3);
            MeshOctree tree = (MeshOctree)typeof(Mesh).GetField("octree",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(retainedChildren);
            var boxes = (List<MeshOctBox>)typeof(MeshOctree).GetField("meshoctboxes",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
            typeof(OctBox).GetField("childBoxes", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(boxes[0], null);
            boxes[0].FacetOverlap = new List<int> { 1 };
            Compare(retainedChildren, () => PatternProjector(11), false, true,
                "root leaf with retained octree boxes must not share an incomplete candidate pool");
            Mesh selective = SelectiveMesh(8, 1);
            Compare(selective, () => SelectiveProjector(8, 37), false, true, "spatially selective mesh CSR");
            Assert(MeshStat<long>(selective, "LastGpuCandidateTriangles") < 37L * selective.Facets.Count / 2,
                "selective octree broadphase must avoid scanning most mesh facets");
            Mesh sharedPool = LayerMesh(new[] { 2.0, 2.4, 2.8 });
            Compare(sharedPool, () => PatternProjector(29), false, true, "shared unsplit candidate pool");
            Assert(MeshStat<long>(sharedPool, "LastGpuCandidateTriangles") == 29 * 3 &&
                MeshStat<int>(sharedPool, "LastGpuFacetReferences") == 3,
                "unsplit ranges share facet storage while counting all per-ray triangle work");

            SharedRaySnapshotsRemainCurrent();
            CustomCallbacksRetainSequentialBehavior();
            SceneRetainsBodyOrder();
            PolicyAndUnsupportedDataFallBack();
        }
        finally { GpuAcceleration.Mode = previous; }
    }

    private static void Compare(Mesh mesh, Func<Projector> create, bool multicore,
        bool expectDispatch, string label)
    {
        GpuAcceleration.Mode = GpuMode.Disabled;
        Projector expected = create();
        Intersect(mesh, expected, multicore);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        Projector actual = create();
        long before = GpuAcceleration.DispatchCount;
        Intersect(mesh, actual, multicore);
        CompareProjectors(expected, actual, label);
        if (expectDispatch && GpuAcceleration.IsAvailable)
            Assert(GpuAcceleration.DispatchCount > before,
                label + " must dispatch on compatible hardware: " + GpuAcceleration.LastError);
        else
            Assert(GpuAcceleration.DispatchCount == before, label + " must use CPU fallback");
    }

    private static void Intersect(Body body, Projector projector, bool multicore)
    {
        if (multicore) body.MultiCoreIntersect(ref projector);
        else body.OneCoreIntersect(ref projector);
    }

    private static Mesh LayerMesh(double[] depth, int perBox = int.MaxValue, int maxDepth = 1)
    {
        var vertices = new Vertices();
        var facets = new List<int[]>();
        for (int layer = 0; layer < depth.Length; layer++)
        {
            int v = vertices.Count;
            vertices.Add(new Vertex(new Point(-2, -2, depth[layer]), new Normal(0, 0, -1), new[] { 0.0, 0.0 }));
            vertices.Add(new Vertex(new Point(2, -2, depth[layer]), new Normal(0, 0, -1), new[] { 1.0, 0.0 }));
            vertices.Add(new Vertex(new Point(0, 2, depth[layer]), new Normal(0, 0, -1), new[] { 0.5, 1.0 }));
            facets.Add(new[] { v, v + 1, v + 2 });
        }
        Mesh mesh = new Mesh(facets, vertices, perBox, maxDepth);
        mesh.AdvanceToTime();
        return mesh;
    }

    private static Projector Project(params RenderRay[] rays)
    {
        Projector projector = new Projector();
        projector.Rays = new RayTree[rays.Length];
        for (int i = 0; i < rays.Length; i++) projector.Rays[i] = new RayTree(rays[i]);
        return projector;
    }

    private static Projector PatternProjector(int count)
    {
        RenderRay[] rays = new RenderRay[count];
        for (int i = 0; i < count; i++)
            rays[i] = new RenderRay(new Point((i % 13 - 6) * 0.11, (i % 7 - 3) * 0.13, 0),
                new Vector(0.003 * (i % 3 - 1), 0.002 * (i % 5 - 2), 1));
        return Project(rays);
    }

    private static Mesh SelectiveMesh(int side, int layers)
    {
        Vertices vertices = new Vertices();
        List<int[]> facets = new List<int[]>();
        for (int x = 0; x < side; x++)
            for (int y = 0; y < side; y++)
                for (int layer = layers - 1; layer >= 0; layer--)
                {
                    double z = 2 + (x + y) * 0.002 + layer * 0.0001;
                    int v = vertices.Count;
                    vertices.Add(new Vertex(new Point(x, y, z), new Normal(0, 0, -1), new[] { 0.0, 0.0 }));
                    vertices.Add(new Vertex(new Point(x + 1, y, z), new Normal(0, 0, -1), new[] { 1.0, 0.0 }));
                    vertices.Add(new Vertex(new Point(x + 1, y + 1, z), new Normal(0, 0, -1), new[] { 1.0, 1.0 }));
                    vertices.Add(new Vertex(new Point(x, y + 1, z), new Normal(0, 0, -1), new[] { 0.0, 1.0 }));
                    facets.Add(new[] { v, v + 1, v + 2 });
                    facets.Add(new[] { v, v + 2, v + 3 });
                }
        Mesh mesh = new Mesh(facets, vertices, 12, 5);
        mesh.AdvanceToTime();
        return mesh;
    }

    private static Projector SelectiveProjector(int side, int count)
    {
        RenderRay[] rays = new RenderRay[count];
        for (int i = 0; i < count; i++)
            rays[i] = new RenderRay(new Point(i % side + 0.23, i / side % side + 0.41, 0),
                new Vector(0, 0, 1));
        return Project(rays);
    }

    private static Projector LimitProjector()
    {
        Projector projector = PatternProjector(9);
        projector.Rays[0].Rays[0].MaximumTravel = 1.9;
        projector.Rays[1].Rays[0].MaximumTravel = 2.0;
        projector.Rays[2].Rays[0].MinimumTravel = 2.1;
        projector.Rays[3].Rays[0].MinimumTravel = 2.0;
        projector.Rays[4].Rays[0].IntersectData.Travel = 1.5;
        projector.Rays[5].Rays[0].IntersectData.Travel = 2.0;
        projector.Rays[6].Rays[0].Origin = new Point(1.9, 1.9, 0); // Inside root, outside triangle.
        projector.Rays[7].Rays[0].Origin = new Point(5, 0, 0); // Outside root.
        projector.Rays[8].Rays[0].Status = RayStatus.Closed;
        return projector;
    }

    private static Projector BoundaryProjector()
    {
        return Project(new RenderRay(new Point(0, -2, 0), new Vector(0, 0, 1)),
            new RenderRay(new Point(-2, -2, 0), new Vector(0, 0, 1)),
            new RenderRay(new Point(0, 0, 0), new Vector(1, 0, 0)),
            new RenderRay(new Point(0, 0, 0), new Vector(0, 0, 1e-9 / 16)),
            new RenderRay(new Point(0.1, 0.1, 0), new Vector(0, 0, 1)));
    }

    private static Projector OpticalProjector()
    {
        Projector projector = PatternProjector(7);
        foreach (RayTree tree in projector.Rays)
        {
            tree.Rays[0].Properties.Add(new Wavelength(new[] { 1e-6, 2e-6 }));
            tree.Rays[0].Properties.Add(new RefractiveIndex(1.0));
            tree.Rays[0].Properties.Add(new Irradiance(8));
            tree.Rays[0].Properties.Add(new SpectralIrradiance(new[] { 1e-6, 2e-6 }, new[] { 3.0, 6.0 }));
        }
        return projector;
    }

    private static void CompareProjectors(Projector expected, Projector actual, string label)
    {
        Assert(expected.Rays.Length == actual.Rays.Length, label + " ray count");
        for (int i = 0; i < expected.Rays.Length; i++)
        {
            Assert(expected.Rays[i].Rays.Count == actual.Rays[i].Rays.Count, label + " tree size");
            for (int r = 0; r < expected.Rays[i].Rays.Count; r++)
                CompareRay(expected.Rays[i].Rays[r], actual.Rays[i].Rays[r], label + " ray " + i);
        }
    }

    private static void CompareRay(RenderRay expected, RenderRay actual, string label)
    {
        Assert(expected.Status == actual.Status && expected.Rank == actual.Rank, label + " status/rank");
        Close(expected.Time, actual.Time, label + " time");
        CompareArray(expected.Origin.Comp, actual.Origin.Comp, label + " origin");
        CompareArray(expected.Dir.Comp, actual.Dir.Comp, label + " direction");
        IntersectData first = expected.IntersectData, second = actual.IntersectData;
        Assert(first.Hit == second.Hit && ReferenceEquals(first.Body, second.Body), label + " hit/body");
        Close(first.Travel, second.Travel, label + " travel");
        if (first.Hit)
        {
            CompareArray(first.IntersectPt.Comp, second.IntersectPt.Comp, label + " intersect point");
            var a = (ShapeSpecificData)first.BodySpecificData;
            var b = (ShapeSpecificData)second.BodySpecificData;
            CompareArray(a.SurfaceNormal.Comp, b.SurfaceNormal.Comp, label + " normal");
            Close(a.U, b.U, label + " U"); Close(a.V, b.V, label + " V");
            Close(a.CosIncAng, b.CosIncAng, label + " incidence");
        }
        CompareProperties(expected.Properties, actual.Properties, label + " properties");
        CompareProperties(first.Properties, second.Properties, label + " material properties");
        Assert(first.CastedRays.Count == second.CastedRays.Count, label + " casted ray count");
        for (int i = 0; i < first.CastedRays.Count; i++)
        {
            Assert(ReferenceEquals(second.CastedRays[i].ParentRay, actual), label + " child parent link");
            CompareRay(first.CastedRays[i], second.CastedRays[i], label + " child " + i);
        }
    }

    private static void CompareProperties(Properties first, Properties second, string label)
    {
        Assert(first.Count == second.Count, label + " count");
        for (int i = 0; i < first.Count; i++)
        {
            Assert(first[i].GetType() == second[i].GetType(), label + " type");
            if (first[i] is Scalar) Close(((Scalar)first[i]).Value, ((Scalar)second[i]).Value, label);
            else if (first[i] is Length) Close(((Length)first[i]).Value, ((Length)second[i]).Value, label);
            else if (first[i] is Wavelength) CompareArray(((Wavelength)first[i]).Value, ((Wavelength)second[i]).Value, label);
            else if (first[i] is Reflectance) CompareArray(((Reflectance)first[i]).Coefficient, ((Reflectance)second[i]).Coefficient, label);
            else if (first[i] is RefractiveIndex) CompareArray(((RefractiveIndex)first[i]).Coefficient, ((RefractiveIndex)second[i]).Coefficient, label);
            else if (first[i] is SpectralIrradiance) CompareArray(((SpectralIrradiance)first[i]).Irradiance, ((SpectralIrradiance)second[i]).Irradiance, label);
            else if (first[i] is Irradiance) Close(((Irradiance)first[i]).Value, ((Irradiance)second[i]).Value, label);
        }
    }

    private static void SharedRaySnapshotsRemainCurrent()
    {
        Mesh mesh = LayerMesh(new[] { 2.0 });
        mesh.Materials.Add(new MaterialStack { new PropertyValue(new Scalar(4)) });
        Compare(mesh, () => {
            RenderRay shared = new RenderRay(new Point(0.1, 0.1, 0), new Vector(0, 0, 1));
            return Project(shared, shared);
        }, false, true, "shared ray data must not apply material twice");
    }

    private static void CustomCallbacksRetainSequentialBehavior()
    {
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        foreach (int kind in new[] { 0, 1, 2, 3 })
        {
            Mesh mesh = LayerMesh(new[] { 2.0 });
            Projector projector = PatternProjector(2);
            int calls = 0;
            Action mutate = () => { calls++; projector.Rays[1].Rays[0].Origin.Comp[0] = 10; };
            if (kind == 0) mesh.Materials.Add(new MaterialStack { new MutatingMaterial(mutate) });
            if (kind == 1) mesh.Alpha = new MutatingMap(mutate);
            if (kind == 2)
            {
                mesh.Materials.Add(new MaterialStack { new Reflective() });
                projector.Rays[0].Rays[0].Properties.Add(new MutatingProperty(mutate));
            }
            if (kind == 3) mesh.Materials.Add(new MaterialStack { new TextureValue(new MutatingMap(mutate)) });
            long before = GpuAcceleration.DispatchCount;
            mesh.OneCoreIntersect(ref projector);
            Assert(GpuAcceleration.DispatchCount == before, "custom callbacks must retain CPU traversal");
            Assert(calls == 1 && !projector.Rays[1].Rays[0].IntersectData.Hit,
                "custom material/alpha/clone must mutate the later ray before its intersection");
        }
        Mesh source = LayerMesh(new[] { 2.0 });
        var derived = new CountingMesh(source.Facets, source.LocalVertices);
        derived.AdvanceToTime();
        Projector derivedProjector = PatternProjector(3);
        long old = GpuAcceleration.DispatchCount;
        derived.OneCoreIntersect(ref derivedProjector);
        Assert(derived.Calls == 3 && GpuAcceleration.DispatchCount == old,
            "Mesh subclasses must retain overridden Intersect dispatch");
    }

    private static void SceneRetainsBodyOrder()
    {
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        Projector projector = PatternProjector(3);
        Mesh first = LayerMesh(new[] { 4.0 });
        first.Materials.Add(new MaterialStack { new MutatingMaterial(() => {
            projector.Rays[0].Rays[0].Dir = new Vector(0, 0, -1);
        }) });
        Mesh second = LayerMesh(new[] { 2.0 });
        Scene scene = new Scene { UseMultiCores = false };
        scene.Bodies.Add(first); scene.Bodies.Add(second);
        long before = GpuAcceleration.DispatchCount;
        scene.Render(ref projector);
        Assert(ReferenceEquals(projector.Rays[0].Rays[0].IntersectData.Body, first),
            "later GPU body must use the ray modified by the earlier body callback");
        Assert(ReferenceEquals(projector.Rays[1].Rays[0].IntersectData.Body, second), "scene closest hit");
        if (GpuAcceleration.IsAvailable)
            Assert(GpuAcceleration.DispatchCount > before, "Scene body entry point must dispatch GPU mesh work");
    }

    private static void PolicyAndUnsupportedDataFallBack()
    {
        Mesh mesh = LayerMesh(new[] { 2.0 });
        GpuAcceleration.Mode = GpuMode.Automatic;
        Projector tiny = PatternProjector(3);
        long before = GpuAcceleration.DispatchCount;
        mesh.OneCoreIntersect(ref tiny);
        Assert(GpuAcceleration.DispatchCount == before, "small automatic workloads must retain CPU traversal");
        if (GpuAcceleration.IsAvailable)
        {
            Mesh selective = SelectiveMesh(16, 4);
            Projector projection = SelectiveProjector(16, 1024);
            before = GpuAcceleration.DispatchCount;
            selective.OneCoreIntersect(ref projection);
            long actual = MeshStat<long>(selective, "LastGpuCandidateTriangles");
            Assert((long)projection.Rays.Length * selective.Facets.Count >= 1000000 &&
                actual == 0 && GpuAcceleration.DispatchCount == before,
                "highly selective octrees must skip automatic packing before repeating CPU broadphase");

            // A dense off-camera patch passes the conservative density guard.
            // The actual queried leaf candidates must still control selection.
            int[] remote = selective.Facets[selective.Facets.Count - 1];
            for (int i = 0; i < 256; i++) selective.Facets.Add((int[])remote.Clone());
            selective.AdvanceToTime(0, true);
            RenderRay[] nearRays = new RenderRay[1024];
            for (int i = 0; i < nearRays.Length; i++)
                nearRays[i] = new RenderRay(new Point(i % 8 + 0.23, i / 8 % 8 + 0.41, 0), new Vector(0, 0, 1));
            projection = Project(nearRays);
            before = GpuAcceleration.DispatchCount;
            selective.OneCoreIntersect(ref projection);
            actual = MeshStat<long>(selective, "LastGpuCandidateTriangles");
            Assert(actual > 0 && actual < 1000000 && GpuAcceleration.DispatchCount == before,
                "automatic eligibility must use queried candidates despite a dense unvisited leaf");
        }
        Compare(mesh, () => Project(new RenderRay(new Point(0.1, 0.1, 0),
            new Vector(0, 0, 1e-100))), false, false, "extreme input fallback");
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        var ray = new RenderRay(new Point(0.1, 0.1, 0), new Vector(0, 0, 1));
        before = GpuAcceleration.DispatchCount;
        mesh.Intersect(ref ray);
        Assert(ray.IntersectData.Hit && GpuAcceleration.DispatchCount == before,
            "individual ray intersections retain the existing CPU implementation");
    }

    public static string Benchmark()
    {
        GpuMode previous = GpuAcceleration.Mode;
        try
        {
            if (!GpuAcceleration.IsAvailable) return "Mesh GPU benchmarks skipped: " + GpuAcceleration.LastError;
            StringBuilder rows = new StringBuilder("operation,shape,rays,facets,candidate_triangles,facet_references,cpu_one_ms,cpu_multi_ms,gpu_ms,speedup_one,speedup_multi,gpu_dispatches,verified_rays,auto_ms,auto_dispatches\n");
            foreach (int facets in new[] { 256, 1024, 4096 })
            {
                double[] depths = new double[facets];
                for (int f = 0; f < facets; f++) depths[f] = 2 + (facets - f) / 8192.0;
                Mesh mesh = LayerMesh(depths);
                int rays = facets == 4096 ? 256 : 1024;
                BenchmarkCase(rows, "dense", mesh, () => PatternProjector(rays));
            }
            foreach (int side in new[] { 8, 16 })
                BenchmarkCase(rows, "selective", SelectiveMesh(side, 8),
                    () => SelectiveProjector(side, 1024));
            return rows.ToString();
        }
        finally { GpuAcceleration.Mode = previous; }
    }

    private static void BenchmarkCase(StringBuilder rows, string shape, Mesh mesh, Func<Projector> create)
    {
        mesh.Materials.Add(new MaterialStack { new PropertyValue(new Scalar(1)) });
        Projector projector = create();
        Projector expected = create();
        Action oneCore = () => {
            foreach (RayTree tree in projector.Rays) tree.Reset();
            mesh.OneCoreIntersect(ref projector);
        };
        Action multiCore = () => {
            foreach (RayTree tree in projector.Rays) tree.Reset();
            mesh.MultiCoreIntersect(ref projector);
        };
        GpuAcceleration.Mode = GpuMode.Disabled;
        mesh.OneCoreIntersect(ref expected);
        oneCore();
        double cpuOne = Median(oneCore);
        CompareProjectors(expected, projector, shape + " benchmark one core");
        multiCore();
        double cpuMulti = Median(multiCore);
        CompareProjectors(expected, projector, shape + " benchmark multiple cores");
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        oneCore(); // Compile and allocate outside the warm timings.
        long before = GpuAcceleration.DispatchCount;
        double gpu = Median(oneCore);
        long dispatches = GpuAcceleration.DispatchCount - before;
        int verified = MeshStat<int>(mesh, "LastGpuVerifiedRays");
        long work = MeshStat<long>(mesh, "LastGpuCandidateTriangles");
        int references = MeshStat<int>(mesh, "LastGpuFacetReferences");
        CompareProjectors(expected, projector, shape + " benchmark GPU");
        GpuAcceleration.Mode = GpuMode.Automatic;
        before = GpuAcceleration.DispatchCount;
        double automatic = Median(oneCore);
        long automaticDispatches = GpuAcceleration.DispatchCount - before;
        CompareProjectors(expected, projector, shape + " benchmark automatic");
        string row;
        if (dispatches == 0 || verified != projector.Rays.Length)
            row = string.Format(CultureInfo.InvariantCulture,
                "Mesh,{0},CPU fallback; no GPU speedup reported; dispatched={1}; verified={2}: {3}\n",
                shape, dispatches, verified, GpuAcceleration.LastError);
        else
            row = string.Format(CultureInfo.InvariantCulture,
                "Mesh,{0},{1},{2},{3},{4},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3},{10},{11},{12:F3},{13}\n",
                shape, projector.Rays.Length, mesh.Facets.Count, work, references,
                cpuOne, cpuMulti, gpu, cpuOne / gpu, cpuMulti / gpu, dispatches, verified,
                automatic, automaticDispatches);
        rows.Append(row);
        Console.Write(row); // Publish each bounded case as it completes.
    }

    private static T MeshStat<T>(Mesh mesh, string name)
    {
        return (T)typeof(Mesh).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(mesh, null);
    }

    private static double Median(Action action)
    {
        double[] times = new double[3];
        for (int i = 0; i < times.Length; i++)
        {
            Stopwatch watch = Stopwatch.StartNew(); action(); watch.Stop(); times[i] = watch.Elapsed.TotalMilliseconds;
        }
        Array.Sort(times); return times[1];
    }

    private static void CompareArray(double[] first, double[] second, string label)
    {
        Assert(first.Length == second.Length, label + " length");
        for (int i = 0; i < first.Length; i++) Close(first[i], second[i], label);
    }

    private static void Close(double first, double second, string label)
    {
        Assert(first.Equals(second) || (!double.IsNaN(first) && !double.IsInfinity(first) &&
            !double.IsNaN(second) && !double.IsInfinity(second) &&
            Math.Abs(first - second) <= 1e-12 * Math.Max(1, Math.Abs(first))), label);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("GPU Render regression: " + message);
    }

    private sealed class MutatingMaterial : Material
    {
        private readonly Action action;
        internal MutatingMaterial(Action action) { this.action = action; }
        public override bool ProcessIntersectData(ref RenderRay ray) { action(); return true; }
    }

    private sealed class MutatingMap : MapTexture
    {
        private readonly Action action;
        internal MutatingMap(Action action) : base(new double[,] { { 1 } }) { this.action = action; }
        public override double GetVal(double u, double v) { action(); return 1; }
    }

    private sealed class MutatingProperty : Property
    {
        private readonly Action action;
        internal MutatingProperty(Action action) { this.action = action; }
        protected override Property CloneImp() { action(); return base.CloneImp(); }
    }

    private sealed class CountingMesh : Mesh
    {
        internal int Calls;
        internal CountingMesh(List<int[]> facets, Vertices vertices) : base(facets, vertices) { }
        public override bool Intersect(ref RenderRay ray) { Calls++; return base.Intersect(ref ray); }
    }
}
