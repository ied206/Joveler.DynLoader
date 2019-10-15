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

            // Retreive platform information
#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                PlatformUnicodeConvention = PlatformUnicodeConvention.Utf16;
                PlatformDataModel = PlatformDataModel.Long32;
            }
#if !NET451
            else
            {
                PlatformUnicodeConvention = PlatformUnicodeConvention.Utf8;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.Arm:
                    case Architecture.X86:
                        PlatformDataModel = PlatformDataModel.Long32;
                        break;
                    case Architecture.Arm64:
                    case Architecture.X64:
                        PlatformDataModel = PlatformDataModel.Long64;
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
            else
            {
                // Prepare chained library files.
                string envVar = null;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    envVar = "LD_LIBRARY_PATH";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    envVar = "DYLD_LIBRARY_PATH";

                if (setLibSearchDir && envVar != null)
                {
                    // Library search path guard with try ~ catch
                    string bakLibSerachPath = Environment.GetEnvironmentVariable(envVar);
                    try
                    {
                        string newLibSearchPath = bakLibSerachPath == null ? libDir : $"{bakLibSerachPath}:{libDir}";
                        Environment.SetEnvironmentVariable(envVar, newLibSearchPath);

                        LoadPosixModule(libPath);
                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable(envVar, bakLibSerachPath);
                    }
                }
                else
                {
                    LoadPosixModule(libPath);
                }
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

        protected void LoadPosixModule(string soPath)
        {
            _hModule = new PosixSafeLibHandle(soPath);
            if (_hModule.IsInvalid)
                throw new ArgumentException($"Unable to load [{soPath}], {NativeMethods.Posix.DLError()}");
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
            else
            {
                funcPtr = NativeMethods.Posix.DLSym(_hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}], {NativeMethods.Posix.DLError()}");
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
        public PlatformDataModel PlatformDataModel { get; }
        public PlatformUnicodeConvention PlatformUnicodeConvention { get; }
        public Encoding PlatformUnicodeEncoding
        {
            get
            {
                switch (PlatformUnicodeConvention)
                {
                    case PlatformUnicodeConvention.Utf16:
                        return Encoding.Unicode;
                    case PlatformUnicodeConvention.Utf8:
                    default:
                        return new UTF8Encoding(false);
                }
            }
        }

        public string MarshalPtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            switch (PlatformUnicodeConvention)
            {
                case PlatformUnicodeConvention.Utf16:
                    return Marshal.PtrToStringUni(ptr);
                case PlatformUnicodeConvention.Utf8:
                default:
                    return Marshal.PtrToStringAnsi(ptr);
            }
        }

        public IntPtr MarshalStringToPtr(string str)
        {
            switch (PlatformUnicodeConvention)
            {
                case PlatformUnicodeConvention.Utf16:
                    return Marshal.StringToHGlobalUni(str);
                case PlatformUnicodeConvention.Utf8:
                default:
                    return Marshal.StringToHGlobalAnsi(str);
            }
        }
        #endregion
    }
}
