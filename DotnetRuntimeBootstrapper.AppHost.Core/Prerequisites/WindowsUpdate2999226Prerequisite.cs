﻿using System;
using System.Linq;
using DotnetRuntimeBootstrapper.AppHost.Core.Platform;
using DotnetRuntimeBootstrapper.AppHost.Core.Utils;

namespace DotnetRuntimeBootstrapper.AppHost.Core.Prerequisites;

// Universal C Runtime
internal class WindowsUpdate2999226Prerequisite : IPrerequisite
{
    private const string Id = "KB2999226";

    public string DisplayName => $"Windows Update {Id}";

    public bool IsInstalled() =>
        OperatingSystemEx.Version >= OperatingSystemVersion.Windows10
        || OperatingSystemEx.GetInstalledUpdates().Contains(Id, StringComparer.OrdinalIgnoreCase);

    private string GetInstallerDownloadUrl()
    {
        if (
            OperatingSystemEx.Version == OperatingSystemVersion.Windows7
            && OperatingSystemEx.ProcessorArchitecture == ProcessorArchitecture.X64
        )
        {
            return "https://download.microsoft.com/download/1/1/5/11565A9A-EA09-4F0A-A57E-520D5D138140/Windows6.1-KB2999226-x64.msu";
        }

        if (
            OperatingSystemEx.Version == OperatingSystemVersion.Windows7
            && OperatingSystemEx.ProcessorArchitecture == ProcessorArchitecture.X86
        )
        {
            return "https://download.microsoft.com/download/4/F/E/4FE73868-5EDD-4B47-8B33-CE1BB7B2B16A/Windows6.1-KB2999226-x86.msu";
        }

        if (
            OperatingSystemEx.Version == OperatingSystemVersion.Windows8
            && OperatingSystemEx.ProcessorArchitecture == ProcessorArchitecture.X64
        )
        {
            return "https://download.microsoft.com/download/A/C/1/AC15393F-A6E6-469B-B222-C44B3BB6ECCC/Windows8-RT-KB2999226-x64.msu";
        }

        if (
            OperatingSystemEx.Version == OperatingSystemVersion.Windows8
            && OperatingSystemEx.ProcessorArchitecture == ProcessorArchitecture.X86
        )
        {
            return "https://download.microsoft.com/download/1/E/8/1E8AFE90-5217-464D-9292-7D0B95A56CE4/Windows8-RT-KB2999226-x86.msu";
        }

        if (
            OperatingSystemEx.Version == OperatingSystemVersion.Windows8_1
            && OperatingSystemEx.ProcessorArchitecture == ProcessorArchitecture.X64
        )
        {
            return "https://download.microsoft.com/download/9/6/F/96FD0525-3DDF-423D-8845-5F92F4A6883E/Windows8.1-KB2999226-x64.msu";
        }

        if (
            OperatingSystemEx.Version == OperatingSystemVersion.Windows8_1
            && OperatingSystemEx.ProcessorArchitecture == ProcessorArchitecture.X86
        )
        {
            return "https://download.microsoft.com/download/E/4/6/E4694323-8290-4A08-82DB-81F2EB9452C2/Windows8.1-KB2999226-x86.msu";
        }

        throw new ApplicationException("Unsupported operating system version.");
    }

    public IPrerequisiteInstaller DownloadInstaller(Action<double>? handleProgress)
    {
        var filePath = FileEx.GenerateTempFilePath($"{Id}.msu");

        Http.DownloadFile(GetInstallerDownloadUrl(), filePath, handleProgress);

        return new WindowsUpdatePrerequisiteInstaller(this, filePath);
    }
}
