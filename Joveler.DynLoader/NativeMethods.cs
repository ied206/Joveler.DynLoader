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
using System.Runtime.InteropServices;

namespace Joveler.DynLoader
{
    internal class NativeMethods
    {
        #region Windows kernel32 API
        internal static class Win32
        {
            public const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
            [DllImport("kernel32.dll", SetLastError = true)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            public static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName, IntPtr hFile, uint dwFlags);
            /// <summary>
            /// 
            /// </summary>
            /// <param name="hModule"></param>
            /// <param name="procName"></param>
            /// <returns></returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            public static extern IntPtr GetProcAddress(SafeHandle hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll")]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            public static extern int FreeLibrary(IntPtr hModule);

            const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
            const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
            const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            [DllImport("kernel32.dll", SetLastError = true)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            private static extern uint FormatMessageW(
                uint dwFlags,
                IntPtr lpSource,
                uint dwMessageId,
                uint dwLanguageId,
                out IntPtr lpBuffer,
                uint nSize,
                IntPtr arguments); // va_list

            public static string GetLastErrorMsg(int errorCode)
            {
                uint flags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM;
                uint langId = MakeLangId(LANG_NEUTRAL, SUBLANG_DEFAULT);
                uint ret = FormatMessageW(flags, IntPtr.Zero, (uint)errorCode, langId, out IntPtr buffer, 0, IntPtr.Zero);
                if (ret == 0)
                    return null;

                string errorMsg = Marshal.PtrToStringUni(buffer);
                Marshal.FreeHGlobal(buffer);

                return errorMsg.Trim();
            }

            const ushort LANG_NEUTRAL = 0x00;
            // const ushort LANG_ENGLISH = 0x09;
            // const ushort SUBLANG_NEUTRAL = 0x00;
            const ushort SUBLANG_DEFAULT = 0x01;
            // const ushort SUBLANG_SYS_DEFAULT = 0x02;
            // const ushort SUBLANG_ENGLISH_US = 0x01; // English (USA)
            public static uint MakeLangId(ushort priLangId, ushort subLangId)
            {
                // LANG_NEUTRAL + SUBLANG_NEUTRAL = Language neutral
                // LANG_NEUTRAL + SUBLANG_DEFAULT = User default language
                // LANG_NEUTRAL + SUBLANG_SYS_DEFAULT = System default language

                // #define MAKELANGID ((((USHORT)(s)) << 10) | (USHORT)(p))
                return (ushort)((subLangId << 10) | priLangId);
            }
        }
        #endregion

        #region Linux libdl API
        internal static class Linux
        {
            public const string Library = "libdl.so.2";
            public const int RTLD_NOW = 0x0002;
            public const int RTLD_GLOBAL = 0x0100;

            [DllImport(Library, EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr DLOpen([MarshalAs(UnmanagedType.LPStr)] string fileName, int flags);

            [DllImport(Library, EntryPoint = "dlclose", CallingConvention = CallingConvention.Cdecl)]
            public static extern int DLClose(IntPtr handle);

            [DllImport(Library, EntryPoint = "dlerror", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr DLErrorPtr();
            public static string DLError()
            {
                IntPtr ptr = DLErrorPtr();
                if (ptr == IntPtr.Zero)
                    return null;

                string str = Marshal.PtrToStringAnsi(ptr);
                return str.Trim();
            }

            [DllImport(Library, EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr DLSym(SafeHandle handle, [MarshalAs(UnmanagedType.LPStr)] string symbol);
        }
        #endregion

        #region MacOS libdl API
        internal static class Mac
        {
            public const string Library = "libdl.dylib";
            public const int RTLD_NOW = 0x0002;
            public const int RTLD_GLOBAL = 0x0100;

            [DllImport(Library, EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr DLOpen([MarshalAs(UnmanagedType.LPStr)] string fileName, int flags);

            [DllImport(Library, EntryPoint = "dlclose", CallingConvention = CallingConvention.Cdecl)]
            public static extern int DLClose(IntPtr handle);

            [DllImport(Library, EntryPoint = "dlerror", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr DLErrorPtr();
            public static string DLError()
            {
                IntPtr ptr = DLErrorPtr();
                if (ptr == IntPtr.Zero)
                    return null;

                string str = Marshal.PtrToStringAnsi(ptr);
                return str.Trim();
            }

            [DllImport(Library, EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr DLSym(SafeHandle handle, [MarshalAs(UnmanagedType.LPStr)] string symbol);
        }
        #endregion
    }
}
