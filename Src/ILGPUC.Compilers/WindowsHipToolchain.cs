// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: WindowsHipToolchain.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System.Diagnostics;

namespace ILGPUC.Compilers;

/// <summary>
/// Discovers a compatible MSVC v143 and Windows SDK environment for Windows HIP
/// compilation. Linux HIP compilation uses the host toolchain and does not use this
/// class.
/// </summary>
internal static class WindowsHipToolchain
{
    private const string VsWhereRelativePath =
        "Microsoft Visual Studio\\Installer\\vswhere.exe";
    private const string V143VersionFile =
        "VC\\Auxiliary\\Build\\Microsoft.VCToolsVersion.v143.default.txt";
    private const string VcVarsRelativePath =
        "VC\\Auxiliary\\Build\\vcvars64.bat";
    private const string VcToolsComponent =
        "Microsoft.VisualStudio.Component.VC.Tools.x86.x64";

    private static readonly string[] s_environmentNames =
    [
        "PATH", "INCLUDE", "LIB", "LIBPATH", "VCToolsVersion",
        "VCToolsInstallDir", "VCINSTALLDIR", "VSINSTALLDIR",
        "WindowsSdkDir", "WindowsSDKVersion", "UniversalCRTSdkDir",
        "UCRTVersion", "VSCMD_ARG_TGT_ARCH", "VSCMD_ARG_HOST_ARCH",
        "VSCMD_ARG_VCVARS_VER",
    ];

    private static readonly Lazy<IReadOnlyDictionary<string, string>> s_environment =
        new(Discover);

    /// <summary>
    /// Gets the selected Windows C++ toolchain environment. Requires an installed
    /// v143 toolset and Windows SDK because HIP 7.x's Windows compiler uses their
    /// standard C++ and system headers.
    /// </summary>
    public static IReadOnlyDictionary<string, string> EnvironmentOverrides =>
        OperatingSystem.IsWindows()
            ? s_environment.Value
            : new Dictionary<string, string>();

    private static IReadOnlyDictionary<string, string> Discover()
    {
        if (TryGetCurrentCompatibleEnvironment(out var currentEnvironment))
            return currentEnvironment;

        var candidates = FindVsWhere()
            .SelectMany(FindV143Toolsets)
            .OrderByDescending(candidate => candidate.Version)
            .ToArray();
        var errors = new List<string>();

        foreach (var candidate in candidates)
        {
            if (TryGetEnvironment(candidate, out var environment, out var error))
                return environment;
            errors.Add(error);
        }

        var details = errors.Count == 0
            ? "No Visual Studio installation with the MSVC v143 C++ tools was found."
            : string.Join(Environment.NewLine, errors);
        throw new InvalidOperationException(
            "Windows HIP compilation requires MSVC v143 and the Windows SDK. " +
            "Install the Visual Studio C++ x64/x86 build tools and Windows SDK, " +
            "or launch ILGPUC from a compatible Developer PowerShell." +
            Environment.NewLine + details);
    }

    private static bool TryGetCurrentCompatibleEnvironment(
        out IReadOnlyDictionary<string, string> environment)
    {
        var values = s_environmentNames
            .Select(name => (Name: name, Value: System.Environment.GetEnvironmentVariable(name)))
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Name, pair => pair.Value!,
                StringComparer.OrdinalIgnoreCase);
        var versionText = values.GetValueOrDefault("VCToolsVersion");
        var include = values.GetValueOrDefault("INCLUDE");
        var sdkRoot = values.GetValueOrDefault("WindowsSdkDir");
        var sdkVersion = values.GetValueOrDefault("WindowsSDKVersion")?.Trim('\\');

        var isV143 = Version.TryParse(versionText, out var version) &&
            version.Major == 14 && version.Minor >= 30 && version.Minor < 50;
        var hasCmath = include?.Split(Path.PathSeparator)
            .Any(path => File.Exists(Path.Combine(path, "cmath"))) == true;
        var hasWindowsSdk = !string.IsNullOrWhiteSpace(sdkRoot) &&
            !string.IsNullOrWhiteSpace(sdkVersion) &&
            File.Exists(Path.Combine(sdkRoot, "Include", sdkVersion,
                "ucrt", "stdlib.h"));

        if (isV143 && hasCmath && hasWindowsSdk)
        {
            environment = values;
            return true;
        }

        environment = new Dictionary<string, string>();
        return false;
    }

    private static IEnumerable<string> FindVsWhere()
    {
        var paths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        };
        foreach (var root in paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var path = Path.Combine(root, VsWhereRelativePath);
            if (File.Exists(path))
                yield return path;
        }
    }

    private static IEnumerable<ToolsetCandidate> FindV143Toolsets(
        string vswherePath)
    {
        if (!TryRun(vswherePath,
            $"-all -products * -requires {VcToolsComponent} " +
            "-property installationPath", out var output, out _))
        {
            yield break;
        }

        foreach (var installation in output.Split(
            ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var versionFile = Path.Combine(installation, V143VersionFile);
            var vcvars = Path.Combine(installation, VcVarsRelativePath);
            if (!File.Exists(versionFile) || !File.Exists(vcvars))
                continue;

            var versionText = File.ReadAllText(versionFile).Trim();
            if (Version.TryParse(versionText, out var version))
            {
                yield return new ToolsetCandidate(
                    installation, vcvars, versionText, version);
            }
        }
    }

    private static bool TryGetEnvironment(
        ToolsetCandidate candidate,
        out IReadOnlyDictionary<string, string> environment,
        out string error)
    {
        var version = candidate.VersionText;
        var versionParts = version.Split('.');
        var vcvarsVersion = string.Join('.', versionParts.Take(2));
        var command =
            $"\"{candidate.VcVarsPath}\" -vcvars_ver={vcvarsVersion} " +
            ">nul && set";
        if (!TryRun("cmd.exe", $"/d /s /c \"{command}\"",
            out var output, out error))
        {
            environment = new Dictionary<string, string>();
            error = $"{candidate.InstallationPath}: {error}";
            return false;
        }

        var values = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var line in output.Split(
            ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf('=');
            if (separator > 0)
                values[line[..separator]] = line[(separator + 1)..];
        }

        var selectedVersion = values.GetValueOrDefault("VCToolsVersion");
        var include = values.GetValueOrDefault("INCLUDE");
        var hasCmath = include?.Split(Path.PathSeparator)
            .Any(path => File.Exists(Path.Combine(path, "cmath"))) == true;
        if (selectedVersion is null ||
            !selectedVersion.StartsWith(versionParts[0] + "." + versionParts[1],
                StringComparison.OrdinalIgnoreCase) ||
            !hasCmath ||
            string.IsNullOrWhiteSpace(values.GetValueOrDefault("WindowsSdkDir")) ||
            string.IsNullOrWhiteSpace(values.GetValueOrDefault("WindowsSDKVersion")))
        {
            environment = new Dictionary<string, string>();
            error = $"{candidate.InstallationPath}: MSVC {version} or Windows " +
                "SDK headers were not available in the Developer environment.";
            return false;
        }

        environment = values
            .Where(pair => s_environmentNames.Contains(
                pair.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
        error = string.Empty;
        return true;
    }

    private static bool TryRun(
        string executable,
        string arguments,
        out string output,
        out string error)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15000))
            {
                process.Kill(entireProcessTree: true);
                output = string.Empty;
                error = $"{executable} timed out.";
                return false;
            }
            output = stdoutTask.GetAwaiter().GetResult();
            var stderr = stderrTask.GetAwaiter().GetResult();
            if (process.ExitCode == 0)
            {
                error = string.Empty;
                return true;
            }
            error = string.IsNullOrWhiteSpace(stderr)
                ? $"{executable} exited with code {process.ExitCode}."
                : stderr.Trim();
            return false;
        }
        catch (Exception ex) when (ex is IOException or
            System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            output = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private sealed record ToolsetCandidate(
        string InstallationPath,
        string VcVarsPath,
        string VersionText,
        Version Version);
}
