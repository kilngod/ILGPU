// Copyright (c) 2026 ILGPU.Maths contributors
// Copyright (C) Advanced Micro Devices, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using ILGPU;
using ILGPU.Runtime;

#pragma warning disable CS0649 // Preserve the original register aggregate layout.

namespace ILGPUC.Tests.Kernels;

/// <summary>gfx1201 float C2C SBRR child for the 51x104x216 source profile.</summary>
/// <remarks>This source-profile child remains undispatched pending verification.</remarks>
public static class Radix13PhiReproducer
{
    private struct Registers { public Complex32 R0,R1,R2,R3,R4,R5,R6,R7; }
    private struct Three { public Complex32 R0,R1,R2; }
    private struct Four { public Complex32 R0,R1,R2,R3; }
    private struct Registers52
    {
        public Complex32 R0,R1,R2,R3;
        public Complex32 R4,R5,R6,R7;
        public Complex32 R8,R9,R10,R11;
        public Complex32 R12,R13,R14,R15;
        public Complex32 R16,R17,R18,R19;
        public Complex32 R20,R21,R22,R23;
        public Complex32 R24,R25,R26,R27;
        public Complex32 R28,R29,R30,R31;
        public Complex32 R32,R33,R34,R35;
        public Complex32 R36,R37,R38,R39;
        public Complex32 R40,R41,R42,R43;
        public Complex32 R44,R45,R46,R47;
        public Complex32 R48,R49,R50,R51;
    }
    private struct Registers16 { public Complex32 R0,R1,R2,R3,R4,R5,R6,R7,R8,R9,R10,R11,R12,R13,R14,R15; }
    private struct Registers10 { public Complex32 R0,R1,R2,R3,R4,R5,R6,R7,R8,R9; }
    private struct Registers5 { public Complex32 R0,R1,R2,R3,R4; }

    /// <summary>Executes one source-profile fused SBRR workgroup.</summary>
    public static void Sbrr51x104x216RealSingle(KernelIndex index, ArrayView<float> input, ArrayView<float> output, ArrayView<float> roots, ArrayView<float> ppStageRoots, ArrayView<float> ppRoots, int batches)
    {
        ArrayView<Complex32> lds = Group.GetSharedMemory<Complex32>(2652);
        int lane = index.GroupIndex, block = (int)index.GridIndex;
        int thread = lane % 2, transform = block * 52 + lane / 2;
        int batch = transform / 22464;
        int y = transform % 104 / 52 + transform * 2 % 104;
        int z = transform / 104 % 216;
        int inputBase = 50 * (y + 104 * (z + 216 * batch));
        int outputBase = 51 * (y + 104 * (z + 216 * batch));
        int local = 51 * (lane / 2);
        bool valid = batch < batches;
        const int direction = -1;
        Registers52 r = default;
        Registers16 thirteen;
        Registers10 ten;
        Registers5 five;
        Four four;
        if (valid)
        {
            lds[local + thread + 0] = Load(input, inputBase + thread + 0);
            lds[local + thread + 2] = Load(input, inputBase + thread + 2);
            lds[local + thread + 4] = Load(input, inputBase + thread + 4);
            lds[local + thread + 6] = Load(input, inputBase + thread + 6);
            lds[local + thread + 8] = Load(input, inputBase + thread + 8);
            lds[local + thread + 10] = Load(input, inputBase + thread + 10);
            lds[local + thread + 12] = Load(input, inputBase + thread + 12);
            lds[local + thread + 14] = Load(input, inputBase + thread + 14);
            lds[local + thread + 16] = Load(input, inputBase + thread + 16);
            lds[local + thread + 18] = Load(input, inputBase + thread + 18);
            lds[local + thread + 20] = Load(input, inputBase + thread + 20);
            lds[local + thread + 22] = Load(input, inputBase + thread + 22);
            lds[local + thread + 24] = Load(input, inputBase + thread + 24);
            lds[local + thread + 26] = Load(input, inputBase + thread + 26);
            lds[local + thread + 28] = Load(input, inputBase + thread + 28);
            lds[local + thread + 30] = Load(input, inputBase + thread + 30);
            lds[local + thread + 32] = Load(input, inputBase + thread + 32);
            lds[local + thread + 34] = Load(input, inputBase + thread + 34);
            lds[local + thread + 36] = Load(input, inputBase + thread + 36);
            lds[local + thread + 38] = Load(input, inputBase + thread + 38);
            lds[local + thread + 40] = Load(input, inputBase + thread + 40);
            lds[local + thread + 42] = Load(input, inputBase + thread + 42);
            lds[local + thread + 44] = Load(input, inputBase + thread + 44);
            lds[local + thread + 46] = Load(input, inputBase + thread + 46);
            lds[local + thread + 48] = Load(input, inputBase + thread + 48);
        }
        Group.Barrier();
        // rocFFT pass 0: radix 10, prior product 1.
        r.R0 = lds[local + thread + 0];
        r.R1 = lds[local + thread + 5];
        r.R2 = lds[local + thread + 10];
        r.R3 = lds[local + thread + 15];
        r.R4 = lds[local + thread + 20];
        r.R5 = lds[local + thread + 25];
        r.R6 = lds[local + thread + 30];
        r.R7 = lds[local + thread + 35];
        r.R8 = lds[local + thread + 40];
        r.R9 = lds[local + thread + 45];
        ten = new Registers10 { R0=r.R0,R1=r.R1,R2=r.R2,R3=r.R3,R4=r.R4,R5=r.R5,R6=r.R6,R7=r.R7,R8=r.R8,R9=r.R9 };
        ten = direction < 0 ? ForwardRadix10(ten) : InverseRadix10(ten);
        r.R0 = ten.R0;
        r.R1 = ten.R1;
        r.R2 = ten.R2;
        r.R3 = ten.R3;
        r.R4 = ten.R4;
        r.R5 = ten.R5;
        r.R6 = ten.R6;
        r.R7 = ten.R7;
        r.R8 = ten.R8;
        r.R9 = ten.R9;
        r.R10 = lds[local + thread + 4];
        r.R11 = lds[local + thread + 9];
        r.R12 = lds[local + thread + 14];
        r.R13 = lds[local + thread + 19];
        r.R14 = lds[local + thread + 24];
        r.R15 = lds[local + thread + 29];
        r.R16 = lds[local + thread + 34];
        r.R17 = lds[local + thread + 39];
        r.R18 = lds[local + thread + 44];
        r.R19 = lds[local + thread + 49];
        ten = new Registers10 { R0=r.R10,R1=r.R11,R2=r.R12,R3=r.R13,R4=r.R14,R5=r.R15,R6=r.R16,R7=r.R17,R8=r.R18,R9=r.R19 };
        ten = direction < 0 ? ForwardRadix10(ten) : InverseRadix10(ten);
        r.R10 = ten.R0;
        r.R11 = ten.R1;
        r.R12 = ten.R2;
        r.R13 = ten.R3;
        r.R14 = ten.R4;
        r.R15 = ten.R5;
        r.R16 = ten.R6;
        r.R17 = ten.R7;
        r.R18 = ten.R8;
        r.R19 = ten.R9;
        if (thread + 4 < 5)
        {
            r.R20 = lds[local + thread + 8];
            r.R21 = lds[local + thread + 13];
            r.R22 = lds[local + thread + 18];
            r.R23 = lds[local + thread + 23];
            r.R24 = lds[local + thread + 28];
            r.R25 = lds[local + thread + 33];
            r.R26 = lds[local + thread + 38];
            r.R27 = lds[local + thread + 43];
            r.R28 = lds[local + thread + 48];
            r.R29 = lds[local + thread + 53];
            ten = new Registers10 { R0=r.R20,R1=r.R21,R2=r.R22,R3=r.R23,R4=r.R24,R5=r.R25,R6=r.R26,R7=r.R27,R8=r.R28,R9=r.R29 };
            ten = direction < 0 ? ForwardRadix10(ten) : InverseRadix10(ten);
            r.R20 = ten.R0;
            r.R21 = ten.R1;
            r.R22 = ten.R2;
            r.R23 = ten.R3;
            r.R24 = ten.R4;
            r.R25 = ten.R5;
            r.R26 = ten.R6;
            r.R27 = ten.R7;
            r.R28 = ten.R8;
            r.R29 = ten.R9;
        }
        Group.Barrier();
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 0] = r.R0;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 1] = r.R1;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 2] = r.R2;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 3] = r.R3;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 4] = r.R4;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 5] = r.R5;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 6] = r.R6;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 7] = r.R7;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 8] = r.R8;
        lds[local + (thread + 0) / 1 * 10 + (thread + 0) % 1 + 9] = r.R9;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 0] = r.R10;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 1] = r.R11;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 2] = r.R12;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 3] = r.R13;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 4] = r.R14;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 5] = r.R15;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 6] = r.R16;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 7] = r.R17;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 8] = r.R18;
        lds[local + (thread + 2) / 1 * 10 + (thread + 2) % 1 + 9] = r.R19;
        if (thread + 4 < 5)
        {
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 0] = r.R20;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 1] = r.R21;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 2] = r.R22;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 3] = r.R23;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 4] = r.R24;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 5] = r.R25;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 6] = r.R26;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 7] = r.R27;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 8] = r.R28;
            lds[local + (thread + 4) / 1 * 10 + (thread + 4) % 1 + 9] = r.R29;
        }
        Group.Barrier();
        // rocFFT pass 1: radix 5, prior product 10.
        r.R0 = lds[local + thread + 0];
        r.R1 = lds[local + thread + 10];
        r.R2 = lds[local + thread + 20];
        r.R3 = lds[local + thread + 30];
        r.R4 = lds[local + thread + 40];
        r.R1 = MultiplyRoot(r.R1, roots, 0 + 4 * ((thread + 0) % 10) + 0, direction);
        r.R2 = MultiplyRoot(r.R2, roots, 0 + 4 * ((thread + 0) % 10) + 1, direction);
        r.R3 = MultiplyRoot(r.R3, roots, 0 + 4 * ((thread + 0) % 10) + 2, direction);
        r.R4 = MultiplyRoot(r.R4, roots, 0 + 4 * ((thread + 0) % 10) + 3, direction);
        five = new Registers5 { R0=r.R0,R1=r.R1,R2=r.R2,R3=r.R3,R4=r.R4 };
        five = direction < 0 ? ForwardRadix5(five) : InverseRadix5(five);
        r.R0 = five.R0;
        r.R1 = five.R1;
        r.R2 = five.R2;
        r.R3 = five.R3;
        r.R4 = five.R4;
        r.R5 = lds[local + thread + 4];
        r.R6 = lds[local + thread + 14];
        r.R7 = lds[local + thread + 24];
        r.R8 = lds[local + thread + 34];
        r.R9 = lds[local + thread + 44];
        r.R6 = MultiplyRoot(r.R6, roots, 0 + 4 * ((thread + 2) % 10) + 0, direction);
        r.R7 = MultiplyRoot(r.R7, roots, 0 + 4 * ((thread + 2) % 10) + 1, direction);
        r.R8 = MultiplyRoot(r.R8, roots, 0 + 4 * ((thread + 2) % 10) + 2, direction);
        r.R9 = MultiplyRoot(r.R9, roots, 0 + 4 * ((thread + 2) % 10) + 3, direction);
        five = new Registers5 { R0=r.R5,R1=r.R6,R2=r.R7,R3=r.R8,R4=r.R9 };
        five = direction < 0 ? ForwardRadix5(five) : InverseRadix5(five);
        r.R5 = five.R0;
        r.R6 = five.R1;
        r.R7 = five.R2;
        r.R8 = five.R3;
        r.R9 = five.R4;
        r.R10 = lds[local + thread + 8];
        r.R11 = lds[local + thread + 18];
        r.R12 = lds[local + thread + 28];
        r.R13 = lds[local + thread + 38];
        r.R14 = lds[local + thread + 48];
        r.R11 = MultiplyRoot(r.R11, roots, 0 + 4 * ((thread + 4) % 10) + 0, direction);
        r.R12 = MultiplyRoot(r.R12, roots, 0 + 4 * ((thread + 4) % 10) + 1, direction);
        r.R13 = MultiplyRoot(r.R13, roots, 0 + 4 * ((thread + 4) % 10) + 2, direction);
        r.R14 = MultiplyRoot(r.R14, roots, 0 + 4 * ((thread + 4) % 10) + 3, direction);
        five = new Registers5 { R0=r.R10,R1=r.R11,R2=r.R12,R3=r.R13,R4=r.R14 };
        five = direction < 0 ? ForwardRadix5(five) : InverseRadix5(five);
        r.R10 = five.R0;
        r.R11 = five.R1;
        r.R12 = five.R2;
        r.R13 = five.R3;
        r.R14 = five.R4;
        r.R15 = lds[local + thread + 12];
        r.R16 = lds[local + thread + 22];
        r.R17 = lds[local + thread + 32];
        r.R18 = lds[local + thread + 42];
        r.R19 = lds[local + thread + 52];
        r.R16 = MultiplyRoot(r.R16, roots, 0 + 4 * ((thread + 6) % 10) + 0, direction);
        r.R17 = MultiplyRoot(r.R17, roots, 0 + 4 * ((thread + 6) % 10) + 1, direction);
        r.R18 = MultiplyRoot(r.R18, roots, 0 + 4 * ((thread + 6) % 10) + 2, direction);
        r.R19 = MultiplyRoot(r.R19, roots, 0 + 4 * ((thread + 6) % 10) + 3, direction);
        five = new Registers5 { R0=r.R15,R1=r.R16,R2=r.R17,R3=r.R18,R4=r.R19 };
        five = direction < 0 ? ForwardRadix5(five) : InverseRadix5(five);
        r.R15 = five.R0;
        r.R16 = five.R1;
        r.R17 = five.R2;
        r.R18 = five.R3;
        r.R19 = five.R4;
        r.R20 = lds[local + thread + 16];
        r.R21 = lds[local + thread + 26];
        r.R22 = lds[local + thread + 36];
        r.R23 = lds[local + thread + 46];
        r.R24 = lds[local + thread + 56];
        r.R21 = MultiplyRoot(r.R21, roots, 0 + 4 * ((thread + 8) % 10) + 0, direction);
        r.R22 = MultiplyRoot(r.R22, roots, 0 + 4 * ((thread + 8) % 10) + 1, direction);
        r.R23 = MultiplyRoot(r.R23, roots, 0 + 4 * ((thread + 8) % 10) + 2, direction);
        r.R24 = MultiplyRoot(r.R24, roots, 0 + 4 * ((thread + 8) % 10) + 3, direction);
        five = new Registers5 { R0=r.R20,R1=r.R21,R2=r.R22,R3=r.R23,R4=r.R24 };
        five = direction < 0 ? ForwardRadix5(five) : InverseRadix5(five);
        r.R20 = five.R0;
        r.R21 = five.R1;
        r.R22 = five.R2;
        r.R23 = five.R3;
        r.R24 = five.R4;
        Group.Barrier();
        lds[local + (thread + 0) / 10 * 50 + (thread + 0) % 10 + 0] = r.R0;
        lds[local + (thread + 0) / 10 * 50 + (thread + 0) % 10 + 10] = r.R1;
        lds[local + (thread + 0) / 10 * 50 + (thread + 0) % 10 + 20] = r.R2;
        lds[local + (thread + 0) / 10 * 50 + (thread + 0) % 10 + 30] = r.R3;
        lds[local + (thread + 0) / 10 * 50 + (thread + 0) % 10 + 40] = r.R4;
        lds[local + (thread + 2) / 10 * 50 + (thread + 2) % 10 + 0] = r.R5;
        lds[local + (thread + 2) / 10 * 50 + (thread + 2) % 10 + 10] = r.R6;
        lds[local + (thread + 2) / 10 * 50 + (thread + 2) % 10 + 20] = r.R7;
        lds[local + (thread + 2) / 10 * 50 + (thread + 2) % 10 + 30] = r.R8;
        lds[local + (thread + 2) / 10 * 50 + (thread + 2) % 10 + 40] = r.R9;
        lds[local + (thread + 4) / 10 * 50 + (thread + 4) % 10 + 0] = r.R10;
        lds[local + (thread + 4) / 10 * 50 + (thread + 4) % 10 + 10] = r.R11;
        lds[local + (thread + 4) / 10 * 50 + (thread + 4) % 10 + 20] = r.R12;
        lds[local + (thread + 4) / 10 * 50 + (thread + 4) % 10 + 30] = r.R13;
        lds[local + (thread + 4) / 10 * 50 + (thread + 4) % 10 + 40] = r.R14;
        lds[local + (thread + 6) / 10 * 50 + (thread + 6) % 10 + 0] = r.R15;
        lds[local + (thread + 6) / 10 * 50 + (thread + 6) % 10 + 10] = r.R16;
        lds[local + (thread + 6) / 10 * 50 + (thread + 6) % 10 + 20] = r.R17;
        lds[local + (thread + 6) / 10 * 50 + (thread + 6) % 10 + 30] = r.R18;
        lds[local + (thread + 6) / 10 * 50 + (thread + 6) % 10 + 40] = r.R19;
        lds[local + (thread + 8) / 10 * 50 + (thread + 8) % 10 + 0] = r.R20;
        lds[local + (thread + 8) / 10 * 50 + (thread + 8) % 10 + 10] = r.R21;
        lds[local + (thread + 8) / 10 * 50 + (thread + 8) % 10 + 20] = r.R22;
        lds[local + (thread + 8) / 10 * 50 + (thread + 8) % 10 + 30] = r.R23;
        lds[local + (thread + 8) / 10 * 50 + (thread + 8) % 10 + 40] = r.R24;
        Group.Barrier();
        Post(lds, local, thread + 0, roots);
        Post(lds, local, thread + 2, roots);
        Post(lds, local, thread + 4, roots);
        Post(lds, local, thread + 6, roots);
        Post(lds, local, thread + 8, roots);
        Post(lds, local, thread + 10, roots);
        Post(lds, local, thread + 12, roots);
        Post(lds, local, thread + 14, roots);
        Post(lds, local, thread + 16, roots);
        Post(lds, local, thread + 18, roots);
        Post(lds, local, thread + 20, roots);
        Post(lds, local, thread + 22, roots);
        Post(lds, local, thread + 24, roots);
        Group.Barrier();
        int pp = (block * 52 + lane) % 51;
        // Source partial pass 0: radix 13.
        if (lane < 51)
        {
            r.R0 = lds[pp + 0];
            r.R1 = lds[pp + 204];
            r.R2 = lds[pp + 408];
            r.R3 = lds[pp + 612];
            r.R4 = lds[pp + 816];
            r.R5 = lds[pp + 1020];
            r.R6 = lds[pp + 1224];
            r.R7 = lds[pp + 1428];
            r.R8 = lds[pp + 1632];
            r.R9 = lds[pp + 1836];
            r.R10 = lds[pp + 2040];
            r.R11 = lds[pp + 2244];
            r.R12 = lds[pp + 2448];
            thirteen = new Registers16 { R0=r.R0,R1=r.R1,R2=r.R2,R3=r.R3,R4=r.R4,R5=r.R5,R6=r.R6,R7=r.R7,R8=r.R8,R9=r.R9,R10=r.R10,R11=r.R11,R12=r.R12 };
            thirteen = Radix13(thirteen,direction);
            r.R0 = thirteen.R0;
            r.R1 = thirteen.R1;
            r.R2 = thirteen.R2;
            r.R3 = thirteen.R3;
            r.R4 = thirteen.R4;
            r.R5 = thirteen.R5;
            r.R6 = thirteen.R6;
            r.R7 = thirteen.R7;
            r.R8 = thirteen.R8;
            r.R9 = thirteen.R9;
            r.R10 = thirteen.R10;
            r.R11 = thirteen.R11;
            r.R12 = thirteen.R12;
            r.R13 = lds[pp + 51];
            r.R14 = lds[pp + 255];
            r.R15 = lds[pp + 459];
            r.R16 = lds[pp + 663];
            r.R17 = lds[pp + 867];
            r.R18 = lds[pp + 1071];
            r.R19 = lds[pp + 1275];
            r.R20 = lds[pp + 1479];
            r.R21 = lds[pp + 1683];
            r.R22 = lds[pp + 1887];
            r.R23 = lds[pp + 2091];
            r.R24 = lds[pp + 2295];
            r.R25 = lds[pp + 2499];
            thirteen = new Registers16 { R0=r.R13,R1=r.R14,R2=r.R15,R3=r.R16,R4=r.R17,R5=r.R18,R6=r.R19,R7=r.R20,R8=r.R21,R9=r.R22,R10=r.R23,R11=r.R24,R12=r.R25 };
            thirteen = Radix13(thirteen,direction);
            r.R13 = thirteen.R0;
            r.R14 = thirteen.R1;
            r.R15 = thirteen.R2;
            r.R16 = thirteen.R3;
            r.R17 = thirteen.R4;
            r.R18 = thirteen.R5;
            r.R19 = thirteen.R6;
            r.R20 = thirteen.R7;
            r.R21 = thirteen.R8;
            r.R22 = thirteen.R9;
            r.R23 = thirteen.R10;
            r.R24 = thirteen.R11;
            r.R25 = thirteen.R12;
            r.R26 = lds[pp + 102];
            r.R27 = lds[pp + 306];
            r.R28 = lds[pp + 510];
            r.R29 = lds[pp + 714];
            r.R30 = lds[pp + 918];
            r.R31 = lds[pp + 1122];
            r.R32 = lds[pp + 1326];
            r.R33 = lds[pp + 1530];
            r.R34 = lds[pp + 1734];
            r.R35 = lds[pp + 1938];
            r.R36 = lds[pp + 2142];
            r.R37 = lds[pp + 2346];
            r.R38 = lds[pp + 2550];
            thirteen = new Registers16 { R0=r.R26,R1=r.R27,R2=r.R28,R3=r.R29,R4=r.R30,R5=r.R31,R6=r.R32,R7=r.R33,R8=r.R34,R9=r.R35,R10=r.R36,R11=r.R37,R12=r.R38 };
            thirteen = Radix13(thirteen,direction);
            r.R26 = thirteen.R0;
            r.R27 = thirteen.R1;
            r.R28 = thirteen.R2;
            r.R29 = thirteen.R3;
            r.R30 = thirteen.R4;
            r.R31 = thirteen.R5;
            r.R32 = thirteen.R6;
            r.R33 = thirteen.R7;
            r.R34 = thirteen.R8;
            r.R35 = thirteen.R9;
            r.R36 = thirteen.R10;
            r.R37 = thirteen.R11;
            r.R38 = thirteen.R12;
            r.R39 = lds[pp + 153];
            r.R40 = lds[pp + 357];
            r.R41 = lds[pp + 561];
            r.R42 = lds[pp + 765];
            r.R43 = lds[pp + 969];
            r.R44 = lds[pp + 1173];
            r.R45 = lds[pp + 1377];
            r.R46 = lds[pp + 1581];
            r.R47 = lds[pp + 1785];
            r.R48 = lds[pp + 1989];
            r.R49 = lds[pp + 2193];
            r.R50 = lds[pp + 2397];
            r.R51 = lds[pp + 2601];
            thirteen = new Registers16 { R0=r.R39,R1=r.R40,R2=r.R41,R3=r.R42,R4=r.R43,R5=r.R44,R6=r.R45,R7=r.R46,R8=r.R47,R9=r.R48,R10=r.R49,R11=r.R50,R12=r.R51 };
            thirteen = Radix13(thirteen,direction);
            r.R39 = thirteen.R0;
            r.R40 = thirteen.R1;
            r.R41 = thirteen.R2;
            r.R42 = thirteen.R3;
            r.R43 = thirteen.R4;
            r.R44 = thirteen.R5;
            r.R45 = thirteen.R6;
            r.R46 = thirteen.R7;
            r.R47 = thirteen.R8;
            r.R48 = thirteen.R9;
            r.R49 = thirteen.R10;
            r.R50 = thirteen.R11;
            r.R51 = thirteen.R12;
        }
        Group.Barrier();
        if (lane < 51)
        {
            lds[pp + 0] = r.R0;
            lds[pp + 51] = r.R1;
            lds[pp + 102] = r.R2;
            lds[pp + 153] = r.R3;
            lds[pp + 204] = r.R4;
            lds[pp + 255] = r.R5;
            lds[pp + 306] = r.R6;
            lds[pp + 357] = r.R7;
            lds[pp + 408] = r.R8;
            lds[pp + 459] = r.R9;
            lds[pp + 510] = r.R10;
            lds[pp + 561] = r.R11;
            lds[pp + 612] = r.R12;
            lds[pp + 663] = r.R13;
            lds[pp + 714] = r.R14;
            lds[pp + 765] = r.R15;
            lds[pp + 816] = r.R16;
            lds[pp + 867] = r.R17;
            lds[pp + 918] = r.R18;
            lds[pp + 969] = r.R19;
            lds[pp + 1020] = r.R20;
            lds[pp + 1071] = r.R21;
            lds[pp + 1122] = r.R22;
            lds[pp + 1173] = r.R23;
            lds[pp + 1224] = r.R24;
            lds[pp + 1275] = r.R25;
            lds[pp + 1326] = r.R26;
            lds[pp + 1377] = r.R27;
            lds[pp + 1428] = r.R28;
            lds[pp + 1479] = r.R29;
            lds[pp + 1530] = r.R30;
            lds[pp + 1581] = r.R31;
            lds[pp + 1632] = r.R32;
            lds[pp + 1683] = r.R33;
            lds[pp + 1734] = r.R34;
            lds[pp + 1785] = r.R35;
            lds[pp + 1836] = r.R36;
            lds[pp + 1887] = r.R37;
            lds[pp + 1938] = r.R38;
            lds[pp + 1989] = r.R39;
            lds[pp + 2040] = r.R40;
            lds[pp + 2091] = r.R41;
            lds[pp + 2142] = r.R42;
            lds[pp + 2193] = r.R43;
            lds[pp + 2244] = r.R44;
            lds[pp + 2295] = r.R45;
            lds[pp + 2346] = r.R46;
            lds[pp + 2397] = r.R47;
            lds[pp + 2448] = r.R48;
            lds[pp + 2499] = r.R49;
            lds[pp + 2550] = r.R50;
            lds[pp + 2601] = r.R51;
        }
        Group.Barrier();
        // Source partial pass 1: radix 4.
        if (lane < 51)
        {
            r.R0 = lds[pp + 0];
            r.R1 = lds[pp + 663];
            r.R2 = lds[pp + 1326];
            r.R3 = lds[pp + 1989];
            r.R1 = MultiplyRoot(r.R1, ppStageRoots, 0, direction);
            r.R2 = MultiplyRoot(r.R2, ppStageRoots, 1, direction);
            r.R3 = MultiplyRoot(r.R3, ppStageRoots, 2, direction);
            four = Radix4(r.R0,r.R1,r.R2,r.R3,direction);
            r.R0 = four.R0;
            r.R1 = four.R1;
            r.R2 = four.R2;
            r.R3 = four.R3;
            r.R0 = MultiplyRoot(r.R0, ppRoots, (block % 2) * 104 + 0, direction);
            r.R1 = MultiplyRoot(r.R1, ppRoots, (block % 2) * 104 + 13, direction);
            r.R2 = MultiplyRoot(r.R2, ppRoots, (block % 2) * 104 + 26, direction);
            r.R3 = MultiplyRoot(r.R3, ppRoots, (block % 2) * 104 + 39, direction);
            r.R4 = lds[pp + 51];
            r.R5 = lds[pp + 714];
            r.R6 = lds[pp + 1377];
            r.R7 = lds[pp + 2040];
            r.R5 = MultiplyRoot(r.R5, ppStageRoots, 3, direction);
            r.R6 = MultiplyRoot(r.R6, ppStageRoots, 4, direction);
            r.R7 = MultiplyRoot(r.R7, ppStageRoots, 5, direction);
            four = Radix4(r.R2,r.R5,r.R6,r.R7,direction);
            r.R4 = four.R0;
            r.R5 = four.R1;
            r.R6 = four.R2;
            r.R7 = four.R3;
            r.R4 = MultiplyRoot(r.R4, ppRoots, (block % 2) * 104 + 1, direction);
            r.R5 = MultiplyRoot(r.R5, ppRoots, (block % 2) * 104 + 14, direction);
            r.R6 = MultiplyRoot(r.R6, ppRoots, (block % 2) * 104 + 27, direction);
            r.R7 = MultiplyRoot(r.R7, ppRoots, (block % 2) * 104 + 40, direction);
            r.R8 = lds[pp + 102];
            r.R9 = lds[pp + 765];
            r.R10 = lds[pp + 1428];
            r.R11 = lds[pp + 2091];
            r.R9 = MultiplyRoot(r.R9, ppStageRoots, 6, direction);
            r.R10 = MultiplyRoot(r.R10, ppStageRoots, 7, direction);
            r.R11 = MultiplyRoot(r.R11, ppStageRoots, 8, direction);
            four = Radix4(r.R4,r.R9,r.R10,r.R11,direction);
            r.R8 = four.R0;
            r.R9 = four.R1;
            r.R10 = four.R2;
            r.R11 = four.R3;
            r.R8 = MultiplyRoot(r.R8, ppRoots, (block % 2) * 104 + 2, direction);
            r.R9 = MultiplyRoot(r.R9, ppRoots, (block % 2) * 104 + 15, direction);
            r.R10 = MultiplyRoot(r.R10, ppRoots, (block % 2) * 104 + 28, direction);
            r.R11 = MultiplyRoot(r.R11, ppRoots, (block % 2) * 104 + 41, direction);
            r.R12 = lds[pp + 153];
            r.R13 = lds[pp + 816];
            r.R14 = lds[pp + 1479];
            r.R15 = lds[pp + 2142];
            r.R13 = MultiplyRoot(r.R13, ppStageRoots, 9, direction);
            r.R14 = MultiplyRoot(r.R14, ppStageRoots, 10, direction);
            r.R15 = MultiplyRoot(r.R15, ppStageRoots, 11, direction);
            four = Radix4(r.R6,r.R13,r.R14,r.R15,direction);
            r.R12 = four.R0;
            r.R13 = four.R1;
            r.R14 = four.R2;
            r.R15 = four.R3;
            r.R12 = MultiplyRoot(r.R12, ppRoots, (block % 2) * 104 + 3, direction);
            r.R13 = MultiplyRoot(r.R13, ppRoots, (block % 2) * 104 + 16, direction);
            r.R14 = MultiplyRoot(r.R14, ppRoots, (block % 2) * 104 + 29, direction);
            r.R15 = MultiplyRoot(r.R15, ppRoots, (block % 2) * 104 + 42, direction);
            r.R16 = lds[pp + 204];
            r.R17 = lds[pp + 867];
            r.R18 = lds[pp + 1530];
            r.R19 = lds[pp + 2193];
            r.R17 = MultiplyRoot(r.R17, ppStageRoots, 12, direction);
            r.R18 = MultiplyRoot(r.R18, ppStageRoots, 13, direction);
            r.R19 = MultiplyRoot(r.R19, ppStageRoots, 14, direction);
            four = Radix4(r.R8,r.R17,r.R18,r.R19,direction);
            r.R16 = four.R0;
            r.R17 = four.R1;
            r.R18 = four.R2;
            r.R19 = four.R3;
            r.R16 = MultiplyRoot(r.R16, ppRoots, (block % 2) * 104 + 4, direction);
            r.R17 = MultiplyRoot(r.R17, ppRoots, (block % 2) * 104 + 17, direction);
            r.R18 = MultiplyRoot(r.R18, ppRoots, (block % 2) * 104 + 30, direction);
            r.R19 = MultiplyRoot(r.R19, ppRoots, (block % 2) * 104 + 43, direction);
            r.R20 = lds[pp + 255];
            r.R21 = lds[pp + 918];
            r.R22 = lds[pp + 1581];
            r.R23 = lds[pp + 2244];
            r.R21 = MultiplyRoot(r.R21, ppStageRoots, 15, direction);
            r.R22 = MultiplyRoot(r.R22, ppStageRoots, 16, direction);
            r.R23 = MultiplyRoot(r.R23, ppStageRoots, 17, direction);
            four = Radix4(r.R10,r.R21,r.R22,r.R23,direction);
            r.R20 = four.R0;
            r.R21 = four.R1;
            r.R22 = four.R2;
            r.R23 = four.R3;
            r.R20 = MultiplyRoot(r.R20, ppRoots, (block % 2) * 104 + 5, direction);
            r.R21 = MultiplyRoot(r.R21, ppRoots, (block % 2) * 104 + 18, direction);
            r.R22 = MultiplyRoot(r.R22, ppRoots, (block % 2) * 104 + 31, direction);
            r.R23 = MultiplyRoot(r.R23, ppRoots, (block % 2) * 104 + 44, direction);
            r.R24 = lds[pp + 306];
            r.R25 = lds[pp + 969];
            r.R26 = lds[pp + 1632];
            r.R27 = lds[pp + 2295];
            r.R25 = MultiplyRoot(r.R25, ppStageRoots, 18, direction);
            r.R26 = MultiplyRoot(r.R26, ppStageRoots, 19, direction);
            r.R27 = MultiplyRoot(r.R27, ppStageRoots, 20, direction);
            four = Radix4(r.R12,r.R25,r.R26,r.R27,direction);
            r.R24 = four.R0;
            r.R25 = four.R1;
            r.R26 = four.R2;
            r.R27 = four.R3;
            r.R24 = MultiplyRoot(r.R24, ppRoots, (block % 2) * 104 + 6, direction);
            r.R25 = MultiplyRoot(r.R25, ppRoots, (block % 2) * 104 + 19, direction);
            r.R26 = MultiplyRoot(r.R26, ppRoots, (block % 2) * 104 + 32, direction);
            r.R27 = MultiplyRoot(r.R27, ppRoots, (block % 2) * 104 + 45, direction);
            r.R28 = lds[pp + 357];
            r.R29 = lds[pp + 1020];
            r.R30 = lds[pp + 1683];
            r.R31 = lds[pp + 2346];
            r.R29 = MultiplyRoot(r.R29, ppStageRoots, 21, direction);
            r.R30 = MultiplyRoot(r.R30, ppStageRoots, 22, direction);
            r.R31 = MultiplyRoot(r.R31, ppStageRoots, 23, direction);
            four = Radix4(r.R14,r.R29,r.R30,r.R31,direction);
            r.R28 = four.R0;
            r.R29 = four.R1;
            r.R30 = four.R2;
            r.R31 = four.R3;
            r.R28 = MultiplyRoot(r.R28, ppRoots, (block % 2) * 104 + 7, direction);
            r.R29 = MultiplyRoot(r.R29, ppRoots, (block % 2) * 104 + 20, direction);
            r.R30 = MultiplyRoot(r.R30, ppRoots, (block % 2) * 104 + 33, direction);
            r.R31 = MultiplyRoot(r.R31, ppRoots, (block % 2) * 104 + 46, direction);
            r.R32 = lds[pp + 408];
            r.R33 = lds[pp + 1071];
            r.R34 = lds[pp + 1734];
            r.R35 = lds[pp + 2397];
            r.R33 = MultiplyRoot(r.R33, ppStageRoots, 24, direction);
            r.R34 = MultiplyRoot(r.R34, ppStageRoots, 25, direction);
            r.R35 = MultiplyRoot(r.R35, ppStageRoots, 26, direction);
            four = Radix4(r.R16,r.R33,r.R34,r.R35,direction);
            r.R32 = four.R0;
            r.R33 = four.R1;
            r.R34 = four.R2;
            r.R35 = four.R3;
            r.R32 = MultiplyRoot(r.R32, ppRoots, (block % 2) * 104 + 8, direction);
            r.R33 = MultiplyRoot(r.R33, ppRoots, (block % 2) * 104 + 21, direction);
            r.R34 = MultiplyRoot(r.R34, ppRoots, (block % 2) * 104 + 34, direction);
            r.R35 = MultiplyRoot(r.R35, ppRoots, (block % 2) * 104 + 47, direction);
            r.R36 = lds[pp + 459];
            r.R37 = lds[pp + 1122];
            r.R38 = lds[pp + 1785];
            r.R39 = lds[pp + 2448];
            r.R37 = MultiplyRoot(r.R37, ppStageRoots, 27, direction);
            r.R38 = MultiplyRoot(r.R38, ppStageRoots, 28, direction);
            r.R39 = MultiplyRoot(r.R39, ppStageRoots, 29, direction);
            four = Radix4(r.R18,r.R37,r.R38,r.R39,direction);
            r.R36 = four.R0;
            r.R37 = four.R1;
            r.R38 = four.R2;
            r.R39 = four.R3;
            r.R36 = MultiplyRoot(r.R36, ppRoots, (block % 2) * 104 + 9, direction);
            r.R37 = MultiplyRoot(r.R37, ppRoots, (block % 2) * 104 + 22, direction);
            r.R38 = MultiplyRoot(r.R38, ppRoots, (block % 2) * 104 + 35, direction);
            r.R39 = MultiplyRoot(r.R39, ppRoots, (block % 2) * 104 + 48, direction);
            r.R40 = lds[pp + 510];
            r.R41 = lds[pp + 1173];
            r.R42 = lds[pp + 1836];
            r.R43 = lds[pp + 2499];
            r.R41 = MultiplyRoot(r.R41, ppStageRoots, 30, direction);
            r.R42 = MultiplyRoot(r.R42, ppStageRoots, 31, direction);
            r.R43 = MultiplyRoot(r.R43, ppStageRoots, 32, direction);
            four = Radix4(r.R20,r.R41,r.R42,r.R43,direction);
            r.R40 = four.R0;
            r.R41 = four.R1;
            r.R42 = four.R2;
            r.R43 = four.R3;
            r.R40 = MultiplyRoot(r.R40, ppRoots, (block % 2) * 104 + 10, direction);
            r.R41 = MultiplyRoot(r.R41, ppRoots, (block % 2) * 104 + 23, direction);
            r.R42 = MultiplyRoot(r.R42, ppRoots, (block % 2) * 104 + 36, direction);
            r.R43 = MultiplyRoot(r.R43, ppRoots, (block % 2) * 104 + 49, direction);
            r.R44 = lds[pp + 561];
            r.R45 = lds[pp + 1224];
            r.R46 = lds[pp + 1887];
            r.R47 = lds[pp + 2550];
            r.R45 = MultiplyRoot(r.R45, ppStageRoots, 33, direction);
            r.R46 = MultiplyRoot(r.R46, ppStageRoots, 34, direction);
            r.R47 = MultiplyRoot(r.R47, ppStageRoots, 35, direction);
            four = Radix4(r.R22,r.R45,r.R46,r.R47,direction);
            r.R44 = four.R0;
            r.R45 = four.R1;
            r.R46 = four.R2;
            r.R47 = four.R3;
            r.R44 = MultiplyRoot(r.R44, ppRoots, (block % 2) * 104 + 11, direction);
            r.R45 = MultiplyRoot(r.R45, ppRoots, (block % 2) * 104 + 24, direction);
            r.R46 = MultiplyRoot(r.R46, ppRoots, (block % 2) * 104 + 37, direction);
            r.R47 = MultiplyRoot(r.R47, ppRoots, (block % 2) * 104 + 50, direction);
            r.R48 = lds[pp + 612];
            r.R49 = lds[pp + 1275];
            r.R50 = lds[pp + 1938];
            r.R51 = lds[pp + 2601];
            r.R49 = MultiplyRoot(r.R49, ppStageRoots, 36, direction);
            r.R50 = MultiplyRoot(r.R50, ppStageRoots, 37, direction);
            r.R51 = MultiplyRoot(r.R51, ppStageRoots, 38, direction);
            four = Radix4(r.R24,r.R49,r.R50,r.R51,direction);
            r.R48 = four.R0;
            r.R49 = four.R1;
            r.R50 = four.R2;
            r.R51 = four.R3;
            r.R48 = MultiplyRoot(r.R48, ppRoots, (block % 2) * 104 + 12, direction);
            r.R49 = MultiplyRoot(r.R49, ppRoots, (block % 2) * 104 + 25, direction);
            r.R50 = MultiplyRoot(r.R50, ppRoots, (block % 2) * 104 + 38, direction);
            r.R51 = MultiplyRoot(r.R51, ppRoots, (block % 2) * 104 + 51, direction);
        }
        Group.Barrier();
        if (lane < 51)
        {
            lds[pp + 0] = r.R0;
            lds[pp + 663] = r.R1;
            lds[pp + 1326] = r.R2;
            lds[pp + 1989] = r.R3;
            lds[pp + 51] = r.R4;
            lds[pp + 714] = r.R5;
            lds[pp + 1377] = r.R6;
            lds[pp + 2040] = r.R7;
            lds[pp + 102] = r.R8;
            lds[pp + 765] = r.R9;
            lds[pp + 1428] = r.R10;
            lds[pp + 2091] = r.R11;
            lds[pp + 153] = r.R12;
            lds[pp + 816] = r.R13;
            lds[pp + 1479] = r.R14;
            lds[pp + 2142] = r.R15;
            lds[pp + 204] = r.R16;
            lds[pp + 867] = r.R17;
            lds[pp + 1530] = r.R18;
            lds[pp + 2193] = r.R19;
            lds[pp + 255] = r.R20;
            lds[pp + 918] = r.R21;
            lds[pp + 1581] = r.R22;
            lds[pp + 2244] = r.R23;
            lds[pp + 306] = r.R24;
            lds[pp + 969] = r.R25;
            lds[pp + 1632] = r.R26;
            lds[pp + 2295] = r.R27;
            lds[pp + 357] = r.R28;
            lds[pp + 1020] = r.R29;
            lds[pp + 1683] = r.R30;
            lds[pp + 2346] = r.R31;
            lds[pp + 408] = r.R32;
            lds[pp + 1071] = r.R33;
            lds[pp + 1734] = r.R34;
            lds[pp + 2397] = r.R35;
            lds[pp + 459] = r.R36;
            lds[pp + 1122] = r.R37;
            lds[pp + 1785] = r.R38;
            lds[pp + 2448] = r.R39;
            lds[pp + 510] = r.R40;
            lds[pp + 1173] = r.R41;
            lds[pp + 1836] = r.R42;
            lds[pp + 2499] = r.R43;
            lds[pp + 561] = r.R44;
            lds[pp + 1224] = r.R45;
            lds[pp + 1887] = r.R46;
            lds[pp + 2550] = r.R47;
            lds[pp + 612] = r.R48;
            lds[pp + 1275] = r.R49;
            lds[pp + 1938] = r.R50;
            lds[pp + 2601] = r.R51;
        }
        Group.Barrier();
        if (valid)
        {
            Store(output, outputBase + thread + 0, lds[local + thread + 0]);
            Store(output, outputBase + thread + 2, lds[local + thread + 2]);
            Store(output, outputBase + thread + 4, lds[local + thread + 4]);
            Store(output, outputBase + thread + 6, lds[local + thread + 6]);
            Store(output, outputBase + thread + 8, lds[local + thread + 8]);
            Store(output, outputBase + thread + 10, lds[local + thread + 10]);
            Store(output, outputBase + thread + 12, lds[local + thread + 12]);
            Store(output, outputBase + thread + 14, lds[local + thread + 14]);
            Store(output, outputBase + thread + 16, lds[local + thread + 16]);
            Store(output, outputBase + thread + 18, lds[local + thread + 18]);
            Store(output, outputBase + thread + 20, lds[local + thread + 20]);
            Store(output, outputBase + thread + 22, lds[local + thread + 22]);
            Store(output, outputBase + thread + 24, lds[local + thread + 24]);
            Store(output, outputBase + thread + 26, lds[local + thread + 26]);
            Store(output, outputBase + thread + 28, lds[local + thread + 28]);
            Store(output, outputBase + thread + 30, lds[local + thread + 30]);
            Store(output, outputBase + thread + 32, lds[local + thread + 32]);
            Store(output, outputBase + thread + 34, lds[local + thread + 34]);
            Store(output, outputBase + thread + 36, lds[local + thread + 36]);
            Store(output, outputBase + thread + 38, lds[local + thread + 38]);
            Store(output, outputBase + thread + 40, lds[local + thread + 40]);
            Store(output, outputBase + thread + 42, lds[local + thread + 42]);
            Store(output, outputBase + thread + 44, lds[local + thread + 44]);
            Store(output, outputBase + thread + 46, lds[local + thread + 46]);
            Store(output, outputBase + thread + 48, lds[local + thread + 48]);
            if (thread == 1) Store(output, outputBase + 50, lds[local + 50]);
        }
    }

    private static Complex32 Load(ArrayView<float> view, int index) =>
        new(view[2 * index], view[2 * index + 1]);
    private static void Store(ArrayView<float> view, int index, Complex32 value)
    {
        view[2 * index] = value.Real;
        view[2 * index + 1] = value.Imaginary;
    }
    private static Complex32 MultiplyRoot(Complex32 value, ArrayView<float> roots,
        int exponent, int direction)
    {
        Complex32 w = Load(roots, exponent);
        if (direction > 0) w = w.Conjugate();
        return value * w;
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
        Complex32 d, Complex32 e, Complex32 f, int direction)
    {
        float qa = (float)(0.5), qb = (float)(0.8660254037844386468);
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

    private static Registers Radix8(Registers r, int direction)
    {
        const float q = 0.70710678118654752440084436210485f;
        Complex32 a = r.R0, b = r.R4, c = r.R2, d = r.R6;
        Complex32 e = r.R1, f = r.R5, g = r.R3, h = r.R7;
        b = a - b; a = 2.0f * a - b;
        d = c - d; c = 2.0f * c - d;
        f = e - f; e = 2.0f * e - f;
        h = g - h; g = 2.0f * g - h;
        c = a - c; a = 2.0f * a - c;
        d = b + new Complex32(direction * d.Imaginary, -direction * d.Real);
        b = 2.0f * b - d;
        g = e - g; e = 2.0f * e - g;
        h = f + new Complex32(direction * h.Imaginary, -direction * h.Real);
        f = 2.0f * f - h;
        e = a - e; a = 2.0f * a - e;
        f = (b - q * f) + direction * q * new Complex32(f.Imaginary, -f.Real);
        b = 2.0f * b - f;
        g = c + new Complex32(direction * g.Imaginary, -direction * g.Real);
        c = 2.0f * c - g;
        h = (d + q * h) + direction * q * new Complex32(h.Imaginary, -h.Real);
        d = 2.0f * d - h;
        r.R0 = a; r.R1 = b; r.R2 = c; r.R3 = d;
        r.R4 = e; r.R5 = f; r.R6 = g; r.R7 = h;
        return r;
    }

    private static Three Radix3(Complex32 a, Complex32 b, Complex32 c, int direction)
    {
        Complex32 sum = b + c, difference = b - c;
        Complex32 middle = a + -0.5f * sum;
        float wi = direction * 0.86602540378443864676f;
        Complex32 turn = new(-wi * difference.Imaginary, wi * difference.Real);
        return new Three { R0 = a + sum, R1 = middle + turn, R2 = middle - turn };
    }

    private static Four Radix4(Complex32 x0, Complex32 x1, Complex32 x2,
        Complex32 x3, int direction)
    {
        Complex32 a = x0, b = x2, c = x1, d = x3;
        b = a - b; a = 2.0f * a - b;
        d = c - d; c = 2.0f * c - d;
        c = a - c; a = 2.0f * a - c;
        d = b + new Complex32(direction * d.Imaginary, -direction * d.Real);
        b = 2.0f * b - d;
        return new Four { R0 = a, R1 = b, R2 = c, R3 = d };
    }

    private static void Post(ArrayView<Complex32> lds, int offset, int p, ArrayView<float> roots)
    {
        if (p >= 25) return;
        int q = 50 - p;
        Complex32 a = lds[offset + p], b = lds[offset + q];
        if (p == 0)
        {
            lds[offset] = new Complex32(a.Real + a.Imaginary, 0.0f);
            lds[offset + 50] = new Complex32(a.Real - a.Imaginary, 0.0f);
            Complex32 quarter = lds[offset + 25];
            lds[offset + 25] = new Complex32(quarter.Real, -quarter.Imaginary);
            return;
        }
        Complex32 u = 0.5f * (a + b), v = 0.5f * (a - b);
        Complex32 w = Load(roots, 40 + p);
        lds[offset + p] = new Complex32(u.Real + v.Real * w.Imaginary + u.Imaginary * w.Real,
            v.Imaginary + u.Imaginary * w.Imaginary - v.Real * w.Real);
        lds[offset + q] = new Complex32(u.Real - v.Real * w.Imaginary - u.Imaginary * w.Real,
            -v.Imaginary + u.Imaginary * w.Imaginary - v.Real * w.Real);
    }
    private static Registers16 Radix13(Registers16 r, int direction)
    {
        Complex32 x0 = r.R0 + r.R1 + r.R2 + r.R3 + r.R4 + r.R5 + r.R6 + r.R7 + r.R8 + r.R9 + r.R10 + r.R11 + r.R12;
        float x1r = r.R0.Real, x1i = r.R0.Imaginary;
        float x2r = r.R0.Real, x2i = r.R0.Imaginary;
        float x3r = r.R0.Real, x3i = r.R0.Imaginary;
        float x4r = r.R0.Real, x4i = r.R0.Imaginary;
        float x5r = r.R0.Real, x5i = r.R0.Imaginary;
        float x6r = r.R0.Real, x6i = r.R0.Imaginary;
        float x7r = r.R0.Real, x7i = r.R0.Imaginary;
        float x8r = r.R0.Real, x8i = r.R0.Imaginary;
        float x9r = r.R0.Real, x9i = r.R0.Imaginary;
        float x10r = r.R0.Real, x10i = r.R0.Imaginary;
        float x11r = r.R0.Real, x11i = r.R0.Imaginary;
        float x12r = r.R0.Real, x12i = r.R0.Imaginary;
        float sign = direction < 0 ? 1.0f : -1.0f;
        { Complex32 dp = r.R1 + r.R12, dm = r.R1 - r.R12;
          x1r += (0.8854560256532098959004f) * dp.Real - sign * (-0.4647231720437685456560f) * dm.Imaginary;
          x1i += (0.8854560256532098959004f) * dp.Imaginary + sign * (-0.4647231720437685456560f) * dm.Real;
          x12r += (0.8854560256532098959004f) * dp.Real + sign * (-0.4647231720437685456560f) * dm.Imaginary;
          x12i += (0.8854560256532098959004f) * dp.Imaginary - sign * (-0.4647231720437685456560f) * dm.Real;
          x2r += (0.5680647467311558025118f) * dp.Real - sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x2i += (0.5680647467311558025118f) * dp.Imaginary + sign * (-0.8229838658936563945796f) * dm.Real;
          x11r += (0.5680647467311558025118f) * dp.Real + sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x11i += (0.5680647467311558025118f) * dp.Imaginary - sign * (-0.8229838658936563945796f) * dm.Real;
          x3r += (0.1205366802553230533491f) * dp.Real - sign * (-0.9927088740980539928007f) * dm.Imaginary;
          x3i += (0.1205366802553230533491f) * dp.Imaginary + sign * (-0.9927088740980539928007f) * dm.Real;
          x10r += (0.1205366802553230533491f) * dp.Real + sign * (-0.9927088740980539928007f) * dm.Imaginary;
          x10i += (0.1205366802553230533491f) * dp.Imaginary - sign * (-0.9927088740980539928007f) * dm.Real;
          x4r += (-0.3546048870425356259696f) * dp.Real - sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x4i += (-0.3546048870425356259696f) * dp.Imaginary + sign * (-0.9350162426854148234398f) * dm.Real;
          x9r += (-0.3546048870425356259696f) * dp.Real + sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x9i += (-0.3546048870425356259696f) * dp.Imaginary - sign * (-0.9350162426854148234398f) * dm.Real;
          x5r += (-0.7485107481711010986346f) * dp.Real - sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x5i += (-0.7485107481711010986346f) * dp.Imaginary + sign * (-0.6631226582407952023768f) * dm.Real;
          x8r += (-0.7485107481711010986346f) * dp.Real + sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x8i += (-0.7485107481711010986346f) * dp.Imaginary - sign * (-0.6631226582407952023768f) * dm.Real;
          x6r += (-0.9709418174260520271570f) * dp.Real - sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x6i += (-0.9709418174260520271570f) * dp.Imaginary + sign * (-0.2393156642875577671488f) * dm.Real;
          x7r += (-0.9709418174260520271570f) * dp.Real + sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x7i += (-0.9709418174260520271570f) * dp.Imaginary - sign * (-0.2393156642875577671488f) * dm.Real;
        }
        { Complex32 dp = r.R2 + r.R11, dm = r.R2 - r.R11;
          x1r += (0.5680647467311558025118f) * dp.Real - sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x1i += (0.5680647467311558025118f) * dp.Imaginary + sign * (-0.8229838658936563945796f) * dm.Real;
          x12r += (0.5680647467311558025118f) * dp.Real + sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x12i += (0.5680647467311558025118f) * dp.Imaginary - sign * (-0.8229838658936563945796f) * dm.Real;
          x2r += (-0.3546048870425356259696f) * dp.Real - sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x2i += (-0.3546048870425356259696f) * dp.Imaginary + sign * (-0.9350162426854148234398f) * dm.Real;
          x11r += (-0.3546048870425356259696f) * dp.Real + sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x11i += (-0.3546048870425356259696f) * dp.Imaginary - sign * (-0.9350162426854148234398f) * dm.Real;
          x3r += (-0.9709418174260520271570f) * dp.Real - sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x3i += (-0.9709418174260520271570f) * dp.Imaginary + sign * (-0.2393156642875577671488f) * dm.Real;
          x10r += (-0.9709418174260520271570f) * dp.Real + sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x10i += (-0.9709418174260520271570f) * dp.Imaginary - sign * (-0.2393156642875577671488f) * dm.Real;
          x4r += (-0.7485107481711010986346f) * dp.Real - sign * (0.6631226582407952023768f) * dm.Imaginary;
          x4i += (-0.7485107481711010986346f) * dp.Imaginary + sign * (0.6631226582407952023768f) * dm.Real;
          x9r += (-0.7485107481711010986346f) * dp.Real + sign * (0.6631226582407952023768f) * dm.Imaginary;
          x9i += (-0.7485107481711010986346f) * dp.Imaginary - sign * (0.6631226582407952023768f) * dm.Real;
          x5r += (0.1205366802553230533491f) * dp.Real - sign * (0.9927088740980539928007f) * dm.Imaginary;
          x5i += (0.1205366802553230533491f) * dp.Imaginary + sign * (0.9927088740980539928007f) * dm.Real;
          x8r += (0.1205366802553230533491f) * dp.Real + sign * (0.9927088740980539928007f) * dm.Imaginary;
          x8i += (0.1205366802553230533491f) * dp.Imaginary - sign * (0.9927088740980539928007f) * dm.Real;
          x6r += (0.8854560256532098959004f) * dp.Real - sign * (0.4647231720437685456560f) * dm.Imaginary;
          x6i += (0.8854560256532098959004f) * dp.Imaginary + sign * (0.4647231720437685456560f) * dm.Real;
          x7r += (0.8854560256532098959004f) * dp.Real + sign * (0.4647231720437685456560f) * dm.Imaginary;
          x7i += (0.8854560256532098959004f) * dp.Imaginary - sign * (0.4647231720437685456560f) * dm.Real;
        }
        { Complex32 dp = r.R3 + r.R10, dm = r.R3 - r.R10;
          x1r += (0.1205366802553230533491f) * dp.Real - sign * (-0.9927088740980539928007f) * dm.Imaginary;
          x1i += (0.1205366802553230533491f) * dp.Imaginary + sign * (-0.9927088740980539928007f) * dm.Real;
          x12r += (0.1205366802553230533491f) * dp.Real + sign * (-0.9927088740980539928007f) * dm.Imaginary;
          x12i += (0.1205366802553230533491f) * dp.Imaginary - sign * (-0.9927088740980539928007f) * dm.Real;
          x2r += (-0.9709418174260520271570f) * dp.Real - sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x2i += (-0.9709418174260520271570f) * dp.Imaginary + sign * (-0.2393156642875577671488f) * dm.Real;
          x11r += (-0.9709418174260520271570f) * dp.Real + sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x11i += (-0.9709418174260520271570f) * dp.Imaginary - sign * (-0.2393156642875577671488f) * dm.Real;
          x3r += (-0.3546048870425356259696f) * dp.Real - sign * (0.9350162426854148234398f) * dm.Imaginary;
          x3i += (-0.3546048870425356259696f) * dp.Imaginary + sign * (0.9350162426854148234398f) * dm.Real;
          x10r += (-0.3546048870425356259696f) * dp.Real + sign * (0.9350162426854148234398f) * dm.Imaginary;
          x10i += (-0.3546048870425356259696f) * dp.Imaginary - sign * (0.9350162426854148234398f) * dm.Real;
          x4r += (0.8854560256532098959004f) * dp.Real - sign * (0.4647231720437685456560f) * dm.Imaginary;
          x4i += (0.8854560256532098959004f) * dp.Imaginary + sign * (0.4647231720437685456560f) * dm.Real;
          x9r += (0.8854560256532098959004f) * dp.Real + sign * (0.4647231720437685456560f) * dm.Imaginary;
          x9i += (0.8854560256532098959004f) * dp.Imaginary - sign * (0.4647231720437685456560f) * dm.Real;
          x5r += (0.5680647467311558025118f) * dp.Real - sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x5i += (0.5680647467311558025118f) * dp.Imaginary + sign * (-0.8229838658936563945796f) * dm.Real;
          x8r += (0.5680647467311558025118f) * dp.Real + sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x8i += (0.5680647467311558025118f) * dp.Imaginary - sign * (-0.8229838658936563945796f) * dm.Real;
          x6r += (-0.7485107481711010986346f) * dp.Real - sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x6i += (-0.7485107481711010986346f) * dp.Imaginary + sign * (-0.6631226582407952023768f) * dm.Real;
          x7r += (-0.7485107481711010986346f) * dp.Real + sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x7i += (-0.7485107481711010986346f) * dp.Imaginary - sign * (-0.6631226582407952023768f) * dm.Real;
        }
        { Complex32 dp = r.R4 + r.R9, dm = r.R4 - r.R9;
          x1r += (-0.3546048870425356259696f) * dp.Real - sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x1i += (-0.3546048870425356259696f) * dp.Imaginary + sign * (-0.9350162426854148234398f) * dm.Real;
          x12r += (-0.3546048870425356259696f) * dp.Real + sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x12i += (-0.3546048870425356259696f) * dp.Imaginary - sign * (-0.9350162426854148234398f) * dm.Real;
          x2r += (-0.7485107481711010986346f) * dp.Real - sign * (0.6631226582407952023768f) * dm.Imaginary;
          x2i += (-0.7485107481711010986346f) * dp.Imaginary + sign * (0.6631226582407952023768f) * dm.Real;
          x11r += (-0.7485107481711010986346f) * dp.Real + sign * (0.6631226582407952023768f) * dm.Imaginary;
          x11i += (-0.7485107481711010986346f) * dp.Imaginary - sign * (0.6631226582407952023768f) * dm.Real;
          x3r += (0.8854560256532098959004f) * dp.Real - sign * (0.4647231720437685456560f) * dm.Imaginary;
          x3i += (0.8854560256532098959004f) * dp.Imaginary + sign * (0.4647231720437685456560f) * dm.Real;
          x10r += (0.8854560256532098959004f) * dp.Real + sign * (0.4647231720437685456560f) * dm.Imaginary;
          x10i += (0.8854560256532098959004f) * dp.Imaginary - sign * (0.4647231720437685456560f) * dm.Real;
          x4r += (0.1205366802553230533491f) * dp.Real - sign * (-0.9927088740980539928007f) * dm.Imaginary;
          x4i += (0.1205366802553230533491f) * dp.Imaginary + sign * (-0.9927088740980539928007f) * dm.Real;
          x9r += (0.1205366802553230533491f) * dp.Real + sign * (-0.9927088740980539928007f) * dm.Imaginary;
          x9i += (0.1205366802553230533491f) * dp.Imaginary - sign * (-0.9927088740980539928007f) * dm.Real;
          x5r += (-0.9709418174260520271570f) * dp.Real - sign * (0.2393156642875577671488f) * dm.Imaginary;
          x5i += (-0.9709418174260520271570f) * dp.Imaginary + sign * (0.2393156642875577671488f) * dm.Real;
          x8r += (-0.9709418174260520271570f) * dp.Real + sign * (0.2393156642875577671488f) * dm.Imaginary;
          x8i += (-0.9709418174260520271570f) * dp.Imaginary - sign * (0.2393156642875577671488f) * dm.Real;
          x6r += (0.5680647467311558025118f) * dp.Real - sign * (0.8229838658936563945796f) * dm.Imaginary;
          x6i += (0.5680647467311558025118f) * dp.Imaginary + sign * (0.8229838658936563945796f) * dm.Real;
          x7r += (0.5680647467311558025118f) * dp.Real + sign * (0.8229838658936563945796f) * dm.Imaginary;
          x7i += (0.5680647467311558025118f) * dp.Imaginary - sign * (0.8229838658936563945796f) * dm.Real;
        }
        { Complex32 dp = r.R5 + r.R8, dm = r.R5 - r.R8;
          x1r += (-0.7485107481711010986346f) * dp.Real - sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x1i += (-0.7485107481711010986346f) * dp.Imaginary + sign * (-0.6631226582407952023768f) * dm.Real;
          x12r += (-0.7485107481711010986346f) * dp.Real + sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x12i += (-0.7485107481711010986346f) * dp.Imaginary - sign * (-0.6631226582407952023768f) * dm.Real;
          x2r += (0.1205366802553230533491f) * dp.Real - sign * (0.9927088740980539928007f) * dm.Imaginary;
          x2i += (0.1205366802553230533491f) * dp.Imaginary + sign * (0.9927088740980539928007f) * dm.Real;
          x11r += (0.1205366802553230533491f) * dp.Real + sign * (0.9927088740980539928007f) * dm.Imaginary;
          x11i += (0.1205366802553230533491f) * dp.Imaginary - sign * (0.9927088740980539928007f) * dm.Real;
          x3r += (0.5680647467311558025118f) * dp.Real - sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x3i += (0.5680647467311558025118f) * dp.Imaginary + sign * (-0.8229838658936563945796f) * dm.Real;
          x10r += (0.5680647467311558025118f) * dp.Real + sign * (-0.8229838658936563945796f) * dm.Imaginary;
          x10i += (0.5680647467311558025118f) * dp.Imaginary - sign * (-0.8229838658936563945796f) * dm.Real;
          x4r += (-0.9709418174260520271570f) * dp.Real - sign * (0.2393156642875577671488f) * dm.Imaginary;
          x4i += (-0.9709418174260520271570f) * dp.Imaginary + sign * (0.2393156642875577671488f) * dm.Real;
          x9r += (-0.9709418174260520271570f) * dp.Real + sign * (0.2393156642875577671488f) * dm.Imaginary;
          x9i += (-0.9709418174260520271570f) * dp.Imaginary - sign * (0.2393156642875577671488f) * dm.Real;
          x5r += (0.8854560256532098959004f) * dp.Real - sign * (0.4647231720437685456560f) * dm.Imaginary;
          x5i += (0.8854560256532098959004f) * dp.Imaginary + sign * (0.4647231720437685456560f) * dm.Real;
          x8r += (0.8854560256532098959004f) * dp.Real + sign * (0.4647231720437685456560f) * dm.Imaginary;
          x8i += (0.8854560256532098959004f) * dp.Imaginary - sign * (0.4647231720437685456560f) * dm.Real;
          x6r += (-0.3546048870425356259696f) * dp.Real - sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x6i += (-0.3546048870425356259696f) * dp.Imaginary + sign * (-0.9350162426854148234398f) * dm.Real;
          x7r += (-0.3546048870425356259696f) * dp.Real + sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x7i += (-0.3546048870425356259696f) * dp.Imaginary - sign * (-0.9350162426854148234398f) * dm.Real;
        }
        { Complex32 dp = r.R6 + r.R7, dm = r.R6 - r.R7;
          x1r += (-0.9709418174260520271570f) * dp.Real - sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x1i += (-0.9709418174260520271570f) * dp.Imaginary + sign * (-0.2393156642875577671488f) * dm.Real;
          x12r += (-0.9709418174260520271570f) * dp.Real + sign * (-0.2393156642875577671488f) * dm.Imaginary;
          x12i += (-0.9709418174260520271570f) * dp.Imaginary - sign * (-0.2393156642875577671488f) * dm.Real;
          x2r += (0.8854560256532098959004f) * dp.Real - sign * (0.4647231720437685456560f) * dm.Imaginary;
          x2i += (0.8854560256532098959004f) * dp.Imaginary + sign * (0.4647231720437685456560f) * dm.Real;
          x11r += (0.8854560256532098959004f) * dp.Real + sign * (0.4647231720437685456560f) * dm.Imaginary;
          x11i += (0.8854560256532098959004f) * dp.Imaginary - sign * (0.4647231720437685456560f) * dm.Real;
          x3r += (-0.7485107481711010986346f) * dp.Real - sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x3i += (-0.7485107481711010986346f) * dp.Imaginary + sign * (-0.6631226582407952023768f) * dm.Real;
          x10r += (-0.7485107481711010986346f) * dp.Real + sign * (-0.6631226582407952023768f) * dm.Imaginary;
          x10i += (-0.7485107481711010986346f) * dp.Imaginary - sign * (-0.6631226582407952023768f) * dm.Real;
          x4r += (0.5680647467311558025118f) * dp.Real - sign * (0.8229838658936563945796f) * dm.Imaginary;
          x4i += (0.5680647467311558025118f) * dp.Imaginary + sign * (0.8229838658936563945796f) * dm.Real;
          x9r += (0.5680647467311558025118f) * dp.Real + sign * (0.8229838658936563945796f) * dm.Imaginary;
          x9i += (0.5680647467311558025118f) * dp.Imaginary - sign * (0.8229838658936563945796f) * dm.Real;
          x5r += (-0.3546048870425356259696f) * dp.Real - sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x5i += (-0.3546048870425356259696f) * dp.Imaginary + sign * (-0.9350162426854148234398f) * dm.Real;
          x8r += (-0.3546048870425356259696f) * dp.Real + sign * (-0.9350162426854148234398f) * dm.Imaginary;
          x8i += (-0.3546048870425356259696f) * dp.Imaginary - sign * (-0.9350162426854148234398f) * dm.Real;
          x6r += (0.1205366802553230533491f) * dp.Real - sign * (0.9927088740980539928007f) * dm.Imaginary;
          x6i += (0.1205366802553230533491f) * dp.Imaginary + sign * (0.9927088740980539928007f) * dm.Real;
          x7r += (0.1205366802553230533491f) * dp.Real + sign * (0.9927088740980539928007f) * dm.Imaginary;
          x7i += (0.1205366802553230533491f) * dp.Imaginary - sign * (0.9927088740980539928007f) * dm.Real;
        }
        r.R0 = x0;
        r.R1 = new Complex32(x1r, x1i);
        r.R2 = new Complex32(x2r, x2i);
        r.R3 = new Complex32(x3r, x3i);
        r.R4 = new Complex32(x4r, x4i);
        r.R5 = new Complex32(x5r, x5i);
        r.R6 = new Complex32(x6r, x6i);
        r.R7 = new Complex32(x7r, x7i);
        r.R8 = new Complex32(x8r, x8i);
        r.R9 = new Complex32(x9r, x9i);
        r.R10 = new Complex32(x10r, x10i);
        r.R11 = new Complex32(x11r, x11i);
        r.R12 = new Complex32(x12r, x12i);
        return r;
    }
    private static Registers10 ForwardRadix10(Registers10 r)
    {
        const float C5QA = 0.30901699437494742410229341718282f;
        const float C5QB = 0.95105651629515357211643933337938f;
        const float C5QC = 0.5f;
        const float C5QD = 0.58778525229247312916870595463907f;
        const float C5QE = 0.80901699437494742410229341718282f;


    float TR0, TI0, TR1, TI1, TR2, TI2, TR3, TI3, TR4, TI4, TR5, TI5, TR6, TI6, TR7, TI7,
        TR8, TI8, TR9, TI9;

    TR0 = r.R0.Real + r.R2.Real + r.R4.Real + r.R6.Real + r.R8.Real;
    TR2 = (r.R0.Real - C5QC * (r.R4.Real + r.R6.Real)) + C5QB * (r.R2.Imaginary - r.R8.Imaginary)
          + C5QD * (r.R4.Imaginary - r.R6.Imaginary) + C5QA * ((r.R2.Real - r.R4.Real) + (r.R8.Real - r.R6.Real));
    TR8 = (r.R0.Real - C5QC * (r.R4.Real + r.R6.Real)) - C5QB * (r.R2.Imaginary - r.R8.Imaginary)
          - C5QD * (r.R4.Imaginary - r.R6.Imaginary) + C5QA * ((r.R2.Real - r.R4.Real) + (r.R8.Real - r.R6.Real));
    TR4 = (r.R0.Real - C5QC * (r.R2.Real + r.R8.Real)) - C5QB * (r.R4.Imaginary - r.R6.Imaginary)
          + C5QD * (r.R2.Imaginary - r.R8.Imaginary) + C5QA * ((r.R4.Real - r.R2.Real) + (r.R6.Real - r.R8.Real));
    TR6 = (r.R0.Real - C5QC * (r.R2.Real + r.R8.Real)) + C5QB * (r.R4.Imaginary - r.R6.Imaginary)
          - C5QD * (r.R2.Imaginary - r.R8.Imaginary) + C5QA * ((r.R4.Real - r.R2.Real) + (r.R6.Real - r.R8.Real));

    TI0 = r.R0.Imaginary + r.R2.Imaginary + r.R4.Imaginary + r.R6.Imaginary + r.R8.Imaginary;
    TI2 = (r.R0.Imaginary - C5QC * (r.R4.Imaginary + r.R6.Imaginary)) - C5QB * (r.R2.Real - r.R8.Real)
          - C5QD * (r.R4.Real - r.R6.Real) + C5QA * ((r.R2.Imaginary - r.R4.Imaginary) + (r.R8.Imaginary - r.R6.Imaginary));
    TI8 = (r.R0.Imaginary - C5QC * (r.R4.Imaginary + r.R6.Imaginary)) + C5QB * (r.R2.Real - r.R8.Real)
          + C5QD * (r.R4.Real - r.R6.Real) + C5QA * ((r.R2.Imaginary - r.R4.Imaginary) + (r.R8.Imaginary - r.R6.Imaginary));
    TI4 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R8.Imaginary)) + C5QB * (r.R4.Real - r.R6.Real)
          - C5QD * (r.R2.Real - r.R8.Real) + C5QA * ((r.R4.Imaginary - r.R2.Imaginary) + (r.R6.Imaginary - r.R8.Imaginary));
    TI6 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R8.Imaginary)) - C5QB * (r.R4.Real - r.R6.Real)
          + C5QD * (r.R2.Real - r.R8.Real) + C5QA * ((r.R4.Imaginary - r.R2.Imaginary) + (r.R6.Imaginary - r.R8.Imaginary));

    TR1 = r.R1.Real + r.R3.Real + r.R5.Real + r.R7.Real + r.R9.Real;
    TR3 = (r.R1.Real - C5QC * (r.R5.Real + r.R7.Real)) + C5QB * (r.R3.Imaginary - r.R9.Imaginary)
          + C5QD * (r.R5.Imaginary - r.R7.Imaginary) + C5QA * ((r.R3.Real - r.R5.Real) + (r.R9.Real - r.R7.Real));
    TR9 = (r.R1.Real - C5QC * (r.R5.Real + r.R7.Real)) - C5QB * (r.R3.Imaginary - r.R9.Imaginary)
          - C5QD * (r.R5.Imaginary - r.R7.Imaginary) + C5QA * ((r.R3.Real - r.R5.Real) + (r.R9.Real - r.R7.Real));
    TR5 = (r.R1.Real - C5QC * (r.R3.Real + r.R9.Real)) - C5QB * (r.R5.Imaginary - r.R7.Imaginary)
          + C5QD * (r.R3.Imaginary - r.R9.Imaginary) + C5QA * ((r.R5.Real - r.R3.Real) + (r.R7.Real - r.R9.Real));
    TR7 = (r.R1.Real - C5QC * (r.R3.Real + r.R9.Real)) + C5QB * (r.R5.Imaginary - r.R7.Imaginary)
          - C5QD * (r.R3.Imaginary - r.R9.Imaginary) + C5QA * ((r.R5.Real - r.R3.Real) + (r.R7.Real - r.R9.Real));

    TI1 = r.R1.Imaginary + r.R3.Imaginary + r.R5.Imaginary + r.R7.Imaginary + r.R9.Imaginary;
    TI3 = (r.R1.Imaginary - C5QC * (r.R5.Imaginary + r.R7.Imaginary)) - C5QB * (r.R3.Real - r.R9.Real)
          - C5QD * (r.R5.Real - r.R7.Real) + C5QA * ((r.R3.Imaginary - r.R5.Imaginary) + (r.R9.Imaginary - r.R7.Imaginary));
    TI9 = (r.R1.Imaginary - C5QC * (r.R5.Imaginary + r.R7.Imaginary)) + C5QB * (r.R3.Real - r.R9.Real)
          + C5QD * (r.R5.Real - r.R7.Real) + C5QA * ((r.R3.Imaginary - r.R5.Imaginary) + (r.R9.Imaginary - r.R7.Imaginary));
    TI5 = (r.R1.Imaginary - C5QC * (r.R3.Imaginary + r.R9.Imaginary)) + C5QB * (r.R5.Real - r.R7.Real)
          - C5QD * (r.R3.Real - r.R9.Real) + C5QA * ((r.R5.Imaginary - r.R3.Imaginary) + (r.R7.Imaginary - r.R9.Imaginary));
    TI7 = (r.R1.Imaginary - C5QC * (r.R3.Imaginary + r.R9.Imaginary)) - C5QB * (r.R5.Real - r.R7.Real)
          + C5QD * (r.R3.Real - r.R9.Real) + C5QA * ((r.R5.Imaginary - r.R3.Imaginary) + (r.R7.Imaginary - r.R9.Imaginary));

        return new Registers10
        {
            R0 = new Complex32(TR0 + TR1, TI0 + TI1),
            R1 = new Complex32(TR2 + (C5QE * TR3 + C5QD * TI3), TI2 + (-C5QD * TR3 + C5QE * TI3)),
            R2 = new Complex32(TR4 + (C5QA * TR5 + C5QB * TI5), TI4 + (-C5QB * TR5 + C5QA * TI5)),
            R3 = new Complex32(TR6 + (-C5QA * TR7 + C5QB * TI7), TI6 + (-C5QB * TR7 - C5QA * TI7)),
            R4 = new Complex32(TR8 + (-C5QE * TR9 + C5QD * TI9), TI8 + (-C5QD * TR9 - C5QE * TI9)),
            R5 = new Complex32(TR0 - TR1, TI0 - TI1),
            R6 = new Complex32(TR2 - (C5QE * TR3 + C5QD * TI3), TI2 - (-C5QD * TR3 + C5QE * TI3)),
            R7 = new Complex32(TR4 - (C5QA * TR5 + C5QB * TI5), TI4 - (-C5QB * TR5 + C5QA * TI5)),
            R8 = new Complex32(TR6 - (-C5QA * TR7 + C5QB * TI7), TI6 - (-C5QB * TR7 - C5QA * TI7)),
            R9 = new Complex32(TR8 - (-C5QE * TR9 + C5QD * TI9), TI8 - (-C5QD * TR9 - C5QE * TI9)),
        };
    }
    private static Registers10 InverseRadix10(Registers10 r)
    {
        const float C5QA = 0.30901699437494742410229341718282f;
        const float C5QB = 0.95105651629515357211643933337938f;
        const float C5QC = 0.5f;
        const float C5QD = 0.58778525229247312916870595463907f;
        const float C5QE = 0.80901699437494742410229341718282f;


    float TR0, TI0, TR1, TI1, TR2, TI2, TR3, TI3, TR4, TI4, TR5, TI5, TR6, TI6, TR7, TI7,
        TR8, TI8, TR9, TI9;

    TR0 = r.R0.Real + r.R2.Real + r.R4.Real + r.R6.Real + r.R8.Real;
    TR2 = (r.R0.Real - C5QC * (r.R4.Real + r.R6.Real)) - C5QB * (r.R2.Imaginary - r.R8.Imaginary)
          - C5QD * (r.R4.Imaginary - r.R6.Imaginary) + C5QA * ((r.R2.Real - r.R4.Real) + (r.R8.Real - r.R6.Real));
    TR8 = (r.R0.Real - C5QC * (r.R4.Real + r.R6.Real)) + C5QB * (r.R2.Imaginary - r.R8.Imaginary)
          + C5QD * (r.R4.Imaginary - r.R6.Imaginary) + C5QA * ((r.R2.Real - r.R4.Real) + (r.R8.Real - r.R6.Real));
    TR4 = (r.R0.Real - C5QC * (r.R2.Real + r.R8.Real)) + C5QB * (r.R4.Imaginary - r.R6.Imaginary)
          - C5QD * (r.R2.Imaginary - r.R8.Imaginary) + C5QA * ((r.R4.Real - r.R2.Real) + (r.R6.Real - r.R8.Real));
    TR6 = (r.R0.Real - C5QC * (r.R2.Real + r.R8.Real)) - C5QB * (r.R4.Imaginary - r.R6.Imaginary)
          + C5QD * (r.R2.Imaginary - r.R8.Imaginary) + C5QA * ((r.R4.Real - r.R2.Real) + (r.R6.Real - r.R8.Real));

    TI0 = r.R0.Imaginary + r.R2.Imaginary + r.R4.Imaginary + r.R6.Imaginary + r.R8.Imaginary;
    TI2 = (r.R0.Imaginary - C5QC * (r.R4.Imaginary + r.R6.Imaginary)) + C5QB * (r.R2.Real - r.R8.Real)
          + C5QD * (r.R4.Real - r.R6.Real) + C5QA * ((r.R2.Imaginary - r.R4.Imaginary) + (r.R8.Imaginary - r.R6.Imaginary));
    TI8 = (r.R0.Imaginary - C5QC * (r.R4.Imaginary + r.R6.Imaginary)) - C5QB * (r.R2.Real - r.R8.Real)
          - C5QD * (r.R4.Real - r.R6.Real) + C5QA * ((r.R2.Imaginary - r.R4.Imaginary) + (r.R8.Imaginary - r.R6.Imaginary));
    TI4 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R8.Imaginary)) - C5QB * (r.R4.Real - r.R6.Real)
          + C5QD * (r.R2.Real - r.R8.Real) + C5QA * ((r.R4.Imaginary - r.R2.Imaginary) + (r.R6.Imaginary - r.R8.Imaginary));
    TI6 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R8.Imaginary)) + C5QB * (r.R4.Real - r.R6.Real)
          - C5QD * (r.R2.Real - r.R8.Real) + C5QA * ((r.R4.Imaginary - r.R2.Imaginary) + (r.R6.Imaginary - r.R8.Imaginary));

    TR1 = r.R1.Real + r.R3.Real + r.R5.Real + r.R7.Real + r.R9.Real;
    TR3 = (r.R1.Real - C5QC * (r.R5.Real + r.R7.Real)) - C5QB * (r.R3.Imaginary - r.R9.Imaginary)
          - C5QD * (r.R5.Imaginary - r.R7.Imaginary) + C5QA * ((r.R3.Real - r.R5.Real) + (r.R9.Real - r.R7.Real));
    TR9 = (r.R1.Real - C5QC * (r.R5.Real + r.R7.Real)) + C5QB * (r.R3.Imaginary - r.R9.Imaginary)
          + C5QD * (r.R5.Imaginary - r.R7.Imaginary) + C5QA * ((r.R3.Real - r.R5.Real) + (r.R9.Real - r.R7.Real));
    TR5 = (r.R1.Real - C5QC * (r.R3.Real + r.R9.Real)) + C5QB * (r.R5.Imaginary - r.R7.Imaginary)
          - C5QD * (r.R3.Imaginary - r.R9.Imaginary) + C5QA * ((r.R5.Real - r.R3.Real) + (r.R7.Real - r.R9.Real));
    TR7 = (r.R1.Real - C5QC * (r.R3.Real + r.R9.Real)) - C5QB * (r.R5.Imaginary - r.R7.Imaginary)
          + C5QD * (r.R3.Imaginary - r.R9.Imaginary) + C5QA * ((r.R5.Real - r.R3.Real) + (r.R7.Real - r.R9.Real));

    TI1 = r.R1.Imaginary + r.R3.Imaginary + r.R5.Imaginary + r.R7.Imaginary + r.R9.Imaginary;
    TI3 = (r.R1.Imaginary - C5QC * (r.R5.Imaginary + r.R7.Imaginary)) + C5QB * (r.R3.Real - r.R9.Real)
          + C5QD * (r.R5.Real - r.R7.Real) + C5QA * ((r.R3.Imaginary - r.R5.Imaginary) + (r.R9.Imaginary - r.R7.Imaginary));
    TI9 = (r.R1.Imaginary - C5QC * (r.R5.Imaginary + r.R7.Imaginary)) - C5QB * (r.R3.Real - r.R9.Real)
          - C5QD * (r.R5.Real - r.R7.Real) + C5QA * ((r.R3.Imaginary - r.R5.Imaginary) + (r.R9.Imaginary - r.R7.Imaginary));
    TI5 = (r.R1.Imaginary - C5QC * (r.R3.Imaginary + r.R9.Imaginary)) - C5QB * (r.R5.Real - r.R7.Real)
          + C5QD * (r.R3.Real - r.R9.Real) + C5QA * ((r.R5.Imaginary - r.R3.Imaginary) + (r.R7.Imaginary - r.R9.Imaginary));
    TI7 = (r.R1.Imaginary - C5QC * (r.R3.Imaginary + r.R9.Imaginary)) + C5QB * (r.R5.Real - r.R7.Real)
          - C5QD * (r.R3.Real - r.R9.Real) + C5QA * ((r.R5.Imaginary - r.R3.Imaginary) + (r.R7.Imaginary - r.R9.Imaginary));

        return new Registers10
        {
            R0 = new Complex32(TR0 + TR1, TI0 + TI1),
            R1 = new Complex32(TR2 + (C5QE * TR3 - C5QD * TI3), TI2 + (C5QD * TR3 + C5QE * TI3)),
            R2 = new Complex32(TR4 + (C5QA * TR5 - C5QB * TI5), TI4 + (C5QB * TR5 + C5QA * TI5)),
            R3 = new Complex32(TR6 + (-C5QA * TR7 - C5QB * TI7), TI6 + (C5QB * TR7 - C5QA * TI7)),
            R4 = new Complex32(TR8 + (-C5QE * TR9 - C5QD * TI9), TI8 + (C5QD * TR9 - C5QE * TI9)),
            R5 = new Complex32(TR0 - TR1, TI0 - TI1),
            R6 = new Complex32(TR2 - (C5QE * TR3 - C5QD * TI3), TI2 - (C5QD * TR3 + C5QE * TI3)),
            R7 = new Complex32(TR4 - (C5QA * TR5 - C5QB * TI5), TI4 - (C5QB * TR5 + C5QA * TI5)),
            R8 = new Complex32(TR6 - (-C5QA * TR7 - C5QB * TI7), TI6 - (C5QB * TR7 - C5QA * TI7)),
            R9 = new Complex32(TR8 - (-C5QE * TR9 - C5QD * TI9), TI8 - (C5QD * TR9 - C5QE * TI9)),
        };
    }
    private static Registers5 ForwardRadix5(Registers5 r)
    {
        const float C5QA = 0.30901699437494742410229341718282f;
        const float C5QB = 0.95105651629515357211643933337938f;
        const float C5QC = 0.5f;
        const float C5QD = 0.58778525229247312916870595463907f;


    float TR0, TI0, TR1, TI1, TR2, TI2, TR3, TI3, TR4, TI4;

    TR0 = r.R0.Real + r.R1.Real + r.R2.Real + r.R3.Real + r.R4.Real;
    TR1 = (r.R0.Real - C5QC * (r.R2.Real + r.R3.Real)) + C5QB * (r.R1.Imaginary - r.R4.Imaginary)
          + C5QD * (r.R2.Imaginary - r.R3.Imaginary) + C5QA * ((r.R1.Real - r.R2.Real) + (r.R4.Real - r.R3.Real));
    TR4 = (r.R0.Real - C5QC * (r.R2.Real + r.R3.Real)) - C5QB * (r.R1.Imaginary - r.R4.Imaginary)
          - C5QD * (r.R2.Imaginary - r.R3.Imaginary) + C5QA * ((r.R1.Real - r.R2.Real) + (r.R4.Real - r.R3.Real));
    TR2 = (r.R0.Real - C5QC * (r.R1.Real + r.R4.Real)) - C5QB * (r.R2.Imaginary - r.R3.Imaginary)
          + C5QD * (r.R1.Imaginary - r.R4.Imaginary) + C5QA * ((r.R2.Real - r.R1.Real) + (r.R3.Real - r.R4.Real));
    TR3 = (r.R0.Real - C5QC * (r.R1.Real + r.R4.Real)) + C5QB * (r.R2.Imaginary - r.R3.Imaginary)
          - C5QD * (r.R1.Imaginary - r.R4.Imaginary) + C5QA * ((r.R2.Real - r.R1.Real) + (r.R3.Real - r.R4.Real));

    TI0 = r.R0.Imaginary + r.R1.Imaginary + r.R2.Imaginary + r.R3.Imaginary + r.R4.Imaginary;
    TI1 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R3.Imaginary)) - C5QB * (r.R1.Real - r.R4.Real)
          - C5QD * (r.R2.Real - r.R3.Real) + C5QA * ((r.R1.Imaginary - r.R2.Imaginary) + (r.R4.Imaginary - r.R3.Imaginary));
    TI4 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R3.Imaginary)) + C5QB * (r.R1.Real - r.R4.Real)
          + C5QD * (r.R2.Real - r.R3.Real) + C5QA * ((r.R1.Imaginary - r.R2.Imaginary) + (r.R4.Imaginary - r.R3.Imaginary));
    TI2 = (r.R0.Imaginary - C5QC * (r.R1.Imaginary + r.R4.Imaginary)) + C5QB * (r.R2.Real - r.R3.Real)
          - C5QD * (r.R1.Real - r.R4.Real) + C5QA * ((r.R2.Imaginary - r.R1.Imaginary) + (r.R3.Imaginary - r.R4.Imaginary));
    TI3 = (r.R0.Imaginary - C5QC * (r.R1.Imaginary + r.R4.Imaginary)) - C5QB * (r.R2.Real - r.R3.Real)
          + C5QD * (r.R1.Real - r.R4.Real) + C5QA * ((r.R2.Imaginary - r.R1.Imaginary) + (r.R3.Imaginary - r.R4.Imaginary));

        return new Registers5
        {
            R0 = new Complex32(TR0, TI0),
            R1 = new Complex32(TR1, TI1),
            R2 = new Complex32(TR2, TI2),
            R3 = new Complex32(TR3, TI3),
            R4 = new Complex32(TR4, TI4),
        };
    }
    private static Registers5 InverseRadix5(Registers5 r)
    {
        const float C5QA = 0.30901699437494742410229341718282f;
        const float C5QB = 0.95105651629515357211643933337938f;
        const float C5QC = 0.5f;
        const float C5QD = 0.58778525229247312916870595463907f;


    float TR0, TI0, TR1, TI1, TR2, TI2, TR3, TI3, TR4, TI4;

    TR0 = r.R0.Real + r.R1.Real + r.R2.Real + r.R3.Real + r.R4.Real;
    TR1 = (r.R0.Real - C5QC * (r.R2.Real + r.R3.Real)) - C5QB * (r.R1.Imaginary - r.R4.Imaginary)
          - C5QD * (r.R2.Imaginary - r.R3.Imaginary) + C5QA * ((r.R1.Real - r.R2.Real) + (r.R4.Real - r.R3.Real));
    TR4 = (r.R0.Real - C5QC * (r.R2.Real + r.R3.Real)) + C5QB * (r.R1.Imaginary - r.R4.Imaginary)
          + C5QD * (r.R2.Imaginary - r.R3.Imaginary) + C5QA * ((r.R1.Real - r.R2.Real) + (r.R4.Real - r.R3.Real));
    TR2 = (r.R0.Real - C5QC * (r.R1.Real + r.R4.Real)) + C5QB * (r.R2.Imaginary - r.R3.Imaginary)
          - C5QD * (r.R1.Imaginary - r.R4.Imaginary) + C5QA * ((r.R2.Real - r.R1.Real) + (r.R3.Real - r.R4.Real));
    TR3 = (r.R0.Real - C5QC * (r.R1.Real + r.R4.Real)) - C5QB * (r.R2.Imaginary - r.R3.Imaginary)
          + C5QD * (r.R1.Imaginary - r.R4.Imaginary) + C5QA * ((r.R2.Real - r.R1.Real) + (r.R3.Real - r.R4.Real));

    TI0 = r.R0.Imaginary + r.R1.Imaginary + r.R2.Imaginary + r.R3.Imaginary + r.R4.Imaginary;
    TI1 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R3.Imaginary)) + C5QB * (r.R1.Real - r.R4.Real)
          + C5QD * (r.R2.Real - r.R3.Real) + C5QA * ((r.R1.Imaginary - r.R2.Imaginary) + (r.R4.Imaginary - r.R3.Imaginary));
    TI4 = (r.R0.Imaginary - C5QC * (r.R2.Imaginary + r.R3.Imaginary)) - C5QB * (r.R1.Real - r.R4.Real)
          - C5QD * (r.R2.Real - r.R3.Real) + C5QA * ((r.R1.Imaginary - r.R2.Imaginary) + (r.R4.Imaginary - r.R3.Imaginary));
    TI2 = (r.R0.Imaginary - C5QC * (r.R1.Imaginary + r.R4.Imaginary)) - C5QB * (r.R2.Real - r.R3.Real)
          + C5QD * (r.R1.Real - r.R4.Real) + C5QA * ((r.R2.Imaginary - r.R1.Imaginary) + (r.R3.Imaginary - r.R4.Imaginary));
    TI3 = (r.R0.Imaginary - C5QC * (r.R1.Imaginary + r.R4.Imaginary)) + C5QB * (r.R2.Real - r.R3.Real)
          - C5QD * (r.R1.Real - r.R4.Real) + C5QA * ((r.R2.Imaginary - r.R1.Imaginary) + (r.R3.Imaginary - r.R4.Imaginary));

        return new Registers5
        {
            R0 = new Complex32(TR0, TI0),
            R1 = new Complex32(TR1, TI1),
            R2 = new Complex32(TR2, TI2),
            R3 = new Complex32(TR3, TI3),
            R4 = new Complex32(TR4, TI4),
        };
    }
}

public readonly struct Complex32
{
    /// <summary>The real component.</summary>
    public readonly float Real;

    /// <summary>The imaginary component.</summary>
    public readonly float Imaginary;

    /// <summary>Creates a complex value.</summary>
    public Complex32(float real, float imaginary)
    {
        Real = real;
        Imaginary = imaginary;
    }

    /// <summary>Returns the complex conjugate.</summary>
    public Complex32 Conjugate() => new(Real, -Imaginary);

    public static Complex32 operator +(Complex32 left, Complex32 right) =>
        new(left.Real + right.Real, left.Imaginary + right.Imaginary);

    public static Complex32 operator -(Complex32 left, Complex32 right) =>
        new(left.Real - right.Real, left.Imaginary - right.Imaginary);

    public static Complex32 operator *(Complex32 left, Complex32 right) =>
        new(
            left.Real * right.Real - left.Imaginary * right.Imaginary,
            left.Real * right.Imaginary + left.Imaginary * right.Real);

    public static Complex32 operator *(float scalar, Complex32 value) =>
        new(scalar * value.Real, scalar * value.Imaginary);
}

