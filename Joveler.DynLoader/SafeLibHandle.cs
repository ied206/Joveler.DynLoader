/*
    Copyright (C) 2019-2023 Hajin Jang
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
#if NETCOREAPP3_1
using System.Reflection;
#endif
using System.Runtime.InteropServices;

namespace Joveler.DynLoader
{
#if NETCOREAPP3_1
    internal class NetSafeLibHandle : SafeHandle
    {
        public NetSafeLibHandle(string libPath) : base(IntPtr.Zero, true)
        {
            // [Reference]
            // https://docs.microsoft.com/en-US/dotnet/api/system.runtime.interopservices.dllimportsearchpath?view=netstandard-2.0
            // https://docs.microsoft.com/en-us/windows/win32/dlls/dynamic-link-library-search-order
            // https://docs.microsoft.com/ko-kr/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw
            // https://docs.microsoft.com/ko-kr/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryw
            // https://github.com/dotnet/runtime/blob/master/src/coreclr/src/vm/dllimport.cpp (LocalLoadLibraryHelper, LoadLibraryFromPath)
            // https://github.com/dotnet/runtime/blob/master/src/coreclr/src/inc/utilcode.h (GetLoadWithAlteredSearchPathFlag)

            // It looks like DllImportSearchPath enum is connected to LoadLibraryEx flags.
            // 1. Flag larger than 256 is directly mapped to LoadLibraryEx flags.
            //    -  256: UseDllDirectoryForDependencies <-> LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR
            //    -  512: ApplicationDirectory           <-> LOAD_LIBRARY_SEARCH_APPLICATION_DIR
            //    - 1024: UserDirectories                <-> LOAD_LIBRARY_SEARCH_USER_DIRS
            //    - 2048: System32                       <-> LOAD_LIBRARY_SEARCH_SYSTEM32
            //    - 4096: SafeDirectories                <-> LOAD_LIBRARY_SEARCH_DEFAULT_DIRS
            // 2. Flag smaller then 256 is loosely connected to LoadLibraryEx flags.
            //    -  0, 8: LegacyBehavior                <-> LOAD_WITH_ALTERED_SEARCH_PATH(?)
            //    -  2, 8: AssemblyDirectory             <-> LOAD_WITH_ALTERED_SEARCH_PATH(?)
            // 3. (Important, Undocumented?) From the function LocalLoadLibraryHelper():
            //    Flag smaller than 256 and larger than 256 look like mutually exclusive to each others.
            //    Flag larger then 256 has priority over smaller than 256.

            const DllImportSearchPath searchPaths = DllImportSearchPath.AssemblyDirectory;
            handle = NativeLibrary.Load(libPath, Assembly.GetExecutingAssembly(), searchPaths);
        }

        /// <inheritdocs />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdocs />
        protected override bool ReleaseHandle()
        {
            NativeLibrary.Free(handle);
            return true;
        }
    }
#else
    internal class WinSafeLibHandle : SafeHandle
    {
        /// <summary>
        /// Load a native library with LoadLibraryEx() or LoadLibrary().
        /// </summary>
        /// <param name="alterSearch">Use LoadLibraryEx(..., LOAD_WITH_ALTERED_SEARCH_PATH) instead of LoadLibrary().</param>
        public WinSafeLibHandle(string libPath) : base(IntPtr.Zero, true)
        {
            handle = NativeMethods.Win32.LoadLibraryExW(libPath, IntPtr.Zero, NativeMethods.Win32.LOAD_WITH_ALTERED_SEARCH_PATH);
        }

        /// <inheritdocs />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdocs />
        protected override bool ReleaseHandle()
        {
            int ret = NativeMethods.Win32.FreeLibrary(handle);
            return ret != 0;
        }
    }

    internal class LinuxSafeLibHandle : SafeHandle
    {
        /// <summary>
        /// Load a native library with dlopen().
        /// </summary>
        public LinuxSafeLibHandle(string libPath) : base(IntPtr.Zero, true)
        {
            handle = NativeMethods.Linux.DLOpen(libPath, NativeMethods.Linux.RTLD_NOW | NativeMethods.Linux.RTLD_GLOBAL);
        }

        /// <inheritdocs />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdocs />
        protected override bool ReleaseHandle()
        {
            int ret = NativeMethods.Linux.DLClose(handle);
            return ret == 0;
        }
    }

    internal class MacSafeLibHandle : SafeHandle
    {
        /// <summary>
        /// Load a native library with dlopen().
        /// </summary>
        public MacSafeLibHandle(string libPath) : base(IntPtr.Zero, true)
        {
            handle = NativeMethods.Mac.DLOpen(libPath, NativeMethods.Mac.RTLD_NOW | NativeMethods.Mac.RTLD_GLOBAL);
        }

        /// <inheritdocs />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdocs />
        protected override bool ReleaseHandle()
        {
            int ret = NativeMethods.Mac.DLClose(handle);
            return ret == 0;
        }
    }
#endif
}
