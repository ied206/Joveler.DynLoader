/*
    Copyright (C) 2019 Hajin Jang
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
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Joveler.DynLoader
{
    public abstract class DynLoaderBase : IDisposable
    {
        #region Constructor
        /// <summary>
        /// Load a native dynamic library from a path of `DefaultLibFileName`.
        /// </summary>
        protected DynLoaderBase() : this(null) { }

        /// <summary>
        /// Load a native dynamic library from a given path.
        /// </summary>
        /// <param name="libPath">A native library file to load.</param>
        protected DynLoaderBase(string libPath)
        {
            // Should DynLoaderBase use default library filename?
            if (libPath == null)
            {
                if (DefaultLibFileName == null)
                    throw new ArgumentNullException(nameof(libPath));

                libPath = DefaultLibFileName;
            }

            // Retreive platform convention
#if NET451
            UnicodeConvention = UnicodeConvention.Utf16;
            PlatformLongSize = PlatformLongSize.Long32;
            PlatformDataModel = Environment.Is64BitProcess ? PlatformDataModel.LLP64 : PlatformDataModel.ILP32;
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
                        PlatformDataModel = PlatformDataModel.ILP32;
                        PlatformLongSize = PlatformLongSize.Long32;
                        break;
                    case Architecture.Arm64:
                    case Architecture.X64:
                    default:
                        PlatformDataModel = PlatformDataModel.LP64;
                        PlatformLongSize = PlatformLongSize.Long64;
                        break;
                }
            }
#endif

            // Check if we need to set proper directory to search. 
            // If we don't, LoadLibrary can fail when loading chained dll files.
            // e.g. Loading x64/A.dll requires implicit load of x64/B.dll -> SetDllDirectory("x64") is required.
            // When the libPath is just the filename itself (e.g. liblzma.dll), this step is not necessary.
            string libDir = Path.GetDirectoryName(libPath);
            bool setLibSearchDir = libDir != null && Directory.Exists(libDir);

#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                if (setLibSearchDir)
                {
                    // Library search path guard with try ~ catch
                    string bakDllDir = NativeMethods.Win32.GetDllDirectory();
                    try
                    {
                        NativeMethods.Win32.SetDllDirectoryW(libDir);
                        LoadWindowsModule(Path.GetFullPath(libPath));
                    }
                    finally
                    {
                        // Restore dll search directory to original state
                        NativeMethods.Win32.SetDllDirectoryW(bakDllDir);
                    }
                }
                else
                {
                    LoadWindowsModule(libPath);
                }
            }
#if !NET451
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Prepare chained library files.
                const string envVar = "LD_LIBRARY_PATH";
                if (setLibSearchDir && envVar != null)
                {
                    // Library search path guard with try ~ catch
                    string bakLibSerachPath = Environment.GetEnvironmentVariable(envVar);
                    try
                    {
                        string newLibSearchPath = bakLibSerachPath == null ? libDir : $"{bakLibSerachPath}:{libDir}";
                        Environment.SetEnvironmentVariable(envVar, newLibSearchPath);

                        LoadLinuxModule(libPath);
                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable(envVar, bakLibSerachPath);
                    }
                }
                else
                {
                    LoadLinuxModule(libPath);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Prepare chained library files.
                const string envVar = "DYLD_LIBRARY_PATH";
                if (setLibSearchDir && envVar != null)
                {
                    // Library search path guard with try ~ catch
                    string bakLibSerachPath = Environment.GetEnvironmentVariable(envVar);
                    try
                    {
                        string newLibSearchPath = bakLibSerachPath == null ? libDir : $"{bakLibSerachPath}:{libDir}";
                        Environment.SetEnvironmentVariable(envVar, newLibSearchPath);

                        LoadMacModule(libPath);
                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable(envVar, bakLibSerachPath);
                    }
                }
                else
                {
                    LoadMacModule(libPath);
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
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

        #region (protected) LoadModule
        private SafeHandle _hModule;
        private bool Loaded => _hModule != null && !_hModule.IsInvalid;

        protected void LoadWindowsModule(string dllPath)
        {
            _hModule = new WinSafeLibHandle(dllPath);
            if (_hModule.IsInvalid)
                throw new ArgumentException($"Unable to load [{dllPath}]", new Win32Exception());
        }

        protected void LoadLinuxModule(string soPath)
        {
            _hModule = new LinuxSafeLibHandle(soPath);
            if (_hModule.IsInvalid)
                throw new ArgumentException($"Unable to load [{soPath}], {NativeMethods.Linux.DLError()}");
        }

        protected void LoadMacModule(string soPath)
        {
            _hModule = new MacSafeLibHandle(soPath);
            if (_hModule.IsInvalid)
                throw new ArgumentException($"Unable to load [{soPath}], {NativeMethods.Mac.DLError()}");
        }
        #endregion

        #region (protected) GetFuncPtr
        /// <summary>
        /// Get a delegate of a native function from a library.
        /// </summary>
        /// <typeparam name="T">Delegate type of a native function.</typeparam>
        /// <param name="funcSymbol">Name of the exported function symbol.</param>
        /// <returns>Delegate instance of a native function.</returns>
        protected T GetFuncPtr<T>(string funcSymbol) where T : Delegate
        {
            IntPtr funcPtr;
#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                funcPtr = NativeMethods.Win32.GetProcAddress(_hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}]", new Win32Exception());
            }
#if !NET451
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                funcPtr = NativeMethods.Linux.DLSym(_hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}], {NativeMethods.Linux.DLError()}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                funcPtr = NativeMethods.Mac.DLSym(_hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}], {NativeMethods.Mac.DLError()}");
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
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

        #region (public) Platform Information (Data Model, Unicode Encoding)
        /// <summary>
        /// Data model of the platform.
        /// </summary>
        public PlatformDataModel PlatformDataModel { get; }
        /// <summary>
        /// Size of the `long` type of the platform.
        /// </summary>
        public PlatformLongSize PlatformLongSize { get; }
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
        /// <returns>IntPtr of the string buffer. You must call Marshal.FreeHGlobal() with return value to prevent memory leak.</returns>
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
        /// <returns>IntPtr of the string buffer. You must call Marshal.FreeCoTaskMem() with return value to prevent memory leak.</returns>
        public IntPtr AutoStringToCoTaskMem(string str)
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
