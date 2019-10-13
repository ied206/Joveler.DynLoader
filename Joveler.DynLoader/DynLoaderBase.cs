using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader
{
    public abstract class DynLoaderBase : IDisposable
    {
        #region (abstract) Properties
        protected abstract string ErrorMsgInitFirst { get; }
        protected abstract string ErrorMsgAlreadyInit { get; }
        protected abstract string DefaultLibFileName { get; }
        #endregion

        #region (private) Properties
        private SafeHandle _hModule;
        private bool Loaded => _hModule != null && !_hModule.IsInvalid;
        #endregion

        #region Constructor
        protected DynLoaderBase() : this(null) { }

        protected DynLoaderBase(string libPath)
        {
            if (Loaded)
                throw new InvalidOperationException(ErrorMsgAlreadyInit);

            if (libPath == null)
            {
                if (DefaultLibFileName == null)
                    throw new ArgumentNullException(nameof(libPath));

                string libExt = ".dll";
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    libExt = ".dll";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    libExt = ".so";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    libExt = ".dylib";
                else
                    throw new ArgumentNullException(nameof(libPath));
#endif

                libPath = Path.ChangeExtension(DefaultLibFileName, libExt);
            }

            /*
            // Check if we need to set proper directory to search. 
            // If we don't, LoadLibrary can fail when loading chained dll files.
            // e.g. Loading x64/A.dll requires implicit load of x64/B.dll -> SetDllDirectory("x64") is required.
            // When the libPath is just the filename itself (e.g. liblzma.dll), this step is not necessary.
            string libDir = Path.GetDirectoryName(libPath);
            bool setLibSearchDir = libDir != null && Directory.Exists(libDir);
            */
#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                _hModule = new WinSafeLibHandle(libPath);
                if (_hModule.IsInvalid)
                    throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());

                /*
                if (setLibSearchDir)
                {
                    // Backup dll search directory state
                    StringBuilder buffer = new StringBuilder(260);
                    int bufferLen = buffer.Capacity;
                    int ret;
                    do
                    {
                        ret = NativeMethods.Win32.GetDllDirectoryW(bufferLen, buffer);
                        if (ret != 0 && bufferLen < ret)
                            buffer.EnsureCapacity(bufferLen + 4);
                    }
                    while (bufferLen < ret);
                    string bakDllDir = ret == 0 ? null : buffer.ToString();

                    // Set SetDllDictionary guard with try ~ catch
                    try
                    {
                        NativeMethods.Win32.SetDllDirectoryW(libDir);

                        libPath = Path.GetFullPath(libPath);
                        _hModule = new WinLibHandle(libPath);
                        if (_hModule.IsInvalid)
                            throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());
                    }
                    finally
                    {
                        // Restore dll search directory to original state
                        NativeMethods.Win32.SetDllDirectoryW(bakDllDir);
                    }
                }
                else
                {
                    _hModule = new WinLibHandle(libPath);
                    if (_hModule.IsInvalid)
                        throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());
                }
                */
            }
#if !NET451
            else
            {
                _hModule = new PosixSafeLibHandle(libPath);
                if (_hModule.IsInvalid)
                    throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Posix.DLError()}");

                /*
                // Prepare chained dll files.
                string envVar = null;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    envVar = "LD_LIBRARY_PATH";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    envVar = "DYLD_LIBRARY_PATH";

                if (setLibSearchDir && envVar != null)
                {
                    string bakLibSerachPath = Environment.GetEnvironmentVariable(envVar);
                    try
                    {
                        string newLibSearchPath = bakLibSerachPath == null ? libDir : $"{bakLibSerachPath}:{libDir}";
                        Environment.SetEnvironmentVariable(envVar, newLibSearchPath);

                        _hModule = new PosixLibHandle(libPath);
                        if (_hModule.IsInvalid)
                            throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Posix.DLError()}");
                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable(envVar, bakLibSerachPath);
                    }
                }
                else
                {
                    _hModule = new PosixLibHandle(libPath);
                    if (_hModule.IsInvalid)
                        throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Posix.DLError()}");
                }
                */
            }
#endif

            // Load functions
            try
            {
                LoadFunctionsPreHook();
                LoadFunctions();
                LoadFunctionsPostHook();
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
            ResetFunctionsPreHook();
            ResetFunctions();
            ResetFunctionsPostHook();

            _hModule.Dispose();
            _hModule = null;
        }
        #endregion

        #region GetFuncPtr
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

        #region EnsureLoaded, EnsureNotLoaded
        public void EnsureLoaded()
        {
            if (!Loaded)
                throw new InvalidOperationException(ErrorMsgInitFirst);
        }

        public void EnsureNotLoaded()
        {
            if (Loaded)
                throw new InvalidOperationException(ErrorMsgAlreadyInit);
        }

        #endregion

        #region (virtual) Hooks for LoadFunctions/ResetFunctions
        protected virtual void LoadFunctionsPreHook() { }
        protected virtual void LoadFunctionsPostHook() { }
        protected virtual void ResetFunctionsPreHook() { }
        protected virtual void ResetFunctionsPostHook() { }
        #endregion

        #region (abstract) LoadFunctions, ResetFunctions
        protected abstract void LoadFunctions();
        protected abstract void ResetFunctions();
        #endregion
    }
}
