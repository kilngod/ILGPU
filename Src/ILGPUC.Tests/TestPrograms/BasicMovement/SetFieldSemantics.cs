// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: SetFieldSemantics.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

static class Kernels
{
    public struct Pair { public double X, Y; }
    public struct Record { public double Before; public Pair Middle; public double After; }

    public static void UpdateFields(Index1D index, ArrayView<double> input, ArrayView<double> output)
    {
        int i = index.X;
        Record record = new Record
        {
            Before = input[0], Middle = new Pair { X = input[1], Y = input[2] }, After = input[3]
        };
        if (i < 2)
        {
            record.Middle = new Pair { X = input[4], Y = input[5] };
            record.Before = input[6];
            if (i == 0) record.Middle.Y = input[7];
        }
        output[4 * i] = record.Before;
        output[4 * i + 1] = record.Middle.X;
        output[4 * i + 2] = record.Middle.Y;
        output[4 * i + 3] = record.After;
    }
}

static class Program
{
    static void Main()
    {
        using var context = Context.Create(b => b.Default());
        using var accelerator = context.GetPreferredDevice(preferCPU: true).CreateAccelerator(context);
        var stream = accelerator.DefaultStream;
        using var input = stream.Allocate1D<double>(8);
        using var output = stream.Allocate1D<double>(12);
        input.CopyFromCPU(new double[] { 11, 12, 13, 14, 21, 22, 23, 24 });
        stream.Launch((Index1D)3, i => Kernels.UpdateFields(i, input.View, output.View));
        var actual = output.GetAsArray1D();
        double[] expected = { 23, 21, 24, 14, 23, 21, 22, 14, 11, 12, 13, 14 };
        int mismatches = 0;
        for (int i = 0; i < expected.Length; ++i)
            if (actual[i] != expected[i]) ++mismatches;
        Console.WriteLine(mismatches);
    }
}
