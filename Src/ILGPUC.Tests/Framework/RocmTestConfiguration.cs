// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: RocmTestConfiguration.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPU.Runtime.ROCm;
using System;
using System.Linq;

namespace ILGPUC.Tests.Framework;

/// <summary>
/// Resolves the architecture used by ROCm code generation in all test layers.
/// </summary>
static class RocmTestConfiguration
{
    private static readonly Lazy<string?> s_deviceArchitecture =
        new(DetectDeviceArchitecture);

    public static string Architecture =>
        Environment.GetEnvironmentVariable("ILGPU_ROCM_ARCH") is { } value &&
        !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : s_deviceArchitecture.Value ?? "gfx1100";

    private static string? DetectDeviceArchitecture()
    {
        try
        {
            return ROCmDevice.GetDevices()
                .OfType<ROCmDevice>()
                .Select(device => device.Architecture)
                .FirstOrDefault(architecture =>
                    !string.IsNullOrWhiteSpace(architecture));
        }
        catch
        {
            // Source-generation tests also run on hosts without a HIP runtime.
            return null;
        }
    }
}
