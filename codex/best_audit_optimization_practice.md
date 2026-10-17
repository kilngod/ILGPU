# ILGPU Audit and Optimization Practice

Use this guide when reviewing correctness or performance in `Src/ILGPU`.
The source is a GPU runtime library, so audit results must distinguish
compilation, device discovery, kernel loading, launch, transfer, and kernel
execution. A faster host-side method is useful only if it preserves the
runtime contract and produces a real benefit in the workload being measured.

## Establish the contract first

1. Identify the public operation, its owner, and its execution phase.
   Trace from `Context` and `Device` through `Accelerator`, streams,
   buffers, and the backend implementation.
2. Record the supported backends, device capabilities, data types, dimensions,
   and synchronous or asynchronous behavior. Include CPU as a reference
   where the operation exists, but do not assume its behavior represents a GPU.
3. Map ownership: who allocates and frees native handles, host buffers,
   device buffers, streams, and loaded kernels? Check normal completion,
   exceptions, disposal, and concurrent use.
4. Check native API status codes, capability gates, bounds, alignment, and
   overflow. Keep validation at a boundary that protects the actual
   invariant.
5. Read the template or generator for generated files. An apparent source
   duplication may come from a T4 template.

## Typical correctness risks

- Kernel or architecture selection accepts an unsupported capability or
  rejects a supported one. `Accelerator.Kernels.cs` sorts compatible
  compiled kernels by specificity and skips failed loads individually;
  preserve that behavior when modifying loading.
- Stream operations introduce an unintended synchronization point, break
  ordering, or release a buffer before queued work finishes.
- Host and device views disagree about length, stride, element size,
  alignment, or byte offsets. Inspect both the public view and backend copy
  path.
- Native resources are leaked, disposed twice, or used after accelerator
  disposal. Follow the `AcceleratorObject` and `DisposeBase` ownership
  path.
- A backend import or structure layout differs from the installed native
  library. Verify ABI, DLL/shared-library name, version, and device
  properties for each affected operating system.
- A discovery method hides an error that should be surfaced during actual
  execution. Broad exception handling is intentional at some discovery
  boundaries; it does not justify hiding launch or transfer failures.
- A change assumes a device or backend is available everywhere. Report a
  skip or unsupported capability clearly.

## Performance investigation

- Define the workload before changing code: device and architecture,
  backend, SDK/driver version, build configuration, kernel, input size,
  warmup, iteration count, and timing scope.
- Separate first-use costs (device enumeration, context creation, code
  generation, kernel loading) from steady-state launch and execution.
  Record both when both matter.
- Measure host allocations, launch latency, device execution time, transfer
  time and bytes, and synchronization separately where possible. Do not
  attribute a total-time change to the kernel without isolating those parts.
- Preserve asynchronous semantics while measuring. Synchronize at a
  documented measurement boundary; otherwise host wall time can omit work
  still queued on the GPU.
- Check data movement before optimizing arithmetic. Extra host/device
  copies and stream synchronization can dominate a small kernel.
- Compare equivalent inputs and outputs on the same hardware. Repeat
  enough to see variance. Do not apply a universal regression threshold or
  accept a speedup smaller than measurement noise.
- Change one diagnosed cause at a time. Capture before/after results and
  confirm correctness on the same operation.

## Where optimization may help

- Repeated launch paths: look for boxing, reflection, closure creation,
  avoidable allocations, repeated capability queries, and redundant binding.
  Typed launchers are an established ILGPU performance technique.
- Transfer paths: inspect buffer allocation, temporary storage, page
  locking, copy direction, stride handling, and unnecessary device waits.
- Kernel loading: avoid repeated work after compatibility is established,
  while preserving device-specific selection and error isolation.
- Parallel CPU runtime paths: inspect lock scope, contention, cache
  lifetime, and worker reuse. The code uses locks and caches intentionally;
  remove or change them only after proving their role and measuring cost.
- Generated/native bindings: correct the template or import definition,
  then inspect generated signatures and runtime behavior.

## Evidence and review outcome

For each finding, record the file and operation, violated contract or
measured bottleneck, affected platforms, reproducer, proposed change,
before/after evidence, and remaining limitation. Keep a correctness
finding separate from a performance hypothesis.

Use the closest relevant checks for the changed layer. Build without
warnings, review style and generated output, and run behavior checks on
hardware that can actually exercise the backend. `Src/CLAUDE.md`
documents the current test layers and the required
`--blame-hang-timeout 360s` for compiler tests. A CPU-only result does
not establish a GPU runtime result; a compile-only result does not
establish kernel execution.

## Source anchors

- `Src/ILGPU/Runtime/Accelerator.Kernels.cs`: compatibility,
  kernel loading, and launch preparation.
- `Src/ILGPU/Runtime/MemoryBuffer.cs` and
  `Src/ILGPU/Runtime/ArrayViewExtensions.cs`: buffer/view and transfer
  contracts.
- `Src/ILGPU/Runtime/AcceleratorObject.cs` and
  `Src/ILGPU/Util/DisposeBase.cs`: native resource lifetime.
- `Src/ILGPU/Runtime/ROCm/ROCmDevice.cs`: discovery and
  capability handling.
- `Src/ILGPU/Util/ParallelCache.cs`: concurrency and reuse.
- `Src/.editorconfig`, `Src/ILGPU/.editorconfig`, and
  `.github/workflows/ci.yml`: contributor-facing style checks.
