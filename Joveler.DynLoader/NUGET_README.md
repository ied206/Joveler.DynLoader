# Joveler.DynLoader

`Joveler.DynLoader` is the cross-platform native dynamic library loader for .NET.

The library provides advanced p/invoke functionality of C functions using [NativeLibrary](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.load?view=netcore-3.1), [LoadLibraryEx](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw) and [libdl](http://man7.org/linux/man-pages/man3/dlopen.3.html). It supports Windows, Linux, and macOS.

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

### Supported OS platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86          | Yes    |
|          | x64          | Yes    |
|          | arm64        | Yes    |
| Linux    | x64          | Yes    |
|          | armhf        | Yes    |
|          | arm64        | Yes    |
| macOS    | x64          | Yes    |
|          | arm64        | Yes    |
