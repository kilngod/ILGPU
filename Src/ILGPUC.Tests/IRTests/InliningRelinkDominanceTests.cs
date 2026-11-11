// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: InliningRelinkDominanceTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System.Collections.Generic;
using ILGPUC.Backends;
using ILGPUC.IR;
using ILGPUC.IR.Analyses;
using ILGPUC.IR.MethodValues;
using ILGPUC.IR.PureValues;
using ILGPUC.IR.Transformations;
using ILGPUC.Tests.Framework;
using ILGPUC.Tests.Kernels;
using Xunit;

namespace ILGPUC.Tests.IRTests;

public sealed class InliningRelinkDominanceTests
{
    [Fact]
    public void InlinedCallerLoadDominatesButterflyDependencies()
    {
        using var helper = new CompilationHelper(BackendType.ROCm);
        var method = typeof(FftRelinkReproducer).GetMethod(nameof(FftRelinkReproducer.Length216_32))!;
        var properties = new CompilationProperties();
        var module = helper.CompileToModule(method, properties, BackendType.ROCm);
        var types = new TypeInformationManager();
        var optimized = properties.OptimizationLevel.CreateTransformer().Apply(properties, types, module);
        foreach (var irMethod in optimized.Methods)
        {
            var placement = CodePlacement.Create(irMethod);
            var blocks = new Dictionary<PureValue, BasicBlock>();
            foreach (var placed in placement)
                foreach (var value in placed.Values)
                    if (value is PureValue pure)
                        blocks[pure] = placed.BasicBlock;

            foreach (var pair in blocks)
                foreach (var operand in pair.Key.Values)
                    if (operand is PureValue pure && blocks.TryGetValue(pure, out var definition))
                        Assert.True(placement.Dominators.Dominates(definition, pair.Value),
                            $"{pair.Key} in {pair.Value} uses {pure} in non-dominating {definition}.");
        }
    }
}
