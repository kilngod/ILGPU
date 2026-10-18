# ILGPUC

ILGPU's AOT compiler toolchain. Compiles GPU kernels at MSBuild time and emits real `CompiledKernel` classes the C# compiler sees instead of your `stream.Launch(...)` lambdas.

## Install

```xml
<ItemGroup>
  <PackageReference Include="ILGPUC" Version="2.0.0-beta1" />
</ItemGroup>
```

ILGPUC declares `ILGPU` (the runtime library) as a transitive dependency, so a single `<PackageReference>` gives the consumer both:

- a runtime reference to `ILGPU.dll` (for `ArrayView<T>`, `Index1D`, `Stream.Launch`, ...)
- the build-time AOT toolchain (`build/` + `tools/` auto-imported by NuGet)

For runtime-only consumption (no AOT, kernels JITted by the GPU runtime at first use), reference `ILGPU` directly:

```xml
<PackageReference Include="ILGPU" Version="2.0.0-beta1" />
```

No additional `<Import>` is needed in either case. The bundled MSBuild integration runs before `CoreCompile` and:

1. Scans `@(Compile)` for `stream.Launch(...)` call sites
2. Compiles each kernel through ILGPUC's IL frontend → IR → optimizer → backend
3. Rewrites the launch site to call the compiled kernel directly
4. Hands `csc` the rewritten sources

Original source files on disk are never modified. `#line` directives map debugger steps back to the original code.

## Configuration

Set in any `<PropertyGroup>` in the consumer's csproj. All optional.

| Property                  | Default | Description                                                                                  |
|---------------------------|---------|----------------------------------------------------------------------------------------------|
| `ILGPUBackend`            | `CPU`   | Target backend(s). Examples: `CPU`, `Metal`, `Cuda`, `CPU Metal`                             |
| `ILGPUBackendOptions`     | *(empty)* | Extra flags. Example: `--simd-width 16`                                                    |
| `ILGPUCompilationOptions` | *(empty)* | Compiler flags. Example: `-opt O2`                                                         |
| `ILGPUCompile`            | `false` | When `true`, ILGPUC invokes the native toolchain (nvcc/hipcc/ocloc/xcrun) and embeds binaries |
| `ILGPUCompilerServices`   | *(empty)* | Semicolon-delimited list of remote `ILGPUC.CompilerService` URLs                           |
| `ILGPURocmCompiler`       | *(empty)* | `local` routes HIP binary compilation to the local HIP 7.x SDK; `auto` selects normal routing |
| `ILGPUCompilerTool`       | *(auto)* | Override the resolved tool path. Auto-resolves to a per-RID R2R native exe or a JIT fallback |

## ROCm/HIP on Windows

Local HIP binary compilation requires a HIP **7.x** SDK and a supported AMD
GPU/driver. Select the GPU architecture explicitly; this example is for the
Radeon RX 9070 XT (`gfx1201`):

```powershell
dotnet .\Bin\Debug\net10.0\ILGPUC.dll probe-services --rocm-compiler local
dotnet build .\Samples\SimpleKernel\SimpleKernel.csproj -p:Configuration=Debug `
  -p:ILGPUBackend=ROCm -p:ILGPUCompile=true `
  -p:ILGPURocmCompiler=local '-p:ILGPUBackendOptions=--rocm-arch gfx1201'
dotnet .\Samples\SimpleKernel\bin\Debug\net10.0\SimpleKernel.dll
```

Run these commands from the repository root after building ILGPU and
ILGPUC. `probe-services` prints the selected compiler path and SDK version.
For a custom SDK, pass `--hipcc-path "C:\path\to\hipcc.exe"` to direct
`compile`, `build`, or `probe-services` commands, or set
`ILGPU_HIPCC_PATH` for MSBuild. An explicit path implies local HIP mode.
The Windows runtime resolves `amdhip64_7.dll` from the configured SDK or
normal system search locations. MSVC v143 14.44 was used for the validated
HIP 7.2 build; select a C++ toolset compatible with your installed SDK.

Only HIP SDK 7.2.60201 on a Radeon RX 9070 XT (`gfx1201`) has been
validated end to end on Windows: `SimpleKernel` produced and ran a native
code object and checked 1,024 outputs. Other HIP 7.x releases, AMD GPUs,
remain unverified. The Windows test-suite result is summarized in
`build/README.md`; Linux test results are recorded separately below. See that
README for discovery order, MSBuild properties, and detailed diagnostics.

## ROCm/HIP on Linux

Linux local compilation and execution were validated on 2026-10-17 on Ubuntu 24.04.4 LTS
(x86_64), kernel `6.8.0-147-generic`, .NET SDK `10.0.111`, ROCm `7.2.4`, and
a Radeon RX 7900 XTX (`gfx1100`). The installed `/usr/bin/hipcc` reports HIP
`7.2.53211` (AMD clang 22.0.0git); ILGPU also found `/opt/rocm/bin/hipcc`
when `/usr/bin` was removed from `PATH`. The `amdgpu` kernel driver was in
use (module vermagic `6.8.0-147-generic SMP preempt mod_unload modversions`);
AMD SMI 26.2.2 reported ROCm 7.2.4. This documents one tested Linux
configuration only.

After building ILGPUC, select the local compiler and the architecture
reported by the HIP runtime, then build and run a sample:

```sh
export ILGPU_ROCM_COMPILER=local
export ILGPU_ROCM_ARCH=gfx1100
mkdir -p codex/temp
export TMPDIR="$PWD/codex/temp"
dotnet Bin/Debug/net10.0/ILGPUC.dll probe-services --rocm-compiler local
dotnet build Samples/SimpleKernel/SimpleKernel.csproj \
  -p:Configuration=Debug -p:ILGPUBackend=ROCm -p:ILGPUCompile=true \
  -p:ILGPURocmCompiler=local \
  '-p:ILGPUBackendOptions=--rocm-arch gfx1100'
ILGPU_SAMPLE_DEVICE=ROCm \
  dotnet Samples/SimpleKernel/bin/Debug/net10.0/SimpleKernel.dll
```

The probe selected `/usr/bin/hipcc` and HIP `7.2.53211-97f5574fe2`. The
sample ran its generated `gfx1100` kernel on the RX 7900 XTX and verified
1,024 results. The Linux ROCm test suite passed 6,110, skipped 1,047, and
failed 0; the ROCm sample matrix passed 30, skipped 3, and excluded 6. Test
and sample skips/exclusions are documented in `Src/ILGPUC.Tests/tests.md`
and `Samples/ROCm.md`. Other Linux SDK/GPU/kernel combinations are not
validated by this result. The remote compiler route remains separate from
local device execution.

### Remote compiler service and Docker

Remote compilation is a separate route. Set `ILGPUCompilerServices` or
the documented service URL environment variables and leave
`ILGPURocmCompiler` unset (or set `auto`). ILGPUC can compile on a Docker
or Linux host while the executing host uses its own compatible AMD runtime
and GPU. Use `probe-services` to inspect the current route; see
`Src/docker/` for the container configuration.

## Performance

ILGPUC ships with ReadyToRun-precompiled native binaries for `osx-arm64`, `linux-x64`, and `win-x64`. The MSBuild targets file picks the right binary for your host RID automatically. Other RIDs fall back to the framework-dependent JIT DLL — same correctness, ~200 ms slower per build. To override:

```xml
<PropertyGroup>
  <ILGPUCompilerTool>dotnet "/path/to/your/ILGPUC.dll" </ILGPUCompilerTool>
</PropertyGroup>
```

## More

- Project page: <http://www.ilgpu.net>
- Samples: <https://github.com/m4rs-mt/ILGPU/tree/master/Samples>
- License: University of Illinois Open Source License (see `LICENSE.txt`)
