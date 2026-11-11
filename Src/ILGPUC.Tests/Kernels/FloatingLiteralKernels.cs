// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: FloatingLiteralKernels.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPU;
using ILGPU.Runtime;

namespace ILGPUC.Tests.Kernels;

static class FloatingLiteralKernels
{
    public static void Single(Index1D index, ArrayView1D<float, Stride1D.Dense> output)
    {
        output[0] = 0.70710677f;
        output[1] = 1e-20f;
        output[2] = float.MaxValue;
        output[3] = float.Epsilon;
        output[4] = -0.0f;
        output[5] = float.PositiveInfinity;
        output[6] = float.NegativeInfinity;
        output[7] = float.NaN;
        output[8] = 1.0f;
    }

    public static void Double(Index1D index, ArrayView1D<double, Stride1D.Dense> output)
    {
        output[0] = 0.7071067811865476;
        output[1] = 1e-200;
        output[2] = double.MaxValue;
        output[3] = double.Epsilon;
        output[4] = -0.0;
        output[5] = double.PositiveInfinity;
        output[6] = double.NegativeInfinity;
        output[7] = double.NaN;
        output[8] = 1.0;
    }
}
