// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: ConditionalStructBarrier.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

static class Kernels
{
    readonly struct Pair
    {
        public readonly double A, B;
        public Pair(double a, double b) { A = a; B = b; }
    }

    static Pair Calculate(double x, double y, int direction)
    {
        double difference = x - y * 0.5;
        double product = direction * (x + y);
        return new Pair(difference + product, difference - product);
    }

    public static void ConditionalStructBarrierKernel(
        KernelIndex index, ArrayView<double> input, ArrayView<double> output,
        int active, int direction)
    {
        var shared = Group.GetSharedMemory<double>(16);
        int lane = index.GroupIndex;
        Pair value = default;
        if (lane < active)
        {
            double x = input[lane];
            double y = input[lane + 8];
            value = Calculate(x, y, direction);
        }
        if (lane < active)
        {
            shared[lane] = value.A;
            shared[lane + 8] = value.B;
        }
        Group.Barrier();
        if (lane < active)
        {
            value = Calculate(shared[lane], shared[lane + 8], direction);
        }
        Group.Barrier();
        if (lane < active)
        {
            output[lane] = value.A;
            output[lane + 8] = value.B;
        }
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
        using var input = stream.Allocate1D<double>(16);
        using var output = stream.Allocate1D<double>(16);
        var data = new double[16];
        for (int i = 0; i < 8; i++) { data[i] = i + 1; data[i + 8] = i + 2; }
        input.CopyFromCPU(data);
        var config = new KernelConfig(1, 8);
        stream.Launch(in config, index => Kernels.ConditionalStructBarrierKernel(
            index, input.View, output.View, 7, 1));
        stream.Synchronize();
        var actual = output.GetAsArray1D();
        int mismatches = 0;
        for (int i = 0; i < 7; i++)
        {
            double x = data[i], y = data[i + 8];
            double a = x - y * 0.5 + x + y;
            double b = x - y * 0.5 - x - y;
            double expectedA = a - b * 0.5 + a + b;
            double expectedB = a - b * 0.5 - a - b;
            if (actual[i] != expectedA || actual[i + 8] != expectedB) mismatches++;
        }
        Console.WriteLine(mismatches);
    }
}
