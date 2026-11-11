// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: FloatingLiteralExecutionTests.cs
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

public sealed class FloatingLiteralExecutionTests : ExecutionTestBase
{
    public FloatingLiteralExecutionTests(ITestOutputHelper output) : base(output, BackendType.ROCm) { }

    [SkippableFact]
    public async Task RocmLiteralsPreserveValuesAndSpecialCases()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/BasicMovement/FloatingLiterals.cs",
            ["Kernels.Single", "Kernels.Double"],
            ["True", "True", "True", "True", "True", "True", "True", "True", "True", "True"]);
    }
}
