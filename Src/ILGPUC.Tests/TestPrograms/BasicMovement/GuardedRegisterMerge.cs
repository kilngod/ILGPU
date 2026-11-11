// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: GuardedRegisterMerge.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

static class Kernels
{
    public readonly struct Pair
    {
        public readonly double Real, Imaginary;
        public Pair(double real, double imaginary) { Real = real; Imaginary = imaginary; }
    }

    public struct Registers
    {
        public Pair R0, R1, R2, R3, R4, R5, R6, R7;
    }

    public static void ScalarControlKernel(
        KernelIndex index, ArrayView<double> input, ArrayView<double> output)
    {
        var shared = Group.GetSharedMemory<Pair>(32);
        int lane = index.GroupIndex;
        shared[lane] = new Pair(input[2 * lane], input[2 * lane + 1]);
        Group.Barrier();

        double r0 = 0, i0 = 0, r1 = 0, i1 = 0;
        if (lane < 16)
        {
            Pair a = shared[lane], b = shared[lane + 16];
            r0 = a.Real; i0 = a.Imaginary;
            r1 = b.Real; i1 = b.Imaginary;
        }
        if (lane < 16)
        {
            output[4 * lane] = r0;
            output[4 * lane + 1] = i0;
            output[4 * lane + 2] = r1;
            output[4 * lane + 3] = i1;
        }
    }

    public static void GuardedRegisterMergeKernel(
        KernelIndex index, ArrayView<double> input, ArrayView<double> output)
    {
        var shared = Group.GetSharedMemory<Pair>(32);
        int lane = index.GroupIndex;
        shared[lane] = new Pair(input[2 * lane], input[2 * lane + 1]);
        Group.Barrier();

        Registers r = default;
        if (lane < 16)
        {
            r.R0 = shared[lane];
            r.R1 = shared[lane + 16];
        }
        if (lane < 16)
        {
            output[4 * lane] = r.R0.Real;
            output[4 * lane + 1] = r.R0.Imaginary;
            output[4 * lane + 2] = r.R1.Real;
            output[4 * lane + 3] = r.R1.Imaginary;
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
        using var input = stream.Allocate1D<double>(64);
        using var output = stream.Allocate1D<double>(64);
        var values = new double[64];
        for (int i = 0; i < values.Length; ++i) values[i] = i + 1;
        input.CopyFromCPU(values);
        output.MemSetToZero();
        var config = new KernelConfig(1, 32);
        stream.Launch(in config, i => Kernels.ScalarControlKernel(i, input.View, output.View));
        Console.WriteLine(CountMismatches(values, output.GetAsArray1D()));
        output.MemSetToZero();
        stream.Launch(in config, i => Kernels.GuardedRegisterMergeKernel(i, input.View, output.View));
        Console.WriteLine(CountMismatches(values, output.GetAsArray1D()));
    }

    private static int CountMismatches(double[] values, double[] actual)
    {
        int mismatches = 0;
        for (int i = 0; i < 16; ++i)
        {
            if (actual[4*i] != values[2*i] || actual[4*i+1] != values[2*i+1] ||
                actual[4*i+2] != values[2*(i+16)] || actual[4*i+3] != values[2*(i+16)+1])
                ++mismatches;
        }
        return mismatches;
    }
}
