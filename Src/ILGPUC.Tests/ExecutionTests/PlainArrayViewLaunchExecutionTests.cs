// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------

using ILGPUC.Backends;
using ILGPUC.Tests.Framework;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ILGPUC.Tests.ExecutionTests;

/// <summary>Checks CPU launcher marshaling for plain scalar ArrayView parameters.</summary>
public sealed class PlainArrayViewLaunchExecutionTests : ExecutionTestBase
{
    public PlainArrayViewLaunchExecutionTests(ITestOutputHelper output)
        : base(output, BackendType.CPU) { }

    [Fact]
    public async Task DenseSubviewAndDefaultPlainViewsMarshalCorrectly()
    {
        await VerifyProgramOutputAsync(
            "TestPrograms/ArrayView/PlainArrayViewLaunch.cs",
            ["Kernels.CopyFirst"],
            ["True", "True"]);
    }
}
