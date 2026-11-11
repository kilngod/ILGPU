// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: SetFieldExecutionTests.cs
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

public abstract class SetFieldExecutionTests : ExecutionTestBase
{
    protected SetFieldExecutionTests(ITestOutputHelper output, BackendType backend)
        : base(output, backend) { }

    [SkippableFact]
    public async Task ChainedUpdatesPreserveUntouchedFieldsAcrossBranches()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/BasicMovement/SetFieldSemantics.cs", ["Kernels.UpdateFields"], ["0"]);
    }
}

public sealed class CPU_SetFieldExecutionTests : SetFieldExecutionTests
{
    public CPU_SetFieldExecutionTests(ITestOutputHelper output) : base(output, BackendType.CPU) { }
}

public sealed class ROCm_SetFieldExecutionTests : SetFieldExecutionTests
{
    public ROCm_SetFieldExecutionTests(ITestOutputHelper output) : base(output, BackendType.ROCm) { }
}
