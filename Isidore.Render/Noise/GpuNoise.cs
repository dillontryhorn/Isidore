using System;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Evaluates independent, ordinary Perlin noise points in one GPU pass.
    /// Custom noise behavior retains the scalar CPU evaluation order.
    /// </summary>
    internal static class GpuNoise
    {
        private const long AutomaticMinimum = 16384;
        private const long MaximumPackedBytes = 256L * 1024 * 1024;
        private static readonly Func<double, double> Normal = Noise.DistFunc();
        private static readonly Func<double, double> LogNormal =
            Noise.DistFunc(NoiseDistribution.LogNormal);

        public static bool TryGetValues(Noise noise, Point[] coordinates,
            Vector shift, double multiplier, double offset,
            Func<double, double> distribution, out double[] values)
        {
            values = null;
            try
            {
                return TryGetValuesCore(noise, coordinates, shift, multiplier,
                    offset, distribution, out values);
            }
            catch (OutOfMemoryException)
            {
                // The original scalar loop only allocates an 8-byte result per
                // point and can still succeed when octave packing cannot fit.
                values = null;
                return false;
            }
        }

        private static bool TryGetValuesCore(Noise noise, Point[] coordinates,
            Vector shift, double multiplier, double offset,
            Func<double, double> distribution, out double[] values)
        {
            values = null;
            Type noiseType = noise.GetType();
            bool plain = noiseType == typeof(Noise);
            bool frequency = noiseType == typeof(FrequencyNoise) ||
                noiseType == typeof(fBmNoise) || noiseType == typeof(PerlinTurbulenceNoise);
            bool spectrum = noiseType == typeof(SpectrumNoise);
            // Arbitrary callbacks can mutate later coordinates, the lookup table,
            // or the noise function itself. Keep those calls on the original path.
            if ((!plain && !frequency && !spectrum) || noise.noiseFunc == null ||
                noise.noiseFunc.GetType() != typeof(PerlinNoiseFunction) ||
                distribution == null ||
                (!distribution.Equals(Normal) && !distribution.Equals(LogNormal)) ||
                coordinates == null || coordinates.Length == 0 ||
                coordinates.LongLength > int.MaxValue / 3 ||
                shift == null || shift.GetType() != typeof(Vector) ||
                shift.Comp == null || shift.Comp.Length > 3 ||
                !IsFinite(multiplier) || !IsFinite(offset))
                return false;

            FrequencyNoise frequencyNoise = frequency ? (FrequencyNoise)noise : null;
            SpectrumNoise spectrumNoise = spectrum ? (SpectrumNoise)noise : null;
            double[] frequencies = null;
            double[] powers = null;
            double minFrequency = 1;
            double lacunarity = 1;
            int octaves = 1;
            if (frequency)
            {
                frequencies = frequencyNoise.Frequency;
                if (frequencies == null || frequencies.Length == 0)
                    return false;
                octaves = frequencies.Length;
                minFrequency = frequencyNoise.minFreq;
                lacunarity = frequencyNoise.Lacunarity;
                if (!IsFinite(minFrequency) || !IsFinite(lacunarity))
                    return false;
            }
            if (spectrum)
            {
                PowerSpectrum powerSpectrum = spectrumNoise.powerSpectrum;
                if (powerSpectrum == null || powerSpectrum.Value == null)
                    return false;
                frequencies = powerSpectrum.Frequency;
                powers = powerSpectrum.Power;
                if (frequencies == null || powers == null || frequencies.Length == 0 ||
                    powers.Length < frequencies.Length)
                    return false;
                octaves = frequencies.Length;
            }

            PerlinNoiseFunction perlin = (PerlinNoiseFunction)noise.noiseFunc;
            int[] table = perlin.LUT;
            if (table == null || table.Length < 2 || (table.Length & 1) != 0)
                return false;
            long samples = coordinates.LongLength * octaves;
            if (samples > int.MaxValue / 3)
                return false;
            // Include the two packed buffers, result, lookup table and at most
            // two octave parameter snapshots before allocating any of them.
            long packedBytes = samples * 36 + coordinates.LongLength * 8 +
                table.LongLength * 4 + (long)octaves * 16;
            if (packedBytes > MaximumPackedBytes)
                return false;

            string kernelName = plain ? "perlin_batch" : "frequency_perlin_batch";
            // These batches benefit after warm-up, but driver startup costs
            // exceed their compute savings within the bounded buffer budget.
            if (!GpuAcceleration.ShouldUseKernel(KernelSource, kernelName, samples,
                AutomaticMinimum, long.MaxValue, 1024L * 1024))
                return false;

            SmoothStep polynomial = perlin.Polynomial;
            if (polynomial == null || polynomial.GetType() != typeof(SmoothStep) ||
                polynomial.Order < 0 || polynomial.Order > 6)
                return false;

            int tableSize = table.Length / 2;
            if ((tableSize & (tableSize - 1)) != 0)
                return false;
            // Both halves are public mutable storage. Do not reconstruct the
            // second half from the first; the CPU uses either half as supplied.
            int[] snapshot = (int[])table.Clone();
            foreach (int entry in snapshot)
                if (entry < 0 || entry >= tableSize)
                    return false;

            // 0: sum, 1: weighted sum, 2: division then optional absolute value.
            int aggregation = 0;
            int absoluteValue = 0;
            double[] parameters = new double[1];
            double[] scales = null;
            if (noiseType == typeof(fBmNoise))
            {
                double hurst = ((fBmNoise)noise).Hurst;
                if (!IsFinite(hurst))
                    return false;
                aggregation = 1;
                parameters = (double[])frequencies.Clone();
                for (int octave = 0; octave < octaves; octave++)
                {
                    parameters[octave] = Math.Pow(parameters[octave], -hurst);
                    if (!IsFinite(parameters[octave]))
                        return false;
                }
            }
            if (noiseType == typeof(PerlinTurbulenceNoise))
            {
                aggregation = 2;
                absoluteValue = ((PerlinTurbulenceNoise)noise).absoluteValueNoise ? 1 : 0;
                parameters = (double[])frequencies.Clone();
                foreach (double denominator in parameters)
                    if (!IsFinite(denominator) || denominator == 0)
                        return false;
            }
            if (spectrum)
            {
                aggregation = 1;
                scales = (double[])frequencies.Clone();
                parameters = new double[octaves];
                Array.Copy(powers, parameters, octaves);
                for (int octave = 0; octave < octaves; octave++)
                    if (!IsFinite(scales[octave]) || !IsFinite(parameters[octave]))
                        return false;
            }

            double[] shiftComponents = (double[])shift.Comp.Clone();
            foreach (double component in shiftComponents)
                if (!IsFinite(component))
                    return false;

            int count = coordinates.Length;
            double[,] fractions = new double[(int)samples, 3];
            int[,] lattice = new int[(int)samples, 3];
            int mask = tableSize - 1;
            double[] combined = new double[3];
            double[] scaled = new double[3];
            for (int index = 0; index < count; index++)
            {
                Point point = coordinates[index];
                if (point == null || point.GetType() != typeof(Point) ||
                    point.Comp == null || point.Comp.Length > 3)
                    return false;
                double[] components = point.Comp;
                int dimensions = Math.Max(components.Length, shiftComponents.Length);
                for (int axis = 0; axis < 3; axis++)
                {
                    bool hasCoordinate = axis < components.Length;
                    bool hasShift = axis < shiftComponents.Length;
                    double coordinate = hasCoordinate ? components[axis] : 0;
                    if (!IsFinite(coordinate))
                        return false;
                    if (hasShift)
                    {
                        // Noise.GetVal clones the longer operand and adds the
                        // shorter one. Retain that addition order, including zero.
                        coordinate = hasCoordinate
                            ? (components.Length > shiftComponents.Length
                                ? coordinate + shiftComponents[axis]
                                : shiftComponents[axis] + coordinate)
                            : shiftComponents[axis];
                    }
                    if (!IsFinite(coordinate))
                        return false;
                    combined[axis] = coordinate;
                    scaled[axis] = frequency && axis < dimensions
                        ? coordinate * minFrequency : coordinate;
                }
                for (int octave = 0; octave < octaves; octave++)
                {
                    int sample = index * octaves + octave;
                    for (int axis = 0; axis < 3; axis++)
                    {
                        // FrequencyNoise scales by minFreq, then lacunarity;
                        // its public Frequency values only control weights.
                        // SpectrumNoise scales the original coordinate anew.
                        double coordinate = spectrum && axis < dimensions
                            ? combined[axis] * scales[octave] : scaled[axis];
                        if (!IsFinite(coordinate) || coordinate < -9223372036854775808.0 ||
                            coordinate >= 9223372036854775808.0)
                            return false;
                        // Match signed Int64 floor and fraction before wrapping.
                        long whole = (long)Math.Floor(coordinate);
                        fractions[sample, axis] = coordinate - whole;
                        lattice[sample, axis] = (int)(whole & mask);
                        if (frequency && axis < dimensions)
                            scaled[axis] *= lacunarity;
                    }
                }
            }

            double[] result = new double[count];
            double standardFactor = perlin.StandardNormal ? 3.901151 : 1.0;
            bool executed;
            if (plain)
                executed = GpuAcceleration.TryExecute(KernelSource, kernelName, count,
                    GpuArgument.Input(fractions), GpuArgument.Input(lattice),
                    GpuArgument.Input(snapshot), GpuArgument.Output(result),
                    GpuArgument.Scalar(count), GpuArgument.Scalar(polynomial.Order),
                    GpuArgument.Scalar(standardFactor));
            else
                executed = GpuAcceleration.TryExecute(KernelSource, kernelName, count,
                    GpuArgument.Input(fractions), GpuArgument.Input(lattice),
                    GpuArgument.Input(snapshot), GpuArgument.Input(parameters), GpuArgument.Output(result),
                    GpuArgument.Scalar(count), GpuArgument.Scalar(octaves),
                    GpuArgument.Scalar(polynomial.Order), GpuArgument.Scalar(standardFactor),
                    GpuArgument.Scalar(aggregation), GpuArgument.Scalar(absoluteValue));
            if (!executed)
                return false;

            // Keep scaling and Math.Exp on the CPU, using the existing delegate
            // and exactly the same multiply, add, distribution sequence.
            for (int index = 0; index < count; index++)
            {
                double value = result[index];
                value *= multiplier;
                value += offset;
                result[index] = distribution(value);
            }
            values = result;
            return true;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private const string KernelSource = @"
#ifdef cl_khr_fp64
#pragma OPENCL EXTENSION cl_khr_fp64 : enable
#elif defined(cl_amd_fp64)
#pragma OPENCL EXTENSION cl_amd_fp64 : enable
#endif
#pragma OPENCL FP_CONTRACT OFF

double noise_gradient(int hash, double x, double y, double z)
{
    int h = hash & 15;
    double a = h < 8 || h == 12 || h == 13 ? x : y;
    double b = h < 4 || h == 12 || h == 13 ? y : z;
    double g0 = (h & 1) > 0 ? -a : a;
    double g1 = (h & 2) > 0 ? -b : b;
    return g0 + g1;
}

double noise_fade(double x, int order)
{
    if (x < 0.0) return 0.0;
    if (x > 1.0) return 1.0;
    switch (order)
    {
        case 0: return x;
        case 1: return (-2.0 * x + 3.0) * x * x;
        case 2: return ((6.0 * x - 15.0) * x + 10.0) * x * x * x;
        case 3: return ((((-20.0 * x + 70.0) * x - 84.0) * x) + 35.0) * x * x * x * x;
        case 4: return ((((70.0 * x - 315.0) * x + 540.0) * x - 420.0) * x + 126.0) * x * x * x * x * x;
        case 5: return ((((((-252.0 * x + 1386.0) * x - 3080.0) * x + 3465.0) * x - 1980.0) * x) + 462.0) * x * x * x * x * x * x;
        default: return (((((((924.0 * x - 6006.0) * x + 16380.0) * x - 24024.0) * x + 20020.0) * x - 9009.0) * x) + 1716.0) * x * x * x * x * x * x * x;
    }
}

double noise_lerp(double t, double first, double second)
{
    return first + t * (second - first);
}

double noise_sample(__global const double* fractions,
    __global const int* lattice, __global const int* table,
    size_t base, int order, double standardFactor)
{
    double dx = fractions[base];
    double dy = fractions[base + 1];
    double dz = fractions[base + 2];
    int ix = lattice[base];
    int iy = lattice[base + 1];
    int iz = lattice[base + 2];
    int p0 = table[ix] + iy;
    int p1 = table[ix + 1] + iy;
    int p00 = table[p0] + iz;
    int p01 = table[p0 + 1] + iz;
    int p10 = table[p1] + iz;
    int p11 = table[p1 + 1] + iz;

    double g000 = noise_gradient(table[p00], dx, dy, dz);
    double g100 = noise_gradient(table[p10], dx - 1.0, dy, dz);
    double g010 = noise_gradient(table[p01], dx, dy - 1.0, dz);
    double g110 = noise_gradient(table[p11], dx - 1.0, dy - 1.0, dz);
    double g001 = noise_gradient(table[p00 + 1], dx, dy, dz - 1.0);
    double g101 = noise_gradient(table[p10 + 1], dx - 1.0, dy, dz - 1.0);
    double g011 = noise_gradient(table[p01 + 1], dx, dy - 1.0, dz - 1.0);
    double g111 = noise_gradient(table[p11 + 1], dx - 1.0, dy - 1.0, dz - 1.0);
    double fx = noise_fade(dx, order);
    double fy = noise_fade(dy, order);
    double fz = noise_fade(dz, order);
    double x00 = noise_lerp(fx, g000, g100);
    double x10 = noise_lerp(fx, g010, g110);
    double x01 = noise_lerp(fx, g001, g101);
    double x11 = noise_lerp(fx, g011, g111);
    double y0 = noise_lerp(fy, x00, x10);
    double y1 = noise_lerp(fy, x01, x11);
    return noise_lerp(fz, y0, y1) * standardFactor;
}

__kernel void perlin_batch(__global const double* fractions,
    __global const int* lattice, __global const int* table,
    __global double* values, int count, int order, double standardFactor)
{
    size_t index = get_global_id(0);
    if (index >= (size_t)count) return;
    values[index] = noise_sample(fractions, lattice, table, index * 3, order, standardFactor);
}

__kernel void frequency_perlin_batch(__global const double* fractions,
    __global const int* lattice, __global const int* table, __global const double* parameters,
    __global double* values, int count, int octaves, int order, double standardFactor,
    int aggregation, int absoluteValue)
{
    size_t index = get_global_id(0);
    if (index >= (size_t)count) return;
    double value = 0.0;
    for (int octave = 0; octave < octaves; octave++)
    {
        size_t base = (index * (size_t)octaves + octave) * 3;
        double component = noise_sample(fractions, lattice, table, base, order, standardFactor);
        if (aggregation == 1) component = component * parameters[octave];
        if (aggregation == 2)
        {
            component = component / parameters[octave];
            if (absoluteValue) component = fabs(component);
        }
        value = value + component;
    }
    values[index] = value;
}
";
    }
}
