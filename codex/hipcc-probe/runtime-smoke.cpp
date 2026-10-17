#include <cstdio>
#include <hip/hip_runtime.h>

int main(int argc, char**) {
    std::printf("hipInit=%d\n", static_cast<int>(hipInit(0)));
    std::fflush(stdout);
    int count = 0;
    const auto countResult = hipGetDeviceCount(&count);
    std::printf("hipGetDeviceCount=%d count=%d\n",
        static_cast<int>(countResult), count);
    std::fflush(stdout);
    hipStream_t stream = nullptr;
    const auto create = argc > 1
        ? hipStreamCreateWithFlags(&stream, hipStreamNonBlocking)
        : hipStreamCreate(&stream);
    std::printf("hipStreamCreate%s=%d\n", argc > 1 ? "WithFlags" : "",
        static_cast<int>(create));
    std::fflush(stdout);
    if (create == hipSuccess)
        std::printf("hipStreamDestroy=%d\n",
            static_cast<int>(hipStreamDestroy(stream)));
    return 0;
}
