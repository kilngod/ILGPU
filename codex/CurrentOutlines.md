# Current Outlines

## Windows hip/ROCm Support Outline

**Status:** Windows steps 1–6 implemented; Steps 7–8 validated for the main
kernel path, with broader Step 7 API coverage outstanding; Windows routing,
documentation, unit-suite, and sample-matrix results are recorded below.
Linux Steps 1–10 are complete in the separate Linux outline.
**Goal:** Let ILGPUC compile ROCm kernels with the Windows HIP SDK installed on
the same machine, then validate that ILGPU can load and execute those kernels
through the Windows HIP runtime. Keep the existing remote compiler route
available for other hosts and backends.

### Current state and design decision

- This machine has the HIP SDK at `C:\Program Files\AMD\ROCm\7.2`. Its `bin`
  directory contains `hipcc.exe`, `hipcc.bat`, `clang++.exe`, and
  `amdhip64_7.dll`. Treat this as the development fixture, not a hard-coded
  installation path.
- `Src/ILGPUC.Compilers/CompilerOptions.cs` permits a HIP compiler path but
  defaults to `/opt/rocm/bin/hipcc`. `CompilerManagerFactory` creates its
  local manager without supplying Windows-aware options and probes a Docker
  service before falling back to local compilation.
- `HipccCompiler` currently invokes `hipcc --genco` and expects an `.hsaco`
  device code object. The exact Windows 7.x output and flags must be proven
  against `hipModuleLoadData` before claiming runtime support.
- `Src/ILGPU/Runtime/ROCm/ROCmAPI.xml` names `amdhip64` on Windows, while the
  installed 7.2 SDK supplies `amdhip64_7.dll`. Confirm how the generated
  import resolves that name and update the Windows binding if necessary.
- Put compiler discovery and routing in `ILGPUC.Compilers`, CLI controls in
  `ILGPUC`, and HIP runtime loading changes in `ILGPU`. Do not make a Docker
  image or WSL a prerequisite for the local Windows route.

### Ordered implementation steps

1. **Establish a Windows baseline (recorded 2026-10-16).** Record the installed SDK version,
   `hipcc.exe --version`, the SDK root, `HIP_PATH`/`ROCM_PATH`/`PATH`, the
   supported GPU architecture, and whether the Windows HIP runtime can
   enumerate the GPU. Inspect the 7.2 compiler's supported `--genco` and
   `--offload-arch` options. Keep these observations separate from code
   assumptions so the implementation remains valid across 7.x releases.

2. **Define one explicit selection switch (implemented).** Add
   `--rocm-compiler auto|local` to ILGPUC's `compile`, `build`, and
   `probe-services` commands. Add `ILGPU_ROCM_COMPILER=local` for callers
   that do not invoke the CLI directly, including test and MSBuild paths.
   Default `auto` retains today's routing. In `local` mode, route only the
   HIP target to the local compiler, even when a ROCm service URL or the
   localhost Docker default is present. Other backend routes continue to
   use their existing precedence. A CLI value overrides the environment.

3. **Make SDK discovery version tolerant within major version 7
   (implemented).** Accept
   an explicit `--hipcc-path` or `ILGPU_HIPCC_PATH` first. Otherwise check
   `HIP_PATH` and `ROCM_PATH` for `bin/hipcc.exe`, then a usable executable
   on `PATH`, then installed `C:\Program Files\AMD\ROCm\7.*` directories.
   Parse numeric versions and choose the highest installed 7.x version;
   never use lexicographic sorting or a literal `7.2` path. Permit a custom
   install root through the explicit path. Reject missing tools and a
   non-7.x SDK with a diagnostic that shows the paths tried. Passing
   `--hipcc-path` should imply local HIP selection or report an explicit
   conflict with another mode.

4. **Wire the selected tool into the existing manager (implemented).** Build
   `CompilerOptions.HipccPath` from discovery and give it to the local
   `CompilerManager` through `CompilerManagerFactory`. Keep path resolution
   centralized rather than duplicating it in each command. Check the
   factory's process-wide caches so a selected mode or path is not hidden
   by an earlier `auto` resolution in the same process. Include the chosen
   executable and detected SDK version in `probe-services` output.

5. **Pass the switch through MSBuild (implemented).** Add a documented property for the
   local ROCm mode to `Src/ILGPUC/build/ILGPU.Kernels.targets` and emit the
   corresponding response-file argument. Preserve the existing
   `ILGPUCompile` and `ILGPUCompilerServices` behavior. Ensure a Windows
   consumer can request a ROCm binary build without setting up a service.

6. **Prove native compilation on Windows (verified 2026-10-16).** Use one small generated ROCm
   kernel and the selected `hipcc.exe` to validate `--genco`, architecture
   flags, quoting of `C:\Program Files\...`, output existence, and code
   object format. Update `HipccCompiler` only where Windows differs; keep
   Linux behavior intact. Report compiler stderr and the chosen path on
   failure. Do not treat `hipcc --version` alone as proof of compilation.

   The generated `BasicMovementKernels.CopyKernel` source is saved at
   `codex/hipcc-probe/input.hip`. Using the installed MSVC v143 14.44
   toolset and `TEMP`/`TMP` set to `codex/hipcc-probe/temp`, the selected
   `C:\Program Files\AMD\ROCm\7.2\bin\hipcc.exe` accepted `--genco` and
   `--offload-arch=gfx1201` and exited 0. Its 9,240-byte output was a
   Clang offload bundle, not a raw HSACO. The bundle's
   `hipv4-amdgcn-amd-amdhsa--gfx1201` member extracted to a 5,144-byte
   `elf64-amdgpu` object (ELF machine `EM_AMDGPU`, architecture
   `EF_AMDGPU_MACH_AMDGCN_GFX1201`). `HipccCompiler` now extracts that
   member when `--genco` returns a bundle; its Linux raw-ELF path is
   unchanged. A full ILGPUC `compile --compile --rocm-compiler local` run
   exited 0 and embedded the same format in the generated kernel class at
   `codex/hipcc-probe/method_CopyKernel_4479_CompiledKernel.cs`. The HIP
   executable path under `C:\Program Files` worked. The default sandbox
   temp directory denied Clang's temporary-file rename, so this probe
   used the explicit `codex` temp directory. Code-object loading and
   kernel execution remain unverified.

7. **Resolve the Windows HIP runtime library (main path validated; broader
   API coverage pending).** Verify whether the existing
   `amdhip64` import loads `amdhip64_7.dll` on this machine. If it does not,
   change the Windows library mapping or loading strategy in the XML/T4
   source and regenerate its C# output. Load from the selected 7.x SDK or
   established system search path without fixing a minor version. Validate
   all native entry points used by device discovery, module loading,
   launching, memory copies, streams, and disposal; account for API or
   structure differences before declaring runtime support.

8. **Exercise one full local path (validated 2026-10-16).** Compile the small kernel, load its
   code object with `hipModuleLoadData`, launch it on a supported Windows
   AMD GPU, copy a result back, and compare it with the expected value.
   This is the gate between compile-only support and end-to-end Windows
   ROCm support. If the SDK cannot produce a loadable code object, record
   that blocker and keep the scope at compile-only until a compatible
   output route is established.

9. **Cover routing and compatibility (Windows checks complete; Linux checks
   complete in its outline).** Add focused checks for Windows
   SDK discovery, explicit path precedence, multiple 7.x installations,
   missing or incompatible SDK diagnostics, and the local-versus-remote
   selection rule. Check that Linux's `/opt/rocm/bin/hipcc` and existing
   remote service behavior still work. Run relevant compile and execution
   checks only on hosts that have the required toolchain or GPU.

10. **Document the user path and limits (implemented for Windows 7.2/gfx1201).** Update the ILGPUC/ROCm usage
    documentation with a Windows example using `--rocm-compiler local`,
    optional `--hipcc-path`, the MSBuild property, SDK 7.x requirement,
    architecture selection, and `probe-services` diagnostics. State which
    Windows SDK versions and GPUs were actually validated. Keep the
    Docker/remote instructions as a separate supported route.

11. **Run the complete ROCm/HIP unit test suite (Windows validated; Linux
    deferred).** Inventory the
    test categories and their current ROCm coverage, including compiler and
    generated-code checks, native compilation, GPU execution, integration
    cases, and IR snapshots. Make the test runner select a ROCm device and
    the host GPU architecture consistently; support local HIP on Windows and
    Linux, with a writable temporary directory and each host's required
    toolchain. Run a small known-good execution test first, then each
    category, then the full suite. Investigate failures in code generation,
    HIP compilation, runtime calls, capabilities, and test assumptions;
    add precise capability skips only for genuinely unsupported behavior
    and preserve CUDA behavior. Record passed, failed, and skipped counts
    separately for the Windows 7.2/gfx1201 machine and the Linux AMD host,
    with SDK, driver, compiler, architecture, and command lines. Require
    real GPU execution and no unexplained failures or skips before calling
    the ROCm suite validated. Keep performance benchmarks as a separately
    reported run if they are outside the ordinary unit test suite.

12. **Build and run the ROCm-capable samples (planned).** Inventory every
    sample's backend selection, build mode, dependencies, and required GPU
    features; identify samples that are intentionally specific to CUDA,
    Metal, OpenCL, or CPU and document their exclusion. Add an explicit way
    for eligible samples to choose ROCm at runtime while preserving their
    existing default selection. Build each eligible project with
    `ILGPUBackend=ROCm`, `ILGPUCompile=true`, `ILGPURocmCompiler=local`,
    and an architecture matched to the host via `ILGPUBackendOptions`;
    provide the local HIP compiler path only when discovery needs it. Run
    the built samples on the Windows and Linux AMD GPUs and check their
    numerical results, assertions, and resource cleanup rather than only
    successful process exit. Fix shared ROCm defects without changing CUDA
    behavior, and report unsupported features with specific reasons.
    Provide a repeatable per-host build/run matrix with pass, fail, and skip
    results; check the remote compiler route separately from local GPU
    execution.

### Step 1: Windows baseline observations (2026-10-16)

These are observations from this workstation. They are not requirements or
hard-coded defaults for other Windows machines or HIP 7.x releases.

| Item | Observation |
| --- | --- |
| SDK root | `C:\Program Files\AMD\ROCm\7.2` |
| SDK version | `bin\.hipVersion` reports `HIP_VERSION_MAJOR=7`, `HIP_VERSION_MINOR=2`, `HIP_VERSION_PATCH=60201`, and Git hash `38d754472`. |
| Compiler version | Direct invocation of `bin\hipcc.exe --version` exits 0 and reports `HIP version: 7.2.60201-38d754472`; Clang `21.0.0git`, target `x86_64-pc-windows-msvc`. |
| `HIP_PATH` | `C:\Program Files\AMD\ROCm\7.2\` |
| `ROCM_PATH` | Unset in the current process. |
| `PATH` | The SDK's `7.2\bin` is absent, and neither `hipcc` nor `hipInfo` resolves by command name. An unrelated `C:\Users\James\AppData\Local\LM-Studio\app-0.2.16\resources\app\.webpack\main\build\Release\ROCm\bin` entry is present. All probes below used the SDK's absolute executable paths. |
| GPU | `hipInfo.exe` reports device 0 as `AMD Radeon RX 9070 XT`, architecture `gfx1201` (major 12, minor 0), with 15.92 GB global memory. [AMD's Windows HIP SDK support list](https://rocm.docs.amd.com/projects/install-on-windows/en/develop/reference/system-requirements.html) lists this model and architecture as supported for the runtime and HIP SDK. |

**Compiler option inspection.** `hipcc.exe --help` and `--help-hidden`
complete successfully and list `--offload-arch=<value>`, including the
`native` form. Neither help output lists `--genco`. Invoking `hipcc.exe
--genco` without a source file does not produce an unknown-option error: it
dispatches Clang with `--cuda-device-only` and exits 1 with `no input files`.
This establishes that the installed wrapper recognizes `--genco`, but does
not establish successful code-object generation. Step 6 will check an
actual kernel and explicit `--offload-arch=gfx1201`.

**Runtime enumeration.** Direct invocation of `bin\hipInfo.exe` prints the
GPU and architecture above, then exits 1 with `HIP API error = 0001
"invalid argument"` at `hipInfo.cpp` line 180. Device enumeration and
basic property reporting therefore work on this installation; the utility
does not complete successfully. The failing API and kernel execution
remain to be investigated in Steps 7–8.

**Implications for later steps.** Discover the compiler from configured
paths and installed SDKs rather than assuming it is on `PATH`. Treat the
observed `gfx1201` and `amdhip64_7.dll` as validation targets on this
machine, not universal requirements for HIP 7.x. Keep compilation and
runtime execution as separate gates.

### Step 7: Windows HIP runtime binding (2026-10-16)

The installed SDK contains `bin\amdhip64_7.dll`. In a 64-bit process,
loading `amdhip64` or `amdhip64_7` by name failed because the SDK `bin`
directory is not on `PATH`; loading the full 7.2 DLL path succeeded. The
Windows XML import now names the major-version DLL (`amdhip64_7`), and
the regenerated T4 output uses that name only on Windows. The resolver
looks beside `ILGPU_HIPCC_PATH`, then under `HIP_PATH` and `ROCM_PATH`,
then on `PATH`, then in installed `Program Files\AMD\ROCm\7.*` roots in
numeric version order. If none load, normal system DLL search still runs.
No minor version is embedded in the source.

The SDK DLL exports all 28 entry points declared in `ROCmAPI.xml`, covering
discovery, module loading and unloading, kernel launch, memory, streams,
occupancy, and peer access. The installed 7.2 header confirms
`sizeof(hipDeviceProp_tR0600) == 1472` and `gcnArchName` at byte 1160.
ILGPU builds cleanly for `net10.0`, its runtime API initializes, and
`ROCmDevice.GetDevices()` finds the RX 9070 XT with architecture `gfx1201`.
The same discovery succeeds with `HIP_PATH`, `ROCM_PATH`, and
`ILGPU_HIPCC_PATH` unset, exercising the installed-SDK fallback.

Runtime execution is **not yet validated**. A standalone 64-bit native
probe returned success from `hipInit` and `hipSetDevice`, then timed out
after 30 seconds in `hipStreamCreate`; the stream-only probe reproduced
the timeout without ILGPU device discovery. A small native C++ HIP program
compiled with the installed SDK and MSVC v143 independently returned
`hipInit=0` and `hipGetDeviceCount=0 count=1`, then also hung in
`hipStreamCreate`. The stream failure therefore occurs outside ILGPU and
.NET on this workstation. A separate `hipModuleLoadData`
attempt using Step 6's extracted HSACO terminated with access violation
`0xC0000005`. These outcomes do not establish whether the cause is the
driver, code object, or another SDK compatibility issue. Stream creation,
memory copies, module loading, launching, and disposal still need
successful call-level validation before claiming Windows runtime support.
The focused probe sources and outputs are under `codex/hipcc-probe`.

After reinstalling the display driver on 2026-10-16, Windows reports
`32.0.31021.6002` for the RX 9070 XT. Its active INF identifies an AMD
Software PRO Edition `26.10.21.06` package; the matching Driver Store
`amdhip64_7.dll` and SDK DLL both report file version `10.0.3679.0`.
The standalone native probe still enumerates one device and hangs in
`hipStreamCreate` when the SDK DLL is loaded from beside the executable
and the process `PATH` starts with the SDK's `7.2\bin`. The persistent
Windows user `Path` still refers to `7.1\bin`/`7.1\lib`, not `7.2\bin`;
that needs updating for ordinary command-line use but does not explain
this controlled stream-creation repro. See `HipHeaderAudit.md` for details.

After the user corrected Windows `Path`, the persistent machine path now
contains `7.2\bin` and `7.2\lib` and no stale 7.1 entries. Rerunning the
native probe with the SDK DLL beside the executable still timed out in
both `hipStreamCreate` and `hipStreamCreateWithFlags` after successfully
enumerating one GPU. ILGPU's .NET discovery probe still finds the
`gfx1201` device. Native stream creation appeared to be the execution
blocker at that stage; the cause was identified after reboot.

**Post-reboot update (2026-10-16):** Verbose HIP logging showed COMGR's
linker could not write its temporary `output\a.so` under the Codex
sandbox's default temporary directory (`permission denied`). With
process-local `TEMP` and `TMP` pointed at writable `codex\temp`, the
native probe passed ordinary and nonblocking stream creation and
destruction. The .NET probe passed device discovery, stream operations,
allocation, memset, host/device copy and 16-byte roundtrip, module load,
function lookup, unload, free, and synchronization. This removes the
previous stream blocker for this controlled environment. A zero-grid
launch returned the expected invalid-value error, so successful kernel
execution is still unverified. A clean ROCm build of `SimpleKernel`
currently reports `No kernel launch sites found`; investigate that as a
separate ILGPUC/sample issue before claiming an ILGPU sample run.

**Launch-site fix and first kernel run (2026-10-16):** The launch-site
analyzer is shared by CUDA and ROCm. Unquoted Windows `Program Files`
paths in the MSBuild response file dropped `System.Runtime.dll` from
Roslyn's references, so it could not resolve `stream.Launch`.
`ILGPU.Kernels.targets` now quotes source, reference, output, and project
paths. The same source finds one launch site with both CUDA and ROCm.
Using the HIP 7.2 SDK, `--rocm-arch gfx1201`, MSVC v143 14.44, and
writable process-local `TEMP`/`TMP`, a binary ROCm build of
`SimpleKernel` succeeded. The sample ran on the RX 9070 XT and completed
its 1,024-element result check without an error. Other samples and the
broader test library remain to be validated.

### Step 9: routing and compatibility checks (2026-10-16)

`HipccDiscoveryTests` simulate Windows SDK layouts without installed tools:
explicit path precedence, `ILGPU_HIPCC_PATH`, `HIP_PATH`, `ROCM_PATH`,
`PATH`, numeric selection of 7.10 above 7.2, missing and incompatible
SDK diagnostics, and Linux's `/opt/rocm/bin/hipcc` fallback. The
production discovery path uses the same selection logic with real
environment, filesystem, and `hipcc --version` inputs.

`RocmRoutingTests` simulate service capabilities and check explicit
remote URLs, trusted environment URLs, the Docker default, `local` HIP
override while CUDA stays remote, explicit `auto` overriding an
environment `local`, and `--hipcc-path` selection/conflict. The existing
remote-service and composite-routing tests still pass.

On this Windows machine, all 12 focused checks and 8 existing remote
checks passed. `probe-services --rocm-compiler local` selected HIP 7.2
from `C:\Program Files\AMD\ROCm\7.2\bin\hipcc.exe`. A fresh
`SimpleKernel` binary build for `gfx1201` succeeded with MSVC v143 14.44,
and the sample ran on the RX 9070 XT, completing its 1,024-element check.
The Codex sandbox required process-local writable `TEMP` and `TMP`.

The synthetic Linux default-path check is not a live Linux compilation.
On the Linux AMD host, run the focused tests and probe, then build and run
the sample using that card's reported `gfx` architecture:

```sh
dotnet test Src/ILGPUC.CompilerService.Tests/ILGPUC.CompilerService.Tests.csproj \
  --filter 'FullyQualifiedName~HipccDiscoveryTests|FullyQualifiedName~RocmRoutingTests|FullyQualifiedName~RemoteCompilerIntegrationTests'
dotnet Bin/Debug/net10.0/ILGPUC.dll probe-services --rocm-compiler local
dotnet build Samples/SimpleKernel/SimpleKernel.csproj -p:Configuration=Debug \
  -p:ILGPUBackend=ROCm -p:ILGPUCompile=true \
  -p:ILGPURocmCompiler=local '-p:ILGPUBackendOptions=--rocm-arch gfxXXXX'
dotnet Samples/SimpleKernel/bin/Debug/net10.0/SimpleKernel.dll
```

Replace `gfxXXXX` with the architecture reported by the Linux HIP runtime.
The Linux host must have a usable HIP 7.x toolchain and AMD GPU for the
last two commands.

### Step 10: documentation and test readiness (2026-10-16)

`Src/ILGPUC/build/README.md` and `Src/ILGPUC/PACKAGE.md` now document
`probe-services --rocm-compiler local`, optional `--hipcc-path` or
`ILGPU_HIPCC_PATH`, the `ILGPURocmCompiler` MSBuild property, HIP 7.x
discovery, `--rocm-arch`, Windows runtime resolution, and the separate
remote/Docker route. The baseline was HIP SDK 7.2.60201, RX 9070 XT
(`gfx1201`), MSVC v143 14.44, and one successful `SimpleKernel` run.

`Src/ILGPUC.Tests/tests.md` now gives ROCm test commands. The test harness
accepts `ILGPU_ROCM_ARCH`, selects a ROCm device explicitly for execution
tests, and writes debug artifacts under its writable test temp directory.
`Src/scripts/Use-WorkspaceTemp.ps1` creates and selects the existing
Git-ignored `codex/temp` for the current PowerShell process. With
`ILGPU_ROCM_COMPILER=local`, `ILGPU_ROCM_ARCH=gfx1201`, the installed HIP
7.2 toolchain, and writable temp, the focused
`ROCm_BasicMovementExecutionTests.Copy_ProducesCorrectOutput` test passed
on this Windows machine. The complete Windows results appear below.

### Step 11: Windows unit suite (2026-10-16)

`Src/scripts/New-RocmRiderRunSettings.ps1` writes Git-ignored
`codex/rocm-tests.runsettings` with the local HIP 7.2 compiler, RX 9070 XT
`gfx1201` architecture, compatible MSVC v143 14.44 environment, and a
writable `codex/temp` directory. Rider can use it as a custom test settings
configuration for GUI run/debug. The test harness also detects the local
ROCm device architecture when `ILGPU_ROCM_ARCH` is unset. ROCm MSBuild
integration rows now cover HelloKernel, MultiKernel, backend properties,
and four sample-family builds.

The final Windows command was `dotnet test
Src/ILGPUC.Tests/ILGPUC.Tests.csproj --no-restore --no-build
-p:Configuration=Debug -p:TargetFramework=net10.0 -m:1 -nr:false
--settings codex/rocm-tests.runsettings` with a TRX logger and
`ILGPU_UPDATE_IR` unset. It passed 6,110, failed 0, and skipped 1,047.
The TRX contains 1,107 ROCm-named passes and no ROCm failures or skips;
the skipped cases target unavailable CUDA, Metal, and OpenCL backends.
The separate `ILGPUC.CompilerService.Tests` project passed 43/43. See
`codex/ROCmUnitTestResults.md` for the category matrix and exact result
files. The optional `CompileBenchFacts` benchmark was not run.

HIP compilation now retries in a private working-directory temp folder
when the system temp denies file renaming, and points HIP/COMGR at the
selected folder. A direct compile succeeded in the restricted sandbox
with the system temp rename denied; the private folder was cleaned up.
The Linux test suite and its per-layer counts are now recorded in the
separate Linux HIP/ROCm outline. Other HIP SDK versions and AMD GPUs remain
unvalidated.

### Step 12: Windows sample matrix (2026-10-16)

`Samples/SampleDeviceSelector.cs` gives executable samples an explicit
`ILGPU_SAMPLE_DEVICE=ROCm` runtime choice while preserving their prior
selection order when unset. `Samples/Directory.Build.targets` shares the
selector among executable projects. `Src/scripts/Invoke-RocmSamples.ps1`
builds with `ILGPUBackend=ROCm`, `ILGPUCompile=true`,
`ILGPURocmCompiler=local`, and `ILGPUBackendOptions=--rocm-arch gfx1201`,
then selects the ROCm GPU and checks output. It writes ignored logs and
`results.json` under `codex/temp`; `Samples/ROCm.md` documents the full
39-project inventory, Rider setup, and both host commands.

The full Windows 7.2/RX 9070 XT/`gfx1201` run built all 33 eligible
executables with native ROCm compilation requested; 25 generated nonempty
kernel manifests. Six algorithm samples generated no kernels because their
launcher sites were not found in the sample compilation. Two passing
memory API samples intentionally generated no kernel. Results were
**23 passed, 7 failed, 3 skipped, 6 excluded**. A ROCm-only field address correction fixed
`FixedSizeBuffers` and `InterleaveFields`; ROCm group predicate barrier
emission fixed `ExplicitlyGroupedKernels`. `PinnedMemoryCopy` now verifies
nonzero round trips and disposes its page-locked result.

Six algorithm API samples fail in the precompiled library's
`AcceleratorStream.LaunchAutoGroupedInternal` because their launcher sites
are outside the sample's AOT rewrite. `WarpShuffle` returns an incorrect
ShuffleDown value; shared IR construction treats `Group.Index` as uniform.
The IO-dependent `MemoryBufferStrides` and `SimpleWriteLine` lack device
output with default settings, and IO-enabled compilation currently fails
IR rewriting. `SimpleDebugAssert` has assertions disabled. Routine reruns
initially skipped the six known unhandled algorithm exceptions unless
`-IncludeKnownLauncherFailures` is specified. The original failures are
preserved in `codex/temp/rocm-samples-final/results.json`.

This paragraph records the earlier Windows matrix before the follow-up fixes;
the current Windows result is given below. A live remote-compiler sample
build remains pending. Linux local-compiler and GPU results are recorded in
the separate Linux HIP/ROCm outline below.

Follow-up: `ILGPUC` now expands simple `Initializer`, `Sequencer`, and
`Transformer` library calls into named kernel methods in the consumer's
source before AOT analysis. A fresh native HIP rebuild and RX 9070 XT run
passed `AlgorithmsInitialize`, `AlgorithmsSequence`, and
`AlgorithmsTransform` with numerical output checks. At that stage, the
sample runner skipped `AlgorithmsRadixSort` and `AlgorithmsScan` by default
to avoid known unhandled launcher exceptions. Those multi-stage library
helpers required a cross-assembly specialization and
rewriting path. The initial full-run counts above remain historical; no new
full matrix has been claimed.

Further follow-up: `AlgorithmsReduce` now passes native compilation and GPU
execution for noncapturing expression lambdas. Its scalar and output-buffer
overloads returned `2016` and `1953` for 64- and 63-element inputs on the
Windows RX 9070 XT. `WarpShuffle` also passes after the IR shuffle fixes.
`AlgorithmsScan` now also passes native compilation and GPU execution for
inclusive and exclusive integer scans with a noncapturing addition lambda.
The 32- and 1,025-element checks cover group boundaries and a partial final
group. `AlgorithmsRadixSort` now passes native compilation and actual GPU
execution with 32-element ascending/descending checks and 1,025 signed
integers with negatives and duplicates, checked against CPU sorting in both
directions. ILGPUC expands dense radix calls into stable one-bit passes
using classification, scan, and scatter kernels. The sample runner has no
remaining known-launcher skips. Other radix overloads, performance, CUDA
execution are not validated by this Windows run. Linux ROCm execution is
recorded in the separate Linux outline below. The initial matrix counts above
remain historical.

The full Windows ROCm sample matrix was rebuilt after these fixes. Its
current result is 30 passed, 0 failed, 3 skipped, and 6 excluded across 39
projects (`codex/temp/rocm-matrix-current/results.json`). The skips are
`MemoryBufferStrides` and `SimpleWriteLine` (GPU `Interop.WriteLine` output
is unavailable with the default build), plus `SimpleDebugAssert`
(assertions disabled). Linux execution is recorded in its separate outline;
CUDA GPU execution remains pending.

### Completion criteria

- Local mode selects the installed Windows HIP SDK without WSL, Docker, or
  an HTTP compiler service and reports the selected executable and version.
- Discovery works with a non-default install path and across compatible
  7.x minor versions; the observed 7.2 installation is one passing case.
- At least one generated kernel compiles to a code object. Full runtime
  support is claimed only after that code object loads and executes on a
  supported Windows AMD GPU.
- Existing Linux local compilation and remote service selection remain
  available, and documentation states any unverified platform or GPU cases.

## Linux HIP/ROCm Support Outline

**Status:** Complete for the tested Linux host. Steps 1–10 are complete: Linux baseline, ROCm toolchain,
compiler routing, native compilation, runtime loading/ABI, end-to-end kernel
execution, focused routing checks, layered/full test suites, and the eligible
sample matrix, with per-host configuration and outcomes documented. ROCm
7.2.4 reports the RX 7900 XTX as `gfx1100`; other Linux SDK/GPU combinations
and remote-compiler sample execution remain unvalidated.
**Goal:** Use the ROCm SDK installed directly on this Linux machine to compile
ILGPU HIP kernels and run them on its AMD GPU. Keep the existing remote
compiler route available and preserve Windows behavior.

### Current state and design

- The host is Ubuntu 24.04.4 LTS, x86_64, with kernel
  `6.8.0-147-generic`; .NET SDK `10.0.111` is installed.
- AMD's ROCm 7.2 Linux system-requirements page lists Ubuntu 24.04.4 with
  kernels 6.8 (GA) or 6.17 (HWE); this host now runs the listed 6.8 GA
  kernel. AMD also notes that multi-GPU systems can require `iommu=pt`.
- PCI inventory reports a Sapphire Pulse Radeon RX 7900 XTX
  (`1002:744c`, Navi 31) using the `amdgpu` kernel driver. The Ryzen 9 9900X
  integrated graphics (`1002:13c0`) no longer appears after the user disabled
  it in BIOS. Live `rocminfo` output lists one GPU,
  `Radeon RX 7900 XTX`, architecture `gfx1100`; the CPU agent appears as a
  separate HSA agent. AMD's ROCm guidance says to disable AMD integrated
  graphics in BIOS before using ROCm because enumerating an IGP may destabilize
  the runtime, even if `HIP_VISIBLE_DEVICES` omits it.
- The user initially described the card as “9700XTX”; the PCI inventory
  identifies a Radeon RX 7900 XTX. The user confirmed this is the intended
  accelerator, and ROCm reports it as `gfx1100`.
- ROCm 7.2.4 is installed under `/opt/rocm-7.2.4`; `hipcc` and `rocminfo`
  resolve through `/opt/rocm` to that installation. `hipcc --version` reports
  HIP 7.2.53211 and AMD clang 22.0.0git (ROCm 7.2.4). `libamdhip64.so` and
  `libhsa-runtime64.so` resolve from `/opt/rocm-7.2.4/lib`. `hipconfig --full`
  reports HIP platform `amd`, compiler `clang`, runtime `rocclr`, and ROCm path
  `/opt/rocm-7.2.4`. `HIP_PATH`, `ROCM_PATH`, `ILGPU_HIPCC_PATH`,
  `ILGPU_ROCM_COMPILER`, `ILGPU_ROCM_ARCH`, and `LD_LIBRARY_PATH` are unset in
  the captured shell environment; record this separately from `hipconfig`'s
  computed defaults.
- The codebase already has a Linux compiler fallback at
  `/opt/rocm/bin/hipcc`; `HipccDiscovery` also checks explicit paths,
  `ILGPU_HIPCC_PATH`, `HIP_PATH`, `ROCM_PATH`, and `PATH`. Local HIP routing
  via `ILGPU_ROCM_COMPILER=local` is implemented. Linux selection is now
  verified: the default shell resolves `/usr/bin/hipcc`, which links to the
  installed ROCm 7.2.4 compiler.
- Linux runtime imports currently use `amdhip64`. Verify the SDK's soname,
  linker-cache visibility, exported symbols, and ABI before changing runtime
  bindings. Do not carry the Windows `amdhip64_7.dll` mapping into Linux.
- Do not assume an architecture string from the product name. Query the live
  ROCm device and use its reported `gfx` architecture for native builds and
  tests.

### Ordered implementation and validation steps

1. **Record the Linux baseline (complete).** Capture Ubuntu release, kernel, CPU
   architecture, .NET SDK, GPU PCI IDs/model, kernel driver, active display
   device, and GPU selection policy. Once ROCm is present, record the exact
   SDK/compiler/driver versions, `rocminfo` architecture and device count,
   `hipcc --version`, environment variables, and compiler/runtime library
   paths. Keep observations separate from code defaults.

2*. **Verify the installed Linux ROCm toolchain (complete).** AMD's
   [ROCm 7.2.4 system requirements](https://rocm.docs.amd.com/projects/install-on-linux/en/docs-7.2.4/reference/system-requirements.html)
   list Ubuntu 24.04.4 on kernel 6.8 GA and the RX 7900 XTX as supported;
   the live device reports `gfx1100`. The installation is the native APT
   package method: `/etc/apt/sources.list.d/rocm.list` points to AMD's 7.2.4
   ROCm and graphics repositories, and `apt-cache policy` shows AMD repo
   priority 600. This matches AMD's
   [Ubuntu installation instructions](https://rocm.docs.amd.com/projects/install-on-linux/en/docs-7.2.4/install/install-methods/package-manager/package-manager-ubuntu.html).
   Installed package metadata reports `rocm` and `rocm-core`
   `7.2.4.70204-93~24.04`, `hip-runtime-amd` `7.2.53211.70204-93~24.04`,
   `hipcc` `1.1.1.70204-93~24.04`, and `rocminfo`
   `1.0.0.70204-93~24.04`. `hipcc --version` reports HIP 7.2.53211 and AMD
   clang 22.0.0git from ROCm 7.2.4. Live `rocminfo` reports the Ryzen 9 9900X
   CPU and one GPU, the Radeon RX 7900 XTX at `gfx1100`. AMD SMI reports
   ROCm 7.2.4 and the in-kernel `amdgpu` driver on kernel
   `6.8.0-147-generic`. The Linux RuntimeProbe returned success for
   `hipInit`, stream creation, and stream destruction, and ILGPU discovery
   found that same GPU. This verifies the installed host runtime; generated
   ILGPU kernel compilation and execution remain later gates.

3. **Verify compiler selection and routing (complete).** Built ILGPUC in
   Debug with zero warnings/errors. `probe-services --rocm-compiler local`
   reports HIP `local (available)`, `/usr/bin/hipcc`, and HIP
   `7.2.53211-97f5574fe2`. Live checks selected the path supplied via
   `--hipcc-path`, `ILGPU_HIPCC_PATH`, `HIP_PATH`, and `ROCM_PATH` in turn;
   with those variables unset, `PATH` selected `/usr/bin/hipcc`, and with
   `PATH` excluding `/usr/bin`, Linux's `/opt/rocm/bin/hipcc` fallback was
   selected. Each resolved to the installed HIP 7.2.53211 compiler. The
   `probe-services --rocm-compiler auto` check fell back locally because no
   remote compiler service is available on this host. The focused
   `HipccDiscoveryTests` and `RocmRoutingTests` passed (12/12): they cover
   explicit path precedence, `ILGPU_ROCM_COMPILER`, local HIP overriding a
   configured remote HIP route while leaving CUDA remote, explicit `auto`,
   and the Docker-default remote route. No live remote service is configured;
   remote-routing behavior is covered by these routing tests.

4. **Prove native compilation (complete).** Built `Samples/SimpleKernel` with
   ILGPU's kernel precompile target, using `ILGPUBackend=ROCm`,
   `ILGPUCompile=true`, `ILGPURocmCompiler=local`, and
   `--rocm-arch gfx1100`. The generated kernel source was passed to
   `/usr/bin/hipcc` with `--genco`, `-O2`, and
   `--offload-arch=gfx1100`; ROCm supplied its configured HIP headers and
   include paths. HIP/COMGR wrote to a writable
   `/tmp/gpu-compilation-<id>` directory, then ILGPU validated the returned
   binary as AMDGPU ELF and embedded it in
   `Samples/SimpleKernel/obj/Debug/net10.0/ilgpu/generated/MyKernel_CompiledKernel.cs`.
   The sample build succeeded with zero errors (one existing generated-code
   hiding warning). Decoding that embedded binary produced a 5,040-byte ELF64
   AMDGPU shared object: `readelf` reports AMD HSA ABI, AMD GPU machine, and
   ELF flags `gfx1100`; its metadata contains
   `amdgcn-amd-amdhsa--gfx1100` and no `gfx1201`. The compiler's `--genco`
   output on this ROCm 7.2.4 Linux installation was a Clang offload bundle,
   so Linux also needed bundle-member extraction. That first exposed a
   bundler lookup bug: `/usr/bin/hipcc` is a symlink and the bundler lives in
   the resolved ROCm installation's `llvm/bin`, not beside the symlink. The
   compiler now resolves the hipcc symlink and looks in the ROCm LLVM bin
   locations; the same sample then compiled successfully. Raw ELF output
   still bypasses extraction and is validated directly. The Windows extraction
   path remains platform-specific only in its bundler executable suffix. This
   proves native compilation and code-object architecture; it does not yet
   prove loading or launching the kernel on the GPU (step 6).

5. **Audit Linux runtime loading and ABI (complete).** No Linux binding change
   was indicated. With HIP/ROCm environment overrides unset, the system loader
   resolves `libamdhip64.so` to `/opt/rocm-7.2.4/lib/libamdhip64.so`; its SONAME
   is `libamdhip64.so.7`, and `ldd` resolved all listed dependencies, including
   ROCm's `libhsa-runtime64.so.1`. `ROCmAPI.xml` uses `amdhip64` for Linux,
   while Windows retains `amdhip64_7`. All 28 imported function names in the
   XML are exported by the installed library. The Linux `RuntimeProbe` loaded
   ILGPU's actual ROCm API (`API supported: True`), and `ROCmDevice.GetDevices`
   found exactly one Radeon RX 7900 XTX at `gfx1100`. It exercised the imported
   `hipInit`, device-count, `hipDeviceGetName`, and versioned device-properties
   calls. The direct HIP probe also returned success for `hipInit`, stream
   creation, and stream destruction when run with GPU device access.

   Compared the installed `/opt/rocm/include/hip/hip_runtime_api.h` definition
   of `hipDeviceProp_tR0600` against ILGPU: both report size 1,472 bytes,
   `gcnArchName` offset 1,160, and `asicRevision` offset 1,468. ROCm's header
   also reports 8-byte `hipStream_t` and `size_t`, 4-byte `hipError_t` and
   device-attribute enum, matching ILGPU's `IntPtr`/`UIntPtr`, `ROCmError`, and
   `HipDeviceAttributeKind` mappings on this x86_64 host. The imported HIP API
   uses the platform C ABI; successful calls through ILGPU's generated
   `LibraryImport` bindings validate the Linux calling convention for these
   exercised signatures. The audit's checks were limited to discovery,
   properties, and stream creation/destruction; step 6 below records the
   separate end-to-end allocation/copy/module/kernel run.

6. **Pass an end-to-end kernel gate (complete).** Rebuilt
   `Samples/SimpleKernel/SimpleKernel.csproj` with
   `ILGPUBackend=ROCm`, `ILGPUCompile=true`,
   `ILGPURocmCompiler=local`, and `--rocm-arch gfx1100`, then ran it with
   `ILGPU_SAMPLE_DEVICE=ROCm` and GPU device access. It selected the Radeon RX
   7900 XTX, loaded the compiled `gfx1100` code object, allocated a device
   buffer, copied initialized host data to the GPU, launched `MyKernel`,
   synchronized, copied results back, and compared every element with
   `index + 42`. It printed `Verified all 1024 kernel results.` and exited
   successfully. The sample now throws on any mismatch so the gate cannot
   silently pass with incorrect output. Its `using` scopes disposed the buffer,
   accelerator, and context, exercising associated stream and loaded-kernel
   cleanup. The build had zero errors and one existing generated-code warning
   (`GeneralAcceleratorType` hides an inherited member). This passes the
   focused native compilation plus execution gate. The broader test and
   sample coverage was completed in steps 7–9 below; remote compiler sample
   execution and other Linux SDK/GPU combinations remain unvalidated.

7. **Run focused routing and compatibility checks (complete).** Built and
   ran the `HipccDiscoveryTests`, `RocmRoutingTests`, and
   `RemoteCompilerIntegrationTests` on Linux with
   `--blame-hang-timeout 360s`: 15 passed, 0 failed, 0 skipped. The remote
   integration cases exercise the service protocol against the in-process
   mock service; no external remote compiler service is configured on this
   host. `probe-services --rocm-compiler local` and
   `probe-services --hipcc-path /usr/bin/hipcc` both selected local HIP with
   `/usr/bin/hipcc`, reporting HIP `7.2.53211-97f5574fe2`. With `PATH` set to
   `/usr/local/sbin:/usr/local/bin:/usr/sbin:/sbin` (excluding `/usr/bin` and
   `/bin`) and HIP/ROCm override variables unset, the Linux fallback selected
   `/opt/rocm/bin/hipcc` and reported the same HIP version. `probe-services
   --rocm-compiler auto` also resolved HIP locally because no remote service
   was available. The routing tests explicitly verify that selecting local
   HIP leaves an explicitly configured CUDA route remote, and exercise
   explicit `auto`, environment precedence, and fallback behavior. Other
   backends remain routed locally in the live probe because no service URLs
   are configured; CUDA, Metal, and OpenCL compiler availability is absent on
   this host. The sandbox initially blocked MSBuild named pipes and the test
   runner loopback listener; using single-node MSBuild and running VSTest with
   host access allowed the complete focused set to pass.

8. **Run the ROCm test suite by layer, then in full (complete).** Ran with
   writable `TMPDIR=/tmp/ilgpu-rocm-test-tmp`,
   `ILGPU_ROCM_COMPILER=local`, `ILGPU_ROCM_ARCH=gfx1100`, .NET SDK
   `10.0.111`, ROCm `7.2.4`, HIP `7.2.53211`, and the physical Radeon RX 7900
   XTX (`gfx1100`). Every invocation used
   `--blame-hang-timeout 360s -m:1`; test execution needed host access for
   VSTest's loopback communication. `Src/CLAUDE.md` also specifies the
   360-second hang timeout. Layer results (passed / skipped / failed):

   | Layer / filter | Passed | Skipped | Failed |
   | --- | ---: | ---: | ---: |
   | IR snapshots (`FullyQualifiedName~IRTests`) | 3,942 | 0 | 0 |
   | Backend tests, combined (`FullyQualifiedName~BackendTests`) | 1,737 | 508 | 0 |
   | Source generation (`FullyQualifiedName~BackendTests.SourceGeneration`) | 1,572 | 28 | 0 |
   | Native compilation (`FullyQualifiedName~BackendTests.NativeCompilation`) | 160 | 480 | 0 |
   | GPU execution (`FullyQualifiedName~ExecutionTests`) | 352 | 533 | 0 |
   | MSBuild integration (`FullyQualifiedName~IntegrationTests`) | 19 | 5 | 0 |
   | Non-kernel tests (`FullyQualifiedName~NonKernelTests`) | 53 | 0 | 0 |
   | Performance regressions (`FullyQualifiedName~PerfTests`) | 4 | 1 | 0 |

   Full suite command:
   `TMPDIR=/tmp/ilgpu-rocm-test-tmp ILGPU_ROCM_COMPILER=local ILGPU_ROCM_ARCH=gfx1100 dotnet test Src/ILGPUC.Tests/ILGPUC.Tests.csproj --configuration Debug --no-build --no-restore --blame-hang-timeout 360s -m:1 --logger 'trx;LogFileName=full-rocm-suite.trx'`.
   Result: 6,110 passed, 1,047 skipped, 0 failed (7,157 total). The full
   TRX result is `Src/ILGPUC.Tests/TestResults/full-rocm-suite.trx`; the raw
   per-layer logs are in `/tmp/ilgpu-rocm-suite-logs/`. Parsing the full TRX
   shows 1,107 test cases with `ROCm` in their test names passed, with no
   ROCm-named failures or skips. Thus the ROCm execution tests ran against the
   available local GPU rather than being skipped.

   All 1,047 full-suite skip reasons were accounted for: unavailable CUDA
   compiler (177 “not available on this machine”, 160 “not available”);
   unavailable OpenCL compiler (174 and 158 with those same messages);
   unavailable Metal compiler (169 and 148); unsupported backend features
   (40 Metal Float64, 8 OpenCL Float64 atomics, 6 Metal Float64 plus Float64
   atomics, 2 Metal Float64 atomics, 1 OpenCL Int64 atomic min/max, 1 Metal
   Int64 atomic min/max); 2 CPU grouped-grid modeling limitations; and 1
   opt-in compile benchmark (`ILGPU_RUN_BENCH=1`). No unexplained ROCm skips
   or failures were recorded. Layer invocations were:
   `dotnet test Src/ILGPUC.Tests/ILGPUC.Tests.csproj --configuration Debug
   --no-build --no-restore --filter <filter> --blame-hang-timeout 360s -m:1`
   with the same environment variables above for each filter.

9. **Build and run eligible samples (complete 2026-10-17).** Built and ran
   the Linux matrix on ROCm 7.2.4 / HIP 7.2.53211, .NET SDK 10.0.111, and the
   Radeon RX 7900 XTX (`gfx1100`). Each eligible project was built with
   `ILGPUBackend=ROCm`, `ILGPUCompile=true`,
   `ILGPURocmCompiler=local`, and `ILGPUBackendOptions=--rocm-arch gfx1100`,
   then run with `ILGPU_SAMPLE_DEVICE=ROCm`. Results: **30 passed, 0 failed,
   3 skipped, 6 excluded** across 39 projects. The 30 passes selected the
   physical RX 7900 XTX and passed their applicable sample-specific output
   and numeric checks; `SimpleKernel` verified all 1,024 results. All sample
   processes exited successfully. Sample resource cleanup follows their
   scoped `using` patterns for buffers, accelerators, and contexts.

   The three intentional skips are `MemoryBufferStrides` (GPU
   `Interop.WriteLine` output is absent with default IO), `SimpleWriteLine`
   (GPU output is absent and enabling IO currently fails IR rewriting), and
   `SimpleDebugAssert` (the sample asserts on nonzero lanes while assertions
   are disabled). Six exclusions: `DeviceInfo` (enumeration only), `Empty`
   and `EmptyAlgorithms` (context construction only), `SimpleViews` (CPU-only
   direct view indexing), `KernelLibraryAttribute/MyKernelLib` (dependency
   covered by its ROCm consumer), and `LocalNuGetConsumer/Consumer` (separate
   local-package validation workflow). The kernel-library consumer was
   built and ran successfully.

   PowerShell 7 is not installed on this Linux host, so the checked-in
   runner's build and runtime settings were reproduced with a sequential
   host-access runner. The PowerShell output validator was also corrected to
   recognize `SimpleKernel`'s explicit `Verified all 1024 kernel results.`
   success line rather than requiring the generic `Done.` marker. Per-project
   build/run logs and machine-readable results are in
   `codex/temp/rocm-samples/`; this is ignored build output. The runner used
   implicit restore because most sample projects did not yet have assets
   files. Remote compiler routing was not exercised; this matrix validates
   local HIP compilation and execution on the GPU.

10. **Document supported host configuration and outcome (complete
    2026-10-17).** Updated `Src/ILGPUC/build/README.md`,
    `Src/ILGPUC/PACKAGE.md`, `Src/ILGPUC.Tests/tests.md`, and `Samples/ROCm.md`
    with the tested Ubuntu 24.04.4 / kernel 6.8.0-147 host, .NET 10.0.111,
    ROCm 7.2.4 / HIP 7.2.53211, RX 7900 XTX (`gfx1100`), in-kernel `amdgpu`
    driver, compiler discovery paths, local compiler/runtime selection,
    build/run/test commands, and sample/test outcomes and skips. Windows
    HIP SDK 7.2/gfx1201 validation remains described as a separate host. The
    documentation limits the Linux claim to this tested configuration;
    other Linux SDK/GPU combinations and remote-compiler sample execution
    remain unvalidated.

### Linux host baseline (2026-10-17)

| Item | Observation |
| --- | --- |
| OS | Ubuntu 24.04.4 LTS (Noble Numbat), x86_64 |
| Kernel | `6.8.0-147-generic` |
| .NET SDK | `10.0.111` |
| Discrete GPU | Sapphire Pulse Radeon RX 7900 XTX, Navi 31 (`1002:744c`), `amdgpu` driver |
| CPU | AMD Ryzen 9 9900X; `hipconfig` reports host target `znver5` |
| Driver version | In-kernel `amdgpu`; AMD SMI reports kernel `6.8.0-147-generic`; module vermagic is `6.8.0-147-generic SMP preempt mod_unload modversions` |
| Additional AMD display device | Ryzen 9 9900X integrated GPU (`1002:13c0`) disabled in BIOS; absent from latest `lspci` output |
| ROCm SDK | ROCm 7.2.4, installed under `/opt/rocm-7.2.4` |
| Installation method | Native APT using AMD's `rocm/apt/7.2.4` and `graphics/7.2.4/ubuntu` Noble repositories; ROCm pin priority 600 |
| Key package versions | `rocm`/`rocm-core` `7.2.4.70204-93~24.04`; `hip-runtime-amd` `7.2.53211.70204-93~24.04`; `hipcc` `1.1.1.70204-93~24.04`; `rocminfo` `1.0.0.70204-93~24.04` |
| AMD SMI | Tool/library `26.2.2`; reports ROCm `7.2.4` |
| HIP compiler | `hipcc` at `/opt/rocm-7.2.4/bin/hipcc`; HIP 7.2.53211; AMD clang 22.0.0git (ROCm 7.2.4) |
| HIP runtime | `libamdhip64.so` at `/opt/rocm-7.2.4/lib`; HSA runtime at `/opt/rocm-7.2.4/lib/libhsa-runtime64.so` |
| ROCm device query | Live `rocminfo` and `RuntimeProbe --discover-only`: one GPU, Radeon RX 7900 XTX, `gfx1100`; API supported; CPU is a separate agent |
| GPU selection policy | Integrated GPU disabled in BIOS; select/use the sole ROCm GPU, the RX 7900 XTX (`gfx1100`) |
| Active display output | RX 7900 XTX (`05:00.0`), DRM `card1`, DisplayPort `DP-1` reports `connected` in `/sys/class/drm` |
| ROCm-related environment | `HSA_*`, `HIP_PATH`, `ROCM_PATH`, `ILGPU_HIPCC_PATH`, `ILGPU_ROCM_COMPILER`, `ILGPU_ROCM_ARCH`, and `LD_LIBRARY_PATH` unset in captured shell; `hipconfig` computes `/opt/rocm-7.2.4` |
| Sample/probe inventory | `codex/hipcc-probe/runtime-smoke.cpp` is a portable HIP C++ runtime/stream probe. `RuntimeProbe` now selects the Windows or Linux HIP runtime library. Other probe artifacts include generated HIP source and compiled code objects. |
| RuntimeProbe execution | Built successfully with zero warnings/errors. On Linux, `--stream-only` returned success for `hipInit`, stream creation, and stream destruction; `--discover-only` found one ROCm device (`gfx1100`). |
| ILGPUC compiler routing | Built `ILGPUC` Debug with zero warnings/errors. Local probe selected HIP 7.2.53211; path-source checks covered CLI, `ILGPU_HIPCC_PATH`, `HIP_PATH`, `ROCM_PATH`, `PATH`, and the Linux `/opt/rocm/bin/hipcc` fallback. Focused discovery/routing tests passed 12/12. |
| Probe execution boundary | The restricted workspace context lacks `/dev/kfd` and `/dev/dri`; running outside that sandbox succeeded. The bundled `device.hsaco` targets `gfx1201`, so the full module/memory probe was not run against this `gfx1100` GPU. |

### Completion criteria

- A supported ROCm SDK is installed directly on this Ubuntu host and
  `hipcc` discovery selects it without Docker, WSL, or an HTTP compiler
  service.
- A generated ILGPU kernel compiles to a valid code object, loads through the
  Linux HIP runtime, executes on the selected AMD GPU, and returns verified
  results.
- Linux runtime API/ABI and the relevant ROCm test layers pass with all
  failures and skips explained. The sample matrix reports per-project
  outcomes rather than treating a successful build as execution proof.
- Documentation records this host's tested versions and keeps untested
  Linux SDK/GPU combinations and remote-compiler behavior clearly scoped.
