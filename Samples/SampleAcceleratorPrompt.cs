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
using System.Threading;
using System.Threading.Tasks;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.ROCm;

namespace ILGPU.Samples;

/// <summary>
/// Prompts for a sample accelerator while using ILGPU's automatic device order
/// as the default selection.
/// </summary>
internal static class SampleAcceleratorPrompt
{
    private const int SelectionTimeoutSeconds = 4;
    private const string NonInteractiveEnvironmentVariable =
        "ILGPU_SAMPLE_NONINTERACTIVE";
    private const string AcceleratorTypeEnvironmentVariable =
        "ILGPU_SAMPLE_DEVICE";

    /// <summary>
    /// Selects an accelerator for a sample. Interactive consoles get a short
    /// selection prompt; redirected consoles use ILGPU's automatic choice.
    /// </summary>
    internal static Device Select(Context context)
    {
        var devices = context.GetPreferredDevices(
            preferCPU: false,
            matchingDevicesOnly: false).ToList();

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
            foreach (var device in context.Devices)
            {
                if (device.AcceleratorType == AcceleratorType.CPU &&
                    !devices.Any(candidate =>
                        candidate.AcceleratorType == AcceleratorType.CPU))
                {
                    devices.Add(device);
                }
            }
        }

        var requestedType = Environment.GetEnvironmentVariable(
            AcceleratorTypeEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(requestedType))
        {
            if (!Enum.TryParse(
                requestedType,
                ignoreCase: true,
                out AcceleratorType type))
            {
                throw new ArgumentException(
                    $"Unknown accelerator type '{requestedType}' in " +
                    $"{AcceleratorTypeEnvironmentVariable}.");
            }

            devices = devices
                .Where(device => device.AcceleratorType == type)
                .ToList();
            if (devices.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No {type} accelerator is available in this context.");
            }
        }

        if (devices.Count == 0)
            return context.GetPreferredDevice();

        // GetPreferredDevices places the normal Auto choice first. This keeps
        // menu behavior aligned with ILGPU's common device selection policy.
        if (string.Equals(
            Environment.GetEnvironmentVariable(NonInteractiveEnvironmentVariable),
            "1",
            StringComparison.Ordinal))
        {
            return devices[0];
        }

        var selectedIndex = 0;
        Console.WriteLine("Available accelerators for this sample:");
        for (int i = 0; i < devices.Count; ++i)
        {
            var device = devices[i];
            var defaultLabel = i == 0 ? " (default)" : string.Empty;
            Console.WriteLine(
                $"  {i + 1}. {device.AcceleratorType}: {device.Name}" +
                $"{GetArchitectureLabel(device)}{defaultLabel}");
        }
        if (Console.IsInputRedirected)
        {
            Console.WriteLine(
                "Press Enter for the default, or type a number and press Enter. " +
                "Auto-running in 4 seconds.");
            selectedIndex = ReadLineSelection(devices.Count);
            return devices[selectedIndex];
        }

        Console.WriteLine("Use Up/Down to choose, Enter to run, or wait 4 seconds.");

        var timeout = DateTime.UtcNow.AddSeconds(SelectionTimeoutSeconds);
        var previousStatusLength = 0;
        while (DateTime.UtcNow < timeout)
        {
            var remaining = Math.Max(
                1,
                (int)Math.Ceiling((timeout - DateTime.UtcNow).TotalSeconds));
            var status =
                $"Selected: {selectedIndex + 1}. " +
                $"{devices[selectedIndex].AcceleratorType}: " +
                $"{devices[selectedIndex].Name}; auto-running in {remaining}s. ";
            Console.Write('\r');
            Console.Write(status.PadRight(previousStatusLength));
            previousStatusLength = status.Length;

            if (TryReadKey(out var key))
            {
                if (key == ConsoleKey.Enter)
                    break;
                if (key == ConsoleKey.UpArrow)
                    selectedIndex = (selectedIndex - 1 + devices.Count) % devices.Count;
                else if (key == ConsoleKey.DownArrow)
                    selectedIndex = (selectedIndex + 1) % devices.Count;
            }
            else
            {
                Thread.Sleep(25);
            }
        }

        Console.WriteLine();
        return devices[selectedIndex];
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

    private static int ReadLineSelection(int deviceCount)
    {
        using var cancellation = new CancellationTokenSource();
        var readTask = Console.In.ReadLineAsync(cancellation.Token).AsTask();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(SelectionTimeoutSeconds));
        if (Task.WhenAny(readTask, timeoutTask).GetAwaiter().GetResult() != readTask)
        {
            cancellation.Cancel();
            return 0;
        }

        var input = readTask.GetAwaiter().GetResult();
        return int.TryParse(input, out int selectedNumber) &&
            selectedNumber > 0 && selectedNumber <= deviceCount
            ? selectedNumber - 1
            : 0;
    }

    private static bool TryReadKey(out ConsoleKey key)
    {
        try
        {
            if (Console.KeyAvailable)
            {
                key = Console.ReadKey(intercept: true).Key;
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Some IDE consoles do not support non-blocking key input.
        }

        key = default;
        return false;
    }
}
