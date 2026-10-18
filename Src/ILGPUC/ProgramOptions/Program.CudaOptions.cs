// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: Program.CudaOptions.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPUC.Backends;
using ILGPUC.Backends.Cuda;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace ILGPUC;

sealed partial class Program
{
    /// <summary>
    /// Represents options for the Cuda backend
    /// </summary>
    sealed class CudaOptions : BackendOptions
    {
        /// <summary>
        /// One or more CUDA compute-capability architecture strings
        /// (e.g. <c>SM_80</c>) to target, or <c>auto</c> to use local CUDA
        /// devices, falling back to SM_75 for remote compilation.
        /// </summary>
        readonly Option<string[]> _cudaArchitecturesOption = new("--cuda-arch")
        {
            Description = "CUDA architecture (default: auto-detect; remote fallback: SM_75)",
            DefaultValueFactory = _ => ["auto"],
            AllowMultipleArgumentsPerToken = true
        };

        /// <summary>
        /// One or more PTX ISA version strings (e.g. <c>8.0</c>) to target.
        /// </summary>
        readonly Option<string[]> _cudaInstructionSetsOption = new("--cuda-isa")
        {
            Description = "Cuda instruction set to use",
            DefaultValueFactory = _ => ["8.0"],
            AllowMultipleArgumentsPerToken = true
        };

        /// <summary>
        /// Registers all options to control PTX compilation properties with the command.
        /// </summary>
        /// <param name="command">The command to attach options to.</param>
        public CudaOptions(Command command) : base(BackendType.Cuda)
        {
            command.Add(_cudaArchitecturesOption);
            command.Add(_cudaInstructionSetsOption);
        }

        /// <summary>
        /// Creates Cuda backend instances with desired configurations.
        /// </summary>
        public override IEnumerable<Backend> CreateBackends(
            CompilationProperties properties,
            ParseResult parseResult)
        {
            var requestedArchitectures = parseResult
                .GetRequiredValue(_cudaArchitecturesOption)
                .ToArray();
            var architectures = ResolveArchitectures(
                "--cuda-arch",
                requestedArchitectures,
                DetectLocalArchitectures,
                "SM_75")
                .Select(architecture =>
                {
                    if (!AcceleratorArchitecture.TryParse(architecture, out var arch))
                    {
                        throw new ArgumentException(
                            $"Invalid CUDA architecture '{architecture}'. " +
                            "Expected SM_NN, for example SM_80.");
                    }
                    return arch;
                })
                .ToArray();
            var instructionSets = parseResult
                .GetRequiredValue(_cudaInstructionSetsOption)
                .Select(instructionSet =>
                {
                    if (!CudaInstructionSet.TryParse(instructionSet, out var isa))
                        isa = CudaInstructionSet.ISA_80;
                    return isa;
                })
                .ToArray();

            foreach (var arch in architectures)
                foreach (var isa in instructionSets)
                    yield return new CudaBackend(arch, isa);
        }

        static IEnumerable<string> DetectLocalArchitectures()
        {
            using (var context = Context.Create(builder => builder.Cuda()))
            {
                var architectures = new List<string>();
                foreach (var device in context.GetDevices<CudaDevice>())
                {
                    if (device.Architecture.HasValue)
                        architectures.Add(device.Architecture.Value.ToString());
                }
                return architectures.ToArray();
            }
        }
    }
}
