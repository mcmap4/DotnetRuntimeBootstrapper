﻿using System;
using System.IO;
using System.Linq;
using DotnetRuntimeBootstrapper.AppHost.Core.Platform;
using DotnetRuntimeBootstrapper.AppHost.Core.Utils;
using DotnetRuntimeBootstrapper.AppHost.Core.Utils.Extensions;
using QuickJson;

namespace DotnetRuntimeBootstrapper.AppHost.Core.Dotnet;

internal partial class DotnetRuntime
{
    public string Name { get; }

    public Version Version { get; }

    public string PlatformTarget { get; }

    public bool IsBase =>
        string.Equals(Name, "Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase);

    public bool IsWindowsDesktop =>
        string.Equals(Name, "Microsoft.WindowsDesktop.App", StringComparison.OrdinalIgnoreCase);

    public bool IsAspNet =>
        string.Equals(Name, "Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase);

    public bool Is32Bit => !string.IsNullOrEmpty(PlatformTarget) &&
            PlatformTarget.Equals(ProcessorArchitecture.X86.ToString(), StringComparison.InvariantCultureIgnoreCase);

    public DotnetRuntime(string name, Version version, string platformTarget = null)
    {
        Name = name;
        Version = version;
        PlatformTarget = platformTarget;
    }

    public bool IsSupersededBy(DotnetRuntime other) =>
        string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
        Version.Major == other.Version.Major &&
        Version <= other.Version;
}

internal partial class DotnetRuntime
{
    public static DotnetRuntime[] GetAllInstalled(string platformTarget, bool is32BitTarget)
    {
        var sharedDirPath = Path.Combine(DotnetInstallation.GetDirectoryPath(is32BitTarget), "shared");
        if (!Directory.Exists(sharedDirPath))
            throw new DirectoryNotFoundException("Could not find directory containing .NET runtime binaries.");

        return (
            from runtimeDirPath in Directory.GetDirectories(sharedDirPath)
            let name = Path.GetFileName(runtimeDirPath)
            from runtimeVersionDirPath in Directory.GetDirectories(runtimeDirPath)
            let version = VersionEx.TryParse(Path.GetFileName(runtimeVersionDirPath))
            where version is not null
            select new DotnetRuntime(name, version, platformTarget)
        ).ToArray();
    }

    public static DotnetRuntime[] GetAllTargets(string runtimeConfigFilePath, string platformTarget)
    {
        static DotnetRuntime ParseRuntime(JsonNode json, string platformTarget)
        {
            var name = json.TryGetChild("name")?.TryGetString();
            var version = json.TryGetChild("version")?.TryGetString()?.Pipe(VersionEx.TryParse);

            return !string.IsNullOrEmpty(name) && version is not null
                ? new DotnetRuntime(name, version, platformTarget)
                : throw new ApplicationException("Could not parse runtime info from runtime config.");
        }

        var json =
            Json.TryParse(File.ReadAllText(runtimeConfigFilePath)) ??
            throw new ApplicationException($"Failed to parse runtime config '{runtimeConfigFilePath}'.");

        return
            // Multiple targets
            json
                .TryGetChild("runtimeOptions")?
                .TryGetChild("frameworks")?
                .EnumerateChildren()
                .Select(n => ParseRuntime(n, platformTarget))
                .ToArray() ??

            // Single target
            json
                .TryGetChild("runtimeOptions")?
                .TryGetChild("framework")?
                .ToSingletonEnumerable()
                .Select(n => ParseRuntime(n, platformTarget))
                .ToArray() ??

            throw new ApplicationException("Could not resolve target runtime from runtime config.");
    }
}