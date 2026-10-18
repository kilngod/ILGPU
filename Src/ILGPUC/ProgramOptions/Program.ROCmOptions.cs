// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: Program.ROCmOptions.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.Linq;
using ILGPU;
using ILGPU.Runtime.ROCm;
using ILGPUC.Backends;
using ILGPUC.Backends.ROCm;

namespace ILGPUC;

sealed partial class Program
{
    /// <summary>
    /// Represents options for the Hip backend
    /// </summary>
    sealed class ROCmOptions : BackendOptions
    {
        /// <summary>
        /// One or more supported ROCm GPU architectures to target, or
        /// <c>auto</c> to use eligible local devices and fall back to gfx1100
        /// when no local device is available.
        /// </summary>
        readonly Option<string[]> _rocmArchitecturesOption = new("--rocm-arch")
        {
            Description = "ROCm architecture (default: auto-detect; remote fallback: gfx1100)",
            DefaultValueFactory = _ => ["auto"],
            AllowMultipleArgumentsPerToken = true
        };

        /// <summary>
        /// Registers all options to control ROCm compilation properties with the command.
        /// </summary>
        /// <param name="command">The command to attach options to.</param>
        public ROCmOptions(Command command) : base(BackendType.ROCm)
        {
            command.Add(_rocmArchitecturesOption);
        }

        /// <summary>
        /// Creates ROCm backend instances with desired configurations.
        /// </summary>
        public override IEnumerable<Backend> CreateBackends(
            CompilationProperties properties,
            ParseResult parseResult)
        {
            var requestedArchitectures = parseResult
                .GetRequiredValue(_rocmArchitecturesOption)
                .ToArray();

            var architectures = ResolveArchitectures(
                "--rocm-arch",
                requestedArchitectures,
                DetectLocalArchitectures,
                "gfx1100");

            foreach (var arch in architectures)
            {
                if (!IsSupportedArchitecture(arch))
                {
                    throw new ArgumentException(
                        $"ROCm architecture '{arch}' is not supported by ILGPU. " +
                        "Supported targets are gfx1100 and newer, plus " +
                        "gfx908, gfx90a, gfx942, gfx950, and gfx1030.");
                }

                yield return new ROCmBackend(arch);
            }
        }

        static IEnumerable<string> DetectLocalArchitectures()
        {
            using (var context = Context.Create(builder => builder.ROCm()))
            {
                var devices = context.GetDevices<ROCmDevice>();
                var architectures = new List<string>();
                var detectedArchitectures = new List<string>();
                foreach (var device in devices)
                {
                    detectedArchitectures.Add(device.Architecture);
                    var architecture = GetProcessorArchitecture(device.Architecture);
                    if (IsSupportedArchitecture(architecture))
                        architectures.Add(architecture);
                }

                if (devices.Count > 0 && architectures.Count == 0)
                {
                    var detected = string.Join(", ", detectedArchitectures);
                    throw new InvalidOperationException(
                        "Local ROCm devices were found, but none use an ILGPU " +
                        $"supported target. Detected: {detected}. " +
                        "Supported targets are gfx1100 and newer, plus gfx908, " +
                        "gfx90a, gfx942, gfx950, and gfx1030.");
                }

                return architectures.ToArray();
            }
        }

        static bool IsSupportedArchitecture(string architecture)
        {
            var processor = GetProcessorArchitecture(architecture);
            if (!processor.StartsWith("gfx", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(
                    processor.AsSpan(3),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out int target))
            {
                return false;
            }

            return target >= 0x1100 ||
                target is 0x908 or 0x90a or 0x942 or 0x950 or 0x1030;
        }

        static string GetProcessorArchitecture(string architecture)
        {
            int featureSeparator = architecture.IndexOf(
                ':',
                StringComparison.Ordinal);
            return featureSeparator >= 0
                ? architecture[..featureSeparator]
                : architecture;
        }
    }
}
