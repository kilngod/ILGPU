// ---------------------------------------------------------------------------------------
//                                    ILGPU Samples
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: Program.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using System.Linq;
using ILGPU;
using ILGPU.Initialization;
using ILGPU.Runtime;
using ILGPU.ScanReduce;

namespace AlgorithmsReduce;

static class Program
{
    static void Main()
    {
        using var context = Context.CreateDefault();

        var device = SampleDeviceSelector.Select(context);

        using var accelerator = device.CreateAccelerator(context);
        Console.WriteLine($"Using {accelerator}");
        var stream = accelerator.DefaultStream;

        using var buffer = stream.Allocate1D<int>(64);

        // Fill the buffer with a sequence 0..63
        stream.Sequence(buffer.View, i => (int)i);

        // Reduce: sum all elements -> returns the scalar result directly
        int result = stream.Reduce(
            buffer.View,
            identity: 0,
            apply: (a, b) => a + b,
            atomicApply: (ref int t, int v) => Atomic.Add(ref t, v));
        stream.Synchronize();

        Console.WriteLine($"Reduced = {result}");
        if (result != 2016)
            throw new InvalidOperationException(
                $"Expected the sum 2016, got {result}");

        using var output = stream.Allocate1D<int>(1);
        stream.Reduce(
            buffer.View.SubView(0, 63),
            output.View.BaseView,
            identity: 0,
            apply: (a, b) => a + b,
            atomicApply: (ref int t, int v) => Atomic.Add(ref t, v));
        stream.Synchronize();

        int outputResult = output.GetAsArray1D()[0];
        Console.WriteLine($"Reduced output = {outputResult}");
        if (outputResult != 1953)
            throw new InvalidOperationException(
                $"Expected the output sum 1953, got {outputResult}");
    }
}
