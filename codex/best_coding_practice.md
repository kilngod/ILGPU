# ILGPU Coding Practice

This guide applies to changes in `Src/ILGPU`, the ILGPU runtime library. It
summarizes conventions visible in the current source and enforced by repository
configuration. Follow the nearest established implementation when a file has a
more specific pattern. The authoritative settings are `Src/.editorconfig`,
`Src/ILGPU/.editorconfig`, `Src/ILGPU/ILGPU.csproj`, and the style checks in
`.github/workflows/ci.yml`.

## Fit the existing architecture

- Put device discovery and capabilities in `Runtime/{CPU,Cuda,OpenCL,ROCm,Metal}`.
  Shared accelerator and memory behavior belongs in `Runtime`. Keep backend
  details in the matching backend directory.
- Preserve the roles of `Context`, `Device`, `Accelerator`,
  `AcceleratorStream`, `MemoryBuffer`, `ArrayView`, and `Kernel`. A
  device descriptor describes hardware; an accelerator owns a live execution
  context; a stream orders asynchronous work. Avoid bypassing those owners.
- Extend existing abstractions and partial classes when a change spans shared
  and backend code. Match the naming and layout of neighboring files.
- Keep native compilation service and compiler selection work in
  `Src/ILGPUC.Compilers` or `Src/ILGPUC`. `Src/ILGPU` owns the runtime
  interfaces and native API bindings.
- Treat a new public member as an API change. Preserve existing overloads and
  behavior unless the change explicitly calls for a breaking change. Document
  capabilities, ownership, lifetime, and failure behavior.

## C# and file style

- Use the ILGPU copyright and license header, with the correct file name and
  year range, following neighboring source files. The repository's copyright
  check is part of CI.
- Use four spaces, CRLF line endings, and a final newline in C# files. Keep
  C# and T4 lines at or below 90 characters unless the file uses the existing
  `// disable: max_line_length` exception. T4 templates have a separate
  line-ending check and must not end with a newline.
- Follow the local `.editorconfig`: braces on new lines; spaces around
  operators and after commas; `using` directives outside the namespace;
  interfaces prefixed with `I`; explicit access modifiers where appropriate.
  Use the style already present in the file for namespace and `using` order.
- Use XML documentation for public and protected APIs, including parameters,
  return values, remarks, and exceptions where they clarify the contract.
  Prefer meaningful comments about invariants or hardware behavior over
  comments that repeat the code.
- Use language keywords (`int`, `bool`, etc.) in declarations. Use
  expression-bodied members where the local style and clarity support them.
  Keep methods focused and avoid unrelated formatting churn.
- Nullable reference types and unsafe blocks are enabled. Model nullability
  accurately, constrain unsafe scopes, and document pointer lifetimes.

## Ownership and synchronization

- Respect the ownership chain visible in `AcceleratorObject` and
  `DisposeBase`. An accelerator registers and disposes its child objects;
  native handles, buffers, streams, and loaded kernels need clear cleanup.
- State whether an object or operation is thread safe. Do not infer safety
  from a neighboring lock: `Accelerator` and `MemoryBuffer` explicitly
  document that their members are not thread safe.
- Use existing binding and stream operations before calling native APIs.
  Preserve stream ordering and make synchronization explicit. A synchronous
  convenience method may synchronize; an asynchronous method must not
  silently acquire that cost.
- Validate public inputs and device capabilities at the appropriate boundary.
  Keep diagnostics meaningful. Backend discovery may suppress native API
  failures to report no device, as `ROCmDevice.GetDevices` does; do not copy
  that broad catch into execution paths.
- Dispose resources on success and failure. Use `try/finally` or the
  established disposal pattern when ownership crosses a throwing call.

## Performance-sensitive code

- Identify whether code runs during discovery, kernel loading, kernel launch,
  memory transfer, or per-element computation. Apply the strongest allocation
  and dispatch scrutiny to repeated launch and transfer paths.
- Prefer typed views, spans, and launchers where they avoid boxing or copies.
  Do not ban `List<T>` or other collections universally: the runtime uses
  them at setup boundaries. Show a measured or structural reason before
  replacing an established collection or adding pooling.
- Avoid unintended host/device copies, extra synchronization, repeated
  device queries, and per-launch allocations. Keep debug and profiling costs
  explicit so normal execution does not pay for them.
- Use `AggressiveInlining`, `AggressiveOptimization`, or unsafe code only
  where the existing pattern and evidence justify them. They are not general
  style requirements.
- Preserve capability checks and architecture-specific behavior. A source
  path that works on CPU does not establish CUDA, ROCm, OpenCL, or Metal
  behavior.

## Generated code and backend changes

- Many files in `Src/ILGPU` are generated from `.tt` and `.ttinclude`
  templates. Edit the template or generator as the source of truth; inspect
  generated output when the change affects its contract.
- Keep target-specific API bindings in their existing XML/T4 or backend
  source locations. Verify names, calling conventions, and platform library
  names before changing native imports.
- When adding a device capability or kernel behavior, update the relevant
  device description, loading checks, and backend-specific implementation
  together. A failure to load one kernel should not corrupt other loaded
  kernels.

## Contribution completion

- Build with no warnings in the configurations relevant to the change.
  `README.md` requests warning-free builds; `ILGPU.csproj` currently
  declares Debug and Release configurations.
- Use the closest relevant source and test layer for behavior changes. The
  broader solution's test guidance is in `Src/CLAUDE.md`; compiler tests
  require `--blame-hang-timeout 360s`.
- Review the diff for generated files, copyright headers, line lengths,
  unintended API changes, and backend-specific assumptions. Report which
  platforms and configurations were actually checked.

## Repository examples

- `Src/ILGPU/Runtime/Accelerator.cs`: API documentation, ownership,
  thread-safety notes, and shared runtime structure.
- `Src/ILGPU/Runtime/Accelerator.Kernels.cs`: kernel compatibility,
  loading, and launch preparation.
- `Src/ILGPU/Runtime/AcceleratorObject.cs` and
  `Src/ILGPU/Util/DisposeBase.cs`: disposal and child ownership.
- `Src/ILGPU/Runtime/ROCm/ROCmDevice.cs`: backend discovery and
  capability boundaries.
- `Src/ILGPU/Util/ParallelCache.cs`: a measured-use cache and explicit
  synchronization rather than a blanket ban on collections.
