// ---------------------------------------------------------------------------------------
//                                    ILGPU Samples
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: SampleDeviceSelector.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

global using ILGPU.Samples;

using System;
using System.Linq;
using ILGPU.Runtime;

namespace ILGPU.Samples;

/// <summary>
/// Selects a device for samples, honoring an explicit runtime backend request.
/// </summary>
internal static class SampleDeviceSelector
{
    internal const string EnvironmentVariable = "ILGPU_SAMPLE_DEVICE";

    internal static Device Select(Context context, bool preferCpu = false)
    {
        var requested = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (!Enum.TryParse<AcceleratorType>(requested, true, out var type) ||
                !Enum.IsDefined(type))
            {
                throw new ArgumentException(
                    $"Invalid {EnvironmentVariable}='{requested}'. " +
                    "Use CPU, Cuda, ROCm, OpenCL, or Metal.");
            }

            return context.Devices.FirstOrDefault(
                device => device.AcceleratorType == type) ??
                throw new InvalidOperationException(
                    $"No {type} device is available for {EnvironmentVariable}. " +
                    "Check the installed runtime, driver, and GPU.");
        }

        if (preferCpu)
            return context.GetPreferredDevice(preferCPU: true);

        return context.Devices
            .OrderByDescending(device => device.AcceleratorType switch
            {
                AcceleratorType.Metal => 5,
                AcceleratorType.Cuda => 4,
                AcceleratorType.ROCm => 3,
                AcceleratorType.OpenCL => 2,
                AcceleratorType.CPU => 1,
                _ => 0,
            })
            .First();
    }
}
