// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: CodePlacementOrderingTests.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using System.Collections.Generic;
using ILGPUC.Backends;
using ILGPUC.Tests.Framework;
using ILGPUC.Tests.Kernels;
using Xunit;

namespace ILGPUC.Tests.BackendTests;

public sealed class CodePlacementOrderingTests
{
    [Fact]
    public void RocmButterflyStructOperandsAreAssignedBeforeUse()
    {
        using var helper = new CompilationHelper(BackendType.ROCm);
        var kernel = typeof(FftRelinkReproducer).GetMethod(nameof(FftRelinkReproducer.Length216_32))!;
        string source = helper.GenerateBackendCode(kernel, BackendType.ROCm).SourceCode;
        var unassigned = FindUnassignedStructOperands(source, out int structureCount);
        Assert.True(structureCount > 0, "Expected generated butterfly struct assignments.");
        Assert.Empty(unassigned);
    }

    [Fact]
    public void DetectsButterflyOperandAssignedAfterStruct()
    {
        const string source = "tmp_1 = (struct_2){ tmp_3 + tmp_4 };\n" +
            "tmp_3 = 1;\ntmp_4 = 2;";
        var unassigned = FindUnassignedStructOperands(source, out int structureCount);
        Assert.Equal(1, structureCount);
        Assert.Equal(new[] { "tmp_3", "tmp_4" }, unassigned);
    }

    private static List<string> FindUnassignedStructOperands(string source, out int structureCount)
    {
        var unassigned = new List<string>();
        var structures = Regex.Matches(source,
            @"\btmp_\d+\s*=\s*\(struct_\d+\)\s*\{([^{}]*)\}\s*;");
        structureCount = structures.Count;
        foreach (Match structure in structures)
        {
            foreach (Match operand in Regex.Matches(structure.Groups[1].Value, @"\btmp_\d+\b"))
            {
                string assignment = $@"\b{Regex.Escape(operand.Value)}\s*=";
                if (!Regex.IsMatch(source[..structure.Index], assignment))
                    unassigned.Add(operand.Value);
            }
        }
        return unassigned;
    }
}
