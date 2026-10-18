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
using ILGPU.RadixSort;
using ILGPU.Runtime;

namespace AlgorithmsRadixSort;

static class Program
{
    static void Main()
    {
        using var context = Context.CreateDefault();

        var device = SampleAcceleratorPrompt.Select(context);

        using var accelerator = device.CreateAccelerator(context);
        Console.WriteLine($"Using {accelerator}");
        var stream = accelerator.DefaultStream;

        const int N = 32;
        using var buffer = stream.Allocate1D<int>(N);

        // Fill with a descending sequence: N-1, N-2, ..., 1, 0
        stream.Sequence(buffer.View, i => 31 - (int)i);

        // Ascending radix sort
        stream.RadixSort<int, AscendingInt32>(buffer.View);
        stream.Synchronize();

        Console.WriteLine("Ascending RadixSort:");
        var data = buffer.GetAsArray1D();
        for (int i = 0, e = data.Length; i < e; ++i)
        {
            if (data[i] != i)
                throw new InvalidOperationException(
                    $"Ascending radix sort failed at {i}: {data[i]}");
            Console.WriteLine($"Data[{i}] = {data[i]}");
        }

        // Descending radix sort
        stream.RadixSort<int, DescendingInt32>(buffer.View);
        stream.Synchronize();

        Console.WriteLine("Descending RadixSort:");
        data = buffer.GetAsArray1D();
        for (int i = 0, e = data.Length; i < e; ++i)
        {
            if (data[i] != N - 1 - i)
                throw new InvalidOperationException(
                    $"Descending radix sort failed at {i}: {data[i]}");
            Console.WriteLine($"Data[{i}] = {data[i]}");
        }

        const int LargeLength = 1025;
        var input = Enumerable.Range(0, LargeLength)
            .Select(i => (i * 37 % 211) - 105)
            .ToArray();
        using var largeBuffer = stream.Allocate1D<int>(LargeLength);
        largeBuffer.CopyFromCPU(input);

        var expected = (int[])input.Clone();
        Array.Sort(expected);
        stream.RadixSort<int, AscendingInt32>(largeBuffer.View);
        stream.Synchronize();
        data = largeBuffer.GetAsArray1D();
        for (int i = 0; i < LargeLength; ++i)
        {
            if (data[i] != expected[i])
                throw new InvalidOperationException(
                    $"Large ascending radix sort failed at {i}: {data[i]}");
        }
        Console.WriteLine("Large ascending radix sort passed.");

        Array.Reverse(expected);
        stream.RadixSort<int, DescendingInt32>(largeBuffer.View);
        stream.Synchronize();
        data = largeBuffer.GetAsArray1D();
        for (int i = 0; i < LargeLength; ++i)
        {
            if (data[i] != expected[i])
                throw new InvalidOperationException(
                    $"Large descending radix sort failed at {i}: {data[i]}");
        }
        Console.WriteLine("Large descending radix sort passed.");
    }
}
