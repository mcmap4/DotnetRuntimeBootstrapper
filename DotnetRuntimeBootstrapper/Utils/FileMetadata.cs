﻿using System;
using System.IO;

namespace DotnetRuntimeBootstrapper.Utils
{
    internal static class FileMetadata
    {
        private static readonly Lazy<string> RceditCliFilePathLazy = new(() =>
        {
            // Same directory as the assembly (true when used from demo project)
            var immediateDirPath =
                Path.GetDirectoryName(typeof(FileMetadata).Assembly.Location) ??
                Directory.GetCurrentDirectory();

            var immediateFilePath = Path.Combine(immediateDirPath, "rcedit.exe");
            if (File.Exists(immediateFilePath))
                return immediateFilePath;

            // Parent directory (true when used from NuGet package)
            var parentDirPath = Directory.GetParent(immediateDirPath)?.FullName;
            if (!string.IsNullOrWhiteSpace(parentDirPath))
            {
                var parentFilePath = Path.Combine(parentDirPath, "rcedit.exe");
                if (File.Exists(parentFilePath))
                    return parentFilePath;
            }

            // Give up and just hope it's on PATH
            return "rcedit.exe";
        });

        private static string RceditCliFilePath => RceditCliFilePathLazy.Value;

        private static string? GetVersionString(string filePath, string name) =>
            CommandLine.TryRunWithOutput(
                RceditCliFilePath,
                new[] {filePath, "--get-version-string", name}
            )?.Trim();

        private static void SetVersionString(string filePath, string name, string value) =>
            CommandLine.RunWithOutput(
                RceditCliFilePath,
                new[] {filePath, "--set-version-string", name, value}
            );

        public static string? GetAuthor(string filePath) =>
            GetVersionString(filePath, "CompanyName");

        public static string? GetProductName(string filePath) =>
            GetVersionString(filePath, "ProductName");

        public static string? GetDescription(string filePath) =>
            GetVersionString(filePath, "FileDescription");

        public static string? GetFileVersion(string filePath) =>
            GetVersionString(filePath, "FileVersion");

        public static string? GetProductVersion(string filePath) =>
            GetVersionString(filePath, "ProductVersion");

        public static string? GetCopyright(string filePath) =>
            GetVersionString(filePath, "LegalCopyright");

        public static void SetAuthor(string filePath, string value) =>
            SetVersionString(filePath, "CompanyName", value);

        public static void SetProductName(string filePath, string value) =>
            SetVersionString(filePath, "ProductName", value);

        public static void SetDescription(string filePath, string value) =>
            SetVersionString(filePath, "FileDescription", value);

        public static void SetFileVersion(string filePath, string value) =>
            // This needs to use the dedicated option because otherwise the version is not written correctly
            // https://github.com/Tyrrrz/DotnetRuntimeBootstrapper/issues/2
            CommandLine.RunWithOutput(
                RceditCliFilePath,
                new[] {filePath, "--set-file-version", value}
            );

        public static void SetProductVersion(string filePath, string value) =>
            // This needs to use the dedicated option because otherwise the version is not written correctly
            CommandLine.RunWithOutput(
                RceditCliFilePath,
                new[] {filePath, "--set-product-version", value}
            );

        public static void SetCopyright(string filePath, string value) =>
            SetVersionString(filePath, "LegalCopyright", value);

        public static void SetIcon(string filePath, string iconFilePath) =>
            CommandLine.RunWithOutput(
                RceditCliFilePath,
                new[] {filePath, "--set-icon", iconFilePath}
            );

        public static void SetManifest(string filePath, string manifestFilePath) =>
            CommandLine.RunWithOutput(
                RceditCliFilePath,
                new[] {filePath, "--application-manifest", manifestFilePath}
            );
    }
}