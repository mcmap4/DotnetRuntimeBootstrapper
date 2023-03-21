using System;
using System.IO;
using DotnetRuntimeBootstrapper.AppHost.Core.Platform;
using Microsoft.Win32;

namespace DotnetRuntimeBootstrapper.AppHost.Core.Dotnet;

internal static class DotnetInstallation
{
    private static string? TryGetDirectoryPathFromRegistry(bool targetIs32Bit)
    {
        var dotnetRegistryKey = Registry.LocalMachine.OpenSubKey(
            ((OperatingSystemEx.ProcessorArchitecture.Is64Bit() && !targetIs32Bit)
                ? "SOFTWARE\\Wow6432Node\\"
                : "SOFTWARE\\") +
            "dotnet\\Setup\\InstalledVersions\\" +
            (targetIs32Bit ? ProcessorArchitecture.X86.ToString().ToLowerInvariant() : OperatingSystemEx.ProcessorArchitecture.GetMoniker()),
            false
        );

        var dotnetDirPath = dotnetRegistryKey?.GetValue("InstallLocation", null) as string;

        return !string.IsNullOrEmpty(dotnetDirPath) && Directory.Exists(dotnetDirPath)
            ? dotnetDirPath
            : null;
    }

    private static string? TryGetDirectoryPathFromEnvironment(bool targetIs32Bit)
    {
        // Environment.GetFolderPath(ProgramFiles) does not return the correct path
        // if the apphost is running in x86 mode on an x64 system, so we rely
        // on an environment variable instead.
        var programFilesDirPath =
            (targetIs32Bit ? Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") : Environment.GetEnvironmentVariable("PROGRAMFILES")) ??
            Environment.GetEnvironmentVariable("ProgramW6432") ??
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        var dotnetDirPath = Path.Combine(programFilesDirPath, "dotnet");

        return !string.IsNullOrEmpty(dotnetDirPath) && Directory.Exists(dotnetDirPath)
            ? dotnetDirPath
            : null;
    }

    // .NET installation location design docs:
    // https://github.com/dotnet/designs/blob/main/accepted/2020/install-locations.md
    public static string GetDirectoryPath(bool targetIs32Bit) =>
        // Try to resolve location from registry (covers both custom and default locations)
        TryGetDirectoryPathFromRegistry(targetIs32Bit) ??
        // Try to resolve location from program files (default location)
        TryGetDirectoryPathFromEnvironment(targetIs32Bit) ??
        throw new DirectoryNotFoundException("Could not find .NET installation directory.");
}