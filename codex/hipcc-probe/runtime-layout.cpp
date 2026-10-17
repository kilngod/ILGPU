#include <cstddef>
#include <hip/hip_runtime_api.h>
#include <hip/hip_deprecated.h>

static_assert(sizeof(hipDeviceProp_tR0600) == 1472);
static_assert(offsetof(hipDeviceProp_tR0600, gcnArchName) == 1160);
static_assert(offsetof(hipDeviceProp_tR0600, asicRevision) == 1468);
static_assert(sizeof(hipDeviceArch_t) == 4);
static_assert(sizeof(hipUUID) == 16);
static_assert(sizeof(hipDeviceProp_tR0000) == 792);
static_assert(offsetof(hipDeviceProp_tR0000, gcnArchName) == 396);
