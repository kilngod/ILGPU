# Running the samples with ROCm

The samples build kernels for the selected backend and all locally detected
ROCm architectures. At runtime, they list the accelerators discovered by
ILGPU. The default is the first CUDA, ROCm, or Metal device, with larger-memory
devices first; OpenCL follows, then CPU. The default ordering gives CUDA,
ROCm, and Metal equal priority. In an interactive console, use Up/Down to
choose and press Enter; the highlighted choice runs automatically after four
seconds. When input is redirected, the default is selected automatically.
Device entries show their architecture when ILGPU reports it, such as
`gfx1201`. `SimpleViews` intentionally uses CPU-only direct view indexing.

Native binary builds require HIP SDK 7.x, a compatible AMD GPU and driver,
and the host C++ toolchain. By default ILGPUC reads the architecture from
local ROCm devices and targets every distinct architecture it finds. Set
`ILGPU_ROCM_ARCH` to a `gfx` name such as `gfx1201` when cross-compiling or
when no local ROCm device is available. Compiler discovery normally finds
`hipcc`; set `ILGPU_HIPCC_PATH` only for a custom install that discovery
cannot locate.

Sample projects share `Samples/Directory.Build.props`, which defaults their
backend to ROCm on the `rocm_fixes` branch. That setting enables native
compilation and Auto compiler routing without a per-project environment
variable. Set `ILGPUBackend` as an MSBuild property only to override the
solution default; set `ILGPU_ROCM_ARCH` in the build environment to pass an
explicit architecture. Compiler routing is a build-time choice. Docker compiler services
compile kernels remotely; they are not listed as execution devices. After
compilation, the sample prompts for a device available to its process. To run
on a remote datacenter GPU, the sample itself must run on a host or GPU-enabled
container that can access that GPU; the compiler service does not execute
sample kernels.

On Windows, run the sample matrix from the repository root:

```powershell
.\Src\scripts\Invoke-RocmSamples.ps1 -Rebuild
```

Pass `-Architecture gfx1201` only to override discovery or cross-compile.

The script builds each eligible executable with `ILGPUBackend=ROCm`,
`ILGPUCompile=true`, `ILGPURocmCompiler=auto`, and automatic architecture
detection unless `-Architecture` is supplied; it runs against ROCm
noninteractively and verifies the reported backend. Auto compiler mode uses a
discovered local HIP compiler when available, then the configured service or
Docker route. Local execution requires the host's ROCm runtime and GPU. The
script writes build logs, run logs, host/compiler metadata, and `results.json` under ignored
`codex/temp/rocm-samples`.
Pass `-ProjectNames SimpleKernel,FixedSizeBuffers` for a focused run.
The initial full run recorded six algorithm launcher failures. Each reported
“No kernel launch sites found” and produced an empty kernel manifest. The
six samples now use consumer-side expansion so their launches are visible to
ILGPUC. `-IncludeKnownLauncherFailures` remains available for investigating
future known launcher failures; none are currently listed.

In Rider, build the Samples solution with its shared defaults. The shared
sample build file enables native compilation and Auto compiler routing. The build
automatically targets every local ROCm device architecture; no architecture
property is needed on a machine with visible ROCm devices. At runtime, the
sample prompt offers locally available accelerators for which the sample has
compiled kernels. Set `ILGPU_SAMPLE_DEVICE=ROCm` to run a ROCm-specific matrix
noninteractively.

On Linux, install the tested ROCm 7.2.4 SDK and host toolchain and set a
writable `TMPDIR`. The build reads architectures from the HIP runtime. The
Linux host was validated on 2026-10-17: Ubuntu 24.04.4 LTS (x86_64), kernel
`6.8.0-147-generic`, .NET SDK
`10.0.111`, ROCm `7.2.4` / HIP `7.2.53211`, Radeon RX 7900 XTX (`gfx1100`),
and the in-kernel `amdgpu` driver. `/usr/bin/hipcc` resolves to the installed
ROCm SDK; ILGPU also found `/opt/rocm/bin/hipcc` when `/usr/bin` was removed
from `PATH`. Other Linux SDK/GPU/kernel combinations have not been validated.

Restore the sample projects first if they have not been restored, then run
the matrix from the repository root:

```bash
mkdir -p codex/temp
export TMPDIR="$PWD/codex/temp"
export ILGPU_ROCM_COMPILER=auto
while IFS= read -r -d '' project; do
  case "$project" in
    Samples/DeviceInfo/DeviceInfo.csproj|Samples/Empty/Empty.csproj|\
    Samples/EmptyAlgorithms/EmptyAlgorithms.csproj|\
    Samples/SimpleViews/SimpleViews.csproj|\
    Samples/KernelLibraryAttribute/MyKernelLib/MyKernelLib.csproj|\
    Samples/LocalNuGetConsumer/Consumer/Consumer.csproj) continue ;;
  esac
  dotnet restore "$project" || exit 1
done < <(find Samples -name '*.csproj' -print0)
pwsh ./Src/scripts/Invoke-RocmSamples.ps1 -Rebuild
```

The script requires PowerShell 7 on Linux. For an individual sample without
PowerShell, use the equivalent build properties:

```sh
dotnet build Samples/SimpleKernel/SimpleKernel.csproj \
  -p:Configuration=Debug -p:ILGPUBackend=ROCm -p:ILGPUCompile=true \
  -p:ILGPURocmCompiler=auto
dotnet Samples/SimpleKernel/bin/Debug/net10.0/SimpleKernel.dll
```

`SimpleKernel` selected the RX 7900 XTX, loaded and ran its generated
`gfx1100` kernel, and verified all 1,024 results. The full 39-project Linux
matrix recorded **30 passed, 0 failed, 3 skipped, and 6 excluded**. All 33
eligible projects built and their processes exited successfully; passes also
met the sample-specific output or numeric checks. The kernel-library
consumer passed. Build logs, run logs, host/compiler metadata, and
per-project `results.json` are written under ignored
`codex/temp/rocm-samples/`.

Linux skips:

- `MemoryBufferStrides`: GPU `Interop.WriteLine` results are absent with
  default IO.
- `SimpleWriteLine`: GPU output is absent; enabling IO currently fails IR
  rewriting.
- `SimpleDebugAssert`: it asserts on nonzero lanes while assertions are
  disabled in this build.

Linux exclusions:

- `DeviceInfo` enumerates devices without running a kernel.
- `Empty` and `EmptyAlgorithms` only create a context.
- `SimpleViews` uses CPU-only direct `ArrayView` indexing.
- `KernelLibraryAttribute/MyKernelLib` is a dependency; its consumer was run.
- `LocalNuGetConsumer/Consumer` is covered by the separate local-package
  validation workflow.

PowerShell 7 was not installed on the validation host, so the same sequential
restore, native build properties, explicit ROCm selection, and output checks
were run with an equivalent Linux runner. The checked-in PowerShell runner
now recognizes `SimpleKernel`'s explicit 1,024-result success line. These
sample executions used local HIP; remote compiler routing was not part of
the matrix.

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
reasons shown in the table. This is the Windows `gfx1201` result. The Linux
`gfx1100` result is recorded above; CUDA GPU execution was not run in either
sample matrix.

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
types and operations, strided views, pairs, and Linux cases beyond the
recorded `gfx1100` matrix remain unvalidated.

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
