using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Isidore.Maths
{
    /// <summary>Controls optional GPU execution without changing the CPU APIs.</summary>
    public enum GpuMode
    {
        /// <summary>Always use the original CPU implementation.</summary>
        Disabled,
        /// <summary>Use a compatible GPU only for sufficiently large workloads.</summary>
        Automatic,
        /// <summary>Attempt supported GPU operations even for small workloads.</summary>
        PreferGpu
    }

    /// <summary>A primitive buffer or scalar argument for a compute kernel.</summary>
    public sealed class GpuArgument
    {
        internal readonly Array Data;
        internal readonly object Value;
        internal readonly bool IsOutput;

        private GpuArgument(Array data, object value, bool output)
        {
            Data = data;
            Value = value;
            IsOutput = output;
        }

        /// <summary>Upload a read-only int or double array in CLR storage order.</summary>
        public static GpuArgument Input(Array data) { return Buffer(data, false); }
        /// <summary>Download a complete int or double array, committing only on success.</summary>
        public static GpuArgument Output(Array data) { return Buffer(data, true); }
        /// <summary>A signed 32-bit integer scalar.</summary>
        public static GpuArgument Scalar(int value) { return new GpuArgument(null, value, false); }
        /// <summary>A double precision scalar.</summary>
        public static GpuArgument Scalar(double value) { return new GpuArgument(null, value, false); }

        private static GpuArgument Buffer(Array data, bool output)
        {
            if (data == null) throw new ArgumentNullException("data");
            Type element = data.GetType().GetElementType();
            if (element != typeof(double) && element != typeof(int))
                throw new ArgumentException("GPU buffers must contain int or double values.", "data");
            for (int axis = 0; axis < data.Rank; axis++)
                if (data.GetLowerBound(axis) != 0)
                    throw new ArgumentException("GPU arrays must use zero-based indices.", "data");
            return new GpuArgument(data, null, output);
        }
    }

    /// <summary>
    /// Optional, dependency-free OpenCL acceleration. A missing or incompatible
    /// driver and failed dispatches return false so callers can use their CPU path.
    /// Programs and bounded device buffers are reused; one queue is synchronized
    /// across callers. Kernels use doubles without relaxed math or fused operations.
    /// </summary>
    public static class GpuAcceleration
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Kernel> Kernels = new Dictionary<string, Kernel>();
        private static readonly HashSet<string> FailedKernels = new HashSet<string>();
        private static int mode = (int)InitialMode();
        private static bool initialized;
        private static IntPtr device;
        private static IntPtr context;
        private static IntPtr queue;
        private static string deviceName = "";
        private static string lastError = "";
        private static long dispatchCount;
        private static long cachedBytes;
        private static long cacheLimit = 512L * 1024 * 1024;
        private static ulong maxAllocation;
        private const string Preamble = "#pragma OPENCL EXTENSION cl_khr_fp64 : enable\n#pragma OPENCL FP_CONTRACT OFF\n";

        static GpuAcceleration()
        {
            AppDomain.CurrentDomain.ProcessExit += delegate { ReleaseResources(); };
            AppDomain.CurrentDomain.DomainUnload += delegate { ReleaseResources(); };
        }

        /// <summary>Execution policy. Automatic is the default; ISIDORE_GPU=off/force overrides startup.</summary>
        public static GpuMode Mode
        {
            get { return (GpuMode)Volatile.Read(ref mode); }
            set
            {
                if (!Enum.IsDefined(typeof(GpuMode), value)) throw new ArgumentOutOfRangeException("value");
                Volatile.Write(ref mode, (int)value);
            }
        }

        /// <summary>Whether a GPU with IEEE double precision and an online compiler is available.</summary>
        public static bool IsAvailable { get { lock (Sync) { Initialize(); return context != IntPtr.Zero; } } }
        /// <summary>The selected GPU name, or an empty string when unavailable.</summary>
        public static string DeviceName { get { lock (Sync) { Initialize(); return deviceName; } } }
        /// <summary>The last driver/compiler failure; empty after successful execution.</summary>
        public static string LastError { get { lock (Sync) return lastError; } }
        /// <summary>Number of successfully completed dispatches, useful for diagnostics and verification.</summary>
        public static long DispatchCount { get { return Interlocked.Read(ref dispatchCount); } }

        /// <summary>Apply the workload threshold before initializing or contacting the driver.</summary>
        public static bool ShouldUse(long work, long automaticMinimum)
        {
            return ShouldUse(work, automaticMinimum, automaticMinimum);
        }

        /// <summary>
        /// Apply separate cold and warm workload thresholds. The cold threshold
        /// avoids paying driver setup costs for a modest one-off operation.
        /// </summary>
        public static bool ShouldUse(long work, long automaticMinimum, long coldMinimum)
        {
            return MeetsWorkloadThreshold(work, automaticMinimum, coldMinimum) && IsAvailable;
        }

        /// <summary>
        /// Check policy and cold/warm workload thresholds without contacting the
        /// driver. Useful before packing a batch whose actual work may be smaller.
        /// This does not establish that a compatible device is available.
        /// </summary>
        public static bool MeetsWorkloadThreshold(long work, long automaticMinimum, long coldMinimum)
        {
            GpuMode policy = Mode;
            if (policy == GpuMode.Disabled || work <= 0 ||
                (policy == GpuMode.Automatic && work < automaticMinimum)) return false;
            if (policy == GpuMode.Automatic)
                lock (Sync)
                    if (!initialized && work < coldMinimum) return false;
            return true;
        }

        /// <summary>
        /// Select a kernel using separate thresholds for warm execution, driver
        /// initialization and first compilation. Merely querying device status
        /// does not make an uncompiled kernel a profitable small workload.
        /// </summary>
        public static bool ShouldUseKernel(string source, string kernelName, long work,
            long automaticMinimum, long initializationMinimum, long compilationMinimum)
        {
            if (!MeetsWorkloadThreshold(work, automaticMinimum, initializationMinimum)) return false;
            lock (Sync)
                if (Mode == GpuMode.Automatic && work < compilationMinimum &&
                    !Kernels.ContainsKey(kernelName + "\n" + source)) return false;
            return IsAvailable;
        }

        /// <summary>
        /// Initialize the driver ahead of repeated compute work. Individual
        /// kernels still compile on their first selected dispatch.
        /// </summary>
        public static bool WarmUp()
        {
            return Mode != GpuMode.Disabled && IsAvailable;
        }

        /// <summary>Run a one-dimensional kernel using a driver-selected workgroup size.</summary>
        public static bool TryExecute(string source, string kernelName, int workItems, params GpuArgument[] arguments)
        {
            return TryExecute(source, kernelName, new long[] { workItems }, null, arguments);
        }

        /// <summary>
        /// Run a kernel. Input buffers are read-only; every output element must be
        /// written by the kernel. Host output buffers remain unchanged on failure.
        /// Return false to request CPU fallback. Does not apply workload thresholds.
        /// </summary>
        public static bool TryExecute(string source, string kernelName, long[] globalSize,
            long[] localSize, params GpuArgument[] arguments)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (kernelName == null) throw new ArgumentNullException("kernelName");
            if (arguments == null) throw new ArgumentNullException("arguments");
            if (globalSize == null || globalSize.Length < 1 || globalSize.Length > 3)
                throw new ArgumentException("One to three global dimensions are required.", "globalSize");
            if (localSize != null && localSize.Length != globalSize.Length)
                throw new ArgumentException("Global and local ranks must match.", "localSize");
            UIntPtr[] global = new UIntPtr[globalSize.Length];
            UIntPtr[] local = localSize == null ? null : new UIntPtr[localSize.Length];
            for (int axis = 0; axis < global.Length; axis++)
            {
                if (globalSize[axis] <= 0) return false;
                global[axis] = Size(globalSize[axis]);
                if (local != null)
                {
                    if (localSize[axis] <= 0 || globalSize[axis] % localSize[axis] != 0)
                        throw new ArgumentException("Each global dimension must be divisible by its local dimension.", "localSize");
                    local[axis] = Size(localSize[axis]);
                }
            }
            foreach (GpuArgument arg in arguments)
            {
                if (arg == null) throw new ArgumentException("Arguments cannot be null.", "arguments");
                if (arg.Data != null && arg.Data.LongLength == 0) return false;
            }
            if (Mode == GpuMode.Disabled) return false;

            lock (Sync)
            {
                Initialize();
                if (context == IntPtr.Zero) return false;
                string key = kernelName + "\n" + source;
                if (FailedKernels.Contains(key)) return false;
                Kernel kernel;
                try
                {
                    // Reject oversized batches before paying compilation costs.
                    ValidateBuffers(arguments);
                    if (!Kernels.TryGetValue(key, out kernel))
                    {
                        kernel = Build(source, kernelName);
                        Kernels.Add(key, kernel);
                    }
                    PrepareBuffers(kernel, arguments);
                    for (int index = 0; index < arguments.Length; index++)
                    {
                        GpuArgument arg = arguments[index];
                        if (arg.Data == null)
                        {
                            if (arg.Value is int)
                            {
                                int value = (int)arg.Value;
                                Check(Native.clSetKernelArgInt(kernel.Handle, (uint)index, Size(4), ref value), "set integer argument");
                            }
                            else
                            {
                                double value = (double)arg.Value;
                                Check(Native.clSetKernelArgDouble(kernel.Handle, (uint)index, Size(8), ref value), "set double argument");
                            }
                        }
                        else
                        {
                            IntPtr buffer = kernel.Buffers[index].Handle;
                            Check(Native.clSetKernelArgBuffer(kernel.Handle, (uint)index, Size(IntPtr.Size), ref buffer), "set buffer argument");
                            if (!arg.IsOutput)
                                Transfer(arg.Data, buffer, false);
                        }
                    }
                    Check(Native.clEnqueueNDRangeKernel(queue, kernel.Handle, (uint)global.Length,
                        null, global, local, 0, null, IntPtr.Zero), "launch " + kernelName);

                    // Stage all downloads before publishing any result. A later
                    // failed read must not leave a partially updated host array.
                    Array[] results = new Array[arguments.Length];
                    for (int index = 0; index < arguments.Length; index++)
                        if (arguments[index].IsOutput)
                        {
                            results[index] = (Array)arguments[index].Data.Clone();
                            Transfer(results[index], kernel.Buffers[index].Handle, true);
                        }
                    Check(Native.clFinish(queue), "finish dispatch");
                    for (int index = 0; index < arguments.Length; index++)
                        if (results[index] != null)
                            Buffer.BlockCopy(results[index], 0, arguments[index].Data, 0, Buffer.ByteLength(results[index]));
                    Interlocked.Increment(ref dispatchCount);
                    lastError = "";
                    return true;
                }
                catch (OpenClException error)
                {
                    lastError = error.Message;
                    Native.clFinish(queue);
                    // A bad source is isolated to its kernel; device/allocation
                    // faults also fall back without changing the public policy.
                    if (error.Code == -11 || error.Code == -46 || error.Code == -48)
                        FailedKernels.Add(key);
                    if (error.Code == -2 || error.Code == -34 || error.Code == -36)
                    {
                        // Stop retrying a lost device on every hot-path call.
                        // Explicit ReleaseResources permits rediscovery later.
                        ReleaseResources();
                        initialized = true;
                    }
                    return false;
                }
                catch (OutOfMemoryException)
                {
                    lastError = "Insufficient memory for GPU execution.";
                    Native.clFinish(queue);
                    ClearBuffers();
                    return false;
                }
                catch (OverflowException)
                {
                    // Very large CLR arrays can exceed this backend's signed
                    // byte-count limit without being invalid CPU workloads.
                    lastError = "The array exceeds this GPU backend's transfer size limit.";
                    Native.clFinish(queue);
                    return false;
                }
            }
        }

        /// <summary>Release cached native resources; the next supported call rediscovers the GPU.</summary>
        public static void ReleaseResources()
        {
            lock (Sync)
            {
                ClearBuffers();
                foreach (Kernel kernel in Kernels.Values)
                {
                    Native.clReleaseKernel(kernel.Handle);
                    Native.clReleaseProgram(kernel.Program);
                }
                Kernels.Clear();
                FailedKernels.Clear();
                if (queue != IntPtr.Zero) Native.clReleaseCommandQueue(queue);
                if (context != IntPtr.Zero) Native.clReleaseContext(context);
                queue = context = device = IntPtr.Zero;
                deviceName = "";
                initialized = false;
            }
        }

        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (IntPtr.Size != 8)
            {
                lastError = "GPU acceleration requires a 64-bit process; CPU execution remains available.";
                return;
            }
            try
            {
                uint count;
                Check(Native.clGetPlatformIDs(0, null, out count), "enumerate OpenCL platforms");
                if (count == 0) { lastError = "No OpenCL platform is installed."; return; }
                IntPtr[] platforms = new IntPtr[count];
                Check(Native.clGetPlatformIDs(count, platforms, out count), "read OpenCL platforms");
                long bestScore = -1;
                foreach (IntPtr platform in platforms)
                {
                    uint deviceCount;
                    int code = Native.clGetDeviceIDs(platform, 4, 0, null, out deviceCount);
                    if (code == -1) continue;
                    Check(code, "enumerate GPUs");
                    IntPtr[] devices = new IntPtr[deviceCount];
                    Check(Native.clGetDeviceIDs(platform, 4, deviceCount, devices, out deviceCount), "read GPUs");
                    foreach (IntPtr candidate in devices)
                    {
                        ulong doubleConfig = DeviceUlong(candidate, 0x1032);
                        // Preserve denormals, infinities/NaNs and nearest rounding.
                        if ((doubleConfig & 7) != 7 || DeviceUint(candidate, 0x1027) == 0 ||
                            DeviceUint(candidate, 0x1028) == 0 ||
                            (DeviceUint(candidate, 0x1026) != 0) != BitConverter.IsLittleEndian) continue;
                        long score = (long)DeviceUint(candidate, 0x1002) * DeviceUint(candidate, 0x100C);
                        if (score > bestScore) { bestScore = score; device = candidate; }
                    }
                }
                if (device == IntPtr.Zero) { lastError = "No compatible double-precision OpenCL GPU was found."; return; }
                int error;
                context = Native.clCreateContext(null, 1, new[] { device }, IntPtr.Zero, IntPtr.Zero, out error);
                Check(error, "create GPU context");
                queue = Native.clCreateCommandQueue(context, device, 0, out error);
                Check(error, "create GPU queue");
                maxAllocation = DeviceUlong(device, 0x1010);
                cacheLimit = (long)Math.Min(512UL * 1024 * 1024, DeviceUlong(device, 0x101F) / 4);
                deviceName = DeviceText(device, 0x102B);
                lastError = "";
            }
            catch (Exception error)
            {
                if (!(error is OpenClException) && !(error is DllNotFoundException) &&
                    !(error is EntryPointNotFoundException) && !(error is BadImageFormatException)) throw;
                lastError = error.Message;
                if (queue != IntPtr.Zero) Native.clReleaseCommandQueue(queue);
                if (context != IntPtr.Zero) Native.clReleaseContext(context);
                queue = context = device = IntPtr.Zero;
            }
        }

        private static Kernel Build(string source, string name)
        {
            int error;
            IntPtr program = Native.clCreateProgramWithSource(context, 1, new[] { Preamble + source }, null, out error);
            Check(error, "create compute program");
            IntPtr handle = IntPtr.Zero;
            try
            {
                error = Native.clBuildProgram(program, 1, new[] { device }, "-cl-std=CL1.2", IntPtr.Zero, IntPtr.Zero);
                if (error != 0) throw new OpenClException(error, "Compile " + name + ": " + BuildLog(program));
                handle = Native.clCreateKernel(program, name, out error);
                Check(error, "create kernel " + name);
                return new Kernel { Program = program, Handle = handle };
            }
            catch
            {
                if (handle != IntPtr.Zero) Native.clReleaseKernel(handle);
                Native.clReleaseProgram(program);
                throw;
            }
        }

        private static void ValidateBuffers(GpuArgument[] arguments)
        {
            long required = 0;
            foreach (GpuArgument argument in arguments)
                if (argument.Data != null)
                {
                    long bytes = argument.Data.LongLength *
                        (argument.Data.GetType().GetElementType() == typeof(double) ? 8L : 4L);
                    if (bytes > int.MaxValue || (ulong)bytes > maxAllocation)
                        throw new OpenClException(-4, "GPU buffer exceeds the device allocation or transfer limit.");
                    required += bytes;
                }
            if (required > cacheLimit) throw new OpenClException(-4, "GPU working set exceeds the bounded device buffer budget.");
        }

        private static void PrepareBuffers(Kernel kernel, GpuArgument[] arguments)
        {
            if (kernel.Buffers == null) kernel.Buffers = new DeviceBuffer[arguments.Length];
            if (kernel.Buffers.Length != arguments.Length)
                throw new OpenClException(-52, "The kernel argument count changed.");
            long growth = 0;
            for (int index = 0; index < arguments.Length; index++)
                if (arguments[index].Data != null)
                {
                    int bytes = Buffer.ByteLength(arguments[index].Data);
                    DeviceBuffer buffer = kernel.Buffers[index];
                    if (buffer == null || buffer.Bytes < bytes) growth += bytes - (buffer == null ? 0 : buffer.Bytes);
                }
            if (cachedBytes + growth > cacheLimit) ClearBuffers();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (arguments[index].Data == null) continue;
                int bytes = Buffer.ByteLength(arguments[index].Data);
                DeviceBuffer buffer = kernel.Buffers[index];
                if (buffer != null && buffer.Bytes >= bytes) continue;
                if (buffer != null)
                {
                    Native.clReleaseMemObject(buffer.Handle);
                    cachedBytes -= buffer.Bytes;
                    kernel.Buffers[index] = null;
                }
                int error;
                IntPtr handle = Native.clCreateBuffer(context, 1, Size(bytes), IntPtr.Zero, out error);
                Check(error, "allocate device buffer");
                kernel.Buffers[index] = new DeviceBuffer { Handle = handle, Bytes = bytes };
                cachedBytes += bytes;
            }
        }

        private static void ClearBuffers()
        {
            foreach (Kernel kernel in Kernels.Values)
                if (kernel.Buffers != null)
                    for (int index = 0; index < kernel.Buffers.Length; index++)
                    {
                        DeviceBuffer buffer = kernel.Buffers[index];
                        if (buffer != null) Native.clReleaseMemObject(buffer.Handle);
                        kernel.Buffers[index] = null;
                    }
            cachedBytes = 0;
        }

        private static void Transfer(Array array, IntPtr buffer, bool read)
        {
            GCHandle pinned = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                int bytes = Buffer.ByteLength(array);
                int error = read
                    ? Native.clEnqueueReadBuffer(queue, buffer, 1, UIntPtr.Zero, Size(bytes), pinned.AddrOfPinnedObject(), 0, null, IntPtr.Zero)
                    : Native.clEnqueueWriteBuffer(queue, buffer, 1, UIntPtr.Zero, Size(bytes), pinned.AddrOfPinnedObject(), 0, null, IntPtr.Zero);
                Check(error, read ? "download GPU result" : "upload GPU input");
            }
            finally { pinned.Free(); }
        }

        private static uint DeviceUint(IntPtr handle, uint field) { return BitConverter.ToUInt32(DeviceBytes(handle, field), 0); }
        private static ulong DeviceUlong(IntPtr handle, uint field) { return BitConverter.ToUInt64(DeviceBytes(handle, field), 0); }
        private static string DeviceText(IntPtr handle, uint field) { return Encoding.UTF8.GetString(DeviceBytes(handle, field)).TrimEnd('\0'); }
        private static byte[] DeviceBytes(IntPtr handle, uint field)
        {
            UIntPtr size;
            Check(Native.clGetDeviceInfo(handle, field, UIntPtr.Zero, null, out size), "query GPU information");
            byte[] bytes = new byte[checked((int)size.ToUInt64())];
            Check(Native.clGetDeviceInfo(handle, field, size, bytes, out size), "read GPU information");
            return bytes;
        }
        private static string BuildLog(IntPtr program)
        {
            UIntPtr size;
            Native.clGetProgramBuildInfo(program, device, 0x1183, UIntPtr.Zero, null, out size);
            byte[] bytes = new byte[checked((int)size.ToUInt64())];
            Native.clGetProgramBuildInfo(program, device, 0x1183, size, bytes, out size);
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
        private static UIntPtr Size(long value) { return new UIntPtr(checked((ulong)value)); }
        private static void Check(int code, string operation) { if (code != 0) throw new OpenClException(code, operation + " (OpenCL " + code + ")."); }
        private static GpuMode InitialMode()
        {
            string setting = Environment.GetEnvironmentVariable("ISIDORE_GPU");
            if (string.Equals(setting, "off", StringComparison.OrdinalIgnoreCase)) return GpuMode.Disabled;
            if (string.Equals(setting, "force", StringComparison.OrdinalIgnoreCase)) return GpuMode.PreferGpu;
            return GpuMode.Automatic;
        }
        private sealed class Kernel { internal IntPtr Program; internal IntPtr Handle; internal DeviceBuffer[] Buffers; }
        private sealed class DeviceBuffer { internal IntPtr Handle; internal int Bytes; }
        private sealed class OpenClException : Exception
        {
            internal readonly int Code;
            internal OpenClException(int code, string message) : base(message) { Code = code; }
        }

        private static class Native
        {
            private const string Library = "OpenCL.dll";
            [DllImport(Library)] internal static extern int clGetPlatformIDs(uint count, [Out] IntPtr[] platforms, out uint returned);
            [DllImport(Library)] internal static extern int clGetDeviceIDs(IntPtr platform, ulong type, uint count, [Out] IntPtr[] devices, out uint returned);
            [DllImport(Library)] internal static extern int clGetDeviceInfo(IntPtr device, uint field, UIntPtr size, [Out] byte[] data, out UIntPtr returned);
            [DllImport(Library)] internal static extern IntPtr clCreateContext(IntPtr[] properties, uint count, IntPtr[] devices, IntPtr callback, IntPtr user, out int error);
            [DllImport(Library)] internal static extern IntPtr clCreateCommandQueue(IntPtr context, IntPtr device, ulong properties, out int error);
            [DllImport(Library, CharSet = CharSet.Ansi)] internal static extern IntPtr clCreateProgramWithSource(IntPtr context, uint count, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] source, UIntPtr[] lengths, out int error);
            [DllImport(Library, CharSet = CharSet.Ansi)] internal static extern int clBuildProgram(IntPtr program, uint count, IntPtr[] devices, string options, IntPtr callback, IntPtr user);
            [DllImport(Library)] internal static extern int clGetProgramBuildInfo(IntPtr program, IntPtr device, uint field, UIntPtr size, [Out] byte[] data, out UIntPtr returned);
            [DllImport(Library, CharSet = CharSet.Ansi)] internal static extern IntPtr clCreateKernel(IntPtr program, string name, out int error);
            [DllImport(Library)] internal static extern IntPtr clCreateBuffer(IntPtr context, ulong flags, UIntPtr size, IntPtr host, out int error);
            [DllImport(Library, EntryPoint = "clSetKernelArg")] internal static extern int clSetKernelArgBuffer(IntPtr kernel, uint index, UIntPtr size, ref IntPtr value);
            [DllImport(Library, EntryPoint = "clSetKernelArg")] internal static extern int clSetKernelArgInt(IntPtr kernel, uint index, UIntPtr size, ref int value);
            [DllImport(Library, EntryPoint = "clSetKernelArg")] internal static extern int clSetKernelArgDouble(IntPtr kernel, uint index, UIntPtr size, ref double value);
            [DllImport(Library)] internal static extern int clEnqueueWriteBuffer(IntPtr queue, IntPtr buffer, uint blocking, UIntPtr offset, UIntPtr size, IntPtr data, uint waitCount, IntPtr[] waits, IntPtr returnedEvent);
            [DllImport(Library)] internal static extern int clEnqueueReadBuffer(IntPtr queue, IntPtr buffer, uint blocking, UIntPtr offset, UIntPtr size, IntPtr data, uint waitCount, IntPtr[] waits, IntPtr returnedEvent);
            [DllImport(Library)] internal static extern int clEnqueueNDRangeKernel(IntPtr queue, IntPtr kernel, uint dimensions, UIntPtr[] offset, UIntPtr[] global, UIntPtr[] local, uint waitCount, IntPtr[] waits, IntPtr returnedEvent);
            [DllImport(Library)] internal static extern int clFinish(IntPtr queue);
            [DllImport(Library)] internal static extern int clReleaseMemObject(IntPtr buffer);
            [DllImport(Library)] internal static extern int clReleaseKernel(IntPtr kernel);
            [DllImport(Library)] internal static extern int clReleaseProgram(IntPtr program);
            [DllImport(Library)] internal static extern int clReleaseCommandQueue(IntPtr queue);
            [DllImport(Library)] internal static extern int clReleaseContext(IntPtr context);
        }
    }
}
