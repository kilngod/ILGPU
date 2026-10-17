#include <hip/hip_runtime_api.h>
#include <hip/driver_types.h>
#include <type_traits>

#define CHECK_HIP_SIGNATURE(name, ...) \
    static_assert(std::is_same_v<decltype(&name), hipError_t (*)(__VA_ARGS__)>)

CHECK_HIP_SIGNATURE(hipModuleLoadData, hipModule_t*, const void*);
CHECK_HIP_SIGNATURE(hipModuleGetFunction, hipFunction_t*, hipModule_t, const char*);
CHECK_HIP_SIGNATURE(hipModuleUnload, hipModule_t);
CHECK_HIP_SIGNATURE(hipStreamCreate, hipStream_t*);
CHECK_HIP_SIGNATURE(hipStreamDestroy, hipStream_t);
CHECK_HIP_SIGNATURE(hipStreamSynchronize, hipStream_t);
CHECK_HIP_SIGNATURE(hipStreamCreateWithFlags, hipStream_t*, unsigned int);
CHECK_HIP_SIGNATURE(hipModuleLaunchKernel, hipFunction_t, unsigned int,
    unsigned int, unsigned int, unsigned int, unsigned int, unsigned int,
    unsigned int, hipStream_t, void**, void**);
CHECK_HIP_SIGNATURE(hipFree, void*);
CHECK_HIP_SIGNATURE(hipMemcpyAsync, void*, const void*, size_t,
    hipMemcpyKind, hipStream_t);
CHECK_HIP_SIGNATURE(hipMemset, void*, int, size_t);
CHECK_HIP_SIGNATURE(hipMemsetAsync, void*, int, size_t, hipStream_t);
CHECK_HIP_SIGNATURE(hipInit, unsigned int);
CHECK_HIP_SIGNATURE(hipGetDeviceCount, int*);
CHECK_HIP_SIGNATURE(hipGetDevice, int*);
CHECK_HIP_SIGNATURE(hipSetDevice, int);
CHECK_HIP_SIGNATURE(hipGetDevicePropertiesR0600, hipDeviceProp_tR0600*, int);
CHECK_HIP_SIGNATURE(hipDeviceGetName, char*, int, hipDevice_t);
CHECK_HIP_SIGNATURE(hipDeviceGetAttribute, int*, hipDeviceAttribute_t, int);
CHECK_HIP_SIGNATURE(hipDeviceTotalMem, size_t*, hipDevice_t);
CHECK_HIP_SIGNATURE(hipDeviceSynchronize, void);
CHECK_HIP_SIGNATURE(hipDeviceReset, void);
CHECK_HIP_SIGNATURE(hipModuleOccupancyMaxActiveBlocksPerMultiprocessor,
    int*, hipFunction_t, int, size_t);
CHECK_HIP_SIGNATURE(hipModuleOccupancyMaxPotentialBlockSize,
    int*, int*, hipFunction_t, size_t, int);
CHECK_HIP_SIGNATURE(hipDeviceCanAccessPeer, int*, int, int);
CHECK_HIP_SIGNATURE(hipDeviceEnablePeerAccess, int, unsigned int);
CHECK_HIP_SIGNATURE(hipDeviceDisablePeerAccess, int);
static_assert(std::is_same_v<decltype(static_cast<hipError_t (*)(void**, size_t)>(
    &hipMalloc)), hipError_t (*)(void**, size_t)>);

static_assert(hipSuccess == 0);
static_assert(hipErrorInvalidValue == 1);
static_assert(hipErrorOutOfMemory == 2);
static_assert(hipErrorNotInitialized == 3);
static_assert(hipErrorUnknown == 999);

static_assert(hipMemcpyHostToHost == 0);
static_assert(hipMemcpyHostToDevice == 1);
static_assert(hipMemcpyDeviceToHost == 2);
static_assert(hipMemcpyDeviceToDevice == 3);
static_assert(hipMemcpyDefault == 4);

static_assert(hipStreamDefault == 0x00);
static_assert(hipStreamNonBlocking == 0x01);
// The three HIP_LAUNCH_PARAM_* pointer macros are 0x01, 0x02, and 0x03
// in the header; integer-to-pointer casts are not C++ constant expressions.

static_assert(hipDeviceAttributeMaxThreadsPerBlock == 56);
static_assert(hipDeviceAttributeMaxBlockDimX == 26);
static_assert(hipDeviceAttributeMaxBlockDimY == 27);
static_assert(hipDeviceAttributeMaxBlockDimZ == 28);
static_assert(hipDeviceAttributeMaxGridDimX == 29);
static_assert(hipDeviceAttributeMaxGridDimY == 30);
static_assert(hipDeviceAttributeMaxGridDimZ == 31);
static_assert(hipDeviceAttributeMaxSharedMemoryPerBlock == 74);
static_assert(hipDeviceAttributeTotalConstantMemory == 83);
static_assert(hipDeviceAttributeWarpSize == 87);
static_assert(hipDeviceAttributeMaxRegistersPerBlock == 71);
static_assert(hipDeviceAttributeClockRate == 5);
static_assert(hipDeviceAttributeMultiprocessorCount == 63);
static_assert(hipDeviceAttributeIntegrated == 16);
static_assert(hipDeviceAttributeCanMapHostMemory == 3);
static_assert(hipDeviceAttributeComputeMode == 6);
static_assert(hipDeviceAttributeMaxThreadsPerMultiProcessor == 57);
static_assert(hipDeviceAttributeManagedMemory == 24);
static_assert(hipDeviceAttributeL2CacheSize == 19);
static_assert(hipDeviceAttributeMaxSharedMemoryPerMultiprocessor == 10002);
