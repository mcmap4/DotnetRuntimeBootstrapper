using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace DotnetRuntimeBootstrapper.AppHost.Core.Native;

internal static class NativeMethods
{
    [System.Flags]
    public enum LoadLibraryFlags : uint
    {
        None = 0,
        DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
        LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
        LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
        LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
        LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
        LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
        LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
        LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
        LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
        LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
        LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008,
        LOAD_LIBRARY_REQUIRE_SIGNED_TARGET = 0x00000080,
        LOAD_LIBRARY_SAFE_CURRENT_DIRS = 0x00002000,
    }

    private const string Kernel32 = "kernel32.dll";
    private const string Shell32 = "shell32.dll";
    private const string NtDll = "ntdll.dll";

    [DllImport(Kernel32, SetLastError = true)]
    public static extern void GetNativeSystemInfo(ref SystemInfo lpSystemInfo);

    [DllImport(NtDll, SetLastError = true)]
    public static extern void RtlGetVersion(ref SystemVersionInfo versionInfo);

    [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CreateJobObject(IntPtr hAttributes, string? lpName);

    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool SetInformationJobObject(
        IntPtr hJob,
        JobObjectInfoType infoType,
        IntPtr lpJobObjectInfo,
        uint cbJobObjectInfoLength
    );

    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

    // This function doesn't come in the Unicode variant
    [DllImport(Kernel32, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport(Shell32, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out ushort lpiIcon);
}