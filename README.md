# Isidore

Isidore is a C# library for scientific geometry, ray tracing, procedural textures,
and turbulence simulation. It targets **.NET Framework 4.8** on Windows. The
solution contains reusable libraries and a console demonstration/test program;
applications use the libraries to construct and trace their own scenes.

## Projects and dependencies

| Project | Purpose | Isidore dependencies |
| --- | --- | --- |
| `Isidore.Maths` | Points, vectors, normals, transforms, intersections, interpolation, arrays, statistics, random distributions, KD trees, marching cubes | None |
| `Isidore.Render` | Scenes, projectors, ray trees, shapes, volumes, materials, physical properties, textures, noise | Maths |
| `Isidore.Models` | Turbulent noise and reference/scatter point turbulence models | Maths, Render |
| `Isidore.Load` | Bitmap, text, Wavefront OBJ, Tecplot, and NASTRAN readers | Maths, Render |
| `Isidore.Library` | Embedded sample meshes and the `R` bitmap | Maths, Render, Load |
| `Isidore.ImgProcess` | Image conversion, Sobel/Canny edge processing, contours, Windows Forms image display | Maths |
| `Isidore.Matlab` | MATLAB COM array transfer and reflection-based data extraction | Maths |
| `Isidore_Tests` | Original console tests and scientific demonstrations, with MATLAB plots/check scripts | All libraries |
| `tests/RegressionTests.csproj` | Automated regression and scene smoke checks | Core libraries; MATLAB wrapper source with a test double |

The `.csproj` files explicitly select the compiled source files. Some files in the
tree are older implementations or unfinished experiments, such as `zRayTracer.cs`,
the `zMesh*` files, `LoadRhino3D.cs`, and the point source implementations. Reading
the project file is the reliable way to distinguish active code from these files.

## How rendering works

1. Create a `Scene`, add bodies to `scene.Bodies`, and add projectors to
   `scene.Projectors`. A `RectangleProjector` generates a grid of rays. With zero
   pixel pointing angles, those rays point along the local positive Z axis.
2. Use each item's `TransformTimeLine` (`KeyFrameTrans`) to place or animate it.
   Geometry is defined in local coordinates and advanced into world coordinates.
3. Call `scene.AdvanceToTime(time)`. The scene updates its bodies and projectors,
   then intersects each projector's open rays with the bodies. Shapes include
   spheres, planes, billboards, and triangle meshes; voxel volumes support
   volumetric boundaries. Bounding boxes and mesh octrees accelerate intersection
   work.
4. Each ray keeps `IntersectData`: whether it hit, its travel distance, the hit
   point, the body, body-specific information, and physical properties. Shape
   data includes normals, incidence angles, and texture coordinates.
5. Materials process the closest intersection and can add properties or cast
   more rays, for example for reflection or transparency. A projector stores a
   `RayTree` for every initial ray and continues tracing until the trees close.
6. Inspect `projector.Ray(x, y).Rays`, or use the projector's intersection/property
   extraction methods to collect result arrays.

The scene skips a repeated time unless `force: true` is supplied. After changing
geometry or materials at the same simulation time, call
`scene.AdvanceToTime(time, true)` to regenerate the result. `UseMultiCores` controls
parallel intersection processing. A `MaterialStack` applies the first enabled
layer whose alpha texture covers the hit; a positive alpha value means coverage.

This pipeline produces intersection and physical-property data. The supplied
`Sources` infrastructure has an unfinished shading stage in `Scene.Render`;
the current renderer does not produce a complete lit image through that stage.

## Minimal scene

Reference `Isidore.Maths` and `Isidore.Render` from a .NET Framework 4.8 application:

```csharp
using System;
using Isidore.Maths;
using Isidore.Render;

var projector = new RectangleProjector(3, 3, 0.1, 0.1);
projector.TransformTimeLine =
    new KeyFrameTrans(Transform.Translate(0, 0, -3));

var scene = new Scene();
scene.Projectors.Add(projector);
scene.Bodies.Add(new Isidore.Render.Sphere(new Point(0, 0, 0), 1));
scene.AdvanceToTime(0);

var hit = projector.Ray(1, 1).Rays[0].IntersectData;
Console.WriteLine("Hit: {0}, travel: {1}", hit.Hit, hit.Travel);
// The central ray hits the sphere at travel distance 2.
```

The regression runner checks this example with both single-core and multicore
intersection processing.

## GPU acceleration

The existing APIs automatically use an OpenCL GPU for supported large batches.
Use a **64-bit process** and a graphics driver exposing double-precision OpenCL 1.2+
with an online compiler. A CUDA toolkit, native build step, and additional NuGet
packages are unnecessary. Missing or incompatible drivers retain CPU execution.

```csharp
// Defaults to Automatic. All policies preserve the CPU fallback.
GpuAcceleration.Mode = GpuMode.Automatic;
GpuAcceleration.WarmUp(); // Initialize the driver; kernels compile when first used.
Console.WriteLine(GpuAcceleration.DeviceName);

// Useful for comparisons and applications that require the original CPU path.
GpuAcceleration.Mode = GpuMode.Disabled;

// Attempt supported kernels even for small batches (primarily for validation).
GpuAcceleration.Mode = GpuMode.PreferGpu;
```

`ISIDORE_GPU=off` or `ISIDORE_GPU=force` selects the startup policy. The types live
in `Isidore.Maths`. `IsAvailable`, `LastError`, and `DispatchCount` expose driver
availability, fallback diagnostics, and actual completed GPU work.

Accelerated paths include:

- `Arr.MatrixMultiply` for `double` and `int` matrices, using shared-memory tiles,
  and large matrix-vector batches using independent row dot products.
- `Arr.Convolve` for `double` and `int` vectors and images, including mixed-type
  overloads that convert to either supported output type.
- `Sobel.Process` and the Sobel stage of `Canny.Process`, combining both gradient
  convolutions and magnitude into one upload and kernel dispatch.
- Supported triangle meshes traced through a projector or scene, batching
  octree-selected triangle candidates and reconstructing accepted intersections
  on the CPU.
- `Noise.GetVal(Point[])` batches backed by three-dimensional Perlin noise,
  including the exact built-in `FrequencyNoise`, `fBmNoise`,
  `PerlinTurbulenceNoise`, and `SpectrumNoise` types and built-in distributions.
  Octaves accumulate in their original order; coordinate floors and `Math.Pow`
  weights are prepared on the CPU.

Double precision and accumulation order are preserved; kernels disable floating
point contraction and relaxed math. Integer kernels retain unchecked overflow.
Image borders, even-sized convolution anchors, mutable Sobel coefficients and
Perlin lookup tables retain their existing meaning. Canny's angle calculation,
thinning, and hysteresis run on the CPU. Mesh alpha, normals, UV coordinates,
materials, secondary rays, and body ordering also retain CPU processing;
ambiguous candidates use the full CPU traversal.

Custom numeric types, subclass behavior, arbitrary callbacks, unsupported noise
dimensions, and small workloads use the existing CPU implementation. Direct
single-ray mesh intersections and highly selective mesh octrees remain on the CPU
in automatic mode. File I/O, MATLAB COM, GUI work, sequential timelines, and
inexpensive scalar geometry are not GPU workloads. The stateful turbulence model
workflows and scalar four-dimensional noise retain CPU processing.

Programs and bounded device buffers are cached and shared safely across callers.
Every call uploads current input data; public mutable arrays never rely on stale
device copies. Failed GPU operations leave output buffers unchanged before CPU
fallback. `GpuAcceleration.ReleaseResources()` clears caches and permits device
rediscovery. The first call includes driver initialization and compilation;
subsequent calls reuse those resources. Hardware and transfer costs affect the
break-even size, so use the benchmark command below for your workload.
Automatic mode uses separate thresholds for driver startup, each kernel's first
compilation, and subsequent execution. `WarmUp()` initializes the driver only;
each kernel's lower warm threshold applies after its first successful dispatch.
Automatic Perlin and matrix-vector batches require prior driver initialization
through `WarmUp()`, an availability query, or another GPU operation because their
startup cost exceeds the savings from a single batch. For repeated modest batches, using
`PreferGpu` for one representative call prepares that kernel; then return to
`Automatic` for subsequent work.

Measured on this Windows machine's NVIDIA GeForce RTX 4060 Laptop GPU (driver
617.42), using Release builds and the median of three warm calls:

| Operation and input | CPU ms | GPU ms | Speedup |
| --- | ---: | ---: | ---: |
| Double matrix product, 256 × 256 | 155.03 | 1.02 | 152.4× |
| Double matrix-vector product, 4096 × 4096 | 119.58 | 18.15 | 6.6× |
| Convolution, 512 × 512 image, 7 × 7 filter | 219.17 | 5.34 | 41.0× |
| Sobel, 1024 × 1037 image | 347.92 | 15.38 | 22.6× |
| Complete Canny, 1024 × 1037 image | 488.82 | 172.63 | 2.8× |
| Perlin, 131,072 points | 42.22 | 22.08 | 1.9× |
| fBm, 131,072 points, ten octaves | 399.75 | 135.76 | 2.9× |
| Dense mesh, 1024 rays and 1024 facets | 352.29 | 70.67 | 5.0× |

Timings include input packing, transfers, result allocation, and remaining CPU
processing. The mesh comparison uses multicore CPU intersection and excludes
scene initialization and octree construction; the other comparisons use their
existing CPU implementations. GPU dispatch and result equivalence were verified
for every measured case. These are representative workloads, not a promise of
the same speedup for every input or device. Forced GPU execution was slower than
multicore CPU traversal on selective mesh benchmarks, so automatic mode bypasses
meshes whose octree leaves all contain fewer than 128 facets.

First maths dispatches took roughly 2–2.5 seconds including driver setup and
compilation. An isolated driver warm-up took about two seconds, with first kernel
execution adding another 0.17–0.57 seconds. Warm figures exclude those startup
costs; automatic mode accounts for them through its separate thresholds.

## Maths, textures, and turbulence

`Point`, `Vector`, `Normal`, and `Transform` form the shared geometry layer.
Transforms support translation, rotation, scaling, projection, and composition.
`KeyFrame<T>` interpolates keyed values and clamps to the first/last value outside
the time range. Keys require matching value/time arrays and finite, strictly
increasing timestamps. `AddKeys` inserts or replaces a key; `Scale`, `Offset`, and
`RemoveKeys` update the cached value. `KeyFrameTrans` interpolates translation and
scale with quaternion interpolation for rotation.

`Stats.Variance` and `Stats.STD` return `[statistic, mean]` using population
normalization (divide by the selected count). Tagged overloads include only
elements whose tags are true. Empty selections are rejected. `Stats.Mean`
preserves the input numeric type, so integer input uses integer arithmetic; use
double input when a fractional result is needed.

Array variance and standard deviation use shifted online moments to avoid
subtracting large, nearly equal sums of squares. The overloads that accept
precomputed sums cannot recover precision already lost in those inputs. Extreme
quadratic coefficients can overflow intermediate arithmetic; consider these
limits when choosing input scales.

Normals transform with the inverse transpose to remain perpendicular to a
transformed surface. Normalize them when a unit direction is needed; rendering
shapes normalize their surface normals before computing incidence and optics.

Textures either sample a `MapTexture` array or evaluate procedural noise at a
point. Noise implementations include Perlin, fractal Brownian motion (`fBmNoise`),
turbulence, frequency/spectrum noise, and cascades. Materials such as
`TextureValue`, `ProceduralValue`, and `ProceduralMixingValue` attach those results
to physical properties. Frequency ranges must be finite and positive, and
lacunarity (the multiplier between successive frequencies) must exceed one.

The models layer builds on that noise system. `TurbulencePointWFS` evolves a
point's noise coordinates over time; its time step must be positive and finite.
`ReferencePointTurbulence` combines reference points and their associated
turbulence points. `ScatterPointTurbulence` provides a scattered-point field, and
`ReferenceTurbulenceMaterial` makes reference turbulence available at intersections.

## Data and assets

```csharp
var objects = Isidore.Load.OBJ.Load("model.obj");
var cube = Isidore.Library.Models.Cube();
var pixels = Isidore.Load.Load.Bitmap("image.png");
var nastran = Isidore.Load.Load.NAS("mesh.dat");
```

The OBJ reader handles file-wide positive/negative indices, independent position,
texture, and normal indices, objects/groups, comments, whitespace, and continued
lines. Convex polygons are triangulated as a fan. Concave polygons need prior
triangulation. Material declarations (`mtllib`/`usemtl`) are ignored; assign Isidore
materials yourself. Smoothing declarations are ignored, and vertices without
normal data retain the default +Z normal. The NASTRAN reader extracts `GRID` and
ten-node `CTETRA` records and supports free/fixed/large field formatting and
continuation records. Four-node `CTETRA` records are unsupported.

The Tecplot reader handles a limited ordered, single-zone ASCII BLOCK format.
It supports multiline quoted variables, optional `J`/`K` dimensions (default one),
and whitespace/tab/comma-separated numbers, including Fortran `D` exponents.
A blank line or `DT=` header separator is optional. Incomplete or extra data is
rejected. File reads are serialized internally. POINT, unstructured, and
multiple-zone data are unsupported.

The asset library embeds OBJ files and a bitmap through `Resources.resx`. Rebuild
the library after replacing an asset. `Isidore.docx` contains additional original
project documentation. `Isidore_Tests/Inputs` contains example input data;
`Isidore_Tests/OutputData` contains MATLAB plotting/check scripts and some sample
outputs.

## Conventions

- Geometry APIs generally use metres, seconds, and radians; physical properties
  document their units in source comments.
- Image and projector arrays use the first index for X and the second for Y.
  `MatLab.Put`/`Append` transpose the first two dimensions for MATLAB's display
  convention, including the slices of 3D/4D arrays. Higher-rank logical arrays
  transfer as numeric 0/1 slices. `MatLab.Get` accepts row/column vectors and
  empty arrays; matrices must be retrieved through a matrix API.
- `Function.LinearIndex` and `SubscriptIndex` use the first dimension as the
  fastest-varying index. This differs from CLR rectangular-array storage order.
- Many classes expose mutable arrays and lists. Prefer the supplied mutation
  methods for keyframes so cached values are refreshed. Rendering methods advance
  time-dependent state, so initialize a scene before reading world-space geometry.
- Rendering collection/base-reference clones preserve concrete types and copy
  their mutable subtype data. Reference-point association lists intentionally
  retain their referenced point objects. `Point.Clone` called through a maths
  `Point` reference returns a `Point`; use a model's own clone method for model data.

`Isidore.Matlab.Net.GetValue` extracts named fields/properties across an array of
objects. An omitted or empty member path extracts the input values themselves.
Vector values form a rectangular output with shorter/missing values padded by
the output type's default value.

## Build

Use Visual Studio or Visual Studio Build Tools with the .NET desktop build tools
and the **.NET Framework 4.8 targeting pack**. The full solution also requires a
local MATLAB installation with its `MLApp` COM type library registered. Use the
Windows/.NET Framework version of MSBuild from a Developer PowerShell:

```powershell
msbuild .\Isidore.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU"
```

The solution maps `Any CPU` to the original test program's x86 configuration; x64
configurations are also provided. Match the executable platform to the installed
MATLAB COM server when running the original suite. Outputs are in each project's
`bin` directory.

The checked-in `packages.config` files describe legacy Enterprise Library and
Rhino3dmIO references. If those packages are needed, restore them into the
solution's `packages` directory using Visual Studio's NuGet restore or:

```powershell
nuget restore .\Isidore.sln
```

The active core library source does not call those external APIs. Rhino native
binaries are copied to the original test output when the restored package contains
them. The Rhino loader/test implementation is currently inactive.

`dotnet msbuild` cannot build the full solution because its MSBuild runtime does
not support `ResolveComReference` for MATLAB. The embedded bitmap resource also
uses the traditional .NET Framework resource build pipeline.

## Run the checks

For the independent regressions, run from the repository root:

```powershell
.\tests\run-regressions.ps1
```

The script locates Windows MSBuild, builds the actual core library projects and
embedded assets, then runs checks for maths, rendering/materials/cloning, loaders,
turbulence, image processing, and a minimal scene. Failures return a nonzero exit
code. If MSBuild cannot be discovered, pass `-MSBuildPath "C:\path\to\MSBuild.exe"`.
MATLAB is unnecessary for this runner: it compiles the production MATLAB wrapper
source against a test double to verify transfer orientation, MAT-file load calls,
and cleanup. Those checks do not verify the real MATLAB COM server.

For GPU/CPU equivalence checks and end-to-end Release benchmarks:

```powershell
.\tests\run-regressions.ps1 -Configuration Release -RequireGpu -Benchmark
.\tests\run-regressions.ps1 -Configuration Release -CpuOnly
```

Hardware comparisons assert that kernels actually dispatched, then compare
results against CPU execution. `-RequireGpu` fails if no compatible device is
available; ordinary runs skip hardware comparisons on those machines. Benchmark
timings include packing, allocation, transfers, downloads and remaining CPU work.
Maths benchmarks report warm and first-dispatch measurements separately. Image,
noise, and mesh benchmarks report warm measurements; mesh output includes both
single-core and multicore CPU comparisons and automatic-policy dispatch counts.

To run the original demonstrations after building the full solution:

```powershell
.\Isidore_Tests\bin\Debug\Isidore_Test.exe
```

`TopLevel.cs` selects the original test groups. They open MATLAB sessions and some
Windows Forms figures, execute `.m` scripts, and may generate files under
`OutputData`. Several demos rely on additional local scripts/assets (for example,
`ShapeTraceSphereCheck2` and `ShapeTraceSphereCheck3` are referenced but absent from
this checkout). Inspect the selected group before relying on it as unattended
validation. The independent regression runner is the repeatable automated check.
