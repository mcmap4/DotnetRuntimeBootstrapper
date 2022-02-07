﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DotnetRuntimeBootstrapper.AppHost.Native;
using DotnetRuntimeBootstrapper.AppHost.Utils;
using DotnetRuntimeBootstrapper.AppHost.Utils.Extensions;

namespace DotnetRuntimeBootstrapper.AppHost.Dotnet;

// Headers for hostfxr.dll:
// https://github.com/dotnet/runtime/blob/57bfe47451/src/native/corehost/hostfxr.h

// Muxer implementation:
// https://github.com/dotnet/runtime/blob/57bfe47451/src/native/corehost/fxr/fx_muxer.cpp

// .NET CLI host implementation:
// https://github.com/dotnet/runtime/blob/57bfe47451/src/native/corehost/corehost.cpp

internal partial class DotnetHost : IDisposable
{
    private readonly NativeLibrary _hostfxrLib;

    public DotnetHost(NativeLibrary hostfxrLib) =>
        _hostfxrLib = hostfxrLib;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto, SetLastError = true)]
    private delegate void HostfxrErrorWriterFn(string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    private delegate void HostfxrSetErrorWriterFn(HostfxrErrorWriterFn errorWriterFn);

    private HostfxrSetErrorWriterFn GetHostfxrSetErrorWriterFn() =>
        _hostfxrLib.GetFunction<HostfxrSetErrorWriterFn>("hostfxr_set_error_writer");

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto, SetLastError = true)]
    private delegate int HostfxrInitializeForCommandLineFn(
        int argc,
        string[] argv,
        IntPtr parameters,
        out IntPtr handle
    );

    private HostfxrInitializeForCommandLineFn GetInitializeForCommandLineFn() =>
        _hostfxrLib.GetFunction<HostfxrInitializeForCommandLineFn>("hostfxr_initialize_for_dotnet_command_line");

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    private delegate int HostfxrRunAppFn(IntPtr handle);

    private HostfxrRunAppFn GetRunAppFn() =>
        _hostfxrLib.GetFunction<HostfxrRunAppFn>("hostfxr_run_app");

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    private delegate int HostfxrCloseFn(IntPtr handle);

    private HostfxrCloseFn GetCloseFn() =>
        _hostfxrLib.GetFunction<HostfxrCloseFn>("hostfxr_close");

    private IntPtr Initialize(string targetFilePath, string[] args)
    {
        // Route errors to a buffer
        var errorBuffer = new StringBuilder();
        GetHostfxrSetErrorWriterFn()(s => errorBuffer.AppendLine(s));

        // Initialize the host as if we're running the app from command line
        var initStatus = GetInitializeForCommandLineFn()(
            args.Length + 1,
            args.Prepend(targetFilePath).ToArray(),
            IntPtr.Zero,
            out var handle
        );

        if (initStatus != 0)
        {
            throw new ApplicationException(
                "Failed to initialize .NET host." +
                Environment.NewLine +
                "Target: " + targetFilePath +
                Environment.NewLine +
                "Arguments: [" + string.Join(", ", args) + ']' +
                Environment.NewLine +
                "Status: " + initStatus +
                Environment.NewLine +
                (errorBuffer.Length > 0 ? errorBuffer : "No error messages reported.")
            );
        }

        return handle;
    }

    private int Run(IntPtr handle)
    {
        try
        {
            return GetRunAppFn()(handle);
        }
        catch (SEHException ex)
        {
            // This is thrown when the app crashes with an unhandled exception.
            // Unfortunately, there is no way to get that exception or its message,
            // so the best we can do is to return the associated exit code.
            // https://github.com/Tyrrrz/DotnetRuntimeBootstrapper/issues/23
            return ex.ErrorCode;
        }
    }

    private void Unload(IntPtr handle) => GetCloseFn()(handle);

    public int Run(string targetFilePath, string[] args)
    {
        var handle = Initialize(targetFilePath, args);

        // Run the app
        try
        {
            return Run(handle);
        }
        finally
        {
            Unload(handle);
        }
    }

    public void Dispose() => _hostfxrLib.Dispose();
}

internal partial class DotnetHost
{
    private static string GetHostfxrFilePath()
    {
        // hostfxr.dll resolution strategy:
        // https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/native/corehost/fxr_resolver.cpp#L55-L135
        // 1. Find the hostfxr directory containing versioned subdirectories
        // 2. Get the hostfxr.dll from the subdirectory with the highest version number

        var hostfxrRootDirPath = Path.Combine(Path.Combine(DotnetInstallation.GetDirectoryPath(), "host"), "fxr");
        if (!Directory.Exists(hostfxrRootDirPath))
            throw new DirectoryNotFoundException("Could not find directory containing hostfxr.dll.");

        var highestVersion = default(Version);
        var highestVersionFilePath = default(string);
        foreach (var dirPath in Directory.GetDirectories(hostfxrRootDirPath))
        {
            var version = VersionEx.TryParse(Path.GetFileName(dirPath));
            if (version is null)
                continue;

            var filePath = Path.Combine(dirPath, "hostfxr.dll");
            if (!File.Exists(filePath))
                continue;

            if (highestVersion is null || version > highestVersion)
            {
                highestVersion = version;
                highestVersionFilePath = filePath;
            }
        }

        return highestVersionFilePath ?? throw new FileNotFoundException("Could not find hostfxr.dll.");
    }

    public static DotnetHost Initialize() => new(NativeLibrary.Load(GetHostfxrFilePath()));
}