// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: GuardedRegisterMergeExecutionTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System.Threading.Tasks;
using ILGPUC.Backends;
using ILGPUC.Tests.Framework;
using Xunit;
using Xunit.Abstractions;

namespace ILGPUC.Tests.ExecutionTests;

/// <summary>Regression for LDS values assigned to aggregate fields inside a lane guard.</summary>
public sealed class GuardedRegisterMergeExecutionTests : ExecutionTestBase
{
    public GuardedRegisterMergeExecutionTests(ITestOutputHelper output)
        : base(output, BackendType.ROCm) { }

    [SkippableFact]
    public async Task GuardedLdsLoadsSurviveAggregateMerge()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/BasicMovement/GuardedRegisterMerge.cs",
            ["Kernels.ScalarControlKernel", "Kernels.GuardedRegisterMergeKernel"], ["0", "0"]);
    }
}
