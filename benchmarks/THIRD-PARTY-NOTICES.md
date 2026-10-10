# Benchmark tooling licensing

BenchmarkDotNet 0.15.8 declares MIT. The benchmark project also directly references
System.Buffers 4.6.1 and System.Numerics.Vectors 4.6.1, both MIT. Its resolved
development-tool dependencies include additional licenses and bundled native
components; their terms must be retained when redistributing benchmark tooling.
These tooling dependencies are separate from the two production performance
packages covered by the repository-root notice.

After restoring the benchmark project, run `benchmarks/export-notices.ps1`. It
generates `benchmarks/bin/Release/net48/THIRD-PARTY-NOTICES.md` and a linked
`notices` directory from the exact restored package graph. Original license and
notice files are copied unchanged, including RTF files. MIT packages without an
embedded license receive the canonical MIT permission text and supplied package
attribution. The two runtime packages also retain .NET Foundation source
attribution and a copy of their runtime source notice. Packages lacking license
information are flagged in the manifest.

System.Runtime.InteropServices.RuntimeInformation 4.0.0 includes Microsoft .NET
Library License terms for the distributed binary. The exporter retains its
actual `dotnet_library_license.txt`; it does not replace those terms with MIT.

Gee.External.Capstone 2.3.0's MIT wrapper bundles a native Capstone 4 engine with
separate BSD-3-Clause and University of Illinois/NCSA notices. The tracked
[Capstone license](licenses/Capstone-4.0.2/LICENSE.TXT) and
[LLVM component license](licenses/Capstone-4.0.2/LICENSE_LLVM.TXT) preserve the
primary upstream 4.0.2 texts. The exporter copies both into the output and links
them from the generated manifest. Include these files and attribution when
redistributing native binaries. The restored win-x64 DLL reports API version
4.0; the exact binary patch version is not identified in its metadata. See the
[provenance record](licenses/Capstone-4.0.2/README.md) for verification and source
URLs. The exporter does not fetch external files.
