# Joveler.DynLoader

<div style="text-align: left">
    <img src="./Image/Logo.svg" height="128">
</div>

`Joveler.DynLoader` is the cross-platform native dynamic library loader for .Net.

The library provides advanced p/invoke functionality of C functions using [LoadLibrary](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryw) and [libdl](http://man7.org/linux/man-pages/man3/dlopen.3.html). It supports Windows, Linux, and macOS.

| CI Server        | Branch    | Build Status   |
|------------------|-----------|----------------|
| AppVeyor         | Master    | [![AppVeyor CI Master Branch Build Status](https://ci.appveyor.com/api/projects/status/69h8nrpyqx875bcm/branch/master?svg=true)](https://ci.appveyor.com/project/ied206/joveler-dynloader/branch/master) |
|                  | Develop   | [![AppVeyor CI Develop Branch Build Status](https://ci.appveyor.com/api/projects/status/69h8nrpyqx875bcm/branch/develop?svg=true)](https://ci.appveyor.com/project/ied206/joveler-dynloader/branch/develop) |
| Azure Pipelines | Master    | [![Azure Pipelines CI Master Branch Build Status](https://dev.azure.com/ied206/Joveler.DynLoader/_apis/build/status/ied206.Joveler.DynLoader?branchName=master)](https://dev.azure.com/ied206/Joveler.DynLoader/_build) |
|                  | Develop   | [![Azure Pipelines CI Develop Branch Build Status](https://dev.azure.com/ied206/Joveler.DynLoader/_apis/build/status/ied206.Joveler.DynLoader?branchName=develop)](https://dev.azure.com/ied206/Joveler.DynLoader/_build) |

## Install

`Joveler.DynLoader` can be obtained via [nuget](https://www.nuget.org/packages/Joveler.DynLoader).

[![NuGet](https://buildstats.info/nuget/Joveler.DynLoader)](https://www.nuget.org/packages/Joveler.DynLoader)

## Features

- `DynLoaderBase`, the cross-platform abstract class designed to wrap native library easily.
- `LoadManagerBase`, the abstract class helps developers to manage `DynLoaderBase` instance in a thread-safe way.
- Platform convention helper properties and methods.

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

### Supported OS platforms

| Platform | Implementation | Tested            |
|----------|----------------|-------------------|
| Windows  | LoadLibrary    | x86, x64          |
| Linux    | libdl          | x64, armhf, arm64 |
| macOS    | libdl          | x64               |

## Usage

See [USAGE.md](./USAGE.md).

## License

Most of the code is licensed under MIT license. Some test code is released as a public domain, to promote easy development.

The logo is licensed under Apache 2.0 License.  
[Memory icon](https://material.io/resources/icons/?icon=memory&style=baseline) from the Material Icons.
