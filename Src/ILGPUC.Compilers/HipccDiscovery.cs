// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: HipccDiscovery.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ILGPUC.Compilers;

/// <summary>
/// Finds a usable HIP 7 compiler without depending on a particular minor version.
/// </summary>
public static class HipccDiscovery
{
    private const string LinuxDefaultPath = "/opt/rocm/bin/hipcc";
    private static readonly Regex s_versionPattern = new(
        @"HIP version:\s*(\d+)\.(\d+)",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Finds a HIP 7 compiler, optionally requiring a valid result.
    /// </summary>
    /// <param name="explicitPath">CLI-specified executable path, if any.</param>
    /// <param name="required">
    /// Whether to fail when no compatible compiler can be found.
    /// </param>
    /// <returns>The selected executable path, or the legacy fallback path.</returns>
    public static string Discover(string? explicitPath = null, bool required = false)
        => DiscoverCore(explicitPath, required, DiscoveryEnvironment.Current());

    internal static string DiscoverCore(
        string? explicitPath,
        bool required,
        DiscoveryEnvironment environment)
    {
        var tried = new List<string>();
        if (explicitPath is not null)
            return CheckExplicit(explicitPath, "--hipcc-path", tried, environment);

        var envPath = environment.HipccPath;
        if (!string.IsNullOrWhiteSpace(envPath))
            return CheckExplicit(envPath, "ILGPU_HIPCC_PATH", tried, environment);

        var executable = environment.IsWindows ? "hipcc.exe" : "hipcc";
        foreach (var (variable, root) in new[]
        {
            ("HIP_PATH", environment.HipPath),
            ("ROCM_PATH", environment.RocmPath),
        })
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            var candidate = Path.Combine(root, "bin", executable);
            if (CheckCandidate(candidate, variable, tried, environment))
                return Path.GetFullPath(candidate);
        }

        foreach (var directory in environment.Path.Split(Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim().Trim('"'), executable);
            if (CheckCandidate(candidate, "PATH", tried, environment))
                return Path.GetFullPath(candidate);
        }

        if (environment.IsWindows)
        {
            var rocmRoot = Path.Combine(
                environment.ProgramFiles, "AMD", "ROCm");
            if (environment.DirectoryExists(rocmRoot))
            {
                var directories = environment.EnumerateDirectories(rocmRoot)
                    .Select(dir => (Directory: dir,
                        Version: ParseDirectoryVersion(Path.GetFileName(dir))))
                    .ToArray();
                foreach (var item in directories.Where(
                    item => item.Version is null))
                {
                    tried.Add(
                        $"installed SDK: {item.Directory} " +
                        "(not a HIP 7.x version directory)");
                }
                var installed = directories
                    .Where(item => item.Version is not null)
                    .OrderByDescending(item => item.Version);
                foreach (var item in installed)
                {
                    var candidate = Path.Combine(item.Directory, "bin", executable);
                    if (CheckCandidate(candidate, "installed SDK", tried,
                        environment))
                        return Path.GetFullPath(candidate);
                }
            }
            else
            {
                tried.Add($"{rocmRoot} (SDK directory not found)");
            }
        }
        else if (CheckCandidate(LinuxDefaultPath, "Linux default", tried,
            environment))
        {
            return LinuxDefaultPath;
        }

        if (required)
            throw DiscoveryFailure(tried);
        return LinuxDefaultPath;
    }

    private static string CheckExplicit(
        string path,
        string source,
        List<string> tried,
        DiscoveryEnvironment environment)
    {
        if (CheckCandidate(path, source, tried, environment))
            return Path.GetFullPath(path);
        throw DiscoveryFailure(tried);
    }

    private static bool CheckCandidate(
        string path,
        string source,
        List<string> tried,
        DiscoveryEnvironment environment)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
            if (!environment.FileExists(fullPath))
            {
                tried.Add($"{source}: {fullPath} (not found)");
                return false;
            }

            var output = environment.ReadVersion(fullPath);
            if (output is null)
            {
                tried.Add($"{source}: {fullPath} (unable to read HIP version)");
                return false;
            }
            var match = s_versionPattern.Match(output);
            if (!match.Success)
            {
                tried.Add($"{source}: {fullPath} (unable to read HIP version)");
                return false;
            }
            if (int.Parse(match.Groups[1].Value) != 7)
            {
                tried.Add($"{source}: {fullPath} (HIP " +
                    $"{match.Groups[1].Value}.{match.Groups[2].Value}; " +
                    "requires 7.x)");
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or
            UnauthorizedAccessException or ArgumentException or
            System.ComponentModel.Win32Exception or TimeoutException)
        {
            tried.Add($"{source}: {path} ({ex.Message})");
            return false;
        }
    }

    private static Version? ParseDirectoryVersion(string name) =>
        Version.TryParse(name, out var version) && version.Major == 7
            ? version
            : null;

    private static InvalidOperationException DiscoveryFailure(
        List<string> tried) =>
        new("No usable HIP 7.x compiler was found. Paths tried:" +
            Environment.NewLine + string.Join(Environment.NewLine, tried));
}

/// <summary>
/// Captures discovery inputs so SDK selection can be checked without changing
/// process environment variables or requiring installed GPU tools.
/// </summary>
internal sealed record DiscoveryEnvironment(
    bool IsWindows,
    string? HipccPath,
    string? HipPath,
    string? RocmPath,
    string Path,
    string ProgramFiles,
    Func<string, bool> FileExists,
    Func<string, bool> DirectoryExists,
    Func<string, IEnumerable<string>> EnumerateDirectories,
    Func<string, string?> ReadVersion)
{
    internal static DiscoveryEnvironment Current() => new(
        OperatingSystem.IsWindows(),
        Environment.GetEnvironmentVariable("ILGPU_HIPCC_PATH"),
        Environment.GetEnvironmentVariable("HIP_PATH"),
        Environment.GetEnvironmentVariable("ROCM_PATH"),
        Environment.GetEnvironmentVariable("PATH") ?? "",
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        File.Exists,
        Directory.Exists,
        Directory.EnumerateDirectories,
        ReadInstalledVersion);

    private static string? ReadInstalledVersion(string path)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };
        process.Start();
        if (!process.WaitForExit(10000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("hipcc --version timed out");
        }
        if (process.ExitCode != 0)
            return null;
        return process.StandardOutput.ReadToEnd() +
            process.StandardError.ReadToEnd();
    }
}
