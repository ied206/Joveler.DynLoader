# Usage

`Joveler.DynLoader` is a cross-platform native dynamic library loader for .Net. It allows developers to create a wrapper of native C libraries easily.

The library provide two abstract class, [DynLoaderBase](#DynLoaderBase) and [LoadManagerBase](#LoadManagerBase).

Please also read [P/Invoke Tips from DynLoader](#Tips) for your easy P/Invoke life.

## DynLoaderBase

[DynLoaderBase](./Joveler.DynLoader/DynLoaderBase.cs) class provides a scaffold of a native library wrapper.

Inherit [DynLoaderBase](./Joveler.DynLoader/DynLoaderBase.cs) to create a wrapper. You have to declare delegates of native functions and override the `LoadFunctions` method.

**Example Files**

[Joveler.DynLoader.Tests](./Joveler.DynLoader.Tests) contains simplified wrappers of [zlib](https://www.zlib.net) and [libmagic](http://www.darwinsys.com/file/) as examples. You can freely adapt them as you need, they are released as public domain.

- zlib : [SimpleZLib.cs](./Joveler.DynLoader.Tests/SimpleZLib.cs)
- magic : [SimpleFileMagic.cs](./Joveler.DynLoader.Tests/SimpleFileMagic.cs)

### Delegate of native functions

You need to provide a prototype of the native functions, similar to traditional [DllImport P/Invoke](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke).

First, translate a prototype of native function into a managed delegate. The delegate must be annotated with [UnmanagedFunctionPointerAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedfunctionpointerattribute). The attribute has similar parameters to [DllImportAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.dllimportattribute).

You should declare a delegate type and a delegate instance as a pair per one native function. The delegate type represents the parameter and return types, while you can call the function by invoking the delegate instance.

**Example**

```csharp
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public unsafe delegate uint adler32(uint adler, byte* buf, uint len);
public adler32 Adler32;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public unsafe delegate uint crc32(uint crc, byte* buf, uint len);
public crc32 Crc32;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate IntPtr zlibVersion();
private zlibVersion ZLibVersionPtr;
public string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
```

### Methods and property to override

```csharp
/// <summary>
/// Default filename of the native libary to use. Override only if the target platform ships with the native library.
/// </summary>
/// <remarks>
/// Throw PlatformNotSupportedException optionally when the library is included only in some of the target platforms.
/// e.g. zlib is often included in Linux and macOS, but not in Windows.
/// </remarks>
protected abstract string DefaultLibFileName { get; }
/// <summary>
/// Load native functions with a GetFuncPtr. Called in the constructors.
/// </summary>
protected abstract void LoadFunctions();
/// <summary>
/// Clear pointer of native functions. Called in Dispose(bool).
/// </summary>
protected abstract void ResetFunctions();
```

#### LoadFunctions()

You must override `LoadFunctions()` with a code loading delegates of native functions. 

Call `GetFuncPtr<T>(string funcSymbol)` with a delegate type (`T`) and function symbol name (`funcSymbol`) to get a C# delegate of a symbol. Assign return value as a delegate instance you previously declared. 

`LoadFunctions()` is called from the class constructor, so you do not need to call it yourself. You can invoke the delegate instances to call extern native functions after the class instance was created.

**Example**

```csharp
protected override void LoadFunctions()
{
    Adler32 = GetFuncPtr<adler32>(nameof(adler32));
    Crc32 = GetFuncPtr<crc32>(nameof(crc32));
    ZLibVersionPtr = GetFuncPtr<zlibVersion>(nameof(zlibVersion));
}
```

#### ResetFunctions()

Override `ResetFunctions()` when you want to explicitly clear native resources and delegate assignments.

Usually, the override of this method is not required, as the `ResetFunctions` method is called when the instance is disposed of. But if you need to clear native resources as well as delegate assignment, you have to override it.

#### DefaultLibFileName

Override `DefaultLibFileName` only if the target platform ships with the native library. If the property is overridden, the constructor will try to load the library with default filename when the `libPath` parameter is null. If not, the constructor will throw `ArgumentNullException` when the `libPath` parameter is null.

If only some of the target platforms ship with the native library, throw `PlatformNotSupportedException` when they do not. For example, when you write a wrapper of zlib, you can load the system zlib on Linux and macOS, but not in Windows. In that case, return `libz.so` and `libz.dylib` on Linux and macOS, and throw `PlatformNotSupportedException` on Windows.

**Example**

```csharp
protected override string DefaultLibFileName
{
    get
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "libz.so";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libz.dylib";
        throw new PlatformNotSupportedException();
    }
}
```

### Constructors

You also have to implement your own constructors, using protected constructors of `DynLoaderBase`.

```csharp
/// <summary>
/// Load a native dynamic library from a given path.
/// </summary>
/// <param name="libPath">A native library file to load.</param>
protected DynLoaderBase(string libPath)
/// <summary>
/// Load a native dynamic library from a path of `DefaultLibFileName`.
/// </summary>
protected DynLoaderBase() : this(null)
```

The constructor with a path parameter loads that specific native library. The parameter is passed directly to the [LoadLibrary](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryw) and [dlsym](http://man7.org/linux/man-pages/man3/dlsym.3.html). 

The parameterless constructor tries to load the default native library from the base system. The loader will load the default library following the [library search order](https://docs.microsoft.com/en-us/windows/win32/dlls/dynamic-link-library-search-order) of the target platform.

### Disposable Pattern

The class implements [Disposable Pattern](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose). You do not need to implement the pattern yourself. The class had implemented for you.

## LoadManagerBase

[LoadManagerBase](./Joveler.DynLoader/LoadManagerBase.cs) class provides a thread-safe way to manage `DynLoaderBase` singleton instance.

### Methods and properties to override

```csharp
/// <summary>
/// Represents parameter-less constructor of DynLoaderBase.
/// </summary>
/// <remarks>
/// Called in GlobalInit().
/// </remarks>
/// <returns>DynLoaderBase instace</returns>
protected abstract T CreateLoader();
/// <summary>
/// Represents constructor of DynLoaderBase with a `libPath` parameter.
/// </summary>
/// <remarks>
/// Called in GlobalInit(string libPath).
/// </remarks>
/// <returns>DynLoaderBase instace</returns>
protected abstract T CreateLoader(string libPath);
/// <summary>
/// "Please init the library first" error message
/// </summary>
protected abstract string ErrorMsgInitFirst { get; }
/// <summary>
/// "The library is already loaded" error message
/// </summary>
protected abstract string ErrorMsgAlreadyLoaded { get; }
```

#### CreateLoader()

Create instance of `DynLoaderBase` in `CreateLoader()` methods. You should implement two variant of `CreateLoader()`.

**Example**

```csharp
protected override SimpleZLib CreateLoader()
{
    return new SimpleZLib();
}

protected override SimpleZLib CreateLoader(string libPath)
{
    return new SimpleZLib(libPath);
}
```

#### ErrorMsgInitFirst, ErrorMsgAlreadyLoaded

Error messages to show when the error has occurred.

**Example**

```csharp
protected override string ErrorMsgInitFirst => "Please init the zlib first!";
protected override string ErrorMsgAlreadyLoaded => "zlib is already loaded.";
```

#### Hooks

These hooks will be called before/after `CreateLoader()`/`Dispose()`. Implementing them is optional.

```csharp
/// <summary>
/// Allocate other external resources before CreateLoader get called.
/// </summary>
/// <remarks>
/// Called in GlobalInit() and GlobalInit(string libPath).
/// </remarks>
protected virtual void PreInitHook() { }
/// <summary>
/// Allocate other external resources after CreateLoader get called.
/// </summary>
/// <remarks>
/// Called in GlobalInit() and GlobalInit(string libPath).
/// </remarks>
protected virtual void PostInitHook() { }
/// <summary>
/// Disallocate other external resources before disposing DynLoaderBase instance.
/// </summary>
/// <remarks>
/// Called in GlobalCleanup().
/// </remarks>
protected virtual void PreDisposeHook() { }
/// <summary>
/// Disallocate other external resources after disposing DynLoaderBase instance.
/// </summary>
/// <remarks>
/// Called in GlobalCleanup().
/// </remarks>
protected virtual void PostDisposeHook() { }
```

## Tips

### Bundling native libraries

When your app depends on user libraries, not a Win32 API, or a system call, you have to bundle the libraries yourself.

1. Set `Copy to Output Directory` property of native binaries.

Add native library files into the project, and set `Copy to Output Directory` to `Copy if newer` in their property.

**Example**: Add this line to .csproj:
```xml
<ItemGroup>
  <None Update="x64\7z.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="x86\7z.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

This method is recommended for application projects, as it is the simplest.

2. Use MSBuild Scripts

The situations change when you want to bundle native libraries within NuGet packages.

**Example**: Add MSBuild script [SimpleZLib.targets](./Joveler.DynLoader.Tests/SimpleZLib.targets) to the project directory. Also add this line to .csproj:
```xml
<Import Project="$(MSBuildProjectDirectory)\SimpleZLib.targets" />
```

You can freely adapt [SimpleZLib.targets](./Joveler.DynLoader.Tests/SimpleZLib.targets) and [SimpleFileMagic.targets](./Joveler.DynLoader.Tests/SimpleFileMagic.targets) from the test code for your need. They are released in public domain, based on work of [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core/).

This method is recommended for NuGet packages. The first method does not automatically work on NuGet packages, so using the MSBuild script is required.

### Calling conventions

Multiple calling conventions are used following the target OS and architecture.

**Recommended Workaround**: Always set calling a convention for `x86`, as they are ignored in the other architectures.

#### x86

On x86, you need to be cautious of calling conventions. 

- Windows: Win32 APIs use stdcall, while the user libraries selectively use cdecl or stdcall.
- Linux, macOS: Every function uses cdecl.

Many libraries originated from the POSIX world often exclusively use cdecl. It is still valid on Windows when the library is cross-platform. In that case, specify `CallingConvention.Cdecl`.

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
```

Similarly, if you are writing a wrapper of Win32 APIs on Windows, specify `CallingConvention.StdCall`.

```csharp
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
```

Some cross-platform libraries use stdcall on Windows and cdecl on POSIX (e.g., zlib), however. In that case, specify `CallingConvention.Winapi`. stdcall is automatically used on Windows while the cdecl is used on POSIX.

```csharp
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
```

#### x64

On x64, every platform enforces using the standardized fastcall convention. So, in theory, you do not need to care about it.

- Windows: Microsoft x64 calling convention
- Linux, macOS: System V AMD64 ABI. 

I still recommend specifying calling conventions (cdecl, stdcall, winapi) for 32bit compatibility, however.

#### armhf and arm64

Similar to x64, platforms are known to enforce one standardized calling convention. 

### Pointer size (`size_t`)

**Recommended Workaround**: Use `UIntPtr`.

When building a wrapper of a cross-platform library, the size difference of `size_t` per architecture may cause trouble.

`size_t` also has a different size per architecture, similar to the `long` size difference per OS. It has the same size as the pointer size, using 4B on 32bit arch (x86, armhf) and using 8B on 64bit arch (x64, arm64).

You can exploit [UIntPtr](https://docs.microsoft.com/en-US/dotnet/api/system.uintptr) (or [IntPtr](https://docs.microsoft.com/en-US/dotnet/api/system.intptr)) struct to handle this problem. While the .Net runtime does not provide the direct mechanism for it, these struct has the same size as the platform's pointer size. Thus, we can safely use `UIntPtr` as the C# equivalent of `size_t`. You must have to take caution, though, because we want to use `UIntPtr` as a value, not an address.

I recommend to use `UIntPtr` instead of `IntPtr` to represent `size_t` for safety. `IntPtr` is often used as a pure pointer itself while the `UIntPtr` is rarely used. Distinguishing `UIntPtr (value)` from the `IntPtr (address)` prevents the mistakes and crashes from confusing these two.

**Example**: [Joveler.Compression.LZ4](https://github.com/ied206/Joveler.Compression/tree/master/Joveler.Compression.LZ4) applied this workaround.

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate UIntPtr LZ4F_getFrameInfo(
    IntPtr dctx,
    FrameInfo frameInfoPtr,
    IntPtr srcCapacity,
    UIntPtr srcSizePtr); // size_t
internal static LZ4F_getFrameInfo GetFrameInfo;
```

### LLP64 and LP64

**Recommended Workaround**: If the native library use `long` in its APIs, declare two sets of delegates, the LP64 model for POSIX 64bit and LLP64 for the other.

In 64bit, `long` can have different sizes per target OS and architecture. Windows use the LLP64 data model (long is 32bit) on 64bit arch, while the POSIX use LP64 (long is 64bit).

If a native library uses `long` in the exported functions, there is no simple solution. You would have to prepare two sets of delegates, and make sure you assign and call the right delegate per target architecture and OS.

Some libraries with a long history (e.g., zlib) have this problem. Fortunately, many modern cross-platform libraries tend to use types of `<stdint.h>` or similar so that they can ensure stable type size across platforms. 

**Example**: [Joveler.Compression.ZLib](https://www.nuget.org/packages/Joveler.Compression.ZLib) applied this workaround.
