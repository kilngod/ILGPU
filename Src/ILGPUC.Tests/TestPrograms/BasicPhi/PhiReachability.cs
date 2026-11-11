// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: PhiReachability.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using ILGPU;
using ILGPU.Runtime;

static class Kernels
{
    private static int Select(int lane, int direction)
    {
        int signedLane = direction < 0 ? lane : -lane;
        return (lane & 1) == 0 ? signedLane + 10 : signedLane - 3;
    }

    public static void PhiReachabilityKernel(Index1D index, ArrayView<int> output)
    {
        int lane = index.X;
        int value = Select(lane, -1);
        for (int i = 0; i < (lane & 3); ++i)
            value += i + 1;
        output[lane] = value;
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
        using var output = stream.Allocate1D<int>(8);
        stream.Launch((Index1D)8, index => Kernels.PhiReachabilityKernel(index, output.View));
        stream.Synchronize();
        foreach (int value in output.GetAsArray1D())
            Console.WriteLine(value);
    }
}
