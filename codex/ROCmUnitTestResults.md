# ROCm/HIP unit test results

## Windows workstation — 2026-10-16

| Environment | Observed value |
| --- | --- |
| GPU / target | AMD Radeon RX 9070 XT / `gfx1201` |
| HIP SDK / compiler | `C:\Program Files\AMD\ROCm\7.2`; `hipcc.exe` reports HIP `7.2.60201-38d754472` |
| Display driver | `32.0.31021.6002` (recorded in `CurrentOutlines.md`) |
| Host C++ toolchain | MSVC v143 `14.44.35207` (HIP 7.2 rejected the newer `14.51` headers) |
| .NET SDK | `10.0.402` |
| Test environment | Local HIP compiler; `TEMP`/`TMP` under `codex/temp`; `codex/rocm-tests.runsettings` |

| Category | Passed | Failed | Skipped | Result file |
| --- | ---: | ---: | ---: | --- |
| HIP discovery and routing | 12 | 0 | 0 | `Src/ILGPUC.CompilerService.Tests/TestResults/rocm-routing.trx` |
| Full compiler-service test project, final rerun | 43 | 0 | 0 | `Src/ILGPUC.CompilerService.Tests/TestResults/rocm-service-final.trx` |
| ROCm source generation, Debug + Release | 320 | 0 | 0 | `Src/ILGPUC.Tests/TestResults/rocm-sourcegen.trx` |
| ROCm native compilation | 160 | 0 | 0 | `Src/ILGPUC.Tests/TestResults/rocm-native.trx` |
| ROCm GPU execution | 177 | 0 | 0 | `Src/ILGPUC.Tests/TestResults/rocm-execution.trx` |
| ROCm MSBuild integration | 3 | 0 | 0 | `Src/ILGPUC.Tests/TestResults/rocm-integration.trx` |
| ROCm sample-family MSBuild integration | 4 | 0 | 0 | `Src/ILGPUC.Tests/TestResults/rocm-sample-family.trx` |
| Non-kernel compiler tests | 53 | 0 | 0 | `Src/ILGPUC.Tests/TestResults/rocm-nonkernel.trx` |
| IR snapshots, all backend rows | 3942 | 0 | 0 | `Src/ILGPUC.Tests/TestResults/rocm-ir.trx` |
| IR snapshots, ROCm rows within preceding run | 442 | 0 | 0 | same file; subset, not additional tests |
| First full `ILGPUC.Tests` suite | 6106 | 0 | 1047 | `Src/ILGPUC.Tests/TestResults/rocm-full-windows.trx` |
| Final full `ILGPUC.Tests` suite, including new sample rows | 6110 | 0 | 1047 | `Src/ILGPUC.Tests/TestResults/rocm-full-windows-final.trx` |

The release source-generation filter was also run separately (160 passed),
but those cases are already included in the 320 above. The 12 ROCm
discovery and routing cases are included in the 43 compiler-service tests.
The first ROCm integration attempt failed because the test subprocess split
`--rocm-arch gfx1201` at the space. After quoting the MSBuild property, all
three ROCm integration rows passed. The first GPU smoke attempt failed with
HIP `cmath` overload errors under MSVC 14.51; selecting 14.44 resolved it.
Neither failure was a skipped case or a GPU execution pass.
The copy execution test also passed with `ILGPU_ROCM_ARCH` removed from the
runsettings file, confirming that the harness detected this GPU's
architecture automatically.
The first full suite had 1,103 passed ROCm-named cases and no ROCm skips.
The 1,047 skips were for unavailable CUDA, OpenCL, and Metal backends.
After that run, four additional ROCm sample-family integration rows passed.
The final full suite had 1,107 passed ROCm-named cases and no ROCm failures
or skips; those four rows account for the difference. The category rows above
overlap with the full suite and must not be summed as separate cases.
The HIP compiler's temp fallback was also exercised by a direct `ILGPUC
compile` under the restricted sandbox default temp directory: its rename
operation was denied, but compilation succeeded using a temporary directory
under the working directory, which was removed afterward.

The tests above used `dotnet test` on the corresponding `.csproj` with
`--no-restore --no-build -p:Configuration=Debug -p:TargetFramework=net10.0
-m:1 -nr:false --settings .\codex\rocm-tests.runsettings`, category filters,
and a TRX logger. `ILGPU_UPDATE_IR` was unset. The full suite run uses the
same command without a filter. `CompileBenchFacts` remains an opt-in
benchmark and is not included as a performance result.

Category filters on `Src/ILGPUC.Tests/ILGPUC.Tests.csproj`:

| Category | `--filter` value |
| --- | --- |
| Source generation, Debug + Release | `FullyQualifiedName~ROCmBackendTests.SourceGeneration` |
| Native compilation | `FullyQualifiedName~ROCmBackendTests.NativeCompilation` |
| ROCm execution | `FullyQualifiedName~ILGPUC.Tests.ExecutionTests.ROCm_` |
| ROCm MSBuild integration | `FullyQualifiedName~MsBuildIntegrationTests&DisplayName~ROCm` |
| IR snapshots | `FullyQualifiedName~IRTests` |
| Non-kernel compiler tests | `FullyQualifiedName~NonKernelTests` |

The compiler-service project was run once with
`--filter 'FullyQualifiedName~HipccDiscoveryTests|FullyQualifiedName~RocmRoutingTests'`
and once without a filter. The combined `ILGPUC.Tests` run has no filter.

## Linux AMD host

The user has not configured this host yet; Linux validation is deferred.
Record its GPU, `gfx` architecture, ROCm
SDK/`hipcc` version, driver, .NET SDK, compiler path, command lines, and
separate pass/fail/skip counts here. Use a writable `TMPDIR`,
`ILGPU_ROCM_COMPILER=local`, and `ILGPU_ROCM_ARCH` matching that GPU; then
run the same categories and full suite. Do not combine Linux counts with the
Windows fixture or call cross-platform ROCm validation complete until its
actual GPU execution and native compilation runs pass.
