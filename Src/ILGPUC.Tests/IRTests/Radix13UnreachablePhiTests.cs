// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: Radix13UnreachablePhiTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPUC.Backends;
using ILGPUC.IR;
using ILGPUC.IR.Transformations;
using ILGPUC.Tests.Framework;
using ILGPUC.Tests.Kernels;
using Xunit;

namespace ILGPUC.Tests.IRTests;

/// <summary>Regression for unreachable phi inputs after inlining radix-13 butterflies.</summary>
public sealed class Radix13UnreachablePhiTests
{
    [Fact]
    public void FullOptimizerCompletesForRadix13Kernel()
    {
        using var helper = new CompilationHelper(BackendType.ROCm);
        var method = typeof(Radix13PhiReproducer).GetMethod(
            nameof(Radix13PhiReproducer.Sbrr51x104x216RealSingle))!;
        var properties = new CompilationProperties();
        var module = helper.CompileToModule(method, properties, BackendType.ROCm);
        var optimized = properties.OptimizationLevel.CreateTransformer().Apply(
            properties, new TypeInformationManager(), module);
        Assert.NotNull(optimized);
    }

    [Fact]
    public void InlinedButterflyDoesNotRetainUnreachablePhiSources()
    {
        using var helper = new CompilationHelper(BackendType.ROCm);
        var method = typeof(Radix13PhiReproducer).GetMethod(
            nameof(Radix13PhiReproducer.Sbrr51x104x216RealSingle))!;
        var properties = new CompilationProperties();
        var module = helper.CompileToModule(method, properties, BackendType.ROCm);
        var pipeline = Transformer.CreateBuilder();
        pipeline.AddBasicOptimizations();
        var optimized = pipeline.ToTransformer().Apply(
            properties, new TypeInformationManager(), module);
        int phiCount = 0;
        foreach (var irMethod in optimized.Methods)
        foreach (var block in irMethod.Blocks)
        foreach (var phi in block.PhiValues)
        {
            ++phiCount;
            foreach (var source in phi.Sources)
            {
                bool isPredecessor = false;
                foreach (var predecessor in block.Predecessors)
                    isPredecessor |= predecessor == source;
                Assert.True(isPredecessor,
                    $"Phi source {source} is not a predecessor of {block}.");
            }
        }
        Assert.True(phiCount > 0, "The reproducer must retain phi nodes.");
    }
}
