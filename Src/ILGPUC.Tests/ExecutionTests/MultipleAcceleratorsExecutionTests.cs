// Copyright (c) 2026 ILGPU Project. All rights reserved.
using System.Threading.Tasks;
using ILGPUC.Backends;
using ILGPUC.Tests.Framework;
using Xunit;
using Xunit.Abstractions;

namespace ILGPUC.Tests.ExecutionTests;

/// <summary>Checks that compiled launchers use the stream's accelerator.</summary>
public sealed class MultipleAcceleratorsExecutionTests : ExecutionTestBase
{
    public MultipleAcceleratorsExecutionTests(ITestOutputHelper output) : base(output, BackendType.ROCm) { }

    [SkippableFact]
    public async Task KernelsRemainBoundToTheirAccelerators()
    {
        await VerifyProgramOutputAsync("TestPrograms/StructView/MultipleAccelerators.cs",
            ["Kernels.Copy"], ["True", "True", "True", "True"]);
    }
}
