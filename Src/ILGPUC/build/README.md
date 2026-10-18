# ILGPU Kernel Build Integration

This directory contains the MSBuild targets file that integrates ILGPUC's Roslyn-based kernel compiler into the standard `dotnet build` pipeline.

## How It Works

When a project imports `ILGPU.Kernels.targets`, the build pipeline gains three additional targets:

1. **ILGPUKernelCompile** — Runs `ilgpuc build` before `CoreCompile`. Scans all `@(Compile)` source files for `stream.Launch()` call sites, compiles the kernels through ILGPUC's pipeline (IL frontend -> IR -> optimizer -> backend codegen), and produces real `CompiledKernel` classes.

2. **ILGPUSwapSources** — Reads the manifest produced by step 1, removes the original source files that were rewritten from `@(Compile)`, and adds the rewritten + generated files. The C# compiler (csc) sees the swapped files instead of the originals.

3. **ILGPUClean** — Removes the `obj/ilgpu/` output directory on `dotnet clean`.

User source files are **never modified on disk**. Rewritten files include `#line` directives so debugging maps back to the original source locations.

## Output Directory Structure

After a build, `$(IntermediateOutputPath)ilgpu/` (typically `obj/Debug/net10.0/ilgpu/`) contains:

```
ilgpu/
  manifest.txt                    # List of original files that were rewritten
  args.rsp                        # Response file passed to ilgpuc
  rewritten/
    Program.cs                    # Rewritten source with CompiledKernel.Launch() calls
    Shaders.cs                    # (one file per rewritten original)
  generated/
    MainKernel_CompiledKernel.cs  # Real compiled kernel classes (per kernel per backend)
    CPUKernelRegistrar.cs         # Kernel registrar (per backend)
```

## Setup

### Option A: NuGet Package (Production)

When ILGPUC is published as a NuGet package, the targets file is automatically imported by any project that references it:

```xml
<PackageReference Include="ILGPUC" Version="1.0.0" />
```

MSBuild auto-imports `build/ILGPUC.targets` (a thin wrapper that imports
`build/ILGPU.Kernels.targets`) from the package. No additional configuration
needed. The wrapper exists because NuGet only auto-imports `build/<PackageId>
.targets` — files with other names live in the package but stay dormant
until explicitly `<Import>`-ed.

### Option B: Local Development

For local development or testing without publishing a NuGet package:

1. **Import the targets file** in your consuming project's `.csproj`:

```xml
<Import Project="/path/to/Src/ILGPUC/build/ILGPU.Kernels.targets" />
```

2. **Set the tool path** to point to your local ILGPUC build:

```xml
<PropertyGroup>
  <ILGPUCompilerTool>dotnet run --project /path/to/Src/ILGPUC -- </ILGPUCompilerTool>
</PropertyGroup>
```

Or, if you've built `ilgpuc` as a standalone tool:

```xml
<PropertyGroup>
  <ILGPUCompilerTool>/path/to/Src/ILGPUC/bin/Debug/net10.0/ilgpuc</ILGPUCompilerTool>
</PropertyGroup>
```

## Configuration

All properties are optional. Set them in a `<PropertyGroup>` in your `.csproj`:

| Property | Default | Description |
|----------|---------|-------------|
| `ILGPUBackend` | `CPU` | Target backend(s). Examples: `CPU`, `Metal`, `Cuda`, `CPU Metal` |
| `ILGPUBackendOptions` | *(empty)* | Extra backend flags passed to `ilgpuc`. Example: `--simd-width 16` |
| `ILGPUCompilationOptions` | *(empty)* | Compilation flags passed to `ilgpuc`. Example: `-opt O2` |
| `ILGPUCompile` | `false` | Invoke the native compiler and embed a binary when `true`. |
| `ILGPUCompilerServices` | *(empty)* | Semicolon-delimited remote compiler service URLs. |
| `ILGPURocmCompiler` | *(empty)* | Set to `local` to route HIP to the local HIP 7.x SDK; `auto` explicitly selects normal routing. Empty honors `ILGPU_ROCM_COMPILER`. Used when `ILGPUCompile=true`. |
| `ILGPUCompilerTool` | `ilgpuc` | Path to the ILGPUC tool. Override for local dev builds. |
| `ILGPUOutputDir` | `$(IntermediateOutputPath)ilgpu/` | Where to write rewritten + generated files |

### Example: Metal backend with optimization

```xml
<PropertyGroup>
  <ILGPUBackend>Metal</ILGPUBackend>
  <ILGPUCompilationOptions>-opt O2</ILGPUCompilationOptions>
</PropertyGroup>
```

### Example: Multiple backends

```xml
<PropertyGroup>
  <ILGPUBackend>CPU Metal</ILGPUBackend>
</PropertyGroup>
```

This generates separate `CompiledKernel` classes and registrars for each backend.

### Windows: compile ROCm kernels with the local HIP SDK

Install a HIP **7.x** SDK and a supported AMD GPU/driver. Use a C++ toolset
compatible with that SDK. On the validated machine, HIP 7.2 compiled with
MSVC v143 14.44; the Visual Studio 2026 default 14.51 headers caused
HIP header conflicts. Determine the GPU's `gfx` architecture from the HIP
runtime rather than copying the value below.

From the repository root, first check which compiler ILGPUC selects:

```powershell
dotnet .\Bin\Debug\net10.0\ILGPUC.dll probe-services --rocm-compiler local
```

The `Hip` line should say `local (available)` and include the selected
`hipcc.exe` path and HIP version. For a custom SDK installation, add
`--hipcc-path "C:\path\to\hipcc.exe"` to this command. That option implies
local HIP selection; it conflicts with `--rocm-compiler auto`. The same
options are available on `ilgpuc compile` and `ilgpuc build`. Without a
CLI path, discovery checks `ILGPU_HIPCC_PATH`, `HIP_PATH`, `ROCM_PATH`,
`PATH`, then installed `C:\Program Files\AMD\ROCm\7.*` SDKs in numeric
version order. A missing tool or a non-7.x explicit tool produces a
diagnostic listing the paths tried.

For an MSBuild consumer, set these properties:

```xml
<PropertyGroup>
  <ILGPUBackend>ROCm</ILGPUBackend>
  <ILGPUBackendOptions>--rocm-arch gfx1201</ILGPUBackendOptions>
  <ILGPUCompile>true</ILGPUCompile>
  <ILGPURocmCompiler>local</ILGPURocmCompiler>
</PropertyGroup>
```

The example's `gfx1201` is specific to the Radeon RX 9070 XT. Replace it
for other GPUs. `ILGPUCompile=true` asks ILGPUC to embed a native code
object. `ILGPURocmCompiler=local` routes HIP to this machine even when a
compiler service is configured. For a custom compiler path in MSBuild,
set `ILGPU_HIPCC_PATH` in the build environment before launching the build.
The CLI `--hipcc-path` option is available for direct `compile`/`build`
invocations. A Windows runtime process also needs access to the matching
`amdhip64_7.dll`; ILGPU searches `ILGPU_HIPCC_PATH`, `HIP_PATH`,
`ROCM_PATH`, `PATH`, and installed 7.x SDK directories.

**Validated on Windows:** HIP SDK 7.2.60201 with a Radeon RX 9070 XT
(`gfx1201`) and the MSVC v143 14.44 toolset. `SimpleKernel` compiled to a
code object, ran on that GPU, and checked 1,024 results. The Windows
`ILGPUC.Tests` suite passed 6,110 tests with 0 failures; 1,107 ROCm-named
cases passed with no ROCm skips. The 1,047 skips were for unavailable CUDA,
Metal, and OpenCL backends. Other HIP 7.x minor releases and GPUs have not
been validated. Linux host validation is tracked separately.

### Linux: compile ROCm kernels with the local HIP SDK

The Linux path was validated on 2026-10-17 on Ubuntu 24.04.4 LTS (x86_64), kernel
`6.8.0-147-generic`, .NET SDK `10.0.111`, ROCm `7.2.4`, and the Radeon RX
7900 XTX (`gfx1100`). `hipcc --version` reports HIP `7.2.53211` and AMD clang
22.0.0git. The system compiler resolves as `/usr/bin/hipcc` to the installed
ROCm SDK; with `/usr/bin` removed from `PATH`, discovery selected
`/opt/rocm/bin/hipcc`. The host uses the in-kernel `amdgpu` driver. This is
one validated Linux host configuration, not a compatibility claim for every
ROCm 7.x release, kernel, GPU, or driver. AMD SMI 26.2.2 reported ROCm
7.2.4; the module vermagic was
`6.8.0-147-generic SMP preempt mod_unload modversions`.

From the repository root, set a writable temporary directory and inspect the
selected compiler:

```sh
mkdir -p codex/temp
export TMPDIR="$PWD/codex/temp"
export ILGPU_ROCM_COMPILER=local
export ILGPU_ROCM_ARCH=gfx1100
dotnet Bin/Debug/net10.0/ILGPUC.dll probe-services --rocm-compiler local
```

The probe reported `/usr/bin/hipcc`, HIP `7.2.53211-97f5574fe2`. To override
discovery, set `ILGPU_HIPCC_PATH=/path/to/hipcc`, `HIP_PATH`, or `ROCM_PATH`.
On Linux, the runtime loads `libamdhip64.so`; this host resolved it from
`/opt/rocm-7.2.4/lib` (SONAME `libamdhip64.so.7`).

Build and run a sample with native ROCm compilation:

```sh
dotnet build Samples/SimpleKernel/SimpleKernel.csproj \
  -p:Configuration=Debug -p:ILGPUBackend=ROCm -p:ILGPUCompile=true \
  -p:ILGPURocmCompiler=local \
  '-p:ILGPUBackendOptions=--rocm-arch gfx1100'
ILGPU_SAMPLE_DEVICE=ROCm \
  dotnet Samples/SimpleKernel/bin/Debug/net10.0/SimpleKernel.dll
```

The sample selected the RX 7900 XTX, loaded and launched the generated
`gfx1100` code object, and verified all 1,024 results. The `ILGPUC.Tests`
suite passed 6,110 tests, skipped 1,047, and had 0 failures on this Linux
host. The eligible sample matrix passed 30, skipped 3 for documented sample
limitations, and excluded 6 non-kernel/separate-workflow projects. See
`Samples/ROCm.md` and `Src/ILGPUC.Tests/tests.md` for detailed results and
commands. Remote compiler routing was covered by focused routing tests and
an in-process mock integration test; this sample matrix used local HIP.

### Architecture selection

For `--cuda-arch`, `--rocm-arch`, and `--metal-arch`, the default `auto`
detects the distinct architectures or GPU families reported by local devices
and creates a backend for each. If the build host has no device for that
backend, ILGPUC uses its conservative fallback target: CUDA `SM_75`, ROCm
`gfx1100`, or Metal `Apple7`. These fallbacks let remote compiler containers
build without GPU passthrough. If the execution GPU is on a different host or
has a different architecture, pass the appropriate explicit option to target
that device. CUDA's PTX ISA remains separately controlled by `--cuda-isa`.

ILGPUC accepts ROCm targets `gfx1100` and newer, plus the explicit older
exceptions `gfx908`, `gfx90a`, `gfx942`, `gfx950`, and `gfx1030`. Local `auto`
detection includes only devices in this set; explicitly requesting another
architecture produces an error. `gfx1100` itself is included.

### Remote compiler service and Docker route

The existing remote service route remains available. Set
`ILGPUCompilerServices` to one or more service URLs and leave
`ILGPURocmCompiler` empty or set it to `auto`. ILGPUC probes explicit
service URLs first, then configured environment URLs, then the localhost
Docker default for non-HIP targets. For HIP in `auto` mode, ILGPUC checks for
a local HIP compiler before the localhost Docker default; an installed local
compiler is selected when available. Explicit service URLs retain precedence.
A remote service can compile on another host, including Linux; the machine
executing the kernel still needs a compatible AMD runtime and GPU. See
`Src/docker/` for the container setup and `probe-services` for the route
selected by the current environment.

### Example: Local development setup

```xml
<PropertyGroup>
  <ILGPUBackend>CPU</ILGPUBackend>
  <ILGPUCompilerTool>dotnet run --project ../ILGPUC -- </ILGPUCompilerTool>
</PropertyGroup>

<Import Project="../ILGPUC/build/ILGPU.Kernels.targets" />
```

## CLI Equivalent

The MSBuild integration invokes the same command you can run manually:

```bash
ilgpuc build \
  --compile-items src/Program.cs src/Shaders.cs \
  --references /path/to/ILGPU.dll ... \
  --output-dir obj/Debug/net10.0/ilgpu/ \
  --project-dir /path/to/project/ \
  -b CPU
```

Use `ilgpuc build --help` for all available options.

## Troubleshooting

- **No kernels found**: If your source files have no supported launch
  calls, the tool writes an empty manifest and exits cleanly. No files are
  swapped. A single-backend native ROCm build expands simple calls to the
  referenced ILGPU `Initialize`, `Sequence`, and `Transform` helpers into
  named kernels before scanning. This requires a stable view expression and
  an inline lambda without captures where a callback is used. Calls to
  multi-stage library algorithms such as `Reduce`, `Scan`, and `RadixSort`
  still have launch sites inside the referenced assembly and require
  cross-assembly AOT specialization; a successful project build alone does
  not prove those algorithms will run.
- **Build errors in rewritten code**: Check `obj/.../ilgpu/rewritten/` to see the rewritten source. The `#line` directives map errors back to original source locations.
- **Response file contents**: Inspect `obj/.../ilgpu/args.rsp` to see exactly what arguments are passed to `ilgpuc`.
- **Temporary-file permission errors**: From the repository root, run
  `& .\Src\scripts\Use-WorkspaceTemp.ps1` in the PowerShell session used
  for the build. It creates the Git-ignored `codex/temp` folder and sets
  process-local `TEMP` and `TMP`; this is useful in restricted development
  environments where HIP COMGR cannot write to the default temp directory.
  HIP compilation checks that it can create and rename a file before
  invoking `hipcc`. If the system temp directory denies renaming, the
  compiler uses a private temporary directory under the current working
  directory or executable directory and points HIP/COMGR there. The directory
  is removed after compilation. If all locations fail, the error lists the
  attempted locations and compiler path.
- **Incremental builds**: The `ILGPUKernelCompile` target uses `Inputs="@(Compile)" Outputs="$(ILGPUManifest)"` for incremental build support. If source files haven't changed, the tool is skipped.
