# ChangeLog

## v2.x

### v2.0.0

Released in 2020.04.24.

- Use `NativeLoader` on .NET Core 3.x build.
- `DynLoaderBase` now throws [DllNotFoundException](https://docs.microsoft.com/en-US/dotnet/api/system.dllnotfoundexception) and [EntryPointNotFoundException](https://docs.microsoft.com/en-US/dotnet/api/system.entrypointnotfoundexception) instead of [ArgumentException](https://docs.microsoft.com/en-US/dotnet/api/system.argumentexception) and [InvalidOperationException](https://docs.microsoft.com/en-us/dotnet/api/system.invalidoperationexception). 
    - The change allows consistent exception throwing and handling between a variety of .NET platforms.
    - Also, the new behavior is uniform with the way how .NET throws an exception on `DllImport`.
- Better and safer recursive library loading.
    - On Windows, [LoadLibrary](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryw) with [SetDllDirectory](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setdlldirectoryw) was substituted by [LoadLibraryEx](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw) with `LOAD_WITH_ALTERED_SEARCH_PATH` flag.
    - On POSIX, unnecessary `LD_LIBRARY_PATH` and `DYLD_LIBRARY_PATH` manipulation was removed.
- Add a simpler version of `GetFuncPtr<T>`.
- Remove unnecessary redundant `size_t` helper methods.

## v1.x

### v1.3.0

Released in 2020.02.29.

- Add `size_t` helper methods.
- Rename AutoStringToCoTaskMem() into StringToCoTaskMemAuto().

### v1.2.1

Released in 2019.10.31.

- Address `libdl.so` naming issue for CentOS ([#1](https://github.com/ied206/Joveler.DynLoader/issues/1))

### v1.2.0

Released in 2019.10.16.

- Add platform convention helper properties and methods

### v1.1.0

Released in 2019.10.15.

- Add `LoadManagerBase` abstract class

### v1.0.0

Released in 2019.10.15.

- The initial release of the cross-platform native dynamic library loader for .NET.
