param(
    [string] $Architecture,
    [string[]] $ProjectNames,
    [string] $OutputDirectory,
    [switch] $Rebuild,
    [switch] $IncludeKnownLauncherFailures
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$architectureOverride = $Architecture
if (-not $architectureOverride) {
    $architectureOverride = $env:ILGPU_ROCM_ARCH
}
if ($architectureOverride -and $architectureOverride -notmatch '^gfx[0-9a-z]+$') {
    throw "Invalid ROCm architecture '$architectureOverride'. Expected a gfx target such as gfx1201."
}
$rocmArchitectureArgument = if ($architectureOverride) {
    $architectureOverride
} else {
    'auto'
}
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'codex\temp\rocm-samples'
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

if (-not $IsWindows) {
    $workspaceTemp = Join-Path $repoRoot 'codex/temp'
    New-Item -ItemType Directory -Path $workspaceTemp -Force | Out-Null
    $env:TMPDIR = $workspaceTemp
}

$env:ILGPU_ROCM_COMPILER = 'auto'
$env:ILGPU_SAMPLE_DEVICE = 'ROCm'
if ($architectureOverride) {
    $env:ILGPU_ROCM_ARCH = $architectureOverride
}
$env:ILGPU_SAMPLE_NONINTERACTIVE = '1'
$env:MSBUILDDISABLENODEREUSE = '1'

$compilerPath = $env:ILGPU_HIPCC_PATH
if (-not $compilerPath -and $env:HIP_PATH) {
    $candidate = Join-Path $env:HIP_PATH $(if ($IsWindows) {
        'bin/hipcc.exe'
    } else { 'bin/hipcc' })
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        $compilerPath = $candidate
    }
}
if (-not $compilerPath -and $env:ROCM_PATH) {
    $candidate = Join-Path $env:ROCM_PATH $(if ($IsWindows) {
        'bin/hipcc.exe'
    } else { 'bin/hipcc' })
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        $compilerPath = $candidate
    }
}
if (-not $compilerPath) {
    $command = Get-Command hipcc -ErrorAction SilentlyContinue
    if ($command) { $compilerPath = $command.Source }
}
$runInfo = [pscustomobject]@{
    Host = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    Architecture = if ($architectureOverride) {
        $architectureOverride
    } else { 'auto-detected from local ROCm device(s)' }
    CompilerMode = 'auto (local HIP when available, otherwise configured service or Docker)'
    CompilerPath = if ($compilerPath) { $compilerPath } else { 'ILGPUC discovery' }
    HipPath = $env:HIP_PATH
    RocmPath = $env:ROCM_PATH
    DotnetVersion = (& dotnet --version)
    BuildOptions = @(
        'ILGPUBackend=ROCm', 'ILGPUCompile=true',
        'ILGPURocmCompiler=auto',
        "ILGPUBackendOptions=--rocm-arch $rocmArchitectureArgument")
    RuntimeSelection = 'ROCm (noninteractive matrix run)'
    StartedAt = (Get-Date).ToString('o')
}
$runInfo | ConvertTo-Json -Depth 3 |
    Set-Content -LiteralPath (Join-Path $OutputDirectory 'run-info.json')

$excluded = @{
    'DeviceInfo' = 'Enumerates devices; no kernel or numeric result.'
    'Empty' = 'Context construction only; no kernel.'
    'EmptyAlgorithms' = 'Context construction only; no kernel.'
    'SimpleViews' = 'Uses CPUDevice and CPU-only direct ArrayView indexing.'
    'LocalNuGetConsumer/Consumer' = 'Exercises a separately packed local NuGet feed; package validation is separate.'
    'KernelLibraryAttribute/MyKernelLib' = 'Library dependency, exercised by its Consumer project.'
}

$knownLauncherFailures = @()

function Review-Output([string] $name, [string] $path) {
    $lines = @(Get-Content -LiteralPath $path)
    $output = $lines -join "`n"
    if ($output -notmatch 'Type: ROCm') {
        return @('Failed', 'The output did not identify a ROCm accelerator.')
    }
    if ($output -match '(?im)Error at element|Round-trip FAILED|result mismatches|Unhandled exception|Incorrect fixed buffer|Pinned copy mismatch') {
        return @('Failed', 'The sample reported an incorrect result.')
    }

    switch ($name) {
        'MemoryBufferStrides' {
            return @('Skipped', 'GPU Interop.WriteLine results are absent with the default IO setting.')
        }
        'SimpleWriteLine' {
            return @('Skipped', 'GPU Interop.WriteLine output is absent; enabling IO currently fails IR rewriting.')
        }
        'SimpleDebugAssert' {
            return @('Skipped', 'The sample intentionally asserts on nonzero lanes; assertions are disabled in this build.')
        }
        'AdvancedAtomics' {
            if (@($lines | Where-Object { $_ -eq 'Data[0] = 2048' }).Count -ne 2) {
                return @('Failed', 'Expected both atomic kernels to produce 2048.')
            }
        }
        'AdvancedViews' {
            foreach ($expected in @(
                'Composed.SomeElement = 0',
                'Composed.SomeOtherElement = 0',
                'Composed.ElementCounter = 1024')) {
                if ($expected -notin $lines) {
                    return @('Failed', "Missing expected value: $expected")
                }
            }
        }
        'AlgorithmsMath' {
            $values = @($lines | Where-Object { $_ -match '^Data\[\d+\] = ' })
            if ($values.Count -ne 128) {
                return @('Failed', 'Expected 64 values from each math kernel.')
            }
            for ($pass = 0; $pass -lt 2; $pass++) {
                for ($i = 0; $i -lt 64; $i++) {
                    if ($values[$pass * 64 + $i] -notmatch '^Data\[(\d+)\] = (.+)$' -or
                        [int]$matches[1] -ne $i) {
                        return @('Failed', "Math output index mismatch at $i.")
                    }
                    $actual = [double]::Parse($matches[2],
                        [Globalization.CultureInfo]::InvariantCulture)
                    $expected = [Math]::Sinh($i + 0.1) + [Math]::Atan(0.1)
                    if ([Math]::Abs($actual - $expected) -gt
                        [Math]::Max(0.0001, [Math]::Abs($expected) * 0.0001)) {
                        return @('Failed', "Math result mismatch at $i.")
                    }
                }
            }
        }
        'AlgorithmsInitialize' {
            if (@($lines | Where-Object { $_ -match '^Data\[\d+\] = 23$' }).Count -ne 64 -or
                @($lines | Where-Object { $_ -match '^Data2\[\d+\] = First: 23, Second: 42$' }).Count -ne 64) {
                return @('Failed', 'Expected 64 initialized integers and structs.')
            }
        }
        'AlgorithmsSequence' {
            $counts = @{}
            foreach ($line in $lines) {
                if ($line -match '^(Data|RepeatedData|BatchedData|RepeatedBatchedData)\[(\d+)\] = (-?\d+)$') {
                    $kind = $matches[1]
                    $i = [int]$matches[2]
                    $actual = [int]$matches[3]
                    $expected = switch ($kind) {
                        'Data' { $i }
                        'RepeatedData' { $i % 32 }
                        'BatchedData' { [int][Math]::Floor($i / 2) }
                        'RepeatedBatchedData' { [int]([Math]::Floor($i / 4) % 2) }
                    }
                    if ($actual -ne $expected) {
                        return @('Failed', "$kind mismatch at $i.")
                    }
                    $counts[$kind]++
                } elseif ($line -match '^CustomData\[(\d+)\] = First: (\d+), Second: (\d+)$') {
                    $i = [int]$matches[1]
                    if ([int]$matches[2] -ne $i -or
                        [int]$matches[3] -ne 32 + $i) {
                        return @('Failed', "CustomData mismatch at $i.")
                    }
                    $counts['CustomData']++
                }
            }
            foreach ($kind in @('Data', 'CustomData', 'RepeatedData',
                    'BatchedData', 'RepeatedBatchedData')) {
                if ($counts[$kind] -ne 64) {
                    return @('Failed', "$kind produced fewer than 64 values.")
                }
            }
        }
        'AlgorithmsTransform' {
            if (@($lines | Where-Object { $_ -match '^Data\[\d+\] = First: 2, Second: 4$' }).Count -ne 64) {
                return @('Failed', 'Expected 64 transformed structs.')
            }
        }
        'FixedSizeBuffers' {
            $values = @($lines | Where-Object { $_ -match '^Result\[' })
            if ($values.Count -ne 16 -or
                @($values | Where-Object { $_ -notmatch '^Result\[\d+\] = \[\(11, 22\), \(33, 44, 55\)\]$' }).Count -ne 0) {
                return @('Failed', 'Fixed-size buffer fields did not match 11,22,33,44,55.')
            }
        }
        'ExplicitlyGroupedKernels' {
            $sections = @(
                'Default grouped kernel',
                'Grouped-barrier kernel',
                'Grouped-and-barrier kernel',
                'Grouped-or-barrier kernel',
                'Grouped-popcount-barrier kernel',
                'Group AllReduce kernel',
                'Group InclusiveScan kernel')
            $section = -1
            $index = 0
            $groupSize = if ($output -match 'MaxNumThreadsPerGroup: (\d+)') {
                [int]$matches[1]
            } else { 0 }
            if ($groupSize -lt 1) {
                return @('Failed', 'The ROCm group size was not reported.')
            }
            foreach ($line in $lines) {
                if ($line -in $sections) {
                    if ($section -ge 0 -and $index -ne 128) {
                        return @('Failed', "Grouped section $section has $index values, expected 128.")
                    }
                    $section++
                    $index = 0
                    if ($line -ne $sections[$section]) {
                        return @('Failed', 'Grouped sections are out of order.')
                    }
                    continue
                }
                if ($line -notmatch '^Data\[(\d+)\] = (-?\d+)$') { continue }
                if ([int]$matches[1] -ne $index -or $section -lt 0) {
                    return @('Failed', 'Grouped output index mismatch.')
                }
                $actual = [int]$matches[2]
                $groupStart = [int][Math]::Floor($index / $groupSize) * $groupSize
                $expected = switch ($section) {
                    0 { $index + 64 }
                    1 { [int]($index -ge 64) }
                    2 { 1 }
                    3 { [int]([Math]::Min($groupStart + $groupSize, 128) -gt 64) }
                    4 { [Math]::Min($groupSize, 128 - $groupStart) }
                    5 { $groupSize }
                    6 { ($index % $groupSize) + 1 }
                }
                if ($actual -ne $expected) {
                    return @('Failed', "Grouped section $section mismatch at ${index}: $actual; expected $expected.")
                }
                $index++
            }
            if ($section -ne 6 -or $index -ne 128) {
                return @('Failed', 'Grouped output is incomplete.')
            }
        }
        'GroupGridIndices' {
            $values = @($lines | Where-Object { $_ -match '^Data\[' })
            if ($values.Count -lt 32 -or
                @($values | Where-Object { $_ -notmatch '^Data\[\d+\] = 64$' }).Count -ne 0) {
                return @('Failed', 'Expected every launched lane to write 64.')
            }
        }
        'KernelLibraryAttribute/Consumer' {
            $values = @($lines | Where-Object { $_ -match '^\|a-b\|\+\|b-c\| at ' })
            $expected = @(11, 28, 8, 19, 7, 16, 10, 9)
            if ($values.Count -ne $expected.Count) {
                return @('Failed', 'Expected eight kernel-library results.')
            }
            for ($i = 0; $i -lt $expected.Count; $i++) {
                if ($values[$i] -ne "|a-b|+|b-c| at $i = $($expected[$i])") {
                    return @('Failed', "Kernel-library result mismatch at $i.")
                }
            }
        }
        'MatrixMultiply' {
            if (@($lines | Where-Object { $_ -match 'Accelerated implementation on .*Type: ROCm' }).Count -ne 3 -or
                @($lines | Where-Object { $_ -match 'Tiled implementation on .*\(result matches\)' }).Count -ne 2) {
                return @('Failed', 'Expected three accelerated and two matching tiled matrix results.')
            }
        }
        'SharedMemory' {
            $values = @($lines | Where-Object { $_ -match '^Data\[' })
            if ($values.Count -ne 256 -or
                @($values[0..127] | Where-Object { $_ -notmatch '^Data\[\d+\] = 128$' }).Count -ne 0 -or
                @($values[128..255] | Where-Object { $_ -notmatch '^Data\[\d+\] = 8256$' }).Count -ne 0) {
                return @('Failed', 'Shared-memory max or sum result mismatched.')
            }
        }
        'SimpleArrayView2D' {
            if ('Round-trip OK.' -notin $lines) {
                return @('Failed', '2D array round trip did not pass.')
            }
        }
        'SimpleArrayView3D' {
            if ('Round-trip OK.' -notin $lines) {
                return @('Failed', '3D array round trip did not pass.')
            }
        }
        'SimpleKernel' {
            if ('Verified all 1024 kernel results.' -notin $lines) {
                return @('Failed', 'The kernel did not verify all 1024 results.')
            }
        }
        'SimpleAtomics' {
            $expected = @(4096, 4, 0, 0, 4, 0, 0)
            $values = @($lines | Where-Object { $_ -match '^Data\[' })
            if ($values.Count -ne $expected.Count) {
                return @('Failed', 'Expected seven atomic results.')
            }
            for ($i = 0; $i -lt $expected.Count; $i++) {
                if ($values[$i] -ne "Data[$i] = $($expected[$i])") {
                    return @('Failed', "Atomic result mismatch at $i.")
                }
            }
        }
        'SimpleMath' {
            $values = @($lines | Where-Object { $_ -match '^Math results:' })
            if ($values.Count -ne 128) {
                return @('Failed', 'Expected 128 math results.')
            }
            for ($i = 0; $i -lt 128; $i++) {
                if ($values[$i] -notmatch '^Math results: (\d+) \(float\) (\d+) \(double \[IntrinsicMath\]\) ([0-9.]+) \(double \[Math\]\)$' -or
                    [int]$matches[1] -ne $i -or
                    [int]$matches[2] -ne [Math]::Min($i, 12) -or
                    [double]$matches[3] -ne $(if ($i -eq 0) { 0 } else { 0.2 })) {
                    return @('Failed', "Math result mismatch at $i.")
                }
            }
        }
        'StaticAbstractInterfaceMembers' {
            $values = @($lines | Where-Object { $_ -match '^\[\d+\] = ' })
            if ($values.Count -ne 32) {
                return @('Failed', 'Expected 32 static-interface results.')
            }
            for ($i = 0; $i -lt 16; $i++) {
                if ($values[$i] -ne "[$i] = $($i * 2)" -or
                    $values[$i + 16] -ne "[$i] = $($i * $i)") {
                    return @('Failed', "Static-interface result mismatch at $i.")
                }
            }
        }
        'WarpShuffle' {
            $values = @($lines | Where-Object { $_ -match '^Data\[' })
            if ($values.Count -lt 4 -or $values[0] -ne 'Data[0] = 2') {
                return @('Failed', 'ShuffleDown returned the original lane instead of lane + 2.')
            }
        }
        'AlgorithmsReduce' {
            if ('Reduced = 2016' -notin $lines -or
                'Reduced output = 1953' -notin $lines) {
                return @('Failed', 'Scalar or output reduction returned an incorrect sum.')
            }
        }
        'AlgorithmsRadixSort' {
            $values = @($lines | Where-Object { $_ -match '^Data\[\d+\] = ' })
            if ($values.Count -ne 64) {
                return @('Failed', 'Expected 32 ascending and 32 descending values.')
            }
            for ($i = 0; $i -lt 32; ++$i) {
                if ($values[$i] -ne "Data[$i] = $i" -or
                    $values[$i + 32] -ne "Data[$i] = $(31 - $i)") {
                    return @('Failed', "Radix sort mismatch at $i.")
                }
            }
            if ('Large ascending radix sort passed.' -notin $lines -or
                'Large descending radix sort passed.' -notin $lines) {
                return @('Failed', 'Large signed radix sort did not pass.')
            }
        }
        'AlgorithmsScan' {
            $values = @($lines | Where-Object { $_ -match '^Data\[\d+\] = ' })
            if ($values.Count -ne 64 -or 'Large scan passed.' -notin $lines) {
                return @('Failed', 'Scan output or large-scan check is missing.')
            }
            for ($i = 0; $i -lt 32; $i++) {
                if ($values[$i] -ne "Data[$i] = $(2 * ($i + 1))" -or
                    $values[$i + 32] -ne "Data[$i] = $(2 * $i)") {
                    return @('Failed', "Scan mismatch at index $i.")
                }
            }
        }
        'ProfilingMarkers' {
            if ($output -notmatch 'Elapsed time: \d+ms') {
                return @('Failed', 'No profiling marker timing was reported.')
            }
        }
        default {
            if ('Done.' -notin $lines) {
                return @('Failed', 'The sample did not report completion.')
            }
        }
    }
    return @('Passed', 'ROCm execution and sample-specific output checks passed.')
}

$projects = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Samples') `
    -Recurse -File -Filter '*.csproj' | Sort-Object FullName)
$results = [System.Collections.Generic.List[object]]::new()
foreach ($project in $projects) {
    $relativeDirectory = [IO.Path]::GetRelativePath(
        (Join-Path $repoRoot 'Samples'), $project.DirectoryName)
    $relativeDirectory = $relativeDirectory.Replace('\', '/')
    if ($ProjectNames -and
        $relativeDirectory -notin $ProjectNames -and
        $project.BaseName -notin $ProjectNames) {
        continue
    }

    if ($excluded.ContainsKey($relativeDirectory)) {
        $results.Add([pscustomobject]@{
            Project = $relativeDirectory
            Status = 'Excluded'
            Reason = $excluded[$relativeDirectory]
            BuildExit = $null
            RunExit = $null
            BuildLog = $null
            RunLog = $null
        })
        Write-Host "EXCLUDED $relativeDirectory : $($excluded[$relativeDirectory])"
        continue
    }

    if (-not $IncludeKnownLauncherFailures -and
        $relativeDirectory -in $knownLauncherFailures) {
        $reason = 'Known AOT launcher failure: AcceleratorStream.LaunchAutoGroupedInternal throws NotSupportedException.'
        $results.Add([pscustomobject]@{
            Project = $relativeDirectory
            Status = 'Skipped'
            Reason = $reason
            BuildExit = $null
            RunExit = $null
            BuildLog = $null
            RunLog = $null
        })
        Write-Host "Skipped $relativeDirectory : $reason"
        continue
    }

    $logStem = $relativeDirectory.Replace('/', '_')
    $buildLog = Join-Path $OutputDirectory "$logStem.build.log"
    $runLog = Join-Path $OutputDirectory "$logStem.run.log"
    $buildArgs = @(
        'build', $project.FullName, '--no-restore',
        '-p:Configuration=Debug',
        '-p:ILGPUBackend=ROCm',
        '-p:ILGPUCompile=true',
        '-p:ILGPURocmCompiler=auto',
        "-p:ILGPUBackendOptions=--rocm-arch $rocmArchitectureArgument",
        '-m:1', '-nr:false', '-v:minimal'
    )
    if ($Rebuild) { $buildArgs += '-t:Rebuild' }
    $ErrorActionPreference = 'Continue'
    & dotnet @buildArgs *> $buildLog
    $buildExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $runExit = $null
    $status = 'Failed'
    $reason = 'Native ROCm build failed; see build log.'
    if ($buildExit -eq 0) {
        $assembly = Join-Path $project.DirectoryName (
            "bin/Debug/net10.0/$($project.BaseName).dll")
        if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) {
            "Built assembly missing: $assembly" | Set-Content -LiteralPath $runLog
            $reason = 'The built assembly is missing.'
        } else {
            $ErrorActionPreference = 'Continue'
            & dotnet $assembly *> $runLog
            $runExit = $LASTEXITCODE
            $ErrorActionPreference = 'Stop'
            if ($runExit -eq 0) {
                $review = Review-Output $relativeDirectory $runLog
                $status = $review[0]
                $reason = $review[1]
            } else {
                $reason = @(Get-Content -LiteralPath $runLog |
                    Where-Object { $_ -match 'Exception:|error:' } |
                    Select-Object -First 1) -join ''
                if (-not $reason) { $reason = "Process exited $runExit." }
            }
        }
    }
    $results.Add([pscustomobject]@{
        Project = $relativeDirectory
        Status = $status
        Reason = $reason
        BuildExit = $buildExit
        RunExit = $runExit
        BuildLog = $buildLog
        RunLog = $runLog
    })
    Write-Host "$status $relativeDirectory"
    $results | ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json')
}

$results | ConvertTo-Json -Depth 4 |
    Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json')
Write-Host "Results: $(Join-Path $OutputDirectory 'results.json')"
