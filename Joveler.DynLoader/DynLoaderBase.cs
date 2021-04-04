/*
    Copyright (C) 2019-2021 Hajin Jang
    Licensed under MIT License.
 
    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
#if !NETCOREAPP3_1
using System.ComponentModel; // For Win32Exception
#endif
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Joveler.DynLoader
{
    public abstract class DynLoaderBase : IDisposable
    {
        #region Constructor
        /// <summary>
        /// Create an instance of DynLoaderBase and set platform conventions.
        /// </summary>
        protected DynLoaderBase()
        { 
            // Set platform conventions.
#if NETFRAMEWORK
            UnicodeConvention = UnicodeConvention.Utf16;
            PlatformLongSize = PlatformLongSize.Long32;
            if (Environment.Is64BitProcess)
            {
                PlatformDataModel = PlatformDataModel.LLP64;
                PlatformBitness = PlatformBitness.Bit64;
            }
            else
            {
                PlatformDataModel = PlatformDataModel.ILP32;
                PlatformBitness = PlatformBitness.Bit32;
            }
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                UnicodeConvention = UnicodeConvention.Utf16;
                PlatformLongSize = PlatformLongSize.Long32;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.Arm:
                    case Architecture.X86:
                        PlatformDataModel = PlatformDataModel.ILP32;
                        break;
                    case Architecture.Arm64:
                    case Architecture.X64:
                    default:
                        PlatformDataModel = PlatformDataModel.LLP64;
                        break;
                }
            }
            else
            {
                UnicodeConvention = UnicodeConvention.Utf8;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.Arm:
                    case Architecture.X86:
                        PlatformLongSize = PlatformLongSize.Long32;
                        PlatformDataModel = PlatformDataModel.ILP32;
                        break;
                    case Architecture.Arm64:
                    case Architecture.X64:
                    default:
                        PlatformLongSize = PlatformLongSize.Long64;
                        PlatformDataModel = PlatformDataModel.LP64;
                        break;
                }
            }

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm:
                case Architecture.X86:
                    PlatformBitness = PlatformBitness.Bit32;
                    break;
                case Architecture.Arm64:
                case Architecture.X64:
                default:
                    PlatformBitness = PlatformBitness.Bit64;
                    break;
            }
#endif
        }

        /// <summary>
        /// Create an instance of DynLoaderBase, and set platform conventions.
        /// </summary>
        [Obsolete("Left as ABI compatibility only, remove its override.")]
        protected DynLoaderBase(string libPath) : this() { }
        #endregion

        #region Disposable Pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Loaded)
            {
                GlobalCleanup();
            }
        }

        private void GlobalCleanup()
        {
            ResetFunctions();

            _hModule.Dispose();
            _hModule = null;
        }
        #endregion

        #region (public) LoadLibrary
        /// <summary>
        /// Load a native dynamic library from a path of `DefaultLibFileName`.
        /// </summary>
        public void LoadLibrary()
        {
            LoadLibrary(null);
        }

        /// <summary>
        /// Load a native dynamic library from a given path.
        /// </summary>
        /// <param name="libPath">A native library file to load.</param>
        public void LoadLibrary(string libPath)
        {
            // Should DynLoaderBase use default library filename?
            if (libPath == null)
            {
                if (DefaultLibFileName == null)
                    throw new ArgumentNullException(nameof(libPath));

                libPath = DefaultLibFileName;
            }

            // Use .NET Core's NativeLibrary when available
#if NETCOREAPP3_1
            // NET's NativeLibrary will throw DllNotFoundException by itself.
            // No need to check _hModule.IsInvalid here.
            _hModule = new NetSafeLibHandle(libPath);
#else
#if !NETFRAMEWORK
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                // https://docs.microsoft.com/en-us/windows/win32/dlls/dynamic-link-library-search-order
                string libDir = Path.GetDirectoryName(libPath);
                if (libDir != null && Directory.Exists(libDir))
                {
                    // Case 1) dllPath is an relative path and contains one or more directory path. Ex) x64\libmagic.dll
                    // Case 2) dllPath is an absolute path. Ex) D:\DynLoader\native.dll
                    string fullDllPath = Path.GetFullPath(libPath);
                    _hModule = new WinSafeLibHandle(fullDllPath);
                }
                else
                { // Case) dllPath does not contain any directory path. Ex) kernel32.dll
                    _hModule = new WinSafeLibHandle(libPath);
                }

                if (_hModule.IsInvalid)
                {
                    // Sample message of .NET Core 3.1's NativeLoader:
                    // Unable to load DLL 'x64\zlibwapi.dll' or one of its dependencies: The specified module could not be found. (0x8007007E).
                    // Unable to load DLL 'ᄒᆞᆫ글ḀḘ韓國Ghost.dll' or one of its dependencies: 지정된 모듈을 찾을 수 없습니다. (0x8007007E)
                    string exceptMsg = $"Unable to load DLL '{libPath}' or one of its dependencies";
                    int errorCode = Marshal.GetLastWin32Error();
                    string errorMsg = NativeMethods.Win32.GetLastErrorMsg(errorCode);
                    if (string.IsNullOrWhiteSpace(errorMsg))
                        throw new DllNotFoundException($"{exceptMsg}: (0x{errorCode:X8})");
                    else
                        throw new DllNotFoundException($"{exceptMsg}: {errorMsg} (0x{errorCode:X8})");
                }
            }
#if !NETFRAMEWORK
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _hModule = new LinuxSafeLibHandle(libPath);
                if (_hModule.IsInvalid)
                {
                    // Sample message of .NET Core 3.1's NativeLoader:
                    // Unable to load shared library 'ᄒᆞᆫ글ḀḘ韓國Ghost.so' or one of its dependencies. In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable: ᄒᆞᆫ글ḀḘ韓國Ghost.so: cannot open shared object file: No such file or directory
                    string exceptMsg = $"Unable to load shared library '{libPath}' or one of its dependencies";
                    string errorMsg = NativeMethods.Linux.DLError();
                    if (string.IsNullOrWhiteSpace(errorMsg))
                        throw new DllNotFoundException($"{exceptMsg}.");
                    else
                        throw new DllNotFoundException($"{exceptMsg}: {errorMsg}");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _hModule = new MacSafeLibHandle(libPath);
                if (_hModule.IsInvalid)
                {
                    // Sample message of .NET Core 3.1's NativeLoader:
                    // Unable to load shared library 'ᄒᆞᆫ글ḀḘ韓國Ghost.dylib' or one of its dependencies. In order to help diagnose loading problems, consider setting the DYLD_PRINT_LIBRARIES environment variable: dlopen(ᄒᆞᆫ글ḀḘ韓國Ghost.dylib, 1): image not found
                    string exceptMsg = $"Unable to load shared library '{libPath}' or one of its dependencies";
                    string errorMsg = NativeMethods.Mac.DLError();
                    if (string.IsNullOrWhiteSpace(errorMsg))
                        throw new DllNotFoundException($"{exceptMsg}.");
                    else
                        throw new DllNotFoundException($"{exceptMsg}: {errorMsg}");
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
#endif
#endif

            // Load functions
            try
            {
                LoadFunctions();
            }
            catch (Exception)
            {
                GlobalCleanup();
                throw;
            }
        }
        #endregion

        #region (private) SafeLibModule
        /// <summary>
        /// Handle of the native library.
        /// </summary>
        private SafeHandle _hModule;
        private bool Loaded => _hModule != null && !_hModule.IsInvalid;
        #endregion

        #region (protected) GetFuncPtr
        /// <summary>
        /// Get a delegate of a native function from a library.
        /// The method will use name of the given delegate T as function symbol.
        /// </summary>
        /// <typeparam name="T">Delegate type of a native function.</typeparam>
        /// <returns>Delegate instance of a native function.</returns>
        protected T GetFuncPtr<T>() where T : Delegate
        {
            string funcSymbol = typeof(T).Name;
            return GetFuncPtr<T>(funcSymbol);
        }

        /// <summary>
        /// Get a delegate of a native function from a library.
        /// </summary>
        /// <typeparam name="T">Delegate type of a native function.</typeparam>
        /// <param name="funcSymbol">Name of the exported function symbol.</param>
        /// <returns>Delegate instance of a native function.</returns>
        protected T GetFuncPtr<T>(string funcSymbol) where T : Delegate
        {
            IntPtr funcPtr;
#if NETCOREAPP3_1
            funcPtr = NativeLibrary.GetExport(_hModule.DangerousGetHandle(), funcSymbol);
#else
#if !NETFRAMEWORK
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                funcPtr = NativeMethods.Win32.GetProcAddress(_hModule, funcSymbol);

                // Sample message of .NET Core 3.1's NativeLoader:
                // Unable to find an entry point named 'not_exist' in DLL.
                if (funcPtr == IntPtr.Zero)
                    throw new EntryPointNotFoundException($"Unable to find an entry point named '{funcSymbol}' in DLL.", new Win32Exception());
            }
#if !NETFRAMEWORK
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                funcPtr = NativeMethods.Linux.DLSym(_hModule, funcSymbol);

                if (funcPtr == IntPtr.Zero)
                {
                    // Sample message of .NET Core 3.1's NativeLoader:
                    // Unable to find an entry point named 'not_exist' in shared library.
                    string exceptMsg = $"Unable to find an entry point named '{funcSymbol}' in shared library";
                    string errorMsg = NativeMethods.Linux.DLError();
                    if (string.IsNullOrWhiteSpace(errorMsg))
                        throw new EntryPointNotFoundException($"{exceptMsg}.");
                    else
                        throw new EntryPointNotFoundException($"{exceptMsg}: {errorMsg}");

                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                funcPtr = NativeMethods.Mac.DLSym(_hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                {
                    // Sample message of .NET Core 3.1's NativeLoader:
                    // Unable to find an entry point named 'not_exist' in shared library.
                    string exceptMsg = $"Unable to find an entry point named '{funcSymbol}' in shared library";
                    string errorMsg = NativeMethods.Mac.DLError();
                    if (string.IsNullOrWhiteSpace(errorMsg))
                        throw new EntryPointNotFoundException($"{exceptMsg}.");
                    else
                        throw new EntryPointNotFoundException($"{exceptMsg}: {errorMsg}");
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
#endif
#endif

            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
        #endregion

        #region (abstract) DefaultLibFileName
        /// <summary>
        /// Default filename of the native libary to use. Override only if the target platform ships with the native library.
        /// </summary>
        /// <remarks>
        /// Throw PlatformNotSupportedException optionally when the library is included only in some of the target platforms.
        /// e.g. zlib is often included in Linux and macOS, but not in Windows.
        /// </remarks>
        protected abstract string DefaultLibFileName { get; }
        #endregion

        #region (abstract) LoadFunctions, ResetFunctions
        /// <summary>
        /// Load native functions with a GetFuncPtr. Called in the constructors.
        /// </summary>
        protected abstract void LoadFunctions();
        /// <summary>
        /// Clear pointer of native functions. Called in Dispose(bool).
        /// </summary>
        protected abstract void ResetFunctions();
        #endregion

        #region (public) Platform Information (Data Model, size_t, Unicode Encoding)
        /// <summary>
        /// Data model of the platform.
        /// </summary>
        public PlatformDataModel PlatformDataModel { get; }
        /// <summary>
        /// Size of the `long` type of the platform.
        /// </summary>
        public PlatformLongSize PlatformLongSize { get; }
        /// <summary>
        /// Bitness of the platform. 
        /// </summary>
        public PlatformBitness PlatformBitness { get; }
        /// <summary>
        /// Default unicode encoding convention of the platform. Overwrite it when the native library does not follow the platform's default convention.
        /// </summary>
        /// <remarks>
        /// Some native libraries does not follow default unicode encoding convention of the platform, so be careful.
        /// </remarks>
        public UnicodeConvention UnicodeConvention { get; protected set; }
        /// <summary>
        /// Default unicode encoding instance of the platform.
        /// </summary>
        /// <remarks>
        /// Some native libraries does not follow default unicode encoding convention of the platform, so be careful.
        /// </remarks>
        public Encoding UnicodeEncoding
        {
            get
            {
                switch (UnicodeConvention)
                {
                    case UnicodeConvention.Utf16:
                        return Encoding.Unicode;
                    case UnicodeConvention.Utf8:
                    default:
                        return new UTF8Encoding(false);
                }
            }
        }

        /// <summary>
        /// Convert buffer pointer to string following platform's default encoding convention. Wrapper of Marshal.PtrToString*().
        /// </summary>
        /// <remarks>
        /// Marshal.PtrToStringAnsi() use UTF-8 on POSIX.
        /// </remarks>
        /// <param name="ptr">Buffer pointer to convert to string</param>
        /// <returns>Converted string.</returns>
        public string PtrToStringAuto(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            switch (UnicodeConvention)
            {
                case UnicodeConvention.Utf16:
                    return Marshal.PtrToStringUni(ptr);
                case UnicodeConvention.Utf8:
                default:
                    return Marshal.PtrToStringAnsi(ptr);
            }
        }

        /// <summary>
        /// Convert string to buffer pointer following platform's default encoding convention. Wrapper of Marshal.StringToHGlobal*().
        /// </summary>
        /// <remarks>
        /// Marshal.StringToHGlobalAnsi() use UTF-8 on POSIX.
        /// </remarks>
        /// <param name="str">String to convert</param>
        /// <returns>IntPtr of the string buffer. You must call Marshal.FreeHGlobal() with returned pointer to prevent memory leak.</returns>
        public IntPtr StringToHGlobalAuto(string str)
        {
            switch (UnicodeConvention)
            {
                case UnicodeConvention.Utf16:
                    return Marshal.StringToHGlobalUni(str);
                case UnicodeConvention.Utf8:
                default:
                    return Marshal.StringToHGlobalAnsi(str);
            }
        }

        /// <summary>
        /// Convert string to buffer pointer following platform's default encoding convention. Wrapper of Marshal.StringToCoTaskMem*().
        /// </summary>
        /// <remarks>
        /// Marshal.StringToCoTaskMemAnsi() use UTF-8 on POSIX.
        /// </remarks>
        /// <param name="str">String to convert</param>
        /// <returns>IntPtr of the string buffer. You must call Marshal.FreeCoTaskMem() with returned pointer to prevent memory leak.</returns>
        public IntPtr StringToCoTaskMemAuto(string str)
        {
            switch (UnicodeConvention)
            {
                case UnicodeConvention.Utf16:
                    return Marshal.StringToCoTaskMemUni(str);
                case UnicodeConvention.Utf8:
                default:
                    return Marshal.StringToCoTaskMemAnsi(str);
            }
        }
        #endregion
    }
}
