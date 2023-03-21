﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DotnetRuntimeBootstrapper.AppHost.Core.Prerequisites;
using DotnetRuntimeBootstrapper.AppHost.Core.Utils;

namespace DotnetRuntimeBootstrapper.AppHost.Core;

public abstract class BootstrapperBase
{
    protected const string LegacyAcceptPromptEnvironmentVariable = "DOTNET_INSTALL_PREREQUISITES";
    protected const string AcceptPromptEnvironmentVariable = "DOTNET_ENABLE_BOOTSTRAPPER";

    protected virtual void ReportError(string message)
    {
        // Report to the Windows Event Log. Adapted from:
        // https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/native/corehost/apphost/apphost.windows.cpp#L37-L51
        try
        {
            var applicationFilePath = Assembly.GetExecutingAssembly().Location;
            var applicationName = Path.GetFileName(applicationFilePath);
            var bootstrapperVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

            var content =
                $"""
                Description: Bootstrapper for a .NET application has failed.
                Application: {applicationName}
                Path: {applicationFilePath}
                AppHost: .NET Runtime Bootstrapper v{bootstrapperVersion}
                Message: {message}
                """;

            EventLog.WriteEntry(".NET Runtime", content, EventLogEntryType.Error, 1023);
        }
        catch
        {
            // Ignore
        }
    }

    protected abstract bool Prompt(
        TargetAssembly targetAssembly,
        IPrerequisite[] missingPrerequisites
    );

    protected abstract bool Install(
        TargetAssembly targetAssembly,
        IPrerequisite[] missingPrerequisites
    );

    private bool PromptAndInstall(
        TargetAssembly targetAssembly,
        IPrerequisite[] missingPrerequisites)
    {
        // Install prompt can be disabled in bootstrap configuration or via environment variable
        var isPromptPreAccepted =
            !Configuration.Instance.IsPromptRequired
            ||
            string.Equals(
                Environment.GetEnvironmentVariable(AcceptPromptEnvironmentVariable),
                "true",
                StringComparison.OrdinalIgnoreCase
            )
            ||
            string.Equals(
                Environment.GetEnvironmentVariable(LegacyAcceptPromptEnvironmentVariable),
                "true",
                StringComparison.OrdinalIgnoreCase
            );

        var isPromptAccepted =
            isPromptPreAccepted ||
            Prompt(targetAssembly, missingPrerequisites);

        return
            isPromptAccepted &&
            Install(targetAssembly, missingPrerequisites);
    }

    private int Run(TargetAssembly targetAssembly, string[] args)
    {
        try
        {
            // Hot path: attempt to run the target first without any checks
            return targetAssembly.Run(args);
        }
        // Possible exception causes:
        // - .NET host not found (DirectoryNotFoundException)
        // - .NET host failed to initialize (ApplicationException)
        catch
        {
            // Check for missing prerequisites and install them
            var missingPrerequisites = targetAssembly.GetMissingPrerequisites();
            if (missingPrerequisites.Any())
            {
                var isReadyToRun = PromptAndInstall(targetAssembly, missingPrerequisites);

                // User did not accept the installation or reboot is required
                if (!isReadyToRun)
                    return 0xB007;

                // Reset the environment to update PATH and other variables
                // that may have been changed by the installation process.
                EnvironmentEx.RefreshEnvironmentVariables();

                // Attempt to run the target again
                return targetAssembly.Run(args);
            }

            // There are no missing prerequisites to install, meaning that the
            // app failed to run for reasons unrelated to the bootstrapper.
            throw;
        }
    }

    public int Run(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ReportError(e.ExceptionObject.ToString());

        try
        {
            var targetAssembly = TargetAssembly.Resolve(
                Path.Combine(
                    Path.GetDirectoryName(EnvironmentEx.ProcessPath) ??
                    AppDomain.CurrentDomain.BaseDirectory,
                    Configuration.Instance.TargetFileName
                ),
                Configuration.Instance.TargetPlatform
            );

            return Run(targetAssembly, args);
        }
        catch (Exception ex)
        {
            ReportError(ex.ToString());
            return 0xDEAD;
        }
    }
}