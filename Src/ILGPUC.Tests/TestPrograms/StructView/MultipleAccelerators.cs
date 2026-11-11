// Copyright (c) 2026 ILGPU Project. All rights reserved.
using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.ROCm;

static class Kernels
{
    public static void Copy(Index1D index, ArrayView<int> input, ArrayView<int> output) => output[index] = input[index];
}

static class Program
{
    static void Main()
    {
        using var context = Context.Create(builder => builder.ROCm());
        var device = context.GetPreferredDevice(preferCPU: false);
        using var first = device.CreateAccelerator(context);
        using var secondContext = Context.Create(builder => builder.ROCm());
        var secondDevice = secondContext.GetPreferredDevice(preferCPU: false);
        using var second = secondDevice.CreateAccelerator(secondContext);
        var firstStream = first.DefaultStream;
        var secondStream = second.DefaultStream;
        using var firstInput = firstStream.Allocate1D<int>(1);
        using var firstOutput = firstStream.Allocate1D<int>(1);
        using var secondInput = secondStream.Allocate1D<int>(1);
        using var secondOutput = secondStream.Allocate1D<int>(1);
        firstInput.CopyFromCPU(new[] { 17 });
        secondInput.CopyFromCPU(new[] { 29 });
        var a = firstInput.View;
        var b = firstOutput.View;
        var c = secondInput.View;
        var d = secondOutput.View;
        firstStream.Launch((Index1D)1, i => Kernels.Copy(i, a, b));
        secondStream.Launch((Index1D)1, i => Kernels.Copy(i, c, d));
        Console.WriteLine(firstOutput.GetAsArray1D()[0] == 17 && secondOutput.GetAsArray1D()[0] == 29);
        bool repeated = true;
        for (int iteration = 0; iteration < 8; ++iteration)
        {
            firstInput.CopyFromCPU(new[] { 100 + iteration });
            secondInput.CopyFromCPU(new[] { 200 + iteration });
            firstStream.Launch((Index1D)1, i => Kernels.Copy(i, a, b));
            secondStream.Launch((Index1D)1, i => Kernels.Copy(i, c, d));
            repeated &= firstOutput.GetAsArray1D()[0] == 100 + iteration;
            repeated &= secondOutput.GetAsArray1D()[0] == 200 + iteration;
        }
        Console.WriteLine(repeated);
        second.Dispose();
        secondContext.Dispose();
        firstStream.Launch((Index1D)1, i => Kernels.Copy(i, a, b));
        Console.WriteLine(firstOutput.GetAsArray1D()[0] == 107);
        var disposedStream = first.CreateStream();
        disposedStream.Dispose();
        try
        {
            disposedStream.Launch((Index1D)1, i => Kernels.Copy(i, a, b));
            Console.WriteLine(false);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine(true);
        }
    }
}
