// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: Program.BackendOptions.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPUC.Backends;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace ILGPUC;

sealed partial class Program
{
    /// <summary>
    /// Abstract base class for backend-specific command-line options.
    /// Each concrete subclass registers its own options with the command and
    /// creates the corresponding <see cref="Backend"/> instances.
    /// </summary>
    /// <param name="backendType">
    /// The backend type represented by this options object.
    /// </param>
    abstract class BackendOptions(BackendType backendType)
    {
        /// <summary>
        /// Resolves an architecture option using local devices when requested,
        /// falling back to the backend's conservative target for remote builds.
        /// </summary>
        protected static string[] ResolveArchitectures(
            string optionName,
            string[] requestedArchitectures,
            Func<IEnumerable<string>> detectArchitectures,
            string fallbackArchitecture)
        {
            if (!requestedArchitectures.Any(
                architecture => string.Equals(
                    architecture,
                    "auto",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return requestedArchitectures;
            }

            if (requestedArchitectures.Length != 1)
            {
                throw new ArgumentException(
                    $"{optionName} auto cannot be combined with explicit architectures.");
            }

            var detectedArchitectures = detectArchitectures()
                .Where(architecture => !string.IsNullOrWhiteSpace(architecture))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // A remote compiler container cannot inspect a host GPU. Use the
            // backend's stable baseline target when local discovery is absent.
            return detectedArchitectures.Length > 0
                ? detectedArchitectures
                : [fallbackArchitecture];
        }

        /// <summary>The backend type represented by this options object.</summary>
        public BackendType BackendType { get; } = backendType;

        /// <summary>
        /// Creates one or more <see cref="Backend"/> instances configured from the
        /// parsed command-line values.
        /// </summary>
        /// <param name="properties">
        /// Compilation properties resolved from CLI options.
        /// </param>
        /// <param name="parseResult">The command-line parse result.</param>
        /// <returns>
        /// A sequence of <see cref="Backend"/> instances ready for code generation.
        /// </returns>
        public abstract IEnumerable<Backend> CreateBackends(
            CompilationProperties properties,
            ParseResult parseResult);
    }
}
