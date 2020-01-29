using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CUDAtrace
{
    
    class Denoiser
    {
        public const string Filename = "OpenImageDenoise.dll";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool ProgressDelegate(IntPtr userPtr, double n);

        [DllImport(Filename, EntryPoint = "oidnNewDevice")]
        public static extern IntPtr CreateDevice(OIDNDeviceType type);
        [DllImport(Filename, EntryPoint = "oidnCommitDevice")]
        public static extern void Commit(IntPtr deviceId);
        [DllImport(Filename, EntryPoint = "oidnNewFilter")]
        public static extern IntPtr CreateFilter(IntPtr deviceId, [MarshalAs(UnmanagedType.LPStr)] string type);
        [DllImport(Filename, EntryPoint = "oidnSetSharedFilterImage")]
        public static extern void SetFilterImage(IntPtr filterId, [MarshalAs(UnmanagedType.LPStr)] string type, IntPtr buffer, OIDNFormat format, ulong width, ulong height, ulong byteOffset, ulong bytePixelStride, ulong byteRowStride);
        [DllImport(Filename, EntryPoint = "oidnSetFilter1b")]
        public static extern void SetFilterBoolean(IntPtr filterId, [MarshalAs(UnmanagedType.LPStr)] string name, bool value);
        [DllImport(Filename, EntryPoint = "oidnCommitFilter")]
        public static extern void CommitFilter(IntPtr filterId);
        [DllImport(Filename, EntryPoint = "oidnExecuteFilter")]
        public static extern void ExecuteFilter(IntPtr filterId);
        [DllImport(Filename, EntryPoint = "oidnReleaseFilter")]
        public static extern void ReleaseFilter(IntPtr filterId);
        [DllImport(Filename, EntryPoint = "oidnReleaseDevice")]
        public static extern void ReleaseDevice(IntPtr deviceId);
        [DllImport(Filename, EntryPoint = "oidnSetFilterProgressMonitorFunction")]
        public static extern void SetFilterProgressMonitorFunction(IntPtr filterId, [MarshalAs(UnmanagedType.FunctionPtr)]ProgressDelegate callback, IntPtr userPtr);
        [DllImport(Filename, EntryPoint = "oidnGetDeviceError")]
        public static extern void GetDeviceError(IntPtr deviceId, [Out][MarshalAsAttribute(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStr)] string[] messages);

    }

    public enum OIDNDeviceType
    {
        OIDN_DEVICE_TYPE_DEFAULT = 0, // select device automatically
        OIDN_DEVICE_TYPE_CPU = 1, // CPU device
    }

    public enum OIDNError
    {
        OIDN_ERROR_NONE = 0, // no error occurred
        OIDN_ERROR_UNKNOWN = 1, // an unknown error occurred
        OIDN_ERROR_INVALID_ARGUMENT = 2, // an invalid argument was specified
        OIDN_ERROR_INVALID_OPERATION = 3, // the operation is not allowed
        OIDN_ERROR_OUT_OF_MEMORY = 4, // not enough memory to execute the operation
        OIDN_ERROR_UNSUPPORTED_HARDWARE = 5, // the hardware (e.g. CPU) is not supported
        OIDN_ERROR_CANCELLED = 6, // the operation was cancelled by the user
    }

    public enum OIDNFormat
    {
        OIDN_FORMAT_UNDEFINED = 0,

        // 32-bit single-precision floating point scalar and vector formats
        OIDN_FORMAT_FLOAT = 1,
        OIDN_FORMAT_FLOAT2 = 2,
        OIDN_FORMAT_FLOAT3 = 3,
        OIDN_FORMAT_FLOAT4 = 4,
    }

    public enum OIDNAccess
    {
        OIDN_ACCESS_READ = 0, // read-only access
        OIDN_ACCESS_WRITE = 1, // write-only access
        OIDN_ACCESS_READ_WRITE = 2, // read and write access
        OIDN_ACCESS_WRITE_DISCARD = 3, // write-only access, previous contents discarded
    }
}
