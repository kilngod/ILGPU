using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using ILGPU.Runtime.ROCm;

if (args.Length > 0 && args[0] == "--layout")
{
    Console.WriteLine($"HipDeviceProperties size: {Marshal.SizeOf<HipDeviceProperties>()}");
    Console.WriteLine($"GcnArchName offset: {Marshal.OffsetOf<HipDeviceProperties>(nameof(HipDeviceProperties.GcnArchName))}");
    Console.WriteLine($"AsicRevision offset: {Marshal.OffsetOf<HipDeviceProperties>(nameof(HipDeviceProperties.AsicRevision))}");
    Console.WriteLine($"HipDevicePropertiesR0000 size: {Marshal.SizeOf<HipDevicePropertiesR0000>()}");
    Console.WriteLine($"R0000 GcnArchName offset: {Marshal.OffsetOf<HipDevicePropertiesR0000>(nameof(HipDevicePropertiesR0000.GcnArchName))}");
    Console.WriteLine($"HipDeviceArch size: {Marshal.SizeOf<HipDeviceArch>()}");
    Console.WriteLine($"HipUuid size: {Marshal.SizeOf<HipUuid>()}");
    return;
}

if (args.Length > 0 && args[0] == "--stream-only")
{
    var runtime = NativeLibrary.Load(@"C:\Program Files\AMD\ROCm\7.2\bin\amdhip64_7.dll");
    try
    {
        var init = Marshal.GetDelegateForFunctionPointer<HipInit>(
            NativeLibrary.GetExport(runtime, "hipInit"));
        var create = Marshal.GetDelegateForFunctionPointer<HipStreamCreate>(
            NativeLibrary.GetExport(runtime, "hipStreamCreate"));
        var destroy = Marshal.GetDelegateForFunctionPointer<HipStreamDestroy>(
            NativeLibrary.GetExport(runtime, "hipStreamDestroy"));
        Console.WriteLine($"hipInit: {init(0)}");
        Console.WriteLine($"hipStreamCreate: {create(out var stream)}");
        if (stream != IntPtr.Zero)
            Console.WriteLine($"hipStreamDestroy: {destroy(stream)}");
    }
    finally
    {
        NativeLibrary.Free(runtime);
    }
    return;
}

if (args.Length > 0 && args[0] == "--module-only")
{
    var runtime = NativeLibrary.Load(@"C:\Program Files\AMD\ROCm\7.2\bin\amdhip64_7.dll");
    try
    {
        T Get<T>(string name) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(
                NativeLibrary.GetExport(runtime, name));
        Console.WriteLine($"hipInit: {Get<HipInit>("hipInit")(0)}");
        var image = File.ReadAllBytes("codex/hipcc-probe/device.hsaco");
        var pin = GCHandle.Alloc(image, GCHandleType.Pinned);
        try
        {
            var result = Get<HipModuleLoadData>("hipModuleLoadData")(
                out var module, pin.AddrOfPinnedObject());
            Console.WriteLine($"hipModuleLoadData: {result}");
            if (result == 0)
                Console.WriteLine($"hipModuleUnload: {Get<HipModuleUnload>("hipModuleUnload")(module)}");
        }
        finally
        {
            pin.Free();
        }
    }
    finally
    {
        NativeLibrary.Free(runtime);
    }
    return;
}

var apiType = typeof(ROCmDevice).Assembly.GetType("ILGPU.Runtime.ROCm.ROCmAPI")!;
var api = apiType.GetProperty("CurrentAPI", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;
Console.WriteLine($"API supported: {apiType.GetProperty("IsSupported")!.GetValue(api)}");
Console.WriteLine($"ROCm devices: {ROCmDevice.GetDevices().Length}");
foreach (var device in ROCmDevice.GetDevices())
    Console.WriteLine($"{device}; architecture={((ROCmDevice)device).Architecture}");
if (args.Length > 0 && args[0] == "--discover-only")
    return;

var library = NativeLibrary.Load(@"C:\Program Files\AMD\ROCm\7.2\bin\amdhip64_7.dll");
try
{
    T Get<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(
            NativeLibrary.GetExport(library, name));
    var init = Get<HipInit>("hipInit");
    Console.WriteLine($"hipInit: {init(0)}");
    Console.WriteLine($"hipSetDevice: {Get<HipSetDevice>("hipSetDevice")(0)}");
    var createStream = Get<HipStreamCreate>("hipStreamCreate");
    var syncStream = Get<HipStreamSynchronize>("hipStreamSynchronize");
    var destroyStream = Get<HipStreamDestroy>("hipStreamDestroy");
    var malloc = Get<HipMalloc>("hipMalloc");
    var free = Get<HipFree>("hipFree");
    var copy = Get<HipMemcpyAsync>("hipMemcpyAsync");
    var memset = Get<HipMemsetAsync>("hipMemsetAsync");
    Console.WriteLine($"hipStreamCreate: {createStream(out var stream)}");
    Console.WriteLine($"hipMalloc: {malloc(out var gpuBuffer, 16)}");
    try
    {
        var source = new byte[16];
        var target = new byte[16];
        for (var i = 0; i < source.Length; i++)
            source[i] = (byte)(i + 1);
        var sourcePin = GCHandle.Alloc(source, GCHandleType.Pinned);
        var targetPin = GCHandle.Alloc(target, GCHandleType.Pinned);
        try
        {
            Console.WriteLine($"hipMemsetAsync: {memset(gpuBuffer, 0, 16, stream)}");
            Console.WriteLine($"hipMemcpyAsync H2D: {copy(gpuBuffer, sourcePin.AddrOfPinnedObject(), 16, 1, stream)}");
            Console.WriteLine($"hipMemcpyAsync D2H: {copy(targetPin.AddrOfPinnedObject(), gpuBuffer, 16, 2, stream)}");
            Console.WriteLine($"hipStreamSynchronize: {syncStream(stream)}");
            Console.WriteLine($"memory roundtrip: {source.AsSpan().SequenceEqual(target)}");
        }
        finally
        {
            sourcePin.Free();
            targetPin.Free();
        }

        var image = File.ReadAllBytes("codex/hipcc-probe/device.hsaco");
        var imagePin = GCHandle.Alloc(image, GCHandleType.Pinned);
        try
        {
            var loadModule = Get<HipModuleLoadData>("hipModuleLoadData");
            var loadResult = loadModule(out var module, imagePin.AddrOfPinnedObject());
            Console.WriteLine($"hipModuleLoadData: {loadResult}");
            if (loadResult == 0)
            {
                try
                {
                    var getFunction = Get<HipModuleGetFunction>("hipModuleGetFunction");
                    Console.WriteLine($"hipModuleGetFunction: {getFunction(out var function, module, "method_CopyKernel_4479")}");
                    if (function != IntPtr.Zero)
                    {
                        var launch = Get<HipModuleLaunchKernel>("hipModuleLaunchKernel");
                        Console.WriteLine($"hipModuleLaunchKernel (zero grid): {launch(function, 0, 1, 1, 1, 1, 1, 0, stream, IntPtr.Zero, IntPtr.Zero)}");
                    }
                }
                finally
                {
                    Console.WriteLine($"hipModuleUnload: {Get<HipModuleUnload>("hipModuleUnload")(module)}");
                }
            }
        }
        finally
        {
            imagePin.Free();
        }
    }
    finally
    {
        Console.WriteLine($"hipFree: {free(gpuBuffer)}");
        Console.WriteLine($"hipStreamDestroy: {destroyStream(stream)}");
    }
    Console.WriteLine($"hipDeviceSynchronize: {Get<HipDeviceSynchronize>("hipDeviceSynchronize")()}");
}
finally
{
    NativeLibrary.Free(library);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipInit(uint flags);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipSetDevice(int device);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipStreamCreate(out IntPtr stream);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipStreamSynchronize(IntPtr stream);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipStreamDestroy(IntPtr stream);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipMalloc(out IntPtr pointer, nuint size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipFree(IntPtr pointer);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipMemcpyAsync(IntPtr destination, IntPtr source, nuint size, int kind, IntPtr stream);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipMemsetAsync(IntPtr destination, int value, nuint size, IntPtr stream);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipModuleLoadData(out IntPtr module, IntPtr image);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
delegate int HipModuleGetFunction(out IntPtr function, IntPtr module, string name);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipModuleUnload(IntPtr module);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipModuleLaunchKernel(IntPtr function, uint gridX, uint gridY, uint gridZ, uint blockX, uint blockY, uint blockZ, uint sharedMemory, IntPtr stream, IntPtr parameters, IntPtr extra);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int HipDeviceSynchronize();
