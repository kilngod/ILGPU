# Running the samples with ROCm

The executable samples accept `ILGPU_SAMPLE_DEVICE=ROCm` to select a local
ROCm device at runtime. An unavailable requested device causes an explicit
error. With the variable unset, each sample keeps its previous device
priority (Metal, CUDA, ROCm, OpenCL, CPU). `SimpleViews` intentionally uses
CPU-only direct view indexing and does not use the selector.

Native binary builds require HIP SDK 7.x, a compatible AMD GPU and driver,
and the host C++ toolchain. Set the architecture to the GPU's `gfx` name;
the Windows run below used `gfx1201`. Compiler discovery normally finds
`hipcc`; set `ILGPU_HIPCC_PATH` only for a custom install that discovery
cannot locate.

On Windows, first create the local Rider/test environment and then run the
sample matrix from the repository root:

```powershell
.\Src\scripts\New-RocmRiderRunSettings.ps1 -Architecture gfx1201
.\Src\scripts\Invoke-RocmSamples.ps1 -Architecture gfx1201 -Rebuild
```

The script builds each eligible executable with `ILGPUBackend=ROCm`,
`ILGPUCompile=true`, `ILGPURocmCompiler=local`, and
`ILGPUBackendOptions=--rocm-arch gfx1201`; it launches with
`ILGPU_SAMPLE_DEVICE=ROCm`. It writes build logs, run logs, host/compiler
metadata, and `results.json` under ignored `codex/temp/rocm-samples`.
Pass `-ProjectNames SimpleKernel,FixedSizeBuffers` for a focused run.
The initial full run recorded six algorithm launcher failures. Each reported
“No kernel launch sites found” and produced an empty kernel manifest. The
six samples now use consumer-side expansion so their launches are visible to
ILGPUC. `-IncludeKnownLauncherFailures` remains available for investigating
future known launcher failures; none are currently listed.

In Rider, use the same four MSBuild properties for a sample project, and
set `ILGPU_SAMPLE_DEVICE=ROCm` in its run/debug configuration. The generated
`codex/rocm-tests.runsettings` is for test execution and provides the
Windows compiler environment for the matrix script.

On Linux, install HIP 7.x and the required host toolchain, set a writable
`TMPDIR`, then run with the Linux AMD card's architecture:

```sh
pwsh ./Src/scripts/Invoke-RocmSamples.ps1 -Architecture gfxXXXX -Rebuild
```

The Linux host in this work has not been configured, so its runtime results
are pending. The script requires PowerShell 7 on Linux. For an individual
sample without PowerShell, use the equivalent build properties:

```sh
dotnet build Samples/SimpleKernel/SimpleKernel.csproj \
  -p:ILGPUBackend=ROCm -p:ILGPUCompile=true \
  -p:ILGPURocmCompiler=local '-p:ILGPUBackendOptions=--rocm-arch gfxXXXX'
ILGPU_SAMPLE_DEVICE=ROCm dotnet Samples/SimpleKernel/bin/Debug/net10.0/SimpleKernel.dll
```

The local route compiles and executes on the same AMD host. To use a remote
compiler, use the existing `ILGPUCompilerServices` property or service URL
with compiler mode `auto`, then select the local ROCm GPU for execution.
The remote route has not been exercised in this sample matrix.

## Sample inventory and Windows results

All entries below target .NET 10. Most projects have no external package or
project dependency. The `Algorithms*` projects use ILGPU's shared algorithm
APIs; the explicit dependencies are shown where relevant. “Passed” means
native build, actual ROCm execution, and the sample-specific output or
numeric checks passed. “Skipped” identifies checks that the sample cannot
currently provide; “Excluded” means the project does not represent a
standalone ROCm kernel execution sample. Each eligible run used local HIP
SDK 7.2 on Radeon RX 9070 XT (`gfx1201`) with MSVC v143 14.44. The
baseline recorded HIP `7.2.60201-38d754472`, display driver
`32.0.31021.6002`, and .NET SDK `10.0.402`.

| Sample | Main feature or dependency | Windows |
| --- | --- | --- |
| AdvancedAtomics | Atomic operations | Passed |
| AdvancedViews | View indexing and slicing | Passed |
| AlgorithmsInitialize | ILGPU algorithm initialization API | Passed after library-launch expansion |
| AlgorithmsMath | Algorithm math functions | Passed |
| AlgorithmsRadixSort | ILGPU radix sort API | Passed after AOT radix expansion |
| AlgorithmsReduce | ILGPU reduction API | Passed after AOT reduction expansion |
| AlgorithmsScan | ILGPU scan API | Passed after two-pass AOT expansion |
| AlgorithmsSequence | ILGPU sequence API | Passed after library-launch expansion |
| AlgorithmsTransform | ILGPU transform API | Passed after library-launch expansion |
| DeviceInfo | Device enumeration only | Excluded: no kernel |
| Empty | Context setup only | Excluded: no kernel |
| EmptyAlgorithms | Algorithm context setup only | Excluded: no kernel |
| ExplicitlyGroupedKernels | Group barriers and predicates | Passed |
| FixedSizeBuffers | Struct field layout | Passed |
| GenericKernel | Generic kernel specialization | Passed |
| GroupGridIndices | Group and grid indices | Passed |
| ImplicitlyGroupedKernels | Automatic grouping | Passed |
| InterleaveFields | Interleaved struct fields | Passed |
| KernelLibraryAttribute/Consumer | `MyKernelLib` project reference | Passed |
| KernelLibraryAttribute/MyKernelLib | Library compiled by Consumer | Excluded: library |
| LocalNuGetConsumer/Consumer | Locally packed `ILGPUC` package | Excluded: separate package workflow |
| MatrixMultiply | Tiled matrix multiplication and shared memory | Passed |
| MemoryBufferStrides | Strided views and GPU output | Skipped: GPU output absent |
| PinnedMemoryCopy | Page-locked host memory and copies | Passed |
| ProfilingMarkers | Event timing | Passed |
| SharedMemory | Group shared memory | Passed |
| SimpleAlloc | Device allocation and disposal | Passed |
| SimpleArrayView2D | Two-dimensional views | Passed |
| SimpleArrayView3D | Three-dimensional views | Passed |
| SimpleAtomics | Integer atomics | Passed |
| SimpleConstants | Constant memory/data | Passed |
| SimpleDebugAssert | Device assertions | Skipped: assertions disabled |
| SimpleKernel | Elementwise kernel and readback | Passed |
| SimpleMath | Device math operations | Passed |
| SimpleStructures | Struct arguments and layout | Passed |
| SimpleViews | CPU-only direct view indexing | Excluded: CPU-only |
| SimpleWriteLine | GPU `Interop.WriteLine` | Skipped: IO output absent |
| StaticAbstractInterfaceMembers | Static interface specialization | Passed |
| WarpShuffle | Warp shuffle and collectives | Passed after IR shuffle fixes |

The initial full Windows run covered 39 projects: **23 passed, 7 failed, 3
skipped, and 6 excluded**. All 33 eligible project builds succeeded with
ROCm native compilation requested. Of those, 25 generated nonempty kernel
manifests. `PinnedMemoryCopy` and `SimpleAlloc` exercise GPU memory APIs
without generated kernels; the six failing algorithm samples generated no
kernels because their launcher sites were not found. Six initial failures were
`NotSupportedException` in
`AcceleratorStream.LaunchAutoGroupedInternal` when an algorithm library
method launches from the precompiled `ILGPU.dll`; the sample's AOT rewrite
does not transform that call site. At the time of that run, `WarpShuffle`
returned each original lane value for ShuffleDown. After the later fixes
below, all six original launcher failures have been addressed individually.

The complete Windows ROCm matrix was rebuilt and rerun after the radix fix:
**30 passed, 0 failed, 3 skipped, and 6 excluded** across 39 projects.
`codex/temp/rocm-matrix-current/results.json` records the per-project
results and links to the build and run logs. The three skips are
`MemoryBufferStrides`, `SimpleDebugAssert`, and `SimpleWriteLine`, for the
reasons shown in the table. This is a Windows `gfx1201` result; the Linux
host and CUDA execution have not been run in this matrix.

The follow-up native ROCm rebuild and GPU run passed all result checks for
`AlgorithmsInitialize` (64 integer and 64 struct values),
`AlgorithmsSequence` (five 64-element sequence patterns), and
`AlgorithmsTransform` (64 transformed structs). `ILGPUC` expands these
single-launch library helpers into named kernels in the consuming source
before normal AOT analysis. It supports simple inline lambdas without
captures and stable view expressions. The initial full-run counts above
remain a historical baseline; the newer full-run counts are recorded above.

A later native ROCm rebuild and GPU run passed `AlgorithmsRadixSort` for
ascending and descending signed 32-bit integers. It checked all 32 values
in each small sort, then compared both directions for 1,025 values with
negative numbers and duplicates against CPU sorting. ILGPUC generates a
stable one-bit radix pass using classification, the existing two-pass scan,
and scatter kernels. This exposes launches in the consumer assembly and
avoids the library method's unavailable AOT launcher. It uses more launches
than the optimized library radix implementation; performance, other element
types and operations, strided views, pairs, and Linux execution remain
unvalidated.

A later native ROCm rebuild and GPU run passed `AlgorithmsReduce` with
noncapturing expression lambdas. The scalar overload returned `2016` for
the 64-element sequence `0..63`; the output-buffer overload returned `1953`
for its 63-element subview. `ILGPUC` generates host-side initialization and
reduction launches in the consuming assembly and uses a normal one-element
buffer for scalar readback, so no temporary allocation manager is required.
Other delegate forms and captured lambdas have not been validated.

A later native ROCm rebuild and GPU run passed `AlgorithmsScan` for both
inclusive and exclusive integer scans with a noncapturing addition lambda.
It checked every value in a 32-element scan and a 1,025-element scan that
crosses group boundaries and has a partial final group. The consumer-side
two-pass implementation computes group totals, scans those totals within
each group, and uses shared memory to distribute the group prefix. It uses
a normal temporary buffer, without requiring an allocation manager. Other
operations, element types, captured lambdas, and Linux execution have not
been validated.

A later native ROCm rebuild and GPU run passed `WarpShuffle` on the Windows
Radeon RX 9070 XT (`gfx1201`). The sample now checks every valid ShuffleDown
lane, a lane-varying struct broadcast, and the warp reduce and scan results.
The IR builder no longer treats `Group.Index` as uniform, and backend
lowering retains built-in shuffle and broadcast nodes. CUDA source generation
was checked for `__shfl_down_sync`; CUDA GPU execution was not available.

`MemoryBufferStrides` and `SimpleWriteLine` use GPU `Interop.WriteLine` as
their observable result, but default binary compilation omits that output;
enabling IO currently fails IR rewriting. `SimpleDebugAssert` intentionally
asserts on nonzero lanes and the default build has assertions disabled.
These are explicit skips rather than passes. The sample matrix is therefore
not yet a fully validated ROCm sample suite.
