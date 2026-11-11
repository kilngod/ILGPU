// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: FloatingLiteralTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPUC.Backends;
using ILGPUC.Tests.Framework;
using ILGPUC.Tests.Kernels;
using Xunit;

namespace ILGPUC.Tests.BackendTests;

public sealed class FloatingLiteralTests
{
    [Theory]
    [InlineData(BackendType.CPU)]
    [InlineData(BackendType.ROCm)]
    public void GeneratedLiteralsPreservePrecisionAndSpecialValues(BackendType backend)
    {
        using var helper = new CompilationHelper(backend);
        var single = helper.GenerateBackendCode(
            typeof(FloatingLiteralKernels).GetMethod(nameof(FloatingLiteralKernels.Single))!, backend)
            .SourceCode;
        var doubleSource = helper.GenerateBackendCode(
            typeof(FloatingLiteralKernels).GetMethod(nameof(FloatingLiteralKernels.Double))!, backend)
            .SourceCode;
        Assert.Contains("0.70710677f", single);
        Assert.Contains("1E-20f", single);
        Assert.Contains("3.4028235E+38f", single);
        Assert.Contains("1E-45f", single);
        Assert.Contains("-0.0f", single);
        Assert.Contains("1.0f", single);
        Assert.Contains("0.7071067811865476", doubleSource);
        Assert.Contains("1E-200", doubleSource);
        Assert.Contains("1.7976931348623157E+308", doubleSource);
        Assert.Contains("5E-324", doubleSource);
        Assert.Contains("-0.0", doubleSource);
        Assert.Contains("1.0", doubleSource);
        if (backend == BackendType.CPU)
        {
            Assert.Contains("float.PositiveInfinity", single);
            Assert.Contains("float.NegativeInfinity", single);
            Assert.Contains("float.NaN", single);
            Assert.Contains("double.PositiveInfinity", doubleSource);
            Assert.Contains("double.NegativeInfinity", doubleSource);
            Assert.Contains("double.NaN", doubleSource);
        }
        else
        {
            Assert.Contains("INFINITY", single);
            Assert.Contains("-INFINITY", single);
            Assert.Contains("NAN", single);
            Assert.DoesNotContain("INFINITYf", single);
            Assert.DoesNotContain("NANf", single);
            Assert.Contains("-INFINITY", doubleSource);
            Assert.Contains("NAN", doubleSource);
        }
    }
}
