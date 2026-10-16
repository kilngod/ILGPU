// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: RocmRoutingTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPUC.Compilers;
using System.Net;
using System.Net.Http.Json;

namespace ILGPUC.CompilerService.Tests;

public class RocmRoutingTests
{
    [Fact]
    public async Task LocalHipOverridesExplicitRemoteButCudaKeepsRemote()
    {
        var remoteUri = new Uri("https://compiler.example/");
        var remote = Remote(remoteUri, CompilationTarget.Cuda,
            CompilationTarget.Hip);
        var local = Local();

        var routes = await Create(
            [remoteUri], RocmCompilerMode.Local, null,
            _ => remote, (_, _) => local);

        Assert.Same(local, routes.Routes[CompilationTarget.Hip]);
        Assert.Same(remote, routes.Routes[CompilationTarget.Cuda]);
    }

    [Fact]
    public async Task AutoKeepsExplicitRemoteHipRoute()
    {
        var remoteUri = new Uri("https://compiler.example/");
        var remote = Remote(remoteUri, CompilationTarget.Hip);

        var routes = await Create(
            [remoteUri], RocmCompilerMode.Auto, null,
            _ => remote, (_, _) => Local());

        Assert.Same(remote, routes.Routes[CompilationTarget.Hip]);
    }

    [Fact]
    public async Task LocalEnvironmentOverridesTrustedServiceUrl()
    {
        var remote = Remote(new Uri("https://compiler.example/"),
            CompilationTarget.Hip);
        var local = Local();
        var environment = new Dictionary<string, string>
        {
            ["ILGPU_ROCM_COMPILER"] = "local",
            ["ILGPU_ROCM_SERVICE_URL"] = remote.BaseUrl.ToString(),
        };

        var routes = await Create(
            null, null, null, _ => remote, (_, _) => local,
            name => environment.GetValueOrDefault(name));

        Assert.Same(local, routes.Routes[CompilationTarget.Hip]);
    }

    [Fact]
    public async Task ExplicitAutoOverridesLocalEnvironment()
    {
        var remote = Remote(new Uri("https://compiler.example/"),
            CompilationTarget.Hip);
        var environment = new Dictionary<string, string>
        {
            ["ILGPU_ROCM_COMPILER"] = "local",
            ["ILGPU_ROCM_SERVICE_URL"] = remote.BaseUrl.ToString(),
        };

        var routes = await Create(
            null, RocmCompilerMode.Auto, null, _ => remote,
            (_, _) => Local(), name => environment.GetValueOrDefault(name));

        Assert.Same(remote, routes.Routes[CompilationTarget.Hip]);
    }

    [Fact]
    public async Task ExplicitHipccPathSelectsLocalAndConflictsWithAuto()
    {
        var selectedPath = Path.Combine(Path.GetTempPath(), "selected-hipcc");
        string? receivedPath = null;
        var local = Local();
        CompilerManager GetLocal(string? path, bool required)
        {
            Assert.True(required);
            receivedPath = path;
            return local;
        }

        var routes = await Create(
            null, null, selectedPath,
            url => Remote(new Uri(url)), GetLocal);
        Assert.Same(local, routes.Routes[CompilationTarget.Hip]);
        Assert.Equal(selectedPath, receivedPath);

        await Assert.ThrowsAsync<ArgumentException>(() => Create(
            null, RocmCompilerMode.Auto, selectedPath,
            url => Remote(new Uri(url)), GetLocal));
    }

    [Fact]
    public async Task DockerDefaultRemainsAnAutoRemoteRoute()
    {
        var hipRemote = Remote(new Uri("http://localhost:5002/"),
            CompilationTarget.Hip);
        var routes = await Create(
            null, RocmCompilerMode.Auto, null,
            url => url.Contains(":5002", StringComparison.Ordinal)
                ? hipRemote
                : Remote(new Uri(url)),
            (_, _) => Local());

        Assert.Same(hipRemote, routes.Routes[CompilationTarget.Hip]);
    }

    private static Task<CompositeCompilerManager> Create(
        IReadOnlyList<Uri>? urls,
        RocmCompilerMode? mode,
        string? hipccPath,
        Func<string, RemoteCompilerManager> getRemote,
        Func<string?, bool, CompilerManager> getLocal,
        Func<string, string?>? getEnvironment = null) =>
        CompilerManagerFactory.CreateCoreAsync(
            urls, allowLocal: false, CancellationToken.None, mode, hipccPath,
            getRemote, getLocal, getEnvironment ?? (_ => null));

    private static CompilerManager Local() => new(new CompilerOptions
    {
        HipccPath = "/test/hipcc",
    });

    private static RemoteCompilerManager Remote(
        Uri uri,
        params CompilationTarget[] targets)
    {
        var capabilities = new CompilationCapabilities
        {
            Compilers = targets.Select(target => new CompilerCapability
            {
                Target = target,
                Available = true,
            }).ToArray(),
        };
        return new RemoteCompilerManager(uri,
            new HttpClient(new CapabilitiesHandler(capabilities)));
    }

    private sealed class CapabilitiesHandler(CompilationCapabilities capabilities)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(capabilities),
                });
    }
}
