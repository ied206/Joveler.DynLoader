# Usage

`Joveler.DynLoader` is a cross-platform native dynamic library loader for .NET. It allows developers to create a wrapper of native C libraries easily.

The library provides two abstract class, [DynLoaderBase](#DynLoaderBase) and [LoadManagerBase](#LoadManagerBase).

Please also read [P/Invoke Tips from DynLoader](#Tips) for easy P/Invoke life.

## Getting Started

To use DynLoader, you should learn about two main classes.

| Class        | Description |
|--------------|-------------|
| [DynLoaderBase](#DynLoaderBase) | Scaffold of a native library wrapper. |
| [LoadManagerBase](#LoadManagerBase) | Manages `DynLoaderBase` singleton instance. |

Follow these steps to create a wrapper of a native library.

1. Implement a child class of `DynLoaderBase`.
    - Translate extern native function prototypes into C\# delegates.
    - Implement abstract methods and properties.
        - Load delegate instance in `LoadFunctions`.
        - Implement `DefaultLibFileName`.
    - Example: [magic.cs](./Joveler.DynLoader.Tests/SimpleFileMagic.cs)
1. Implement a child class of `LoadManagerBase`.
    - Implement abstract methods and properties.
        - Implement `ErrorMsgInitFirst`, `ErrorMsgAlreadyLoaded`, `CreateLoader()`
    ```csharp
    internal class MagicLoadManager : LoadManagerBase<MagicLoader>
    {
        protected override string ErrorMsgInitFirst => "Please call Magic.GlobalInit() first!";
        protected override string ErrorMsgAlreadyLoaded => "Joveler.FileMagician is already initialized.";
        protected override MagicLoader CreateLoader() => new MagicLoader();
    }
    ```

Follow these steps to use a wrapper library.

1. Make an interface that calls `LoadManagerBase.GlobalInit()`.
    ```csharp
    public class Magic : IDisposable
    {
        internal static MagicLoadManager Manager = new MagicLoadManager();
        internal static MagicLoader Lib => Manager.Lib;
        public static void GlobalInit() => Manager.GlobalInit();
        public static void GlobalInit(string libPath) => Manager.GlobalInit(libPath);
        public static void GlobalCleanup() => Manager.GlobalCleanup();
    }
    ```
1. Call `GlobalInit()` to load native functions.
1. Call delegate instances to call corresponding native functions.

## DynLoaderBase

[DynLoaderBase](./Joveler.DynLoader/DynLoaderBase.cs) class provides a scaffold of a native library wrapper.

Inherit [DynLoaderBase](./Joveler.DynLoader/DynLoaderBase.cs) to create a wrapper. You have to declare delegates of native functions and override the `LoadFunctions` method.

**Example Files**

[Joveler.DynLoader.Tests](./Joveler.DynLoader.Tests) contains simplified wrappers of [zlib](https://www.zlib.net) and [libmagic](http://www.darwinsys.com/file/) as examples. Freely adapt them as you need, as they are released as public domain.

- zlib : [SimpleZLib.cs](./Joveler.DynLoader.Tests/SimpleZLib.cs)
- magic : [SimpleFileMagic.cs](./Joveler.DynLoader.Tests/SimpleFileMagic.cs)

The test project also showcases per-platform delegate declarations. Read [SimplePlatform.cs](./Joveler.DynLoader.Tests/SimplePlatform.cs).

### Delegate native functions

You need to provide a prototype of the native functions, similar to traditional [DllImport P/Invoke](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke).

First, translate a prototype of the native function into a managed delegate. The delegate must have [UnmanagedFunctionPointerAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedfunctionpointerattribute). The attribute has similar parameters to [DllImportAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.dllimportattribute).

You should declare a delegate type and a delegate instance as a pair per one native function. The delegate type represents the parameter and returns types, while you can call the function by invoking the delegate instance.

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

### Constructor

You have to declare a parameterless constructor in a derived class.

```csharp
// DynLoaderBase constructor signature
protected DynLoaderBase() { ... }
// -------------------------------------------------------------------
// Parameterless constructor of a derived class
public SimpleFileMagic() : base() { }
```

### LoadLibrary

After creating an instance of a derived class, make sure to call `LoadLibrary()` to load a native library. After that, you can invoke extern native functions via delegate instances.

`LoadLibrary(string libPath)` loads that specific native library. The parameterless version loads the default native library from the base system. 

When it fails to find a native library, [DllNotFoundException](https://docs.microsoft.com/en-US/dotnet/api/system.dllnotfoundexception?view=netcore-3.1) is thrown. 

#### How a native library is loaded

On .NET Core 3.x or later, DynLoader depends on .NET's own [NativeLibrary](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.load?view=netcore-3.1) class to load a native library. On .NET Framework and .NET Standard build, DynLoader calls platform-native APIs to load dynamic libraries at runtime. DynLoader tries its best to ensure consistent behavior regardless of which .NET platform you are using.

Under the hood, DynLoader calls [LoadLibraryEx](https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw) with `LOAD_WITH_ALTERED_SEARCH_PATH` flag on Windows, and [dlopen](http://man7.org/linux/man-pages/man3/dlopen.3.html) with `RTLD_NOW | RTLD_GLOBAL` on POSIX. [NativeLibrary](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.load?view=netcore-3.1) also uses similar tactics.

DynLoader follows the OS's library resolving order. On Windows, it follows [alternative library search order](https://docs.microsoft.com/en-us/windows/win32/dlls/dynamic-link-library-search-order#alternate-search-order-for-desktop-applications). On POSIX, it follows the order explained on [dlopen manual](http://man7.org/linux/man-pages/man3/dlopen.3.html).

### Methods and properties to override

```csharp
/// <summary>
/// Default filename of the native library to use. Override only if the target platform ships with the native library.
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

You must override `LoadFunctions()` with a code loading delegate of native functions. 

Call `GetFuncPtr<T>(string funcSymbol)` with a delegate type (`T`) and function symbol name (`funcSymbol`) to get a C# delegate of a symbol. Assign the return value as a delegate instance you previously declared. 

The parameterless `GetFuncPtr<T>()` is a slow but more convenient variant. It uses reflection (`typeof(T).Name`) to get a real name of `T` at runtime. If your target platform restricts the use of reflection, do not use it.

When `GetFuncPtr<T>` fails to load a native function, [EntryPointNotFoundException](https://docs.microsoft.com/en-US/dotnet/api/system.entrypointnotfoundexception?view=netcore-3.1) is thrown. 

After the library is loaded, invoke the delegate instances to call extern native functions.

**Example**

```csharp
protected override void LoadFunctions()
{
    // Invoke GetFuncPtr<T>(string funcSymbol);
    Adler32 = GetFuncPtr<adler32>(nameof(adler32));
    Crc32 = GetFuncPtr<crc32>(nameof(crc32));
    // Invoke GetFuncPtr<T>();
    ZLibVersionPtr = GetFuncPtr<zlibVersion>();
}
```

#### ResetFunctions()

Override `ResetFunctions()` when you want to explicitly clear native resources and delegate assignments.

Usually, the override of this method is not required, as the library handle is automatically cleared when the instance is disposed of. But if you need to clear delegate assignments manually, you have to implement them.

#### DefaultLibFileName

Override `DefaultLibFileName` only if the target platform ships with the native library. If the property is overridden, the constructor will try to load the library with the default filename when the `libPath` parameter is null. If not, the constructor will throw `ArgumentNullException`.

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

### Platform Conventions

Native function signatures are changed by platform differences, such as OS and architecture. Sometimes you have to maintain two or more signature sets to accommodate this difference. To make your life easy, `DynLoaderBase` provides helper properties and methods.

Please refer to [Tips](#Tips) section for more background.

```csharp
/// <summary>
/// The data model of the platform.
/// </summary>
public enum PlatformDataModel
{
    /// <summary>
    /// The data model of 64bit POSIX.
    /// <para>In C, int = 32bit, long = 64bit, pointer = 64bit.</para>
    /// </summary>
    LP64 = 0,
    /// <summary>
    /// The data model of 64bit Windows.
    /// <para>In C, int = 32bit, long = 32bit, long long = 64bit, pointer = 64bit.</para>
    /// </summary>
    LLP64 = 1,
    /// <summary>
    /// The data model of 32bit Windows and 32bit POSIX.
    /// <para>In C, int = 32bit, long = 32bit, pointer = 32bit.</para>
    /// </summary>
    ILP32 = 2,
}
/// <summary>
/// Size of the long type of platform.
/// </summary>
public enum PlatformLongSize
{
    /// <summary>
    /// In C, long is 64bit.
    /// <para>The size of the long in 64bit POSIX (LP64).</para>
    /// </summary>
    Long64 = 0,
    /// <summary>
    /// In C, long is 32bit.
    /// <para>The size of the long in 32bit Windows (ILP32) and POSIX (LLP64).</para>
    /// </summary>
    Long32 = 1,
}
/// <summary>
/// The bitness of the Platform. Equal to the size of address space and size_t.
/// </summary>
public enum PlatformBitness
{
    /// <summary>
    /// Platform is 32bit.
    /// </summary>
    Bit32 = 0,
    /// <summary>
    /// Platform is 64bit.
    /// </summary>
    Bit64 = 1,
}
/// <summary>
/// Default unicode encoding convention of the platform. 
/// </summary>
/// <remarks>
/// Some native libraries do not follow the default Unicode encoding convention of the platform, be careful.
/// </remarks>
public enum UnicodeConvention
{
    /// <summary>
    /// Default unicode encoding of POSIX.
    Utf8 = 0,
    /// <summary>
    /// Default non-unicode encoding of Windows.
    /// </summary>
    Ansi = 0,
    /// <summary>
    /// Default unicode encoding of Windows.
    /// </summary>
    Utf16 = 1,
}

public PlatformDataModel PlatformDataModel { get; }
public PlatformLongSize PlatformLongSize { get; }
public PlatformUnicodeConvention PlatformUnicodeConvention { get; }
public Encoding PlatformUnicodeEncoding { get; }

/// <summary>
/// Convert buffer pointer to string following the platform's default encoding convention. The wrapper of Marshal.PtrToString*().
/// </summary>
/// <remarks>
/// Marshal.PtrToStringAnsi() use UTF-8 on POSIX.
/// </remarks>
/// <param name="ptr">Buffer pointer to convert to string</param>
/// <returns>Converted string.</returns>
public string PtrToStringAuto(IntPtr ptr);
/// <summary>
/// <summary>
/// Convert string to buffer pointer following the platform's default encoding convention. The wrapper of Marshal.StringToHGlobal*().
/// </summary>
/// <remarks>
/// Marshal.StringToHGlobalAnsi() use UTF-8 on POSIX.
/// </remarks>
/// <param name="str">String to convert</param>
/// <returns>IntPtr of the string buffer. You must call Marshal.FreeHGlobal() with return value to prevent memory leak.</returns>
public IntPtr StringToHGlobalAuto(string str);
/// Convert string to buffer pointer following the platform's default encoding convention. The wrapper of Marshal.StringToCoTaskMem*().
/// </summary>
/// <remarks>
/// Marshal.StringToCoTaskMemAnsi() uses UTF-8 on POSIX.
/// </remarks>
/// <param name="str">String to convert</param>
/// <returns>IntPtr of the string buffer. You must call Marshal.FreeCoTaskMem() with return value to prevent memory leak.</returns>
public IntPtr StringToCoTaskMemAuto(string str);
```

#### PlaformDataModel, PlatformLongSize

In C language, the size of a data type may change per target platform. It is called a data model. The most notorious problem is the various size of the `long` data type. These enum properties provide such information.

| Property            | Windows 32bit | Windows 64bit | POSIX 32bit | POSIX 64bit | 
|---------------------|---------------|---------------|-------------|-------------|
| `PlatformDataModel` | `ILP32`       | `LLP64`       | `ILP32`     | `LP64`      |
| `PlatformLongSize`  | `Long32`      | `Long32`      | `Long32`    | `Long64`    |

#### PlatformBitness

`PlatformBitness` represents the bitness of the platform, which is equal to the size of the address space and `size_t`.

| Property              | 32bit   | 64bit   |
|-----------------------|---------|---------|
| `PlatformBitness`     | `Bit32` | `Bit64` |
| Size of the `UIntPtr` | 32bit   | 64bit   |

It is useful when have to write different code per bitness or handle marshaling of `size_t`.

`size_t` can be represented as `UIntPtr` in P/Invoke signatures. .NET makes sure that `UIntPtr` does not store the value larger than the platform's bit size. For example, assigning `ulong.MaxValue` to `UIntPtr` on 32bit platforms invoke `OverflowException`.

#### PlatformUnicodeConvention, PlatformUnicodeEncoding

Windows often use UTF-16 LE, while many POSIX libraries use UTF-8 without BOM. 

| Property            | Windows | POSIX  |
|---------------------|---------|--------|
| `UnicodeConvention` | `Utf16` | `Utf8` |
| `UnicodeEncoding`   | `Encoding.UTF16` (UTF-16 LE) | `new UTF8Encoding(false)` (UTF-8 without BOM) |

`string PtrToStringAuto(IntPtr ptr)`, `IntPtr StringToHGlobalAuto(string str)` and `IntPtr StringToCoTaskMemAuto(string str)` is a wrapper methods of `Marshal.PtrToString*` and  `Marshal.StringTo*`. They decide which encoding to use automatically depending on the value of the `UnicodeConvention` property.

**WARNING**: Native libraries may not follow the platform's default Unicode encoding convention! It is your responsibility to check which encoding library is used. For example, some cross-platform libraries which originated from the POSIX world do not use `wchar_t`, effectively using `ANSI` encoding on Windows instead of `UTF-16`. That is why you can overwrite the `UnicodeConvention` value after the class was initialized.

### Disposable Pattern

The class implements [Disposable Pattern](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose), but you do not need to implement the pattern yourself. The class had already implemented it for you.

## LoadManagerBase

[LoadManagerBase](./Joveler.DynLoader/LoadManagerBase.cs) class provides a thread-safe way to manage the `DynLoaderBase` singleton instance.

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
/// "Please init the library first" error message
/// </summary>
protected abstract string ErrorMsgInitFirst { get; }
/// <summary>
/// "The library is already loaded" error message
/// </summary>
protected abstract string ErrorMsgAlreadyLoaded { get; }
```

#### CreateLoader()

The `CreateLoader()` method creates instances of `DynLoaderBase`. You should implement `CreateLoader()` like this example.

**Example**

```csharp
protected override SimpleZLib CreateLoader()
{
    return new SimpleZLib();
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
/// Allocate other external resources before CreateLoader gets called.
/// </summary>
/// <remarks>
/// Called in GlobalInit() and GlobalInit(string libPath).
/// </remarks>
protected virtual void PreInitHook() { }
/// <summary>
/// Allocate other external resources after CreateLoader gets called.
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
/// Disallocate other external resources after disposing of DynLoaderBase instance.
/// </summary>
/// <remarks>
/// Called in GlobalCleanup().
/// </remarks>
protected virtual void PostDisposeHook() { }
```

## Tips

### Bundling native libraries

When your app depends on user libraries, not a Win32 API, or a system call, you have to bundle the libraries yourself.

#### 1) Set the `Copy to Output Directory` property of native binaries.

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
  <None Update="arm64\7z.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

This method is recommended for application projects, as it is the simplest. However, it does not work on NuGet packages, so other methods should be used.

#### 2) Standard NuGet package layout

If you want to bundle native libraries within a .NET Core NuGet package, place files following the standard NuGet package layout.

```
Place native libraries like this on .nupkg file:

- runtimes\win-x86\native\zlibwapi.dll
- runtimes\win-x64\native\zlibwapi.dll
- runtimes\win-arm64\native\zlibwapi.dll
- runtimes\linux-x64\native\libz.so
- runtimes\linux-arm\native\libz.so
- runtimes\linux-arm64\native\libz.so
- runtimes\osx-x64\native\libz.dylib
```

For more info, read [NuGet package layout](https://docs.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks) document.

This method does not work on application projects.

#### 3) Use MSBuild scripts

For the .NET Framework NuGet package, write an MSBuild script to handle native libraries. 

**Example**: Add MSBuild script [SampleScript.netfx.targets](./Joveler.DynLoader.Tests/SampleScript.netfx.targets) to the project directory. Also, add this line to .csproj:
```xml
<Import Project="$(MSBuildProjectDirectory)\SampleScript.netfx.targets" />
```

You can freely adapt [SampleScript.netfx.targets](./Joveler.DynLoader.Tests/SampleScript.netfx.targets) from the test code for your need. They are released in public domain, based on work of [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core/).

#### Example

This is the snippet extracted from the sample .csproj file.

- (1) Use `Copy to Output Directory` for application build.
- (2) Create a standard NuGet package layout for .NET Core nupkg.
- (3) Use MSBuild scripts for .NET Framework nupkg.

```xml
<!-- (Method 1) Native Library for .NET Framework 4.6 -->
<ItemGroup Condition=" '$(TargetFramework)' == 'net46' ">
    <None Include="runtimes\win-x86\native\*.dll">
        <Link>x86\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\win-x64\native\*.dll">
        <Link>x64\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\win-arm64\native\*.dll">
        <Link>arm64\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
<!-- (Method 1) Native Library for .NET Standard 2.0 & 2.1 -->
<ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0' or '$(TargetFramework)' == 'netstandard2.1'">
    <None Include="runtimes\win-x86\native\*.dll">
        <Link>runtimes\win-x86\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\win-x64\native\*.dll">
        <Link>runtimes\win-x64\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\win-arm64\native\*.dll">
        <Link>runtimes\win-arm64\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\linux-x64\native\*.so">
        <Link>runtimes\linux-x64\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\linux-arm\native\*.so">
        <Link>runtimes\linux-arm\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\linux-arm64\native\*.so">
        <Link>runtimes\linux-arm64\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\osx-x64\native\*.dylib">
        <Link>runtimes\osx-x64\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="runtimes\osx-arm64\native\*.dylib">
        <Link>runtimes\osx-arm64\native\%(FileName)%(Extension)</Link> <!-- Project Reference -->
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
<!-- NuGet Pacakge -->
<ItemGroup>
    <!-- (Method 2) Create standard NuGet package layout -->
    <None Include="runtimes\win-x86\native\*.dll"    Pack="true" PackagePath="runtimes\win-x86\native"/>
    <None Include="runtimes\win-x64\native\*.dll"    Pack="true" PackagePath="runtimes\win-x64\native"/>
    <None Include="runtimes\win-arm64\native\*.dll"  Pack="true" PackagePath="runtimes\win-arm64\native"/>
    <None Include="runtimes\linux-x64\native\*.so"   Pack="true" PackagePath="runtimes\linux-x64\native"/>
    <None Include="runtimes\linux-arm\native\*.so"   Pack="true" PackagePath="runtimes\linux-arm\native"/>
    <None Include="runtimes\linux-arm64\native\*.so" Pack="true" PackagePath="runtimes\linux-arm64\native"/>
    <None Include="runtimes\osx-x64\native\*.dylib"  Pack="true" PackagePath="runtimes\osx-x64\native"/>
    <None Include="runtimes\osx-arm64\native\*.dylib"  Pack="true" PackagePath="runtimes\osx-arm64\native"/>
    <!-- (Method 3) Build Script for .NET Framework -->
    <None Include="Joveler.FileMagician.netfx.targets" Pack="true" PackagePath="build\net451\Joveler.FileMagician.targets"/>
</ItemGroup>
```

### Calling conventions

Multiple calling conventions are used following the target OS and architecture.

**Recommended Workaround**: Always set calling a convention for `x86`, as they are ignored in the other architectures.

#### x86/i686

On x86, you need to be cautious of calling conventions. 

- Windows: Win32 APIs use stdcall, while the user libraries selectively use cdecl or stdcall.
- Linux, macOS: Every function uses cdecl.

Many libraries originating from the POSIX world often exclusively use cdecl. It is still valid on Windows when the library is cross-platform. In that case, specify `CallingConvention.Cdecl`.

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
```

Similarly, if you are writing a wrapper of Win32 APIs on Windows, specify `CallingConvention.StdCall`.

```csharp
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
```

Some cross-platform libraries use stdcall on Windows and cdecl on POSIX (e.g., `zlibwapi.dll` build of zlib), however. In that case, specify `CallingConvention.Winapi`. stdcall is automatically used on Windows while the cdecl is used on POSIX.

```csharp
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
```

#### x64/amd64

On x64, every platform enforces using the standardized fastcall convention. So, in theory, you do not need to care about it.

- Windows: Microsoft x64 calling convention
- POSIX: System V AMD64 ABI

I still recommend specifying calling conventions (cdecl, stdcall, winapi) for x86 compatibility, however.

#### armhf and arm64

Similar to x64, these platforms are known to enforce one standardized calling convention. 

### Pointer size (`size_t`)

**Recommended Workaround until .NET 6**: Use `UIntPtr` in the P/Invoke signature while using `ulong` in the .NET world. 

`size_t` has a different size per architecture. It has the same size as the pointer size, using 4B on 32bit arch (x86, armhf) and using 8B on 64bit arch (x64, arm64). It is troublesome in cross-platform P/Invoke, as no direct counterpart exists in .NET.

You can exploit [UIntPtr](https://docs.microsoft.com/en-US/dotnet/api/system.uintptr) (or [IntPtr](https://docs.microsoft.com/en-US/dotnet/api/system.intptr)) struct to handle this problem. While the .NET runtime does not provide the direct mechanism, this struct has the same size as the platform's pointer size. Thus, we can safely use `UIntPtr` as the C# equivalent of `size_t`. You must have to take caution, though, because we want to use `UIntPtr` as a value, not an address.

I recommend using `UIntPtr` instead of `IntPtr` to represent `size_t` for safety. `IntPtr` is often used as a pure pointer itself while the `UIntPtr` is rarely used. Distinguishing `UIntPtr (value)` from the `IntPtr (address)` prevents the mistakes and crashes from confusing these two.

**Example**: [Joveler.Compression.LZ4](https://github.com/ied206/Joveler.Compression/tree/master/Joveler.Compression.LZ4) use this trick.

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate UIntPtr LZ4F_getFrameInfo(
    IntPtr dctx,
    FrameInfo frameInfoPtr,
    IntPtr srcCapacity,
    UIntPtr srcSizePtr); // size_t
internal static LZ4F_getFrameInfo GetFrameInfo;
```

**Recommended Workaround since .NET 5**: Use `nuint` in both P/Invoke signature and .NET world.

C\# 9.0 or later supports [`nint` and `nuint`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nint-nuint), which are sized after platform native integer size. Internally they are represented with `IntPtr` and `UIntPtr`. 

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate nuint LZ4F_getFrameInfo(
    IntPtr dctx,
    FrameInfo frameInfoPtr,
    IntPtr srcCapacity,
    nuint srcSizePtr);
internal static LZ4F_getFrameInfo GetFrameInfo;
```

### Data model and `long` size

**Recommended Workaround**: If the native library use `long` in its APIs, declare two sets of delegates, the LP64 model for POSIX 64bit and LLP64 for the other.

In 64bit, `long` can have different sizes per target OS and architecture. Windows uses the LLP64 data model (long is 32bit) on 64bit arch, while the POSIX use LP64 (long is 64bit).

If a native library uses `long` in the exported functions, there is no simple solution. You would have to prepare two sets of delegates and make sure you assign and call the right delegate per target architecture and OS.

Some libraries with a long history (e.g., zlib) have this problem. Fortunately, many modern cross-platform libraries tend to use types of `<stdint.h>` or similar so that they can ensure stable type size across platforms. 

**Example**: [Joveler.Compression.ZLib](https://www.nuget.org/packages/Joveler.Compression.ZLib) applied this workaround.

### String encoding

**Recommended Workaround**: Use the `IntPtr` type, and convert it to/from a string in runtime with helper methods.

Different platforms have different charset and encoding conventions, and native libraries often follow them.

- Windows: `UTF-16`, `ANSI`
- POSIX: `UTF-8`

Look for which data type the library used for strings.

- `char*`: `ANSI` on Windows and `UTF-8` on POSIX. Mostly used in POSIX libraries.
- `wchar_t*`: `UTF-16` on Windows and `UTF-32` on POSIX. Windows libraries use it but rarely in POSIX libraries.
- `tchar*`: `UTF-16` on Windows and `UTF-8` on POSIX. Windows libraries and some cross-platform POSIX libraries use it.

Fortunately, you do not need to duplicate structs in most cases. Put `IntPtr` in place of a string field, then return string as a property using `DynLoaderBase.StringTo*Auto()` and `DynLoaderBase.PtrToStringAuto()` helper methods.

**Example**

This example shows two solutions:

- Declaring two sets of delegates
- Use `IntPtr` and convert them in runtime.

```csharp
internal class Utf8d
{
    internal const UnmanagedType StrType = UnmanagedType.LPStr;
    internal const CharSet StructCharSet = CharSet.Ansi;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ErrorCode wimlib_set_error_file_by_name(
        [MarshalAs(StrType)] string path);
    internal wimlib_set_error_file_by_name SetErrorFile;
    #endregion
}

internal class Utf16d
{
    internal const UnmanagedType StrType = UnmanagedType.LPWStr;
    internal const CharSet StructCharSet = CharSet.Unicode;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ErrorCode wimlib_set_error_file_by_name([MarshalAs(StrType)] string path);
    internal wimlib_set_error_file_by_name SetErrorFile;
    #endregion
}

[StructLayout(LayoutKind.Sequential)]
internal struct DirEntryBase
{
    /// <summary>
    /// Name of the file, or null if this file is unnamed. Only the root directory of an image will be unnamed.
    /// </summary>
    public string FileName => Wim.Lib.PtrToStringAuto(_fileNamePtr);
    private IntPtr _fileNamePtr;
}

[StructLayout(LayoutKind.Sequential, CharSet = StructCharSet)]
public struct CaptureSourceBaseL64
{
    /// <summary>
    /// Absolute or relative path to a file or directory on the external filesystem to be included in the image.
    /// </summary>
    public string FsSourcePath;
    /// <summary>
    /// Destination path in the image.
    /// To specify the root directory of the image, use @"\". 
    /// </summary>
    public string WimTargetPath;
};
```
