// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: HipccDiscoveryTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPUC.Compilers;

namespace ILGPUC.CompilerService.Tests;

public class HipccDiscoveryTests
{
    private static readonly string s_root = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "ilgpuc-discovery-tests"));

    [Fact]
    public void ExplicitPathPrecedesEveryOtherSource()
    {
        var explicitPath = Tool("explicit", "hipcc.exe");
        var envPath = Tool("environment", "hipcc.exe");
        var inputs = Fake(
            (explicitPath, "HIP version: 7.2.0"),
            (envPath, "HIP version: 7.1.0")) with
        {
            HipccPath = envPath,
            HipPath = Path.Combine(s_root, "other"),
        };

        Assert.Equal(explicitPath,
            HipccDiscovery.DiscoverCore(explicitPath, true, inputs));
    }

    [Fact]
    public void InvalidExplicitPathFailsWithoutUsingFallback()
    {
        var missing = Tool(Path.Combine("missing", "bin"), "hipcc.exe");
        var fallback = Tool("fallback", "hipcc.exe");
        var inputs = Fake((fallback, "HIP version: 7.2.0")) with
        {
            HipccPath = fallback,
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            HipccDiscovery.DiscoverCore(missing, true, inputs));
        Assert.Contains(missing, error.Message);
        Assert.Contains("not found", error.Message);
    }

    [Fact]
    public void EnvironmentRootAndPathFollowDocumentedPrecedence()
    {
        var env = Tool("env", "hipcc.exe");
        var hip = Tool(Path.Combine("hip", "bin"), "hipcc.exe");
        var rocm = Tool(Path.Combine("rocm", "bin"), "hipcc.exe");
        var onPath = Tool("path", "hipcc.exe");
        var inputs = Fake(
            (env, "HIP version: 7.2.0"),
            (hip, "HIP version: 7.2.0"),
            (rocm, "HIP version: 7.2.0"),
            (onPath, "HIP version: 7.2.0")) with
        {
            HipccPath = env,
            HipPath = Path.Combine(s_root, "hip"),
            RocmPath = Path.Combine(s_root, "rocm"),
            Path = Path.GetDirectoryName(onPath)!,
        };

        Assert.Equal(env, HipccDiscovery.DiscoverCore(null, true, inputs));
        Assert.Equal(hip, HipccDiscovery.DiscoverCore(null, true,
            inputs with { HipccPath = null }));
        Assert.Equal(rocm, HipccDiscovery.DiscoverCore(null, true,
            inputs with { HipccPath = null, HipPath = null }));
        Assert.Equal(onPath, HipccDiscovery.DiscoverCore(null, true,
            inputs with { HipccPath = null, HipPath = null, RocmPath = null }));
    }

    [Fact]
    public void InstalledWindowsSdksUseHighestNumericVersion()
    {
        var installRoot = Path.Combine(s_root, "AMD", "ROCm");
        var v72 = Path.Combine(installRoot, "7.2");
        var v710 = Path.Combine(installRoot, "7.10");
        var v8 = Path.Combine(installRoot, "8.0");
        var inputs = Fake(
            (Path.Combine(v72, "bin", "hipcc.exe"), "HIP version: 7.2.0"),
            (Path.Combine(v710, "bin", "hipcc.exe"), "HIP version: 7.10.0"),
            (Path.Combine(v8, "bin", "hipcc.exe"), "HIP version: 8.0.0")) with
        {
            DirectoryExists = path => path == installRoot,
            EnumerateDirectories = _ => [v72, v8, v710],
        };

        Assert.Equal(Path.Combine(v710, "bin", "hipcc.exe"),
            HipccDiscovery.DiscoverCore(null, true, inputs));
    }

    [Fact]
    public void IncompatibleSdkAndMissingToolReportPathsTried()
    {
        var incompatible = Tool(Path.Combine("old", "bin"), "hipcc.exe");
        var missing = Tool(Path.Combine("missing", "bin"), "hipcc.exe");
        var inputs = Fake((incompatible, "HIP version: 6.4.0")) with
        {
            HipPath = Path.Combine(s_root, "old"),
            RocmPath = Path.Combine(s_root, "missing"),
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            HipccDiscovery.DiscoverCore(null, true, inputs));
        Assert.Contains(incompatible, error.Message);
        Assert.Contains("requires 7.x", error.Message);
        Assert.Contains(missing, error.Message);
        Assert.Contains("not found", error.Message);
    }

    [Fact]
    public void LinuxDefaultRemainsAvailable()
    {
        var inputs = Fake((Path.GetFullPath("/opt/rocm/bin/hipcc"),
            "HIP version: 7.2.0")) with
        {
            IsWindows = false,
        };

        Assert.Equal("/opt/rocm/bin/hipcc",
            HipccDiscovery.DiscoverCore(null, true, inputs));
    }

    private static string Tool(string directory, string name) =>
        Path.Combine(s_root, directory, name);

    private static DiscoveryEnvironment Fake(
        params (string Path, string Version)[] tools)
    {
        var versions = tools.ToDictionary(item => item.Path, item => item.Version);
        return new DiscoveryEnvironment(
            IsWindows: true,
            HipccPath: null,
            HipPath: null,
            RocmPath: null,
            Path: "",
            ProgramFiles: s_root,
            FileExists: versions.ContainsKey,
            DirectoryExists: _ => false,
            EnumerateDirectories: _ => [],
            ReadVersion: path => versions.GetValueOrDefault(path));
    }
}
