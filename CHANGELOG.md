# ChangeLog

## v2.x

### v2.3.1

Released on 2023-01.29.

- Add nullability attributes.

### v2.3.0

Released on 2023-09-03.

- Add `LoadManagerBase<T>.TryGlobalCleanup()`.
    - It is useful when you want to ensure that there is no loaded native library in the manager.

### v2.2.1

Released on 2023-08-28.

- Fix failing the second call of `LoadManagerBase<T>.GlobalInit()` if the first call throws an exception.

### v2.2.0

Released on 2023-08-08.

- Allow passing a custom object when loading a native library.
    - Add `LoadManagerBase.GlobalInit()` overloadings with custom object parameter.
    - Add `DynLoaderBase.LoadLibrary()` overloadings with custom object parameter.
    - Add virtual method `DynLoaderBase.HandleLoadData()`.
    - Add helper method `DynLoaderBase.HasFuncSymbol()`.

### v2.1.1

Released on 2022-02-15.

- Official support for ARM64 macOS.
- Unify .NET Framework 4.5.1 codebase and .NET Standard 2.0 codebase.

### v2.1.0

Released on 2021-04-05.

- Avoid calling virtual methods from constructors in `DynLoaderBase`.
    - Users must call `DynLoaderBase.LoadLibrary` after creating an instance.
    - Constructor with a library path is now obsolete. Pass a path into `LoadLibrary` instead.
    - It breaks the ABI compatibility of `DynLoaderBase`. However, `LoadManagerBase` was also patched to accommodate these changes.
    - If you used the `LoadManagerBase` interface, you can safely update Joveler.DynLoader without any code change.

### v2.0.0

Released on 2020-04-24.

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

Released on 2020-02-29.

- Add `size_t` helper methods.
- Rename AutoStringToCoTaskMem() into StringToCoTaskMemAuto().

### v1.2.1

Released on 2019-10-31.

- Address `libdl.so` naming issue for CentOS ([#1](https://github.com/ied206/Joveler.DynLoader/issues/1))

### v1.2.0

Released on 2019-10-16.

- Add platform convention helper properties and methods

### v1.1.0

Released on 2019-10-15.

- Add `LoadManagerBase` abstract class

### v1.0.0

Released on 2019-10-15.

- The initial release of the cross-platform native dynamic library loader for .NET.
