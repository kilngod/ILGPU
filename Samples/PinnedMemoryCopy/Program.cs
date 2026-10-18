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


// disable: max_line_length
using System;
using System.Linq;
using System.Runtime.InteropServices;
using ILGPU;
using ILGPU.Runtime;

namespace PinnedMemoryCopy;

static class Program
{
    static void PerformPinnedCopyUsingGCHandle(Accelerator accelerator, int dataSize)
    {
        var array = new int[dataSize];
        for (int i = 0; i < array.Length; i++)
            array[i] = i * 3 + 1;
        var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        try
        {
            using var bufferOnGPU = accelerator.DefaultStream.Allocate1D<int>(array.Length);
            var stream = accelerator.DefaultStream;

            using var scope = accelerator.CreatePageLockFromPinned(array);
            bufferOnGPU.View.CopyFrom(stream, scope.ArrayView);

            stream.Synchronize();
            VerifyCopy(bufferOnGPU.GetAsArray1D(), dataSize);
        }
        finally
        {
            handle.Free();
        }
    }

    static void PerformPinnedCopyUsingGCAllocateArray(Accelerator accelerator, int dataSize)
    {
        var array = GC.AllocateArray<int>(dataSize, pinned: true);
        for (int i = 0; i < array.Length; i++)
            array[i] = i * 3 + 1;

        using var bufferOnGPU = accelerator.DefaultStream.Allocate1D<int>(array.Length);
        var stream = accelerator.DefaultStream;

        using var scope = accelerator.CreatePageLockFromPinned(array);
        bufferOnGPU.View.CopyFrom(stream, scope.ArrayView);

        stream.Synchronize();
        VerifyCopy(bufferOnGPU.GetAsArray1D(), dataSize);
    }

    static void PerformPinnedCopyUsingAllocatePageLockedArray(
        Accelerator accelerator,
        int dataSize)
    {
        using var array = accelerator.AllocatePageLocked1D<int>(dataSize);
        for (int i = 0; i < dataSize; i++)
            array[i] = i * 3 + 1;

        using var bufferOnGPU = accelerator.DefaultStream.Allocate1D<int>(array.Length);
        var stream = accelerator.DefaultStream;

        bufferOnGPU.View.CopyFrom(stream, array.ArrayView);

        stream.Synchronize();

        // Retrieve results into existing page locked array
        bufferOnGPU.View.CopyTo(stream, array.ArrayView);

        // Retrieve results into a new array
        var result1 = bufferOnGPU.GetAsArray1D();

        // Retrieve results into a new page locked array
        using var result2 = bufferOnGPU.View.GetAsPageLocked1D();
        stream.Synchronize();
        VerifyCopy(result1, dataSize);
        VerifyCopy(result2.GetArray(), dataSize);
        VerifyCopy(array.GetArray(), dataSize);
    }

    static void VerifyCopy(int[] data, int dataSize)
    {
        for (int i = 0; i < dataSize; i++)
        {
            if (data[i] != i * 3 + 1)
                throw new InvalidOperationException(
                    $"Pinned copy mismatch at {i}: {data[i]}");
        }
    }

    static void Main()
    {
        const int DataSize = 1024;

        using (var context = Context.CreateDefault())
        {
            var device = SampleAcceleratorPrompt.Select(context);

            using var accelerator = device.CreateAccelerator(context);
            Console.WriteLine($"Using {accelerator}");

            PerformPinnedCopyUsingGCHandle(accelerator, DataSize);
            PerformPinnedCopyUsingGCAllocateArray(accelerator, DataSize);
            PerformPinnedCopyUsingAllocatePageLockedArray(accelerator, DataSize);
        }

        // Enable automatic page locking
        using (var context = Context.Create(builder =>
            builder.Default().PageLocking(PageLockingMode.Auto)))
        {
            var device = SampleAcceleratorPrompt.Select(context);

            using var accelerator = device.CreateAccelerator(context);
            Console.WriteLine($"Using {accelerator} (Automatic Page Locking)");

            PerformPinnedCopyUsingAllocatePageLockedArray(accelerator, DataSize);
        }

        Console.WriteLine("Done.");
    }
}
