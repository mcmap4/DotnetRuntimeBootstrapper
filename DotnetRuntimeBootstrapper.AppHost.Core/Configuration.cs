using System;
using System.Collections.Generic;
using System.Reflection;
using DotnetRuntimeBootstrapper.AppHost.Core.Utils.Extensions;

namespace DotnetRuntimeBootstrapper.AppHost.Core;

public partial class Configuration
{
    public string TargetFileName { get; init; } = default!;

    public bool IsPromptRequired { get; init; } = true;

    public string TargetPlatform { get; init; } = default!;
}

public partial class Configuration
{
    public static Configuration Instance { get; } = Resolve();

    private static Configuration Resolve()
    {
        string data;
        
        try
        {
            data = Assembly.GetExecutingAssembly().GetManifestResourceString(nameof(Configuration));
        }
        catch (Exception ex)
        {
#if DEBUG
            data = $"""
            TargetFileName=C:\project\DotnetRuntimeBootstrapper\DotnetRuntimeBootstrapper.Demo.Gui\bin\Debug\net7.0-windows\DotnetRuntimeBootstrapper.Demo.Gui.dll
            IsPromptRequired=true
            TargetPlatform=x86
            """;
#else
            throw;
#endif
        }

        var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in data.Split('\n'))
        {
            var components = line.Split('=');
            if (components.Length != 2)
                continue;

            var key = components[0].Trim();
            var value = components[1].Trim();

            parsed[key] = value;
        }

        return new Configuration
        {
            TargetFileName = parsed[nameof(TargetFileName)],

            IsPromptRequired = string.Equals(
                parsed[nameof(IsPromptRequired)],
                "true",
                StringComparison.OrdinalIgnoreCase
            ),

            TargetPlatform = parsed[nameof(TargetPlatform)]
        };
    }
}