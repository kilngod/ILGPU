// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: ConvertFloatKernels.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPU;
using ILGPU.Runtime;

namespace ILGPUC.Tests.Kernels;

/// <summary>
/// Kernel definitions for float conversion tests.
/// </summary>
static class ConvertFloatKernels
{
    /// <summary>
    /// Float to int conversion.
    /// </summary>
    public static void FloatToIntKernel(
        Index1D index,
        ArrayView1D<int, Stride1D.Dense> data,
        float value)
    {
        data[index] = (int)value;
    }

    /// <summary>
    /// Int to float conversion, stored as float.
    /// </summary>
    public static void IntToFloatKernel(
        Index1D index,
        ArrayView1D<float, Stride1D.Dense> data,
        int value)
    {
        data[index] = (float)value;
    }

    /// <summary>
    /// Float to double promotion.
    /// </summary>
    public static void FloatToDoubleKernel(
        Index1D index,
        ArrayView1D<double, Stride1D.Dense> data,
        float value)
    {
        data[index] = (double)value;
    }

    /// <summary>
    /// Double to float truncation.
    /// </summary>
    public static void DoubleToFloatKernel(
        Index1D index,
        ArrayView1D<float, Stride1D.Dense> data,
        double value)
    {
        data[index] = (float)value;
    }

    /// <summary>
    /// Converts an unsigned 32-bit integer to float via conv.r.un.
    /// </summary>
    public static void UIntToFloatKernel(
        Index1D index,
        ArrayView1D<float, Stride1D.Dense> data,
        uint value)
    {
        data[index] = (float)value;
    }

    /// <summary>
    /// Converts an unsigned 64-bit integer to double via conv.r.un.
    /// </summary>
    public static void ULongToDoubleKernel(
        Index1D index,
        ArrayView1D<double, Stride1D.Dense> data,
        ulong value)
    {
        data[index] = (double)value;
    }
}
