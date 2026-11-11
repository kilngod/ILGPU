// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: StructViewLaunchExecutionTests.cs
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

/// <summary>Regression for marshaling a plain ArrayView of an unmanaged struct.</summary>
public sealed class StructViewLaunchExecutionTests : ExecutionTestBase
{
    public StructViewLaunchExecutionTests(ITestOutputHelper output) : base(output, BackendType.ROCm) { }

    [SkippableFact]
    public async Task PlainStructViewPreservesSubviewOffsetLengthAndDefault()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/StructView/StructViewLaunch.cs",
            ["Kernels.Plain"],
            ["True", "True"]);
    }
    [SkippableFact]
    public async Task DenseStructViewPreservesSubviewOffsetLengthAndDefault()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/StructView/DenseStructViewLaunch.cs",
            ["Kernels.Dense"],
            ["True", "True"]);
    }
    [SkippableFact]
    public async Task DenseViewPassedToPlainStructViewPreservesOffsetLengthAndDefault()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/StructView/DenseAsPlainStructViewLaunch.cs",
            ["Kernels.Plain"],
            ["True", "True"]);
    }
}
