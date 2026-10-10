using System;
using System.Collections.Generic;
using Isidore.Maths;
using Isidore.Render;
using RenderPlane = Isidore.Render.Plane;
using RenderSphere = Isidore.Render.Sphere;

public static class AdditionalRenderRegression
{
    public static void Run()
    {
        ReflectionScalesTheCorrectSpectrum();
        OpticalMaterialsStopTheLayerStack();
        TransparencySplitsPropertiesWithoutIndexShifts();
        SphereUVCoordinatesAreFiniteAtPoles();
        BillboardInitializesAndRejectsParallelRays();
        ShapeLengthsRefreshCachedGeometry();
        MaximumTravelLimitsAllBodyIntersections();
        MeshNormalsRemainUnitVectors();
        ScaledSurfaceNormalsAreNormalized();
        MeshOctreeKeepsSearchingForCloserFacets();
        InverseLocalMeshTransformsAreHonored();
        TranslatedBoxesIntersectWorldPlanes();
        SpectrumCopiesOwnTheirArrays();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Additional Render regression: " + message);
    }

    private static void Close(double actual, double expected, string message)
    {
        Assert(!double.IsNaN(actual) && Math.Abs(actual - expected) < 1e-10, message);
    }

    private static RenderRay SurfaceRay(Properties properties = null)
    {
        return new RenderRay(Point.Zero(), new Vector(0, 0, 1), Properties: properties,
            IntersectData: new IntersectData(true, 2, new Point(0, 0, 2), null,
                new ShapeSpecificData(new Normal(0, 0, -1), 1, 0.5, 0.5)));
    }

    private static void ReflectionScalesTheCorrectSpectrum()
    {
        foreach (bool scalarFirst in new[] { false, true })
        {
            var spectral = new SpectralIrradiance(new[] { 1e-6, 1.5e-6, 2e-6 },
                new[] { 4.0, 8.0, 12.0 });
            var properties = new Properties();
            if (scalarFirst)
                properties.Add(new Irradiance(8));
            properties.Add(spectral);
            var ray = SurfaceRay(properties);
            var material = new Reflective(new Reflectance(new[] { 1e-6, 2e-6 },
                new[] { 0.25, 0.5 }));
            Assert(material.ProcessIntersectData(ref ray), "reflection reports material interaction");
            Assert(ray.IntersectData.CastedRays.Count == 1, "reflection casts one ray");
            var reflected = ray.IntersectData.CastedRays[0];
            var reflectedSpectrum = Find<SpectralIrradiance>(reflected.Properties);
            Close(reflectedSpectrum.Irradiance[0], 1, "reflected spectral first sample");
            Close(reflectedSpectrum.Irradiance[1], 3, "reflected interpolated spectral sample");
            Close(reflectedSpectrum.Irradiance[2], 6, "reflected spectral final sample");
            Close(spectral.Irradiance[1], 8, "reflection preserves incoming spectrum");
            Close(reflected.Dir.Comp[2], -1, "reflected direction");
            Assert(ReferenceEquals(reflected.ParentRay, ray), "reflected ray retains parent link");
            if (scalarFirst)
                Close(Find<Irradiance>(reflected.Properties).Value, 3,
                    "scalar irradiance uses mean reflectance independently");
        }
    }

    private static T Find<T>(Properties properties) where T : Property
    {
        foreach (var property in properties)
            if (property.GetType() == typeof(T))
                return (T)property;
        throw new Exception("Additional Render regression: missing " + typeof(T).Name);
    }

    private static void OpticalMaterialsStopTheLayerStack()
    {
        foreach (Material optical in new Material[] { new Reflective(), new Transparency() })
        {
            var ray = SurfaceRay();
            var stack = new MaterialStack { optical, new PropertyValue(new Scalar(99)) };
            stack.Apply(ref ray);
            Assert(ray.IntersectData.CastedRays.Count > 0, "optical material casts child rays");
            Assert(ray.IntersectData.Properties.Count == 0,
                "successful optical layer prevents fallback material application");
        }
    }

    private static void TransparencySplitsPropertiesWithoutIndexShifts()
    {
        foreach (bool indexFirst in new[] { false, true })
        {
            var wavelengths = new Wavelength(new[] { 1e-6, 2e-6 });
            var refractive = new RefractiveIndex(new[] { 1e-6, 2e-6 }, new[] { 1.1, 1.2 });
            var properties = new Properties();
            if (indexFirst)
            {
                properties.Add(refractive);
                properties.Add(wavelengths);
            }
            else
            {
                properties.Add(wavelengths);
                properties.Add(refractive);
            }
            properties.Add(new Scalar(7));
            var ray = SurfaceRay(properties);
            var material = new Transparency(new RefractiveIndex(new[] { 1e-6, 2e-6 },
                new[] { 1.5, 1.6 })) { CastReflectedRays = true };
            Assert(material.ProcessIntersectData(ref ray), "transparency reports material interaction");
            Assert(ray.IntersectData.CastedRays.Count == 4, "two wavelengths cast four optical rays");
            for (int idx = 0; idx < 2; idx++)
            {
                var transmitted = ray.IntersectData.CastedRays[2 * idx];
                var reflected = ray.IntersectData.CastedRays[2 * idx + 1];
                var transmitIndex = Find<RefractiveIndex>(transmitted.Properties);
                var reflectIndex = Find<RefractiveIndex>(reflected.Properties);
                Assert(transmitIndex.Coefficient.Length == 1 && reflectIndex.Coefficient.Length == 1,
                    "each optical child has a discrete refractive index");
                Close(transmitIndex.Coefficient[0], 1.5 + 0.1 * idx,
                    "transmitted child refractive index");
                Close(reflectIndex.Coefficient[0], 1.1 + 0.1 * idx,
                    "reflected child incoming refractive index");
                foreach (var child in new[] { transmitted, reflected })
                {
                    Assert(Find<Wavelength>(child.Properties).Value.Length == 1,
                        "each optical child has one wavelength");
                    Close(Find<Wavelength>(child.Properties).Value[0], wavelengths.Value[idx],
                        "optical child wavelength");
                    Close(Find<Scalar>(child.Properties).Value, 7,
                        "unrelated property survives optical splitting");
                }
            }
            Assert(properties.Count == 3 && refractive.Coefficient.Length == 2,
                "transparency preserves incoming properties");
        }

        var sampled = SurfaceRay(new Properties {
            new RefractiveIndex(1.25), new Wavelength(new[] { 1e-6, 2e-6 })
        });
        var dispersive = new Transparency(new RefractiveIndex(new[] { 1e-6, 2e-6 },
            new[] { 1.5, 1.6 }));
        Assert(dispersive.ProcessIntersectData(ref sampled), "constant incoming index is interpolated");
        Assert(sampled.IntersectData.CastedRays.Count == 2,
            "explicit ray wavelengths determine spectral splitting with constant incoming index");
        Close(Find<RefractiveIndex>(sampled.IntersectData.CastedRays[1].Properties).Coefficient[0], 1.6,
            "material index samples the ray wavelength rather than constant index placeholder");
    }

    private static void SphereUVCoordinatesAreFiniteAtPoles()
    {
        var sphere = new RenderSphere();
        sphere.TransformTimeLine = new KeyFrameTrans(Transform.Scale(2, 3, 4));
        sphere.AdvanceToTime(0);
        foreach (double sign in new[] { -1.0, 1.0 })
        {
            var ray = new RenderRay(new Point(0, 2 * sign, 0), new Vector(0, -sign, 0));
            Assert(sphere.Intersect(ref ray), "sphere pole intersection");
            var data = (ShapeSpecificData)ray.IntersectData.BodySpecificData;
            Assert(!double.IsNaN(data.U) && !double.IsInfinity(data.U) && data.U >= 0 && data.U <= 1,
                "sphere pole longitude is finite and bounded");
            Close(data.V, sign < 0 ? 0 : 1, "sphere pole latitude survives axis scaling");
        }
    }

    private static void BillboardInitializesAndRejectsParallelRays()
    {
        var billboard = new Billboard();
        var ray = new RenderRay(new Point(0, 0, -1), new Vector(0, 0, 1));
        Assert(billboard.Intersect(ref ray), "standalone billboard initializes from ray time");
        Close(ray.IntersectData.Travel, 1, "standalone billboard travel");
        var parallel = new RenderRay(Point.Zero(), new Vector(1, 0, 0));
        Assert(!billboard.Intersect(ref parallel) && !parallel.IntersectData.Hit,
            "coplanar billboard ray does not produce NaN hit");

        billboard.TransformTimeLine = new KeyFrameTrans(
            new[] { Transform.Translate(0, 0, 0), Transform.Translate(0, 0, 2) },
            new[] { 0.0, 1.0 });
        billboard.AdvanceToTime(0);
        var later = new RenderRay(new Point(0, 0, -1), new Vector(0, 0, 1), Time: 1);
        Assert(billboard.Intersect(ref later), "billboard updates geometry at later ray time");
        Close(later.IntersectData.Travel, 3, "billboard later-time travel");
    }

    private static void ShapeLengthsRefreshCachedGeometry()
    {
        var plane = new RenderPlane();
        plane.AdvanceToTime(0);
        plane.Ulength = 2;
        plane.Vlength = 2;
        var ray = new RenderRay(new Point(0.25, 0.25, -1), new Vector(0, 0, 1));
        Assert(plane.Intersect(ref ray), "plane intersects after texture length changes");
        var data = (ShapeSpecificData)ray.IntersectData.BodySpecificData;
        Close(data.U, 0.125, "plane U length updates cached UV mapping");
        Close(data.V, 0.125, "plane V length updates cached UV mapping");

        var billboard = new Billboard();
        billboard.AdvanceToTime(0);
        billboard.Width = 2;
        billboard.Height = 2;
        var enlarged = new RenderRay(new Point(0.75, 0.75, -1), new Vector(0, 0, 1));
        Assert(billboard.Intersect(ref enlarged), "billboard dimensions update cached geometry immediately");
    }

    private static Mesh TriangleMesh(double z)
    {
        return new Mesh(new int[,] { { 0, 1, 2 } },
            new double[,] { { -1, -1, z }, { 1, -1, z }, { 0, 1, z } });
    }

    private static void MaximumTravelLimitsAllBodyIntersections()
    {
        Body[] bodies = {
            new RenderSphere(new Point(0, 0, 5), 1),
            new RenderPlane(new Point(0, 0, 3)),
            new Billboard(new Point(-0.5, -0.5, 3)),
            TriangleMesh(3),
            new Voxel(new Point(0, 0, 3), new[] { 1, 1, 1 }, new[] { 2.0, 2.0, 2.0 })
        };
        double[] distances = { 4, 3, 3, 3, 2 };
        for (int idx = 0; idx < bodies.Length; idx++)
        {
            bodies[idx].AdvanceToTime(0);
            var limited = new RenderRay(Point.Zero(), new Vector(0, 0, 1)) { MaximumTravel = 1 };
            Assert(!bodies[idx].Intersect(ref limited) && !limited.IntersectData.Hit,
                bodies[idx].GetType().Name + " honors maximum ray travel");
            var endpoint = new RenderRay(Point.Zero(), new Vector(0, 0, 1)) {
                MaximumTravel = distances[idx]
            };
            Assert(bodies[idx].Intersect(ref endpoint),
                bodies[idx].GetType().Name + " accepts intersection at maximum travel");
            Close(endpoint.IntersectData.Travel, distances[idx], "maximum-travel endpoint distance");
        }
    }

    private static void MeshNormalsRemainUnitVectors()
    {
        var mesh = TriangleMesh(2);
        mesh.LocalVertices[0].Normal = new Normal(0, 0, -1);
        mesh.LocalVertices[1].Normal = new Normal(1, 0, 0);
        mesh.LocalVertices[2].Normal = new Normal(0, 0, -1);
        mesh.AdvanceToTime(0);
        var ray = new RenderRay(Point.Zero(), new Vector(0, 0, 1));
        Assert(mesh.Intersect(ref ray), "smooth-normal mesh intersects");
        var data = (ShapeSpecificData)ray.IntersectData.BodySpecificData;
        Close(data.SurfaceNormal.Mag(), 1, "interpolated mesh surface normal is normalized");
        new Reflective().ProcessIntersectData(ref ray);
        Close(ray.IntersectData.CastedRays[0].Dir.Mag(), 1, "mesh reflection preserves direction magnitude");

        mesh.LocalVertices[0].Normal = new Normal(1, 0, 0);
        mesh.LocalVertices[1].Normal = new Normal(-1, 0, 0);
        mesh.AdvanceToTime(0, true);
        var edge = new RenderRay(new Point(0, -1, 0), new Vector(0, 0, 1));
        Assert(mesh.Intersect(ref edge), "mesh edge with cancelling vertex normals intersects");
        Close(((ShapeSpecificData)edge.IntersectData.BodySpecificData).SurfaceNormal.Mag(), 1,
            "cancelling normals fall back to geometric facet normal");
    }

    private static void ScaledSurfaceNormalsAreNormalized()
    {
        foreach (Shape shape in new Shape[] { new RenderPlane(), new Billboard() })
        {
            shape.TransformTimeLine = new KeyFrameTrans(Transform.Scale(2, 3, 4));
            shape.AdvanceToTime(0);
            var ray = new RenderRay(new Point(0, 0, -1), new Vector(0, 0, 1));
            Assert(shape.Intersect(ref ray), "scaled " + shape.GetType().Name + " intersects");
            var data = (ShapeSpecificData)ray.IntersectData.BodySpecificData;
            Close(data.SurfaceNormal.Mag(), 1, "scaled shape has unit surface normal");
            Close(data.CosIncAng, 1, "scaled shape normal-incidence cosine");
            new Reflective().ProcessIntersectData(ref ray);
            var reflected = ray.IntersectData.CastedRays[0];
            Close(reflected.Dir.Comp[2], -1, "scaled shape reflects normal-incidence ray backward");
            Close(reflected.Dir.Mag(), 1, "scaled shape reflection preserves ray direction magnitude");
        }
    }

    private static void MeshOctreeKeepsSearchingForCloserFacets()
    {
        var mesh = new Mesh(new int[,] { { 0, 1, 2 }, { 3, 4, 5 } }, new double[,] {
            { -1, -1, 1 }, { 1, -1, 10 }, { 0, 1, 10 },
            { -0.25, -0.25, 6 }, { 0.25, -0.25, 6 }, { 0, 0.25, 6 }
        }, maxFacetPerOctBox: 1, maxOctreeDepth: 2);
        mesh.AdvanceToTime(0);
        var ray = new RenderRay(Point.Zero(), new Vector(0, 0, 1));
        Assert(mesh.Intersect(ref ray), "subdivided mesh intersects");
        Close(ray.IntersectData.Travel, 6,
            "octree overlapping farther facet cannot hide closer facet in later cell");
    }

    private static void InverseLocalMeshTransformsAreHonored()
    {
        var mesh = TriangleMesh(2);
        mesh.LocalTransform(Transform.Translate(4, 5, 6), true);
        Close(mesh.LocalVertices[0].Position.Comp[0], -5, "inverse local mesh transform X");
        Close(mesh.LocalVertices[0].Position.Comp[1], -6, "inverse local mesh transform Y");
        Close(mesh.LocalVertices[0].Position.Comp[2], -4, "inverse local mesh transform Z");
    }

    private static void TranslatedBoxesIntersectWorldPlanes()
    {
        var box = new AABB(new Point(0, 0, 10), new[] { 2.0, 2.0, 2.0 });
        var through = new RenderPlane(new Point(0, 0, 10), new Normal(0, 0, 1));
        var outside = new RenderPlane(Point.Zero(), new Normal(0, 0, 1));
        through.AdvanceToTime(0);
        outside.AdvanceToTime(0);
        Assert(box.PlaneOverlap(through), "world plane through translated box overlaps");
        Assert(!box.PlaneOverlap(outside), "world plane away from translated box misses");
        Assert(box.PlaneOverlap(new Vector(0, 0, 1), -10), "plane coefficient overload uses world coordinates");
        Assert(box.TriangleOverlap(new Point(-0.5, -0.5, 10), new Point(0.5, -0.5, 10),
            new Point(0, 0.5, 10)), "translated triangle overlap still uses local plane calculation");
    }

    private static void SpectrumCopiesOwnTheirArrays()
    {
        var spectrum = new Spectrum<double, double>(new[] { 1.0, 2.0 }, new[] { 3.0, 4.0 });
        var list = new Spectrums { spectrum };
        var copiedList = list.Clone();
        ((Spectrum<double, double>)copiedList[0]).Value[0] = 9;
        Close(spectrum.Value[0], 3, "spectrum collection clone owns spectrum entries");
        var empty = new Spectrum<double, double>();
        var emptyCopy = (Spectrum<double, double>)empty.Clone();
        Assert(emptyCopy.Sample == null && emptyCopy.Value == null, "empty spectrum can be cloned");
        Assert(new Spectrum<double, double>(empty).Sample == null, "empty spectrum copy constructor works");
        bool rejected = false;
        try { new Spectrum<double, double>(new[] { 1.0 }, null); }
        catch (ArgumentException) { rejected = true; }
        Assert(rejected, "incomplete spectrum arrays produce argument error");
    }
}
