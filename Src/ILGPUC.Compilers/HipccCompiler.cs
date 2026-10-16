// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: HipccCompiler.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

namespace ILGPUC.Compilers;

/// <summary>
/// Compiler wrapper for AMD HIP, invoking <c>hipcc</c> to produce an object file from
/// HIP source code.
/// </summary>
public sealed class HipccCompiler : CompilerBase
{
    private const string DefaultPath = "/opt/rocm/bin/hipcc";

    /// <summary>
    /// Initializes a new <see cref="HipccCompiler"/> with a custom executable path.
    /// </summary>
    /// <param name="hipccPath">The absolute path to the <c>hipcc</c> executable.</param>
    public HipccCompiler(string hipccPath = DefaultPath)
        : base(hipccPath)
    { }

    /// <inheritdoc/>
    public override CompilationTarget Target => CompilationTarget.Hip;

    /// <summary>
    /// Compiles the HIP source code in <paramref name="request"/> to an object file and
    /// returns the base-64-encoded output on success.
    /// </summary>
    /// <param name="request">
    /// The compilation request containing source and options.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A <see cref="CompilationResult"/> describing the outcome.</returns>
    public override async Task<CompilationResult> CompileAsync(
        CompileRequest request,
        CancellationToken ct)
    {
        string tempDir;
        string? tempWarning;
        try
        {
            (tempDir, tempWarning) = CreateHipTempDirectory();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return TempFailure(request, ex);
        }
        try
        {
            var inputFile = Path.Combine(tempDir, "input.hip");
            await File.WriteAllTextAsync(inputFile, request.SourceCode, ct)
                .ConfigureAwait(false);

            // --genco emits a device code object that hipModuleLoadData can
            // load directly; "-c" would produce a host object with the device
            // code embedded, which the module loader rejects. The target GPU
            // architecture (--offload-arch) is passed via ExtraFlags.
            var outputFile = Path.Combine(tempDir, "output.hsaco");
            var optFlag = $"-O{(int)request.OptimizationLevel}";
            var includes = BuildIncludeArgs(request);
            var directives = BuildDirectiveArgs(request);
            var extraFlags = SanitizeCompilerFlags(request.ExtraFlags);
            var args =
                $"--genco -o \"{outputFile}\" \"{inputFile}\"" +
                $" {optFlag} {extraFlags}{includes}{directives}";

            int exitCode;
            string stdout;
            string stderr;
            try
            {
                (exitCode, stdout, stderr) = await RunProcessAsync(
                    ExecutablePath,
                    args.Trim(),
                    tempDir,
                    ct,
                    HipTempEnvironment(tempDir)).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or
                System.ComponentModel.Win32Exception)
            {
                return Failure(
                    "",
                    $"HIP compiler: {ExecutablePath}{Environment.NewLine}{ex.Message}",
                    -1,
                    request.OutputType);
            }

            if (exitCode != 0)
            {
                return Failure(
                    stdout,
                    $"HIP compiler: {ExecutablePath}{Environment.NewLine}{stderr}",
                    exitCode,
                    request.OutputType);
            }

            if (!File.Exists(outputFile))
            {
                return Failure(
                    stdout,
                    $"HIP compiler: {ExecutablePath}{Environment.NewLine}" +
                    $"hipcc exited successfully without creating {outputFile}." +
                    $"{Environment.NewLine}{stderr}",
                    -1,
                    request.OutputType);
            }

            // Windows HIP 7.2 --genco produces a Clang offload bundle. Extract
            // its AMDGPU device member; Linux's raw ELF output needs no change.
            var codeObjectFile = outputFile;
            if (IsOffloadBundle(outputFile))
            {
                var bundler = Path.Combine(
                    Path.GetDirectoryName(ExecutablePath)!,
                    OperatingSystem.IsWindows()
                        ? "clang-offload-bundler.exe"
                        : "clang-offload-bundler");
                var (listExit, listOutput, listError) = await RunBundlerAsync(
                    bundler,
                    $"--list --type=o --input=\"{outputFile}\"",
                    tempDir,
                    ct).ConfigureAwait(false);
                if (listExit != 0)
                {
                    return Failure(
                        stdout,
                        $"HIP compiler: {ExecutablePath}{Environment.NewLine}" +
                        $"Bundle tool: {bundler}{Environment.NewLine}" +
                        $"Could not list device code objects.{Environment.NewLine}" +
                        $"{stderr}{Environment.NewLine}{listError}",
                        listExit,
                        request.OutputType);
                }

                var deviceTargets = listOutput.Split(
                    ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Where(target =>
                        target.StartsWith("hip", StringComparison.Ordinal) &&
                        target.Contains(
                            "-amdgcn-amd-amdhsa", StringComparison.Ordinal))
                    .ToArray();
                if (deviceTargets.Length != 1)
                {
                    return Failure(
                        stdout,
                        $"HIP compiler: {ExecutablePath}{Environment.NewLine}" +
                        $"Expected one AMDGPU device object, found " +
                        $"{deviceTargets.Length}.{Environment.NewLine}" +
                        $"{listOutput}{Environment.NewLine}{stderr}",
                        -1,
                        request.OutputType);
                }

                codeObjectFile = Path.Combine(tempDir, "device.hsaco");
                var (unbundleExit, unbundleOutput, unbundleError) =
                    await RunBundlerAsync(
                        bundler,
                        $"--unbundle --type=o --targets={deviceTargets[0]} " +
                        $"--input=\"{outputFile}\" " +
                        $"--output=\"{codeObjectFile}\"",
                        tempDir,
                        ct).ConfigureAwait(false);
                if (unbundleExit != 0 || !File.Exists(codeObjectFile))
                {
                    return Failure(
                        stdout + unbundleOutput,
                        $"HIP compiler: {ExecutablePath}{Environment.NewLine}" +
                        $"Bundle tool: {bundler}{Environment.NewLine}" +
                        $"Could not extract the AMDGPU code object." +
                        $"{Environment.NewLine}{stderr}" +
                        $"{Environment.NewLine}{unbundleError}",
                        unbundleExit == 0 ? -1 : unbundleExit,
                        request.OutputType);
                }
            }

            // HSACO is a 64-bit little-endian AMDGPU ELF code object on both
            // Linux and Windows. A zero process exit does not prove this.
            using (var codeObject = File.OpenRead(codeObjectFile))
            {
                Span<byte> header = stackalloc byte[20];
                var hasHeader = codeObject.Length >= header.Length;
                if (hasHeader)
                    codeObject.ReadExactly(header);
                if (!hasHeader ||
                    header[0] != 0x7f || header[1] != (byte)'E' ||
                    header[2] != (byte)'L' || header[3] != (byte)'F' ||
                    header[4] != 2 || header[5] != 1 ||
                    header[18] != 0xe0 || header[19] != 0)
                {
                    return Failure(
                        stdout,
                        $"HIP compiler: {ExecutablePath}{Environment.NewLine}" +
                        $"Output is not an AMDGPU ELF code object: " +
                        $"{codeObjectFile}." +
                        $"{Environment.NewLine}{stderr}",
                        -1,
                        request.OutputType);
                }
            }

            var output = Base64EncodeFile(codeObjectFile);

            return new CompilationResult
            {
                Success = true,
                Output = output,
                StdOut = tempWarning is null
                    ? stdout
                    : $"{tempWarning}{Environment.NewLine}{stdout}",
                StdErr = stderr,
                ExitCode = exitCode,
                Target = CompilationTarget.Hip,
                OutputType = request.OutputType,
            };
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    private static (string path, string? warning) CreateHipTempDirectory()
    {
        var errors = new List<string>();
        var candidates = new[]
        {
            Path.GetTempPath(),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in candidates)
        {
            var path = Path.Combine(root, $"gpu-compilation-{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(path);
                // HIP COMGR renames a temporary code object. File creation
                // alone does not prove that the sandbox permits this step.
                var probe = Path.Combine(path, "probe.tmp");
                var renamed = Path.Combine(path, "probe.so");
                File.WriteAllText(probe, "probe");
                File.Move(probe, renamed);
                File.Delete(renamed);
                var warning = errors.Count == 0
                    ? null
                    : $"HIP temporary directory: {path} (system temp could not rename files).";
                return (path, warning);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{root}: {ex.Message}");
                CleanupTempDirectory(path);
            }
        }

        throw new IOException(
            "No temporary directory supports HIP's create-and-rename operation. " +
            string.Join("; ", errors));
    }

    private static IReadOnlyDictionary<string, string> HipTempEnvironment(
        string path) =>
        new Dictionary<string, string>
        {
            ["TEMP"] = path,
            ["TMP"] = path,
            ["TMPDIR"] = path,
        };

    private CompilationResult TempFailure(CompileRequest request, Exception ex) =>
        Failure(
            "",
            $"HIP compiler: {ExecutablePath}{Environment.NewLine}" +
            $"HIP/COMGR cannot create and rename temporary files. " +
            $"Set TEMP and TMP on Windows, or TMPDIR on Linux, to a writable " +
            $"directory before launching ILGPUC or the test runner." +
            $"{Environment.NewLine}{ex.Message}",
            -1,
            request.OutputType);

    private static bool IsOffloadBundle(string path)
    {
        using var output = File.OpenRead(path);
        ReadOnlySpan<byte> signature = "__CLANG_OFFLOAD_BUNDLE__"u8;
        if (output.Length < signature.Length)
            return false;
        Span<byte> prefix = stackalloc byte[signature.Length];
        output.ReadExactly(prefix);
        return prefix.SequenceEqual(signature);
    }

    private static async Task<(int exitCode, string stdout, string stderr)>
        RunBundlerAsync(
        string executable,
        string arguments,
        string workingDirectory,
        CancellationToken ct)
    {
        try
        {
            return await RunProcessAsync(
                executable, arguments, workingDirectory, ct,
                HipTempEnvironment(workingDirectory))
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or
            System.ComponentModel.Win32Exception)
        {
            return (-1, "", ex.Message);
        }
    }
}
