// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

static class Kernels
{
    public static void CopyFirst(Index1D index, ArrayView<int> input,
        ArrayView<int> output)
    {
        if (index == 0 && input.Length > 0)
            output[0] = input[0] + (int)input.Length;
    }
}

static class Program
{
    static void Main()
    {
        using var context = Context.Create(builder => builder.Default());
        using var accelerator = context.GetPreferredDevice(preferCPU: true)
            .CreateAccelerator(context);
        var stream = accelerator.DefaultStream;
        using var input = stream.Allocate1D<int>(4);
        using var output = stream.Allocate1D<int>(1);
        input.CopyFromCPU(new[] { 99, 10, 11, 88 });
        ArrayView1D<int, Stride1D.Dense> subview = input.View.SubView(1, 2);
        stream.Launch((Index1D)1, index => Kernels.CopyFirst(index, subview,
            output.View));
        int[] result = output.GetAsArray1D();
        Console.WriteLine(result[0] == 12);

        output.CopyFromCPU(new[] { 77 });
        ArrayView<int> empty = default;
        stream.Launch((Index1D)1, index => Kernels.CopyFirst(index, empty,
            output.View));
        result = output.GetAsArray1D();
        Console.WriteLine(result[0] == 77);
    }
}
