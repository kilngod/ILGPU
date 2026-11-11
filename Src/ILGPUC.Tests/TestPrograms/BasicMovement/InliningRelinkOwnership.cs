// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: InliningRelinkOwnership.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

static class Kernels
{
    private static int Twice(int value) => value * 2;
    private static int Add(int a, int b) => a + b;

    public static void InliningRelinkOwnershipKernel(
        Index1D index, ArrayView<int> input, ArrayView<int> output)
    {
        int lane = index.X;
        int sharedInput = input[0]; // One caller-owned load per lane.
        int branchValue = 0;
        if (lane < 2)
            branchValue = Twice(sharedInput);
        // This branch has different lanes from the first branch. The original
        // caller-owned load must be available to these lanes as well.
        if (lane >= 2)
            branchValue = Add(sharedInput, lane);
        // Consecutive inline calls also move a genuine instruction chain.
        output[lane] = Add(Twice(branchValue), sharedInput);
    }
}

static class Program
{
    static void Main()
    {
        using var context = Context.Create(b => b.Default());
        using var accelerator = context.GetPreferredDevice(preferCPU: true)
            .CreateAccelerator(context);
        var stream = accelerator.DefaultStream;
        using var input = stream.Allocate1D<int>(1);
        using var output = stream.Allocate1D<int>(4);
        input.CopyFromCPU(new[] { 7 });
        stream.Launch((Index1D)4, index => Kernels.InliningRelinkOwnershipKernel(
            index, input.View, output.View));
        stream.Synchronize();
        foreach (int value in output.GetAsArray1D())
            Console.WriteLine(value);
    }
}
