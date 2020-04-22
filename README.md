# Joveler.DynLoader

<div style="text-align: left">
    <img src="./Image/Logo.svg" height="128">
</div>

`Joveler.DynLoader` is the cross-platform native dynamic library loader for .NET.

The library provides advanced p/invoke functionality of C functions using [NativeLibrary](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.load?view=netcore-3.1), [LoadLibraryEx](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw) and [libdl](http://man7.org/linux/man-pages/man3/dlopen.3.html). It supports Windows, Linux, and macOS.

| CI Server       | Branch    | Build Status   |
|-----------------|-----------|----------------|
| AppVeyor        | Master    | [![AppVeyor CI Master Branch Build Status](https://ci.appveyor.com/api/projects/status/69h8nrpyqx875bcm/branch/master?svg=true)](https://ci.appveyor.com/project/ied206/joveler-dynloader/branch/master) |
|                 | Develop   | [![AppVeyor CI Develop Branch Build Status](https://ci.appveyor.com/api/projects/status/69h8nrpyqx875bcm/branch/develop?svg=true)](https://ci.appveyor.com/project/ied206/joveler-dynloader/branch/develop) |
| Azure Pipelines | Master    | [![Azure Pipelines CI Master Branch Build Status](https://dev.azure.com/ied206/Joveler.DynLoader/_apis/build/status/ied206.Joveler.DynLoader?branchName=master)](https://dev.azure.com/ied206/Joveler.DynLoader/_build) |
|                 | Develop   | [![Azure Pipelines CI Develop Branch Build Status](https://dev.azure.com/ied206/Joveler.DynLoader/_apis/build/status/ied206.Joveler.DynLoader?branchName=develop)](https://dev.azure.com/ied206/Joveler.DynLoader/_build) |

## Install

`Joveler.DynLoader` can be obtained via [nuget](https://www.nuget.org/packages/Joveler.DynLoader).

[![NuGet](https://buildstats.info/nuget/Joveler.DynLoader)](https://www.nuget.org/packages/Joveler.DynLoader)

## Features

- `DynLoaderBase`, the cross-platform abstract class designed to wrap native library easily.
- `LoadManagerBase`, the abstract class helps developers to manage `DynLoaderBase` instance in a thread-safe way.
- Platform convention helper properties and methods.

## Support

### Targeted .NET platforms

- .NET Core 3.1
    - Depends on .NET's [NativeLibrary](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary).
- .NET Standard 2.0 (.NET Framework 4.6.1+, .NET Core 2.0+)
- .NET Framework 4.5.1
    - Depends on [LoadLibraryEx](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw) and [libdl](http://man7.org/linux/man-pages/man3/dlopen.3.html) native API.

### Tested OS platforms

| Platform | Implementation               | Tested            |
|----------|------------------------------|-------------------|
| Windows  | NativeLibrary, LoadLibraryEx | x86, x64          |
| Linux    | NativeLibrary, libdl         | x64, armhf, arm64 |
| macOS    | NativeLibrary, libdl         | x64               |

## Usage

See [USAGE.md](./USAGE.md).

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## License

Most of the code is licensed under MIT license. Some test code is released as a public domain, to promote easy development.

The logo is [Memory icon](https://material.io/resources/icons/?icon=memory&style=baseline) from the Material Icons, under the Apache 2.0 License.  
