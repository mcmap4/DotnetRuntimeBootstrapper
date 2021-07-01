# DotnetRuntimeBootstrapper

[![Build](https://github.com/Tyrrrz/DotnetRuntimeBootstrapper/workflows/CI/badge.svg?branch=master)](https://github.com/Tyrrrz/DotnetRuntimeBootstrapper/actions)
[![Version](https://img.shields.io/nuget/v/DotnetRuntimeBootstrapper.svg)](https://nuget.org/packages/DotnetRuntimeBootstrapper)
[![Downloads](https://img.shields.io/nuget/dt/DotnetRuntimeBootstrapper.svg)](https://nuget.org/packages/DotnetRuntimeBootstrapper)
[![Donate](https://img.shields.io/badge/donate-$$$-purple.svg)](https://tyrrrz.me/donate)

✅ **Project status: active**.

DotnetRuntimeBootstrapper replaces the default application host `exe` file generated by MSBuild for Windows executables with a fully featured bootstrapper that can download and install .NET runtime and other missing components required by your application.

> ⚠️ Currently, bootstrapper's user experience is optimized for desktop applications.
Using it with projects of other types (i.e. console or web) is possible but not recommended.

## Download

📦 [NuGet](https://nuget.org/packages/DotnetRuntimeBootstrapper): `dotnet add package DotnetRuntimeBootstrapper`

## Features

- Acts as a tight wrapper around the target assembly
- Single executable for all CPU architectures
- Installs .NET runtime version required by the application
- Installs Visual C++ redistributable binaries, if missing
- Installs required Windows updates, if missing
- Includes GUI to guide the user through the installation process
- Works out-of-the-box on Windows 7 and higher

## Video

https://user-images.githubusercontent.com/1935960/123711355-346ed380-d825-11eb-982f-6272a9e55ebd.mp4

## Usage

### Build integration

To add DotnetRuntimeBootstrapper to your project, simply install the corresponding [NuGet package](https://nuget.org/packages/DotnetRuntimeBootstrapper).
MSBuild will automatically pick up the `props` and `targets` files provided by the package and integrate them inside the build process.

Once that's done, building or publishing the project should produce two additional files in the output directory:

```ini
MyApp.exe                 <-- bootstrapper executable
MyApp.exe.config          <-- runtime config required by the executable
MyApp.dll
MyApp.pdb
MyApp.deps.json
MyApp.runtimeconfig.json
```

> ⚠️ Make sure to include the `.config` file when distributing your application.
Bootstrapper executable may not be able to run without it.

During build, DotnetRuntimeBootstrapper relies on the following common project-level properties for configuration:

- `<TargetFramework>` -- used to determine the version of .NET runtime checked by the bootstrapper
- `<AssemblyTitle>` -- used as a display name of the application shown by the bootstrapper
- `<ApplicationIcon>` -- used to locate the icon resource to inject it inside the bootstrapper
- `<ApplicationManifest>` -- used to locate the manifest resource to inject it inside the bootstrapper

### How it works

Bootstrapper executable is a pre-compiled binary built against legacy .NET Framework v3.5, which allows it to run out-of-the-box on all operating systems starting with Windows 7.
It's deployed during build by a custom MSBuild task in a series of steps:

1. Copy bootstrapper executable (`MyApp.exe`) to the output directory
2. Copy config file (`MyApp.exe.config`) to the output directory
3. Inject execution parameters (containing target file name, required runtime version, etc.) as an embedded resource inside the executable
4. Inject file metadata (`ProductName`, `FileDescription`, `FileVersion`, `ProductVersion`, `LegalCopyright`, etc.) inside the executable
5. Inject application icon and manifest resource files inside the executable

When the end-user runs `MyApp.exe`, the bootstrapper executable will first ensure that the required version of .NET runtime is available and then delegate execution to the target assembly.
In case the runtime or any of its prerequisites are missing, the user is prompted with an option to download and install them automatically.
