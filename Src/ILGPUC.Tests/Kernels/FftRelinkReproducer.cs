// Copyright (C) 2016-2025 Advanced Micro Devices, Inc. All rights reserved.
// Copyright (c) 2026 ILGPU Project
// SPDX-License-Identifier: MIT
// Reduced from ILGPU.Maths BlockStockham216Kernels for an inliner regression.
using System.Runtime.CompilerServices;
using ILGPU;
using ILGPU.Runtime;

namespace ILGPUC.Tests.Kernels;

static class FftRelinkReproducer
{
    private const int Tpb = 9;
    private const int TransformScalars = 2 * 216 * Tpb;
    private readonly struct Complex32
    {
        public readonly float Real, Imaginary;
        public Complex32(float real, float imaginary) { Real = real; Imaginary = imaginary; }
    }

    private readonly struct Radix6_32
        {
            public readonly Complex32 A, B, C, D, E, F;
            public Radix6_32(Complex32 a, Complex32 b, Complex32 c, Complex32 d,
                Complex32 e, Complex32 f)
            {
                A = a; B = b; C = c; D = d; E = e; F = f;
            }
        }

    private static Radix6_32 Butterfly6(Complex32 a, Complex32 b, Complex32 c,
            Complex32 d, Complex32 e, Complex32 f, int direction, float root3)
        {
            float qa = 0.5f, qb = root3;
            float sign = direction;
            float tr0 = a.Real + c.Real + e.Real;
            float ti0 = a.Imaginary + c.Imaginary + e.Imaginary;
            float tr2 = a.Real - qa * (c.Real + e.Real) +
                sign * qb * (c.Imaginary - e.Imaginary);
            float tr4 = a.Real - qa * (c.Real + e.Real) -
                sign * qb * (c.Imaginary - e.Imaginary);
            float ti2 = a.Imaginary - qa * (c.Imaginary + e.Imaginary) -
                sign * qb * (c.Real - e.Real);
            float ti4 = a.Imaginary - qa * (c.Imaginary + e.Imaginary) +
                sign * qb * (c.Real - e.Real);
            float tr1 = b.Real + d.Real + f.Real;
            float ti1 = b.Imaginary + d.Imaginary + f.Imaginary;
            float tr3 = b.Real - qa * (d.Real + f.Real) +
                sign * qb * (d.Imaginary - f.Imaginary);
            float tr5 = b.Real - qa * (d.Real + f.Real) -
                sign * qb * (d.Imaginary - f.Imaginary);
            float ti3 = b.Imaginary - qa * (d.Imaginary + f.Imaginary) -
                sign * qb * (d.Real - f.Real);
            float ti5 = b.Imaginary - qa * (d.Imaginary + f.Imaginary) +
                sign * qb * (d.Real - f.Real);
            float ur1 = qa * tr3 + sign * qb * ti3;
            float ui1 = -sign * qb * tr3 + qa * ti3;
            float ur2 = -qa * tr5 + sign * qb * ti5;
            float ui2 = -sign * qb * tr5 - qa * ti5;
            return new Radix6_32(new(tr0 + tr1, ti0 + ti1),
                new(tr2 + ur1, ti2 + ui1), new(tr4 + ur2, ti4 + ui2),
                new(tr0 - tr1, ti0 - ti1), new(tr2 - ur1, ti2 - ui1),
                new(tr4 - ur2, ti4 - ui2));
        }

    private static Complex32 MultiplyRoot(ref float roots, int exponent,
            int direction, Complex32 value)
        {
            int offset = 2 * exponent;
            float real = Unsafe.Add(ref roots, offset);
            float imaginary = Unsafe.Add(ref roots, offset + 1) * direction;
            return new(value.Real * real - value.Imaginary * imaginary,
                value.Real * imaginary + value.Imaginary * real);
        }

    public static void Length216_32(KernelIndex index, ArrayView<float> input,
            ArrayView<float> output, ArrayView<float> roots, int batches,
            int inputStride, int inputDistance, int outputStride, int outputDistance,
            int direction, float scale)
        {
            ArrayView<float> lds = Group.GetSharedMemory<float>(TransformScalars);
            int lane = index.GroupIndex, thread = lane / Tpb, transform = lane % Tpb;
            int batch = (int)index.GridIndex * Tpb + transform;
            bool valid = batch < batches;
            ref float outputBase = ref output[0], rootBase = ref roots[0];
            float root3 = Unsafe.Add(ref rootBase, 36);
            Complex32 r0 = default, r1 = default, r2 = default, r3 = default, r4 = default, r5 = default;
            if (valid && thread < 36)
            {
                int in0_0 = 2 * (batch * inputDistance + (thread + 0) * inputStride);
                r0 = new(input[in0_0], input[in0_0 + 1]);
                int in0_1 = 2 * (batch * inputDistance + (thread + 36) * inputStride);
                r1 = new(input[in0_1], input[in0_1 + 1]);
                int in0_2 = 2 * (batch * inputDistance + (thread + 72) * inputStride);
                r2 = new(input[in0_2], input[in0_2 + 1]);
                int in0_3 = 2 * (batch * inputDistance + (thread + 108) * inputStride);
                r3 = new(input[in0_3], input[in0_3 + 1]);
                int in0_4 = 2 * (batch * inputDistance + (thread + 144) * inputStride);
                r4 = new(input[in0_4], input[in0_4 + 1]);
                int in0_5 = 2 * (batch * inputDistance + (thread + 180) * inputStride);
                r5 = new(input[in0_5], input[in0_5 + 1]);
                var pass0 = Butterfly6(r0, r1, r2, r3, r4, r5, direction, root3);
                r0 = pass0.A;
                r1 = pass0.B;
                r2 = pass0.C;
                r3 = pass0.D;
                r4 = pass0.E;
                r5 = pass0.F;
            }
            if (valid && thread < 36)
            {
                int st0_0 = 2 * (transform + Tpb * ((thread / 1) * 6 + thread % 1 + 0));
                lds[st0_0] = r0.Real; lds[st0_0 + 1] = r0.Imaginary;
                int st0_1 = 2 * (transform + Tpb * ((thread / 1) * 6 + thread % 1 + 1));
                lds[st0_1] = r1.Real; lds[st0_1 + 1] = r1.Imaginary;
                int st0_2 = 2 * (transform + Tpb * ((thread / 1) * 6 + thread % 1 + 2));
                lds[st0_2] = r2.Real; lds[st0_2 + 1] = r2.Imaginary;
                int st0_3 = 2 * (transform + Tpb * ((thread / 1) * 6 + thread % 1 + 3));
                lds[st0_3] = r3.Real; lds[st0_3 + 1] = r3.Imaginary;
                int st0_4 = 2 * (transform + Tpb * ((thread / 1) * 6 + thread % 1 + 4));
                lds[st0_4] = r4.Real; lds[st0_4 + 1] = r4.Imaginary;
                int st0_5 = 2 * (transform + Tpb * ((thread / 1) * 6 + thread % 1 + 5));
                lds[st0_5] = r5.Real; lds[st0_5 + 1] = r5.Imaginary;
            }
            Group.Barrier();
            if (valid && thread < 36)
            {
                int ld1_0 = 2 * (transform + Tpb * (thread + 0));
                r0 = new(lds[ld1_0], lds[ld1_0 + 1]);
                int ld1_1 = 2 * (transform + Tpb * (thread + 36));
                r1 = new(lds[ld1_1], lds[ld1_1 + 1]);
                r1 = MultiplyRoot(ref rootBase, 6 * (thread % 6) * 1, direction, r1);
                int ld1_2 = 2 * (transform + Tpb * (thread + 72));
                r2 = new(lds[ld1_2], lds[ld1_2 + 1]);
                r2 = MultiplyRoot(ref rootBase, 6 * (thread % 6) * 2, direction, r2);
                int ld1_3 = 2 * (transform + Tpb * (thread + 108));
                r3 = new(lds[ld1_3], lds[ld1_3 + 1]);
                r3 = MultiplyRoot(ref rootBase, 6 * (thread % 6) * 3, direction, r3);
                int ld1_4 = 2 * (transform + Tpb * (thread + 144));
                r4 = new(lds[ld1_4], lds[ld1_4 + 1]);
                r4 = MultiplyRoot(ref rootBase, 6 * (thread % 6) * 4, direction, r4);
                int ld1_5 = 2 * (transform + Tpb * (thread + 180));
                r5 = new(lds[ld1_5], lds[ld1_5 + 1]);
                r5 = MultiplyRoot(ref rootBase, 6 * (thread % 6) * 5, direction, r5);
                var pass1 = Butterfly6(r0, r1, r2, r3, r4, r5, direction, root3);
                r0 = pass1.A;
                r1 = pass1.B;
                r2 = pass1.C;
                r3 = pass1.D;
                r4 = pass1.E;
                r5 = pass1.F;
            }
            Group.Barrier();
            if (valid && thread < 36)
            {
                int st1_0 = 2 * (transform + Tpb * ((thread / 6) * 36 + thread % 6 + 0));
                lds[st1_0] = r0.Real; lds[st1_0 + 1] = r0.Imaginary;
                int st1_1 = 2 * (transform + Tpb * ((thread / 6) * 36 + thread % 6 + 6));
                lds[st1_1] = r1.Real; lds[st1_1 + 1] = r1.Imaginary;
                int st1_2 = 2 * (transform + Tpb * ((thread / 6) * 36 + thread % 6 + 12));
                lds[st1_2] = r2.Real; lds[st1_2 + 1] = r2.Imaginary;
                int st1_3 = 2 * (transform + Tpb * ((thread / 6) * 36 + thread % 6 + 18));
                lds[st1_3] = r3.Real; lds[st1_3 + 1] = r3.Imaginary;
                int st1_4 = 2 * (transform + Tpb * ((thread / 6) * 36 + thread % 6 + 24));
                lds[st1_4] = r4.Real; lds[st1_4 + 1] = r4.Imaginary;
                int st1_5 = 2 * (transform + Tpb * ((thread / 6) * 36 + thread % 6 + 30));
                lds[st1_5] = r5.Real; lds[st1_5 + 1] = r5.Imaginary;
            }
            Group.Barrier();
            if (valid && thread < 36)
            {
                int ld2_0 = 2 * (transform + Tpb * (thread + 0));
                r0 = new(lds[ld2_0], lds[ld2_0 + 1]);
                int ld2_1 = 2 * (transform + Tpb * (thread + 36));
                r1 = new(lds[ld2_1], lds[ld2_1 + 1]);
                r1 = MultiplyRoot(ref rootBase, 1 * (thread % 36) * 1, direction, r1);
                int ld2_2 = 2 * (transform + Tpb * (thread + 72));
                r2 = new(lds[ld2_2], lds[ld2_2 + 1]);
                r2 = MultiplyRoot(ref rootBase, 1 * (thread % 36) * 2, direction, r2);
                int ld2_3 = 2 * (transform + Tpb * (thread + 108));
                r3 = new(lds[ld2_3], lds[ld2_3 + 1]);
                r3 = MultiplyRoot(ref rootBase, 1 * (thread % 36) * 3, direction, r3);
                int ld2_4 = 2 * (transform + Tpb * (thread + 144));
                r4 = new(lds[ld2_4], lds[ld2_4 + 1]);
                r4 = MultiplyRoot(ref rootBase, 1 * (thread % 36) * 4, direction, r4);
                int ld2_5 = 2 * (transform + Tpb * (thread + 180));
                r5 = new(lds[ld2_5], lds[ld2_5 + 1]);
                r5 = MultiplyRoot(ref rootBase, 1 * (thread % 36) * 5, direction, r5);
                var pass2 = Butterfly6(r0, r1, r2, r3, r4, r5, direction, root3);
                r0 = pass2.A;
                r1 = pass2.B;
                r2 = pass2.C;
                r3 = pass2.D;
                r4 = pass2.E;
                r5 = pass2.F;
                int out0 = 2 * (batch * outputDistance + (thread + 0) * outputStride);
                Unsafe.Add(ref outputBase, out0) = r0.Real * scale;
                Unsafe.Add(ref outputBase, out0 + 1) = r0.Imaginary * scale;
                int out1 = 2 * (batch * outputDistance + (thread + 36) * outputStride);
                Unsafe.Add(ref outputBase, out1) = r1.Real * scale;
                Unsafe.Add(ref outputBase, out1 + 1) = r1.Imaginary * scale;
                int out2 = 2 * (batch * outputDistance + (thread + 72) * outputStride);
                Unsafe.Add(ref outputBase, out2) = r2.Real * scale;
                Unsafe.Add(ref outputBase, out2 + 1) = r2.Imaginary * scale;
                int out3 = 2 * (batch * outputDistance + (thread + 108) * outputStride);
                Unsafe.Add(ref outputBase, out3) = r3.Real * scale;
                Unsafe.Add(ref outputBase, out3 + 1) = r3.Imaginary * scale;
                int out4 = 2 * (batch * outputDistance + (thread + 144) * outputStride);
                Unsafe.Add(ref outputBase, out4) = r4.Real * scale;
                Unsafe.Add(ref outputBase, out4 + 1) = r4.Imaginary * scale;
                int out5 = 2 * (batch * outputDistance + (thread + 180) * outputStride);
                Unsafe.Add(ref outputBase, out5) = r5.Real * scale;
                Unsafe.Add(ref outputBase, out5 + 1) = r5.Imaginary * scale;
            }
        }
}
