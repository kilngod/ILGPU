// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: DenseAsPlainStructViewLaunch.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

public struct Pair
{
    public double Real;
    public double Imaginary;
}

static class Kernels
{
    public static void Plain(Index1D index, ArrayView<Pair> input, ArrayView<double> output)
    {
        output[2] = input.Length;
        if (input.Length > 0)
        {
            Pair value = input[index];
            output[0] = value.Real;
            output[1] = value.Imaginary;
        }
    }
}

static class Program
{
    static void Main()
    {
        using var context = Context.Create(b => b.Default());
        using var accelerator = context.GetPreferredDevice(preferCPU: true).CreateAccelerator(context);
        var stream = accelerator.DefaultStream;
        using var input = stream.Allocate1D<Pair>(4);
        using var output = stream.Allocate1D<double>(3);
        input.CopyFromCPU(new[]
        {
            new Pair { Real = 10, Imaginary = 11 },
            new Pair { Real = 23, Imaginary = -41 },
            new Pair { Real = 30, Imaginary = 31 },
            new Pair { Real = 40, Imaginary = 41 },
        });
        ArrayView1D<Pair, Stride1D.Dense> subview = input.View.SubView(1, 2);
        stream.Launch((Index1D)1, i => Kernels.Plain(i, subview, output.View));
        var result = output.GetAsArray1D();
        Console.WriteLine(result[0] == 23 && result[1] == -41 && result[2] == 2);

        output.CopyFromCPU(new double[] { 77, 88, 99 });
        ArrayView1D<Pair, Stride1D.Dense> empty = default;
        stream.Launch((Index1D)1, i => Kernels.Plain(i, empty, output.View));
        result = output.GetAsArray1D();
        Console.WriteLine(result[0] == 77 && result[1] == 88 && result[2] == 0);
    }
}
