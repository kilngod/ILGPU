# Windows HIP 7.2 header audit (2026-10-16)

Compared `Src/ILGPU/Runtime/ROCm/ROCmAPI.cs`, `ROCmAPI.xml`, and
`ROCmDeviceProperties.cs` with the installed headers in
`C:\Program Files\AMD\ROCm\7.2\include\hip` on this workstation.
This audit covers declared values and ABI shapes; it is not a successful
kernel execution check.

| Area | Result |
| --- | --- |
| `hipDeviceAttribute_t` | All 20 values declared by `HipDeviceAttributeKind` match `hip_runtime_api.h`. |
| `hipError_t` | All five values declared by `ROCmError` match. |
| `hipMemcpyKind` | All five values declared by `HipMemcpyKind` match `driver_types.h`. |
| Stream flags | `hipStreamDefault=0` and `hipStreamNonBlocking=1` match. |
| Launch parameter tags | `HIP_LAUNCH_PARAM_BUFFER_POINTER=1`, `HIP_LAUNCH_PARAM_BUFFER_SIZE=2`, and `HIP_LAUNCH_PARAM_END=3` match. |
| Native calls | All 28 `ROCmAPI.xml` entry points are exported by `amdhip64_7.dll` and their parameter types/order match the header declarations. `hipModuleGetFunction` uses a UTF-8 `char*` name. |
| Current property layout | HIP R0600 and C# `HipDeviceProperties` are each 1,472 bytes; `gcnArchName` starts at 1,160 and `asicRevision` at 1,468. |
| Legacy property layout | HIP R0000 and C# `HipDevicePropertiesR0000` are each 792 bytes; `gcnArchName` starts at 396. The R0000 struct is currently unused by the runtime binding. |
| Feature flags and UUID | `hipDeviceArch_t` and `HipDeviceArch` are 4 bytes; `hipUUID` and `HipUuid` are 16 bytes. Header bit-field order matches the 17 C# accessors. |

The checks are reproducible from `hip-header-constants.cpp`,
`runtime-layout.cpp`, and the `RuntimeProbe --layout` mode in this folder.
Both C++ files passed syntax-only compilation with the SDK's `clang-cl.exe`
and MSVC v143 14.44; the managed layout probe printed the same sizes and
offsets.

**Initial execution symptom:** Native and .NET probes returned success from
`hipInit` but appeared to hang in `hipStreamCreate`, including ILGPU's
nonblocking stream variant. An earlier module-loading attempt terminated
with access violation `0xC0000005`. The stream cause was later identified
as temporary-file access in this Codex session (see below).

**After display-driver reinstall (2026-10-16):** The active RX 9070 XT
driver changed from Windows version `32.0.31041.5005` to
`32.0.31021.6002` (`oem14.inf`). The INF identifies its package as
`26.10.21.06-260717a-203129C-AMD-Software-PRO-Edition`; it does not label
the package `25.30`. The new Driver Store also has `amdhip64_7.dll` file
version `10.0.3679.0`, matching the SDK copy. With that SDK DLL beside the
standalone executable and a process-local `PATH` headed by the SDK `bin`
directory, the probe again printed `hipInit=0` and
`hipGetDeviceCount=0 count=1`, then hung in `hipStreamCreate`. This isolates
the remaining stream hang from ILGPU and from the stale system `PATH`.
The SDK's `hipInfo.exe` still enumerated `gfx1201`, then exited 1 with
`HIP API error = 0001 "invalid argument"` at its peer-query stage.
At that point the persistent Windows `Path` still contained 7.1 entries;
the user subsequently corrected it to 7.2.

**After Path correction (2026-10-16):** The persistent machine `Path` now
contains `C:\Program Files\AMD\ROCm\7.2\bin` and `7.2\lib`; the stale
7.1 entries are gone. With the SDK 7.2 DLL placed beside the native probe,
`hipInit` and `hipGetDeviceCount` still succeed, but both `hipStreamCreate`
and `hipStreamCreateWithFlags(..., hipStreamNonBlocking)` timed out after
12 seconds. The .NET ILGPU probe still reports `API supported: True` and
discovers one `gfx1201` ROCm device. The corrected Path does not resolve
the apparent native stream hang.

**After reboot and writable TEMP/TMP (2026-10-16):** With
`AMD_LOG_LEVEL=4` and `AMD_LOG_MASK=0xC61`, the native probe revealed the
failure: COMGR's linker reported `permission denied` while writing
`output\a.so` under this Codex session's sandbox temporary directory,
followed by blit-manager initialization failures. Setting process-local
`TEMP` and `TMP` to the writable `codex\temp` directory resolved it.
Native `hipInit`, `hipGetDeviceCount`, `hipStreamCreate`, and
`hipStreamDestroy` succeeded; the nonblocking stream variant also
succeeded. The stream symptom was caused by temporary-file access.

Under the same process-local environment, the .NET probe found one
`gfx1201` device and passed `hipSetDevice`, stream creation, allocation,
memset, host/device memory copies, synchronization, module loading,
function lookup, module unload, free, stream destruction, and device
synchronization. The 16-byte memory roundtrip matched. A zero-grid
`hipModuleLaunchKernel` returned `hipErrorInvalidValue` as expected;
actual kernel execution remains unverified. The output is in
`writable-temp-runtime-out.txt`. An old `SimpleKernel` binary selected
OpenCL and crashed; it contains an older ILGPU assembly and does not
represent a ROCm result. A clean rebuild exposed a separate ILGPUC issue:
`No kernel launch sites found`.

**Launch-site resolution and kernel execution (2026-10-16):** The launch
finder is shared by CUDA and ROCm. Its Roslyn compilation could not
resolve `stream.Launch` because `System.Runtime.dll` was missing from
references: the MSBuild response file did not quote paths under
`C:\Program Files`. Quoting response-file paths in
`Src/ILGPUC/build/ILGPU.Kernels.targets` resolved one launch site for
both backends. A ROCm binary build required `--rocm-arch gfx1201` and
MSVC v143 (14.44); the Visual Studio 2026 default MSVC 14.51 headers
conflicted with HIP 7.2. With process-local writable `TEMP`/`TMP`, the
resulting `SimpleKernel` ran on the RX 9070 XT, checked all 1,024 outputs,
and printed `Done` without errors. This is the first successful ILGPU
ROCm kernel execution observed on this Windows machine.

`Src/ILGPU.sln` contains ILGPUC test projects, but no ILGPU runtime test
project. The existing `Src/ILGPU.Tests*` directories contain only ignored
`bin` and `obj` folders in this checkout. The samples provide an initial
execution path after HIP stream creation is resolved; `SimpleKernel` can
be used as a small kernel check, with ROCm device selection made explicit.
