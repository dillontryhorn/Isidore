# Native Capstone license provenance

The license texts in this directory were obtained from the primary Capstone
upstream tag `4.0.2`, without replacing or abridging their terms:

- [LICENSE.TXT](https://raw.githubusercontent.com/capstone-engine/capstone/4.0.2/LICENSE.TXT)
- [LICENSE_LLVM.TXT](https://raw.githubusercontent.com/capstone-engine/capstone/4.0.2/LICENSE_LLVM.TXT)

Gee.External.Capstone 2.3.0 describes support for Capstone 4. Its restored
`runtimes/win-x64/native/capstone.dll` reports API version `4.0` through
`cs_version`. The DLL provides no PE file/product version metadata, and the
NuGet metadata does not identify the native patch version. The `4.0.2` directory
therefore identifies the upstream license snapshot rather than asserting the
exact native patch build.

Verified win-x64 native DLL SHA-256:
`F03321188A1615D044314B1183DCD9A7FFDA09286C31C369D109165D4892DCBB`.

Native attribution is retained in the license texts: COSEINC, Nguyen Anh Quynh,
and the University of Illinois/LLVM contributors. The NuGet wrapper's MIT
license applies separately. The exporter copies these files and this provenance
record into the benchmark output alongside its wrapper notices.
