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

namespace AlgorithmsScan;

static class Program
{
    static void Main()
    {
        using var context = Context.CreateDefault();

        var device = SampleDeviceSelector.Select(context);

        using var accelerator = device.CreateAccelerator(context);
        Console.WriteLine($"Using {accelerator}");
        var stream = accelerator.DefaultStream;

        const int N = 32;
        using var sourceBuffer = stream.Allocate1D<int>(N);
        stream.Initialize(sourceBuffer.View, 2);

        // Inclusive scan
        using (var targetBuffer = stream.Allocate1D<int>(N))
        {
            stream.InclusiveScan(
                sourceBuffer.View,
                targetBuffer.View,
                identity: 0,
                operation: (a, b) => a + b);
            stream.Synchronize();

            Console.WriteLine("Inclusive Scan:");
            var data = targetBuffer.GetAsArray1D();
            for (int i = 0, e = data.Length; i < e; ++i)
                Console.WriteLine($"Data[{i}] = {data[i]}");
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] != 2 * (i + 1))
                    throw new InvalidOperationException(
                        $"Inclusive scan mismatch at {i}: {data[i]}");
            }
        }

        // Exclusive scan
        using (var targetBuffer = stream.Allocate1D<int>(N))
        {
            stream.ExclusiveScan(
                sourceBuffer.View,
                targetBuffer.View,
                identity: 0,
                operation: (a, b) => a + b);
            stream.Synchronize();

            Console.WriteLine("Exclusive Scan:");
            var data = targetBuffer.GetAsArray1D();
            for (int i = 0, e = data.Length; i < e; ++i)
                Console.WriteLine($"Data[{i}] = {data[i]}");
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] != 2 * i)
                    throw new InvalidOperationException(
                        $"Exclusive scan mismatch at {i}: {data[i]}");
            }
        }

        // Cross several groups and include a partially filled final group.
        const int largeN = 1025;
        using var largeSource = stream.Allocate1D<int>(largeN);
        using var largeTarget = stream.Allocate1D<int>(largeN);
        stream.Initialize(largeSource.View, 2);
        stream.InclusiveScan(
            largeSource.View, largeTarget.View, 0, (a, b) => a + b);
        stream.Synchronize();
        var inclusive = largeTarget.GetAsArray1D();
        for (int i = 0; i < largeN; ++i)
        {
            if (inclusive[i] != 2 * (i + 1))
                throw new InvalidOperationException(
                    $"Large inclusive scan mismatch at {i}: {inclusive[i]}");
        }

        stream.ExclusiveScan(
            largeSource.View, largeTarget.View, 0, (a, b) => a + b);
        stream.Synchronize();
        var exclusive = largeTarget.GetAsArray1D();
        for (int i = 0; i < largeN; ++i)
        {
            if (exclusive[i] != 2 * i)
                throw new InvalidOperationException(
                    $"Large exclusive scan mismatch at {i}: {exclusive[i]}");
        }
        Console.WriteLine("Large scan passed.");
    }
}
