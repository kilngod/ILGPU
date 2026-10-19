// ---------------------------------------------------------------------------------------
//                                    ILGPU Samples
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: SampleAcceleratorPrompt.cs
//
// This file is part of ILGPU and distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.ROCm;

namespace ILGPU.Samples;

/// <summary>
/// Selects a sample accelerator using ILGPU's automatic device order.
/// </summary>
internal static class SampleAcceleratorPrompt
{
    /// <summary>
    /// Selects the first available accelerator in ILGPU's preferred order.
    /// </summary>
    internal static Device Select(Context context)
    {
        var devices = context.GetPreferredDevices(
            preferCPU: false,
            matchingDevicesOnly: false).ToList();
        var detectedDevices = context.Devices.ToList();

        var compiledAcceleratorTypes = GetCompiledAcceleratorTypes();
        if (compiledAcceleratorTypes.Count > 0)
        {
            devices = devices
                .Where(device => compiledAcceleratorTypes.Contains(
                    device.AcceleratorType))
                .ToList();
            if (devices.Count == 0)
            {
                throw new InvalidOperationException(
                    "No available accelerator matches the kernels compiled " +
                    $"for this sample ({string.Join(", ", compiledAcceleratorTypes)}). " +
                    "Rebuild the sample with ILGPUBackend set to the desired backend.");
            }
        }

        // Include CPU as the final menu option even when a GPU is available.
        if (compiledAcceleratorTypes.Count == 0 ||
            compiledAcceleratorTypes.Contains(AcceleratorType.CPU))
        {
            foreach (var cpuDevice in context.Devices)
            {
                if (cpuDevice.AcceleratorType == AcceleratorType.CPU &&
                    !devices.Any(candidate =>
                        candidate.AcceleratorType == AcceleratorType.CPU))
                {
                    devices.Add(cpuDevice);
                }
            }
        }

        if (devices.Count == 0)
            return context.GetPreferredDevice();

        var device = devices[0];
        Console.WriteLine(
            $"Executing on {device.AcceleratorType}: " +
            $"{device.Name}{GetArchitectureLabel(device)} (default)");

        var alternatives = compiledAcceleratorTypes.Count > 0
            ? detectedDevices
                .Where(candidate => !compiledAcceleratorTypes.Contains(
                    candidate.AcceleratorType))
                .ToList()
            : detectedDevices
                .Where(candidate => candidate.AcceleratorType !=
                    device.AcceleratorType)
                .ToList();
        if (alternatives.Count > 0)
        {
            Console.WriteLine(
                compiledAcceleratorTypes.Count > 0
                    ? "Other ILGPU accelerators detected (not compiled for this sample):"
                    : "Other ILGPU accelerators detected:");
            foreach (var alternative in alternatives)
            {
                Console.WriteLine(
                    $"  {alternative.AcceleratorType}: {alternative.Name}" +
                    GetArchitectureLabel(alternative));
            }
        }

        var backendOptions = detectedDevices
            .Select(candidate => candidate.AcceleratorType)
            .Append(device.AcceleratorType)
            .Distinct()
            .ToArray();
        var otherBackendOptions = backendOptions
            .Where(type => type != device.AcceleratorType)
            .ToArray();
        var otherBackendLabel = otherBackendOptions.Length > 0
            ? $" (or {string.Join(", ", otherBackendOptions)})"
            : string.Empty;
        Console.WriteLine(
            $"To build for another backend, set ILGPUBackend=" +
            $"{device.AcceleratorType}{otherBackendLabel} in " +
            "Samples/Directory.Build.props, then rebuild.");

        return device;
    }

    private static string GetArchitectureLabel(Device device) => device switch
    {
        ROCmDevice rocm when !string.IsNullOrWhiteSpace(rocm.Architecture) =>
            $" ({rocm.Architecture})",
        CudaDevice cuda when cuda.Architecture is { } architecture =>
            $" (sm_{architecture.Major}{architecture.Minor})",
        _ => string.Empty,
    };

    private static HashSet<AcceleratorType> GetCompiledAcceleratorTypes()
    {
        var acceleratorTypes = new HashSet<AcceleratorType>();
        if (Context.NumCompiledKernels == 0)
            return acceleratorTypes;

        foreach (var acceleratorType in Enum.GetValues<AcceleratorType>())
        {
            if (acceleratorType != AcceleratorType.None &&
                acceleratorType != AcceleratorType.NumAcceleratorTypes &&
                Context.GetNumCompiledKernels(acceleratorType) > 0)
            {
                acceleratorTypes.Add(acceleratorType);
            }
        }

        return acceleratorTypes;
    }

}
