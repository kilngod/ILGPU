// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: Program.MetalOptions.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Metal;
using ILGPUC.Backends;
using ILGPUC.Backends.Metal;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace ILGPUC;

sealed partial class Program
{
    /// <summary>
    /// Represents options for the Metal backend.
    /// </summary>
    sealed class MetalOptions : BackendOptions
    {
        /// <summary>
        /// One or more Metal GPU family strings
        /// (e.g. <c>Apple7</c>, <c>Apple8</c>, <c>Apple9</c>) to target, or
        /// <c>auto</c> to use local devices, falling back to Apple7 remotely.
        /// </summary>
        readonly Option<string[]> _metalArchOption = new("--metal-arch")
        {
            Description = "Metal GPU family (default: auto-detect; remote fallback: Apple7)",
            DefaultValueFactory = _ => ["auto"],
            AllowMultipleArgumentsPerToken = true
        };

        /// <summary>Metal target platform string (<c>macOS</c> or <c>iOS</c>).</summary>
        readonly Option<string> _metalPlatformOption = new("--metal-platform")
        {
            Description = "Metal target platform (macOS, iOS)",
            DefaultValueFactory = _ => "macOS"
        };

        /// <summary>
        /// Registers Metal backend options with the command.
        /// </summary>
        /// <param name="command">The command to attach options to.</param>
        public MetalOptions(Command command) : base(BackendType.Metal)
        {
            command.Add(_metalArchOption);
            command.Add(_metalPlatformOption);
        }

        /// <summary>
        /// Creates Metal backend instances with desired configurations.
        /// </summary>
        public override IEnumerable<Backend> CreateBackends(
            CompilationProperties properties,
            ParseResult parseResult)
        {
            var platform = Enum.Parse<MetalTargetOS>(
                parseResult.GetRequiredValue(_metalPlatformOption),
                ignoreCase: true);

            var requestedArchitectures = parseResult
                .GetRequiredValue(_metalArchOption)
                .ToArray();
            var architectures = ResolveArchitectures(
                "--metal-arch",
                requestedArchitectures,
                DetectLocalArchitectures,
                nameof(MetalGPUFamily.Apple7))
                .Select(architecture => Enum.Parse<MetalGPUFamily>(
                    architecture, ignoreCase: true))
                .ToArray();

            foreach (var arch in architectures)
                yield return new MetalBackend(platform, arch);
        }

        static IEnumerable<string> DetectLocalArchitectures()
        {
            using (var context = Context.Create(builder => builder.Metal()))
            {
                var architectures = new List<string>();
                foreach (var device in context.GetDevices<MetalDevice>())
                    architectures.Add(device.GPUFamily.ToString());
                return architectures.ToArray();
            }
        }
    }
}
