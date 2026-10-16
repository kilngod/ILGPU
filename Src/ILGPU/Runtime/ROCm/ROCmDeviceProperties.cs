// ---------------------------------------------------------------------------------------
//                                   ILGPU Algorithms
//                        Copyright (c) 2019-2024 ILGPU Project
//                                    www.ilgpu.net
//
// File: ROCmDeviceProperties.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace ILGPU.Runtime.ROCm;

/// <summary>
/// Represents a HIP UUID.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct HipUuid
{
    /// <summary>
    /// The raw UUID bytes.
    /// </summary>
    public fixed byte Bytes[16];
}

/// <summary>
/// Represents HIP device architecture feature flags.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct HipDeviceArch
{
    /// <summary>
    /// The raw HIP bit-field storage.
    /// </summary>
    public readonly uint Flags;

    /// <summary>
    /// Returns true if global 32-bit integer atomics are supported.
    /// </summary>
    public bool HasGlobalInt32Atomics => HasFlag(0);

    /// <summary>
    /// Returns true if global floating-point atomic exchange is supported.
    /// </summary>
    public bool HasGlobalFloatAtomicExch => HasFlag(1);

    /// <summary>
    /// Returns true if shared 32-bit integer atomics are supported.
    /// </summary>
    public bool HasSharedInt32Atomics => HasFlag(2);

    /// <summary>
    /// Returns true if shared floating-point atomic exchange is supported.
    /// </summary>
    public bool HasSharedFloatAtomicExch => HasFlag(3);

    /// <summary>
    /// Returns true if floating-point atomic add is supported.
    /// </summary>
    public bool HasFloatAtomicAdd => HasFlag(4);

    /// <summary>
    /// Returns true if global 64-bit integer atomics are supported.
    /// </summary>
    public bool HasGlobalInt64Atomics => HasFlag(5);

    /// <summary>
    /// Returns true if shared 64-bit integer atomics are supported.
    /// </summary>
    public bool HasSharedInt64Atomics => HasFlag(6);

    /// <summary>
    /// Returns true if double precision is supported.
    /// </summary>
    public bool HasDoubles => HasFlag(7);

    /// <summary>
    /// Returns true if warp vote functions are supported.
    /// </summary>
    public bool HasWarpVote => HasFlag(8);

    /// <summary>
    /// Returns true if warp ballot functions are supported.
    /// </summary>
    public bool HasWarpBallot => HasFlag(9);

    /// <summary>
    /// Returns true if warp shuffle functions are supported.
    /// </summary>
    public bool HasWarpShuffle => HasFlag(10);

    /// <summary>
    /// Returns true if funnel shift functions are supported.
    /// </summary>
    public bool HasFunnelShift => HasFlag(11);

    /// <summary>
    /// Returns true if system thread fences are supported.
    /// </summary>
    public bool HasThreadFenceSystem => HasFlag(12);

    /// <summary>
    /// Returns true if extended synchronization functions are supported.
    /// </summary>
    public bool HasSyncThreadsExt => HasFlag(13);

    /// <summary>
    /// Returns true if surface functions are supported.
    /// </summary>
    public bool HasSurfaceFuncs => HasFlag(14);

    /// <summary>
    /// Returns true if 3D grids are supported.
    /// </summary>
    public bool Has3dGrid => HasFlag(15);

    /// <summary>
    /// Returns true if dynamic parallelism is supported.
    /// </summary>
    public bool HasDynamicParallelism => HasFlag(16);

    private bool HasFlag(int bit) =>
        (Flags & (1u << bit)) != 0;
}

/// <summary>
/// Represents HIP device properties.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct HipDeviceProperties
{
    /// <summary>
    /// The device name.
    /// </summary>
    public fixed byte Name[256];

    /// <summary>
    /// The device UUID.
    /// </summary>
    public HipUuid Uuid;

    /// <summary>
    /// The device LUID on Windows.
    /// </summary>
    public fixed byte Luid[8];

    /// <summary>
    /// The LUID device node mask.
    /// </summary>
    public uint LuidDeviceNodeMask;

    /// <summary>
    /// The total global memory in bytes.
    /// </summary>
    public UIntPtr TotalGlobalMem;

    /// <summary>
    /// The shared memory per block in bytes.
    /// </summary>
    public UIntPtr SharedMemPerBlock;

    /// <summary>
    /// The registers per block.
    /// </summary>
    public int RegsPerBlock;

    /// <summary>
    /// The warp size.
    /// </summary>
    public int WarpSize;

    /// <summary>
    /// The maximum pitch in bytes.
    /// </summary>
    public UIntPtr MemPitch;

    /// <summary>
    /// The maximum threads per block.
    /// </summary>
    public int MaxThreadsPerBlock;

    /// <summary>
    /// The maximum threads in each block dimension.
    /// </summary>
    public fixed int MaxThreadsDim[3];

    /// <summary>
    /// The maximum grid size in each dimension.
    /// </summary>
    public fixed int MaxGridSize[3];

    /// <summary>
    /// The maximum clock frequency in kHz.
    /// </summary>
    public int ClockRate;

    /// <summary>
    /// The total constant memory in bytes.
    /// </summary>
    public UIntPtr TotalConstMem;

    /// <summary>
    /// The major compute capability.
    /// </summary>
    public int Major;

    /// <summary>
    /// The minor compute capability.
    /// </summary>
    public int Minor;

    /// <summary>
    /// The texture alignment.
    /// </summary>
    public UIntPtr TextureAlignment;

    /// <summary>
    /// The texture pitch alignment.
    /// </summary>
    public UIntPtr TexturePitchAlignment;

    /// <summary>
    /// Deprecated; use <see cref="AsyncEngineCount"/>.
    /// </summary>
    public int DeviceOverlap;

    /// <summary>
    /// The number of compute units.
    /// </summary>
    public int MultiProcessorCount;

    /// <summary>
    /// Indicates whether kernel timeout is enabled.
    /// </summary>
    public int KernelExecTimeoutEnabled;

    /// <summary>
    /// Indicates whether this is an integrated GPU.
    /// </summary>
    public int Integrated;

    /// <summary>
    /// Indicates whether host memory can be mapped.
    /// </summary>
    public int CanMapHostMemory;

    /// <summary>
    /// The compute mode.
    /// </summary>
    public int ComputeMode;

    /// <summary>
    /// The maximum 1D texture size.
    /// </summary>
    public int MaxTexture1D;

    /// <summary>
    /// The maximum 1D mipmap texture size.
    /// </summary>
    public int MaxTexture1DMipmap;

    /// <summary>
    /// The maximum 1D linear texture size.
    /// </summary>
    public int MaxTexture1DLinear;

    /// <summary>
    /// The maximum 2D texture dimensions.
    /// </summary>
    public fixed int MaxTexture2D[2];

    /// <summary>
    /// The maximum 2D mipmap texture dimensions.
    /// </summary>
    public fixed int MaxTexture2DMipmap[2];

    /// <summary>
    /// The maximum 2D linear texture dimensions.
    /// </summary>
    public fixed int MaxTexture2DLinear[3];

    /// <summary>
    /// The maximum 2D gather texture dimensions.
    /// </summary>
    public fixed int MaxTexture2DGather[2];

    /// <summary>
    /// The maximum 3D texture dimensions.
    /// </summary>
    public fixed int MaxTexture3D[3];

    /// <summary>
    /// The maximum alternate 3D texture dimensions.
    /// </summary>
    public fixed int MaxTexture3DAlt[3];

    /// <summary>
    /// The maximum cubemap texture size.
    /// </summary>
    public int MaxTextureCubemap;

    /// <summary>
    /// The maximum 1D layered texture dimensions.
    /// </summary>
    public fixed int MaxTexture1DLayered[2];

    /// <summary>
    /// The maximum 2D layered texture dimensions.
    /// </summary>
    public fixed int MaxTexture2DLayered[3];

    /// <summary>
    /// The maximum layered cubemap texture dimensions.
    /// </summary>
    public fixed int MaxTextureCubemapLayered[2];

    /// <summary>
    /// The maximum 1D surface size.
    /// </summary>
    public int MaxSurface1D;

    /// <summary>
    /// The maximum 2D surface dimensions.
    /// </summary>
    public fixed int MaxSurface2D[2];

    /// <summary>
    /// The maximum 3D surface dimensions.
    /// </summary>
    public fixed int MaxSurface3D[3];

    /// <summary>
    /// The maximum 1D layered surface dimensions.
    /// </summary>
    public fixed int MaxSurface1DLayered[2];

    /// <summary>
    /// The maximum 2D layered surface dimensions.
    /// </summary>
    public fixed int MaxSurface2DLayered[3];

    /// <summary>
    /// The maximum cubemap surface size.
    /// </summary>
    public int MaxSurfaceCubemap;

    /// <summary>
    /// The maximum layered cubemap surface dimensions.
    /// </summary>
    public fixed int MaxSurfaceCubemapLayered[2];

    /// <summary>
    /// The surface alignment.
    /// </summary>
    public UIntPtr SurfaceAlignment;

    /// <summary>
    /// Indicates whether concurrent kernels are supported.
    /// </summary>
    public int ConcurrentKernels;

    /// <summary>
    /// Indicates whether ECC is enabled.
    /// </summary>
    public int EccEnabled;

    /// <summary>
    /// The PCI bus ID.
    /// </summary>
    public int PciBusId;

    /// <summary>
    /// The PCI device ID.
    /// </summary>
    public int PciDeviceId;

    /// <summary>
    /// The PCI domain ID.
    /// </summary>
    public int PciDomainId;

    /// <summary>
    /// Indicates whether the device uses TCC driver mode.
    /// </summary>
    public int TccDriver;

    /// <summary>
    /// The number of asynchronous engines.
    /// </summary>
    public int AsyncEngineCount;

    /// <summary>
    /// Indicates whether unified addressing is supported.
    /// </summary>
    public int UnifiedAddressing;

    /// <summary>
    /// The maximum memory clock frequency in kHz.
    /// </summary>
    public int MemoryClockRate;

    /// <summary>
    /// The global memory bus width in bits.
    /// </summary>
    public int MemoryBusWidth;

    /// <summary>
    /// The L2 cache size in bytes.
    /// </summary>
    public int L2CacheSize;

    /// <summary>
    /// The maximum persisting L2 cache size.
    /// </summary>
    public int PersistingL2CacheMaxSize;

    /// <summary>
    /// The maximum resident threads per multiprocessor.
    /// </summary>
    public int MaxThreadsPerMultiProcessor;

    /// <summary>
    /// Indicates whether stream priorities are supported.
    /// </summary>
    public int StreamPrioritiesSupported;

    /// <summary>
    /// Indicates whether global memory is cached in L1.
    /// </summary>
    public int GlobalL1CacheSupported;

    /// <summary>
    /// Indicates whether local memory is cached in L1.
    /// </summary>
    public int LocalL1CacheSupported;

    /// <summary>
    /// The shared memory per multiprocessor in bytes.
    /// </summary>
    public UIntPtr SharedMemPerMultiprocessor;

    /// <summary>
    /// The registers per multiprocessor.
    /// </summary>
    public int RegsPerMultiprocessor;

    /// <summary>
    /// Indicates whether managed memory is supported.
    /// </summary>
    public int ManagedMemory;

    /// <summary>
    /// Indicates whether this device is on a multi-GPU board.
    /// </summary>
    public int IsMultiGpuBoard;

    /// <summary>
    /// The multi-GPU board group ID.
    /// </summary>
    public int MultiGpuBoardGroupId;

    /// <summary>
    /// Indicates whether host-native atomics are supported.
    /// </summary>
    public int HostNativeAtomicSupported;

    /// <summary>
    /// Deprecated CUDA-only field.
    /// </summary>
    public int SingleToDoublePrecisionPerfRatio;

    /// <summary>
    /// Indicates whether pageable memory access is supported.
    /// </summary>
    public int PageableMemoryAccess;

    /// <summary>
    /// Indicates whether concurrent managed access is supported.
    /// </summary>
    public int ConcurrentManagedAccess;

    /// <summary>
    /// Indicates whether compute preemption is supported.
    /// </summary>
    public int ComputePreemptionSupported;

    /// <summary>
    /// Indicates whether host pointers can be used for registered memory.
    /// </summary>
    public int CanUseHostPointerForRegisteredMem;

    /// <summary>
    /// Indicates whether cooperative launch is supported.
    /// </summary>
    public int CooperativeLaunch;

    /// <summary>
    /// Indicates whether cooperative multi-device launch is supported.
    /// </summary>
    public int CooperativeMultiDeviceLaunch;

    /// <summary>
    /// The opt-in shared memory per block in bytes.
    /// </summary>
    public UIntPtr SharedMemPerBlockOptin;

    /// <summary>
    /// Indicates whether pageable memory access uses host page tables.
    /// </summary>
    public int PageableMemoryAccessUsesHostPageTables;

    /// <summary>
    /// Indicates whether the host can directly access managed memory.
    /// </summary>
    public int DirectManagedMemAccessFromHost;

    /// <summary>
    /// The maximum blocks per multiprocessor.
    /// </summary>
    public int MaxBlocksPerMultiProcessor;

    /// <summary>
    /// The maximum access-policy window size.
    /// </summary>
    public int AccessPolicyMaxWindowSize;

    /// <summary>
    /// The shared memory reserved by the driver per block.
    /// </summary>
    public UIntPtr ReservedSharedMemPerBlock;

    /// <summary>
    /// Indicates whether host registration is supported.
    /// </summary>
    public int HostRegisterSupported;

    /// <summary>
    /// Indicates whether sparse HIP arrays are supported.
    /// </summary>
    public int SparseHipArraySupported;

    /// <summary>
    /// Indicates whether read-only host registration is supported.
    /// </summary>
    public int HostRegisterReadOnlySupported;

    /// <summary>
    /// Indicates whether timeline semaphore interop is supported.
    /// </summary>
    public int TimelineSemaphoreInteropSupported;

    /// <summary>
    /// Indicates whether memory pools are supported.
    /// </summary>
    public int MemoryPoolsSupported;

    /// <summary>
    /// Indicates whether GPU Direct RDMA is supported.
    /// </summary>
    public int GpuDirectRDMASupported;

    /// <summary>
    /// GPU Direct RDMA flush write options.
    /// </summary>
    public uint GpuDirectRDMAFlushWritesOptions;

    /// <summary>
    /// GPU Direct RDMA write ordering.
    /// </summary>
    public int GpuDirectRDMAWritesOrdering;

    /// <summary>
    /// Supported memory pool handle types.
    /// </summary>
    public uint MemoryPoolSupportedHandleTypes;

    /// <summary>
    /// Indicates whether deferred HIP array mapping is supported.
    /// </summary>
    public int DeferredMappingHipArraySupported;

    /// <summary>
    /// Indicates whether IPC events are supported.
    /// </summary>
    public int IpcEventSupported;

    /// <summary>
    /// Indicates whether cluster launch is supported.
    /// </summary>
    public int ClusterLaunch;

    /// <summary>
    /// Indicates whether unified function pointers are supported.
    /// </summary>
    public int UnifiedFunctionPointers;

    /// <summary>
    /// Reserved CUDA fields.
    /// </summary>
    public fixed int Reserved[63];

    /// <summary>
    /// Reserved HIP fields.
    /// </summary>
    public fixed int HipReserved[32];

    /// <summary>
    /// The AMD GCN architecture name.
    /// </summary>
    public fixed byte GcnArchName[256];

    /// <summary>
    /// The maximum shared memory per multiprocessor in bytes.
    /// </summary>
    public UIntPtr MaxSharedMemoryPerMultiProcessor;

    /// <summary>
    /// The device-side clock instruction frequency in kHz.
    /// </summary>
    public int ClockInstructionRate;

    /// <summary>
    /// The architectural feature flags.
    /// </summary>
    public HipDeviceArch Arch;

    /// <summary>
    /// The HDP memory flush-control register address.
    /// </summary>
    public IntPtr HdpMemFlushCntl;

    /// <summary>
    /// The HDP register flush-control register address.
    /// </summary>
    public IntPtr HdpRegFlushCntl;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device functions are supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedFunc;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device grid dimensions are supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedGridDim;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device block dimensions are supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedBlockDim;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device shared memory is supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedSharedMem;

    /// <summary>
    /// Indicates whether this is a large PCI BAR device.
    /// </summary>
    public int IsLargeBar;

    /// <summary>
    /// The ASIC revision.
    /// </summary>
    public int AsicRevision;

    /// <summary>
    /// Returns the device name.
    /// </summary>
    public string GetName()
    {
        fixed (byte* ptr = Name)
            return CreateString(ptr);
    }

    /// <summary>
    /// Returns the AMD GCN architecture name.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "This method pins a fixed buffer to create the string.")]
    public string GetGcnArchName()
    {
        fixed (byte* ptr = GcnArchName)
            return CreateString(ptr);
    }

    private static string CreateString(byte* ptr) =>
        ptr == null || *ptr == 0
        ? string.Empty
        : new string((sbyte*)ptr);
}

/// <summary>
/// Represents the deprecated HIP R0000 device properties layout.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct HipDevicePropertiesR0000
{
    /// <summary>
    /// The device name.
    /// </summary>
    public fixed byte Name[256];

    /// <summary>
    /// The total global memory in bytes.
    /// </summary>
    public UIntPtr TotalGlobalMem;

    /// <summary>
    /// The shared memory per block in bytes.
    /// </summary>
    public UIntPtr SharedMemPerBlock;

    /// <summary>
    /// The registers per block.
    /// </summary>
    public int RegsPerBlock;

    /// <summary>
    /// The warp size.
    /// </summary>
    public int WarpSize;

    /// <summary>
    /// The maximum threads per block.
    /// </summary>
    public int MaxThreadsPerBlock;

    /// <summary>
    /// The maximum threads in each block dimension.
    /// </summary>
    public fixed int MaxThreadsDim[3];

    /// <summary>
    /// The maximum grid size in each dimension.
    /// </summary>
    public fixed int MaxGridSize[3];

    /// <summary>
    /// The maximum clock frequency in kHz.
    /// </summary>
    public int ClockRate;

    /// <summary>
    /// The maximum memory clock frequency in kHz.
    /// </summary>
    public int MemoryClockRate;

    /// <summary>
    /// The global memory bus width in bits.
    /// </summary>
    public int MemoryBusWidth;

    /// <summary>
    /// The total constant memory in bytes.
    /// </summary>
    public UIntPtr TotalConstMem;

    /// <summary>
    /// The major compute capability.
    /// </summary>
    public int Major;

    /// <summary>
    /// The minor compute capability.
    /// </summary>
    public int Minor;

    /// <summary>
    /// The number of compute units.
    /// </summary>
    public int MultiProcessorCount;

    /// <summary>
    /// The L2 cache size in bytes.
    /// </summary>
    public int L2CacheSize;

    /// <summary>
    /// The maximum resident threads per multiprocessor.
    /// </summary>
    public int MaxThreadsPerMultiProcessor;

    /// <summary>
    /// The compute mode.
    /// </summary>
    public int ComputeMode;

    /// <summary>
    /// The device-side clock instruction frequency in kHz.
    /// </summary>
    public int ClockInstructionRate;

    /// <summary>
    /// The architectural feature flags.
    /// </summary>
    public HipDeviceArch Arch;

    /// <summary>
    /// Indicates whether concurrent kernels are supported.
    /// </summary>
    public int ConcurrentKernels;

    /// <summary>
    /// The PCI domain ID.
    /// </summary>
    public int PciDomainId;

    /// <summary>
    /// The PCI bus ID.
    /// </summary>
    public int PciBusId;

    /// <summary>
    /// The PCI device ID.
    /// </summary>
    public int PciDeviceId;

    /// <summary>
    /// The maximum shared memory per multiprocessor in bytes.
    /// </summary>
    public UIntPtr MaxSharedMemoryPerMultiProcessor;

    /// <summary>
    /// Indicates whether this device is on a multi-GPU board.
    /// </summary>
    public int IsMultiGpuBoard;

    /// <summary>
    /// Indicates whether host memory can be mapped.
    /// </summary>
    public int CanMapHostMemory;

    /// <summary>
    /// Deprecated GCN architecture value.
    /// </summary>
    public int GcnArch;

    /// <summary>
    /// The AMD GCN architecture name.
    /// </summary>
    public fixed byte GcnArchName[256];

    /// <summary>
    /// Indicates whether this is an integrated GPU.
    /// </summary>
    public int Integrated;

    /// <summary>
    /// Indicates whether cooperative launch is supported.
    /// </summary>
    public int CooperativeLaunch;

    /// <summary>
    /// Indicates whether cooperative multi-device launch is supported.
    /// </summary>
    public int CooperativeMultiDeviceLaunch;

    /// <summary>
    /// The maximum 1D linear texture size.
    /// </summary>
    public int MaxTexture1DLinear;

    /// <summary>
    /// The maximum 1D texture size.
    /// </summary>
    public int MaxTexture1D;

    /// <summary>
    /// The maximum 2D texture dimensions.
    /// </summary>
    public fixed int MaxTexture2D[2];

    /// <summary>
    /// The maximum 3D texture dimensions.
    /// </summary>
    public fixed int MaxTexture3D[3];

    /// <summary>
    /// The HDP memory flush-control register address.
    /// </summary>
    public IntPtr HdpMemFlushCntl;

    /// <summary>
    /// The HDP register flush-control register address.
    /// </summary>
    public IntPtr HdpRegFlushCntl;

    /// <summary>
    /// The maximum pitch in bytes.
    /// </summary>
    public UIntPtr MemPitch;

    /// <summary>
    /// The texture alignment.
    /// </summary>
    public UIntPtr TextureAlignment;

    /// <summary>
    /// The texture pitch alignment.
    /// </summary>
    public UIntPtr TexturePitchAlignment;

    /// <summary>
    /// Indicates whether kernel timeout is enabled.
    /// </summary>
    public int KernelExecTimeoutEnabled;

    /// <summary>
    /// Indicates whether ECC is enabled.
    /// </summary>
    public int EccEnabled;

    /// <summary>
    /// Indicates whether the device uses TCC driver mode.
    /// </summary>
    public int TccDriver;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device functions are supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedFunc;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device grid dimensions are supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedGridDim;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device block dimensions are supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedBlockDim;

    /// <summary>
    /// Indicates whether unmatched cooperative multi-device shared memory is supported.
    /// </summary>
    public int CooperativeMultiDeviceUnmatchedSharedMem;

    /// <summary>
    /// Indicates whether this is a large PCI BAR device.
    /// </summary>
    public int IsLargeBar;

    /// <summary>
    /// The ASIC revision.
    /// </summary>
    public int AsicRevision;

    /// <summary>
    /// Indicates whether managed memory is supported.
    /// </summary>
    public int ManagedMemory;

    /// <summary>
    /// Indicates whether the host can directly access managed memory.
    /// </summary>
    public int DirectManagedMemAccessFromHost;

    /// <summary>
    /// Indicates whether concurrent managed access is supported.
    /// </summary>
    public int ConcurrentManagedAccess;

    /// <summary>
    /// Indicates whether pageable memory access is supported.
    /// </summary>
    public int PageableMemoryAccess;

    /// <summary>
    /// Indicates whether pageable memory access uses host page tables.
    /// </summary>
    public int PageableMemoryAccessUsesHostPageTables;

    /// <summary>
    /// Returns the AMD GCN architecture name.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "This method pins a fixed buffer to create the string.")]
    public string GetGcnArchName()
    {
        fixed (byte* ptr = GcnArchName)
            return ptr == null || *ptr == 0
                ? string.Empty
                : new string((sbyte*)ptr);
    }
}
