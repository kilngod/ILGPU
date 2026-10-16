// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: RocmCompilerMode.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

namespace ILGPUC.Compilers;

/// <summary>
/// Selects how HIP compilation is routed.
/// </summary>
public enum RocmCompilerMode
{
    /// <summary>
    /// Use the normal remote-service discovery chain, then the local fallback.
    /// </summary>
    Auto,

    /// <summary>
    /// Use the local HIP compiler regardless of configured or discovered services.
    /// </summary>
    Local,
}
