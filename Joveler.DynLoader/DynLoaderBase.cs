using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

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

                libPath = DefaultLibFileName;
            }

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

        #region (public) EnsureLoaded, EnsureNotLoaded
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

        #region (abstract) LoadFunctions, ResetFunctions
        protected abstract void LoadFunctions();
        protected abstract void ResetFunctions();
        #endregion
    }
}
