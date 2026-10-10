using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Isidore.Maths;
using Isidore.Render;

internal static class GpuNoiseRegression
{
    public static void Run()
    {
        GpuMode previousMode = GpuAcceleration.Mode;
        try
        {
            Point[] coordinates = CreateCoordinates(173, true);
            foreach (int order in new int[] { 0, 1, 2, 3, 4, 5, 6 })
                foreach (bool standardNormal in new bool[] { false, true })
                    for (int dimensions = 0; dimensions <= 3; dimensions++)
                    {
                        double[] components = new double[dimensions];
                        for (int axis = 0; axis < dimensions; axis++)
                            components[axis] = axis % 2 == 0 ? 0.375 : -2.125;
                        Noise noise = new Noise(new PerlinNoiseFunction(173, 8, standardNormal, order),
                            new Vector(components), -1.75, 0.375);
                        Compare(noise, coordinates, true,
                            "smoothstep " + order + ", standard " + standardNormal + ", shift dimensions " + dimensions);
                    }

            foreach (int power in new int[] { 0, 1, 3, 10 })
                Compare(new Noise(new PerlinNoiseFunction(41, power), new Vector(0)),
                    coordinates, true, "lookup table power " + power);

            PerlinNoiseFunction mutable = new PerlinNoiseFunction(67, 3);
            Noise mutableNoise = new Noise(mutable, new Vector(0));
            Compare(mutableNoise, coordinates, true, "initial lookup table");
            for (int index = 0; index < mutable.LUT.Length; index++)
                mutable.LUT[index] = index < 8 ? (3 * index + 1) % 8 : (5 * index + 2) % 8;
            Compare(mutableNoise, coordinates, true, "independently edited lookup table halves");

            Noise logNormal = new Noise(new PerlinNoiseFunction(19, 8, true, 6),
                new Vector(0.25, -0.5, 0.125), 0.7, -0.3, Noise.DistFunc(NoiseDistribution.LogNormal));
            Compare(logNormal, coordinates, true, "CPU log-normal distribution after GPU noise");
            logNormal.distFunc = (Func<double, double>)logNormal.distFunc.Clone();
            Compare(logNormal, coordinates, true, "cloned built-in distribution delegate");

            CompareFrequencyModels(coordinates);

            Compare(new Noise(new PerlinNoiseFunction(53), new Vector(0)),
                WrappingCoordinates(), true, "signed Int64 lattice wrapping and exact boundaries");
            Compare(new Noise(new PerlinNoiseFunction(53), new Vector(0)),
                new Point[] { new Point(new double[] { double.Epsilon, -double.Epsilon, 0.125 }),
                    new Point(new double[] { -0.0, 0.0, -0.0 }) }, true, "subnormal and signed-zero coordinates");

            Noise plain = new Noise(new PerlinNoiseFunction(11), new Vector(0));
            Compare(plain, new Point[0], false, "empty batch");
            Compare(plain, new Point[] { new Point(new double[] { 0.1, 0.2, 0.3, 0.4 }) }, false,
                "four-dimensional CPU noise");
            Compare(new Noise(new PerlinNoiseFunction(11), new Vector(new double[] { 0.1, 0.2, 0.3, 0.4 })),
                coordinates, false, "four-dimensional shift");
            Compare(plain, new Point[] { new Point(double.NaN, 0.25, -0.75) }, false, "NaN coordinate");
            Compare(plain, new Point[] { new Point(double.PositiveInfinity, 0.25, -0.75) }, false,
                "infinite coordinate");
            Compare(plain, new Point[] { new Point(9223372036854775808.0, 0.25, -0.75) }, false,
                "coordinate outside signed Int64 conversion range");
            Compare(new Noise(new PerlinNoiseFunction(11), new Vector(double.NaN, 0, 0)),
                coordinates, false, "nonfinite shift");
            Compare(new Noise(new PerlinNoiseFunction(11), new Vector(0), double.PositiveInfinity, 0),
                coordinates, false, "nonfinite multiplier");
            Compare(new Noise(new PerlinNoiseFunction(11), new Vector(0), 1, double.NaN),
                coordinates, false, "nonfinite offset");

            // Custom callbacks must run once per point in scalar order, including
            // mutations that change subsequent samples or replace the function.
            int expectedCallbacks;
            int actualCallbacks;
            GpuAcceleration.Mode = GpuMode.Disabled;
            double[] expectedCallback = CallbackScenario(out expectedCallbacks);
            GpuAcceleration.Mode = GpuMode.PreferGpu;
            long beforeCallback = GpuAcceleration.DispatchCount;
            double[] actualCallback = CallbackScenario(out actualCallbacks);
            CompareValues(expectedCallback, actualCallback, "mutating distribution callback");
            Assert(expectedCallbacks == 17 && actualCallbacks == 17 &&
                GpuAcceleration.DispatchCount == beforeCallback,
                "Custom callbacks must retain their scalar invocation count and order.");
            GpuAcceleration.Mode = GpuMode.Disabled;
            expectedCallback = CallbackScenario(out expectedCallbacks, true);
            GpuAcceleration.Mode = GpuMode.PreferGpu;
            beforeCallback = GpuAcceleration.DispatchCount;
            actualCallback = CallbackScenario(out actualCallbacks, true);
            CompareValues(expectedCallback, actualCallback, "mutating fractal distribution callback");
            Assert(expectedCallbacks == 17 && actualCallbacks == 17 &&
                GpuAcceleration.DispatchCount == beforeCallback,
                "Frequency callbacks must retain mutations and scalar invocation order.");

            Compare(new RecordingNoise(), coordinates, false, "Noise subclass scalar override");
            Compare(new Noise(new RecordingFunction(), new Vector(0)), coordinates, false,
                "custom NoiseFunction");
            Compare(new Noise(new DerivedPerlin(), new Vector(0)), coordinates, false,
                "PerlinNoiseFunction subclass");
            Func<double, double> multicast = Noise.DistFunc();
            multicast += Noise.DistFunc();
            Compare(new Noise(new PerlinNoiseFunction(11), new Vector(0), 1, 0, multicast),
                coordinates, false, "multicast distribution delegate");

            PerlinNoiseFunction invalid = new PerlinNoiseFunction(1, 3);
            invalid.LUT[0] = -1;
            CompareFailure(new Noise(invalid, new Vector(0)),
                new Point[] { new Point(0.125, 0.25, 0.5) }, "invalid public lookup table");
            CompareFailure(plain, new Point[] { coordinates[0], null }, "null point after valid prefix");
            CompareFailure(plain, new Point[] { new Point(5) }, "unsupported point dimensions");

            GpuAcceleration.Mode = GpuMode.Disabled;
            long beforeDisabled = GpuAcceleration.DispatchCount;
            plain.GetVal(CreateCoordinates(20000, false));
            Assert(GpuAcceleration.DispatchCount == beforeDisabled, "Disabled noise must not dispatch.");
            GpuAcceleration.Mode = GpuMode.Automatic;
            long beforeSmall = GpuAcceleration.DispatchCount;
            plain.GetVal(coordinates);
            Assert(GpuAcceleration.DispatchCount == beforeSmall, "Small automatic noise batches must use the CPU.");
        }
        finally { GpuAcceleration.Mode = previousMode; }
    }

    private static void Compare(Noise noise, Point[] coordinates, bool eligible, string label)
    {
        GpuAcceleration.Mode = GpuMode.Disabled;
        double[] expected = noise.GetVal(coordinates);
        ResetRecording(noise);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        bool available = GpuAcceleration.IsAvailable;
        long before = GpuAcceleration.DispatchCount;
        double[] actual = noise.GetVal(coordinates);
        CompareValues(expected, actual, label);
        if (eligible && available)
            Assert(GpuAcceleration.DispatchCount == before + 1,
                label + ": eligible batch must execute on compatible hardware. " + GpuAcceleration.LastError);
        else
            Assert(GpuAcceleration.DispatchCount == before, label + ": batch must retain CPU fallback.");
    }

    private static void ResetRecording(Noise noise)
    {
        RecordingNoise recordingNoise = noise as RecordingNoise;
        if (recordingNoise != null) recordingNoise.Calls = 0;
        RecordingFunction recordingFunction = noise.noiseFunc as RecordingFunction;
        if (recordingFunction != null) recordingFunction.Calls = 0;
        DerivedPerlin derivedPerlin = noise.noiseFunc as DerivedPerlin;
        if (derivedPerlin != null) derivedPerlin.Calls = 0;
        DerivedFractal derivedFractal = noise as DerivedFractal;
        if (derivedFractal != null) derivedFractal.Calls = 0;
    }

    private static void CompareFrequencyModels(Point[] coordinates)
    {
        foreach (int order in new int[] { 0, 1, 2, 3, 4, 5, 6 })
            foreach (bool standardNormal in new bool[] { false, true })
            {
                Vector shift = new Vector(new double[] { 0.375, -0.125 });
                Compare(new FrequencyNoise(new PerlinNoiseFunction(113, 3, standardNormal, order),
                    0.25, 5, 1.75, shift, 1.3, -0.2), coordinates, true,
                    "frequency sum smoothstep " + order + ", standard " + standardNormal);
                Compare(new fBmNoise(new PerlinNoiseFunction(113, 3, standardNormal, order),
                    0.25, 5, 0.73, 1.75, shift, -1.3, 0.2), coordinates, true,
                    "fractional Brownian smoothstep " + order + ", standard " + standardNormal);
                Compare(new PerlinTurbulenceNoise(new PerlinNoiseFunction(113, 3, standardNormal, order),
                    0.25, 5, order % 2 == 0, 1.75, shift, 0.8, -0.1), coordinates, true,
                    "turbulence division smoothstep " + order + ", standard " + standardNormal);
                Compare(new SpectrumNoise(new PerlinNoiseFunction(113, 3, standardNormal, order),
                    TestSpectrum(), shift, 0.9, 0.1), coordinates, true,
                    "independent spectrum scales smoothstep " + order + ", standard " + standardNormal);
            }

        PerlinNoiseFunction perlin = new PerlinNoiseFunction(157, 3, true, 5);
        fBmNoise fractal = new fBmNoise(perlin, 0.25, 8, 0.4, 1.7, new Vector(0));
        Compare(fractal, coordinates, true, "fractal before public mutations");
        for (int octave = 0; octave < fractal.Frequency.Length; octave++)
            fractal.Frequency[octave] = 0.3 + octave * 0.625;
        for (int index = 0; index < perlin.LUT.Length; index++)
            perlin.LUT[index] = index < 8 ? (3 * index + 1) % 8 : (5 * index + 2) % 8;
        fractal.Hurst = -0.6;
        Compare(fractal, coordinates, true, "mutable fractal weights and independent lookup table halves");
        fractal.minFreq = 0.5;
        fractal.Lacunarity = 1.8;
        fractal.distFunc = Noise.DistFunc(NoiseDistribution.LogNormal);
        Compare(fractal, coordinates, true, "updated fractal coordinate scaling and CPU distribution");

        FrequencyNoise unweighted = new FrequencyNoise(new PerlinNoiseFunction(159),
            0.25, 4, 1.7, new Vector(0));
        for (int octave = 0; octave < unweighted.Frequency.Length; octave++)
            unweighted.Frequency[octave] = double.NaN;
        Compare(unweighted, coordinates, true, "unweighted frequency values are unused for coordinates");

        PerlinTurbulenceNoise turbulence = new PerlinTurbulenceNoise(new PerlinNoiseFunction(163),
            0.25, 4, false, 1.7, new Vector(0));
        for (int octave = 0; octave < turbulence.Frequency.Length; octave++)
            turbulence.Frequency[octave] = octave % 2 == 0 ? -0.7 - octave : 0.3 + octave;
        Compare(turbulence, coordinates, true, "mutable signed turbulence denominators");
        turbulence.absoluteValueNoise = true;
        Compare(turbulence, coordinates, true, "absolute value follows division");

        SpectrumNoise spectrum = new SpectrumNoise(new PerlinNoiseFunction(167), TestSpectrum(), new Vector(0));
        Compare(spectrum, coordinates, true, "spectrum before public mutations");
        spectrum.powerSpectrum.Frequency[0] = -0.375;
        spectrum.powerSpectrum.Frequency[2] = 0;
        spectrum.powerSpectrum.Power[1] = -1.25;
        Compare(spectrum, coordinates, true, "mutated signed and zero spectrum frequencies and powers");
        spectrum.powerSpectrum.Value = new Spectrum<double, double>(
            new double[] { 0.2, 1.125, 3.7 }, new double[] { 1.0, -0.5, 0.125 });
        Compare(spectrum, coordinates, true, "replaced spectrum arrays and octave count");

        Compare(new DerivedFractal(), coordinates, false, "frequency subclass component override");
        Compare(new fBmNoise(new RecordingFunction(), shift: new Vector(0)), coordinates, false,
            "frequency custom noise function");
        Compare(new fBmNoise(new PerlinNoiseFunction(173), shift: new Vector(0))
            { distFunc = value => value * 0.5 }, coordinates, false, "frequency custom distribution");
        Compare(new Isidore.Models.TurbulentNoise(), coordinates, false,
            "model-specific turbulence subclass remains CPU");
        Point[] fourDimensional = new Point[] { new Point(new double[] { 0.1, -0.25, 0.75, 0.375 }) };
        Compare(fractal, fourDimensional, false, "four-dimensional fractal fallback");
        Compare(turbulence, fourDimensional, false, "four-dimensional turbulence fallback");
        Compare(spectrum, fourDimensional, false, "four-dimensional spectrum fallback");

        fractal.Hurst = double.NaN;
        Compare(fractal, coordinates, false, "nonfinite fractal weighting fallback");
        turbulence.Frequency[0] = 0;
        Compare(turbulence, coordinates, false, "zero turbulence denominator fallback");
        spectrum.powerSpectrum.Power[0] = double.NaN;
        Compare(spectrum, coordinates, false, "nonfinite spectrum power fallback");
        spectrum.powerSpectrum.Power[0] = 1;
        spectrum.powerSpectrum.Frequency[0] = double.PositiveInfinity;
        Compare(spectrum, coordinates, false, "nonfinite spectrum frequency fallback");
        Compare(new FrequencyNoise(new PerlinNoiseFunction(179), 1, 2, 2, new Vector(0)),
            new Point[] { new Point(4611686018427387904.0, 0.25, 0.5) }, false,
            "later octave exceeds signed Int64 lattice range");

        // An invalid first point fails quickly on the scalar fallback. Its batch
        // dimensions alone must reject packing that would exceed 256 MiB.
        double[] manyFrequencies = new double[1024];
        double[] manyPowers = new double[1024];
        for (int octave = 0; octave < manyFrequencies.Length; octave++)
        {
            manyFrequencies[octave] = octave + 1;
            manyPowers[octave] = 1;
        }
        CompareFailure(new SpectrumNoise(powerSpectrum: new PowerSpectrum(manyFrequencies, manyPowers)),
            new Point[8192], "bounded octave packing fallback");
    }

    private static PowerSpectrum TestSpectrum()
    {
        return new PowerSpectrum(new double[] { 0.125, 0.7, 1.9, 4.25, 11.5 },
            new double[] { 0.6, -0.25, 1.0, 0.125, 0.01 });
    }

    private static double[] CallbackScenario(out int calls, bool frequency = false)
    {
        int invocation = 0;
        Point[] points = CreateCoordinates(17, false);
        PerlinNoiseFunction perlin = new PerlinNoiseFunction(77, 3);
        Noise noise = frequency
            ? new fBmNoise(perlin, 0.25, 4, 0.73, 1.7, new Vector(0))
            : new Noise(perlin, new Vector(0));
        noise.distFunc = value =>
        {
            invocation++;
            if (invocation < points.Length) points[invocation].Comp[0] += 2.125;
            perlin.LUT[0] = (perlin.LUT[0] + 1) % 8;
            if (frequency) ((fBmNoise)noise).Frequency[0] += 0.125;
            if (invocation == 4) noise.noiseFunc = new RecordingFunction();
            return value + invocation * 0.1;
        };
        double[] result = noise.GetVal(points);
        calls = invocation;
        return result;
    }

    private static void CompareFailure(Noise noise, Point[] coordinates, string label)
    {
        GpuAcceleration.Mode = GpuMode.Disabled;
        Type expected = FailureType(() => noise.GetVal(coordinates));
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        long before = GpuAcceleration.DispatchCount;
        Type actual = FailureType(() => noise.GetVal(coordinates));
        Assert(expected != null && actual == expected && GpuAcceleration.DispatchCount == before,
            label + ": fallback must retain the CPU exception without a GPU dispatch.");
    }

    private static Type FailureType(Action operation)
    {
        try { operation(); return null; }
        catch (Exception error) { return error.GetType(); }
    }

    private static void CompareValues(double[] expected, double[] actual, string label)
    {
        Assert(expected.Length == actual.Length, label + ": result lengths differ.");
        for (int index = 0; index < expected.Length; index++)
        {
            double first = expected[index];
            double second = actual[index];
            if (double.IsNaN(first))
                Assert(double.IsNaN(second), label + ": NaN propagation differs.");
            else if (double.IsInfinity(first))
                Assert(first == second, label + ": infinity propagation differs.");
            else
                Assert(Math.Abs(first - second) <= 2e-12 * Math.Max(1, Math.Abs(first)),
                    label + ": CPU/GPU value differs at point " + index + ".");
        }
    }

    private static Point[] WrappingCoordinates()
    {
        return new Point[]
        {
            new Point(-0.125, -255.5, 256.75), new Point(-256.0, 255.0, -1.0),
            new Point(4294967296.125, -4294967296.5, 8589934592.75),
            new Point(-9223372036854775808.0, 0.25, -0.75),
            new Point(9223372036854774784.0, -0.25, 0.75),
            new Point(9007199254740992.0, -9007199254740992.0, 0.125),
            new Point(-1.0 - 2.220446049250313e-16, 1.0 - 1.1102230246251565e-16, 0.5)
        };
    }

    private static Point[] CreateCoordinates(int count, bool mixedDimensions)
    {
        Point[] coordinates = new Point[count];
        for (int index = 0; index < count; index++)
        {
            int dimensions = mixedDimensions ? index % 4 : 3;
            double[] components = new double[dimensions];
            for (int axis = 0; axis < dimensions; axis++)
                components[axis] = ((index * (17 + 6 * axis)) % 113 - 53) * 0.125 + axis * 0.0625;
            coordinates[index] = new Point(components);
        }
        return coordinates;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class RecordingNoise : Noise
    {
        public int Calls;
        public RecordingNoise() : base(new PerlinNoiseFunction(29), new Vector(0)) { }
        public override double GetVal(Point coordinate, Vector shift, double multiplier,
            double offset, Func<double, double> distribution)
        {
            Calls++;
            return base.GetVal(coordinate, shift, multiplier, offset, distribution) + Calls * 0.03125;
        }
    }

    private sealed class RecordingFunction : NoiseFunction
    {
        public int Calls;
        public override double GetVal(Point coordinate)
        {
            Calls++;
            double value = Calls * 0.03125;
            foreach (double component in coordinate.Comp) value += component;
            return value;
        }
    }

    private sealed class DerivedPerlin : PerlinNoiseFunction
    {
        public int Calls;
        public DerivedPerlin() : base(31) { }
        public override double GetVal(Point coordinate)
        {
            Calls++;
            return base.GetVal(coordinate) + Calls * 0.03125;
        }
    }

    private sealed class DerivedFractal : fBmNoise
    {
        public int Calls;
        public DerivedFractal() : base(new PerlinNoiseFunction(181), 0.25, 4, 0.75, 1.7, new Vector(0)) { }
        public override double[] GetComponents(Point coordinate)
        {
            Calls++;
            double[] components = base.GetComponents(coordinate);
            components[0] += Calls * 0.03125;
            return components;
        }
    }

    /// <summary>
    /// Warm end-to-end timings include coordinate packing, table snapshots,
    /// transfers, standard normalization and the CPU distribution function.
    /// </summary>
    public static string Benchmark()
    {
        GpuMode previousMode = GpuAcceleration.Mode;
        StringBuilder rows = new StringBuilder("operation,points,cpu_ms,gpu_ms,speedup,gpu_dispatches\n");
        try
        {
            if (!GpuAcceleration.IsAvailable)
                return "Noise GPU benchmarks skipped: " + GpuAcceleration.LastError;
            foreach (int count in new int[] { 16384, 131072, 524288 })
            {
                Point[] coordinates = CreateCoordinates(count, false);
                BenchmarkOperation(rows, "Perlin normal", coordinates,
                    new Noise(new PerlinNoiseFunction(53, 8, true), new Vector(0.25, -0.5, 0.125), 1.75, -0.375));
                BenchmarkOperation(rows, "Perlin log-normal", coordinates,
                    new Noise(new PerlinNoiseFunction(53, 8, true), new Vector(0.25, -0.5, 0.125), 0.7, -0.3,
                        Noise.DistFunc(NoiseDistribution.LogNormal)));
            }
            Point[] frequencyCoordinates = CreateCoordinates(131072, false);
            BenchmarkOperation(rows, "fBm ten octaves", frequencyCoordinates,
                new fBmNoise(new PerlinNoiseFunction(53, 8, true), 0.125, 64, 0.75, 2,
                    new Vector(0.25, -0.5, 0.125), 1.75, -0.375));
            BenchmarkOperation(rows, "Spectrum five frequencies", frequencyCoordinates,
                new SpectrumNoise(new PerlinNoiseFunction(53, 8, true), TestSpectrum(),
                    new Vector(0.25, -0.5, 0.125), 1.75, -0.375));
            return rows.ToString();
        }
        finally { GpuAcceleration.Mode = previousMode; }
    }

    private static void BenchmarkOperation(StringBuilder rows, string name, Point[] coordinates, Noise noise)
    {
        Action operation = () => noise.GetVal(coordinates);
        GpuAcceleration.Mode = GpuMode.Disabled;
        operation();
        double cpuTime = MedianMilliseconds(operation);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        operation();
        long before = GpuAcceleration.DispatchCount;
        double gpuTime = MedianMilliseconds(operation);
        long dispatched = GpuAcceleration.DispatchCount - before;
        if (dispatched == 0)
        {
            rows.Append(name).Append(",GPU dispatch failed: ").Append(GpuAcceleration.LastError).Append('\n');
            return;
        }
        rows.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2:F3},{3:F3},{4:F3},{5}\n",
            name, coordinates.Length, cpuTime, gpuTime, cpuTime / gpuTime, dispatched);
    }

    private static double MedianMilliseconds(Action operation)
    {
        double[] elapsed = new double[3];
        for (int sample = 0; sample < elapsed.Length; sample++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            operation();
            watch.Stop();
            elapsed[sample] = watch.Elapsed.TotalMilliseconds;
        }
        Array.Sort(elapsed);
        return elapsed[elapsed.Length / 2];
    }
}
