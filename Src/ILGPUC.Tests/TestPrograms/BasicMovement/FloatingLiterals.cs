// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: FloatingLiterals.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

static class Kernels
{
    public static void Single(Index1D index, ArrayView1D<float, Stride1D.Dense> output)
    {
        output[0] = 0.70710677f;
        output[1] = 1e-20f;
        output[2] = float.MaxValue;
        output[3] = -0.0f;
        output[4] = float.PositiveInfinity;
        output[5] = float.NegativeInfinity;
        output[6] = float.NaN;
    }

    public static void Double(Index1D index, ArrayView1D<double, Stride1D.Dense> output)
    {
        output[0] = 0.7071067811865476;
        output[1] = 1e-200;
        output[2] = double.MaxValue;
        output[3] = -0.0;
        output[4] = double.PositiveInfinity;
        output[5] = double.NegativeInfinity;
        output[6] = double.NaN;
    }
}

static class Program
{
    static void Main()
    {
        using var context = Context.Create(b => b.Default());
        using var accelerator = context.GetPreferredDevice(preferCPU: true).CreateAccelerator(context);
        var stream = accelerator.DefaultStream;
        using var singles = stream.Allocate1D<float>(7);
        using var doubles = stream.Allocate1D<double>(7);
        stream.Launch((Index1D)1, i => Kernels.Single(i, singles.View));
        stream.Launch((Index1D)1, i => Kernels.Double(i, doubles.View));
        stream.Synchronize();
        var f = singles.GetAsArray1D();
        var d = doubles.GetAsArray1D();
        Console.WriteLine(BitConverter.SingleToInt32Bits(f[0]) == BitConverter.SingleToInt32Bits(0.70710677f));
        Console.WriteLine(BitConverter.SingleToInt32Bits(f[1]) == BitConverter.SingleToInt32Bits(1e-20f));
        Console.WriteLine(f[2] == float.MaxValue);
        Console.WriteLine(BitConverter.SingleToInt32Bits(f[3]) == int.MinValue);
        Console.WriteLine(float.IsPositiveInfinity(f[4]) && float.IsNegativeInfinity(f[5]) && float.IsNaN(f[6]));
        Console.WriteLine(BitConverter.DoubleToInt64Bits(d[0]) == BitConverter.DoubleToInt64Bits(0.7071067811865476));
        Console.WriteLine(BitConverter.DoubleToInt64Bits(d[1]) == BitConverter.DoubleToInt64Bits(1e-200));
        Console.WriteLine(d[2] == double.MaxValue);
        Console.WriteLine(BitConverter.DoubleToInt64Bits(d[3]) == long.MinValue);
        Console.WriteLine(double.IsPositiveInfinity(d[4]) && double.IsNegativeInfinity(d[5]) && double.IsNaN(d[6]));
    }
}
