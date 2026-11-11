// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: BasicMovementExecutionTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPUC.Backends;
using ILGPUC.Tests.Framework;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ILGPUC.Tests.ExecutionTests;

public abstract class BasicMovementExecutionTests : ExecutionTestBase
{
    protected BasicMovementExecutionTests(ITestOutputHelper output, BackendType backend)
        : base(output, backend) { }

    [SkippableFact]
    public async Task Copy_ProducesCorrectOutput()
    {
        // source: [10,20,30,40] → target: [10,20,30,40]
        await VerifyProgramOutputAsync(
            "TestPrograms/BasicMovement/Copy.cs",
            ["Kernels.CopyKernel"],
            ["10", "20", "30", "40"]);
    }

    [SkippableFact]
    public async Task BarrierOrdering_ProducesCorrectOutput()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/BasicMovement/BarrierOrdering.cs",
            ["Kernels.BarrierOrderingKernel"],
            ["1", "3", "5", "7"]);
    }

    [SkippableFact]
    public async Task ConditionalStructBarrier_ProducesCorrectOutput()
    {
        Skip.If(Backend != BackendType.ROCm,
            "This regression exercises ROCm code placement; CPU struct lowering is separate.");
        await VerifyProgramOutputAsync(
            "TestPrograms/BasicMovement/ConditionalStructBarrier.cs",
            ["Kernels.ConditionalStructBarrierKernel"],
            ["0"]);
    }

    [SkippableFact]
    public async Task InliningRelinkOwnership_ProducesCorrectOutput()
    {
        Skip.If(Backend != BackendType.ROCm,
            "The CPU emitter currently cannot compile this kernel's array views.");
        await VerifyProgramOutputAsync(
            "TestPrograms/BasicMovement/InliningRelinkOwnership.cs",
            ["Kernels.InliningRelinkOwnershipKernel"],
            ["35", "35", "25", "27"]);
    }
}
