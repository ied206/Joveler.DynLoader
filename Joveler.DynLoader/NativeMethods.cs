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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Joveler.DynLoader
{
    [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments")]
    internal class NativeMethods
    {
        #region Windows kernel32 API
        internal static class Win32
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);
            /// <summary>
            /// 
            /// </summary>
            /// <param name="hModule"></param>
            /// <param name="procName"></param>
            /// <returns></returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            internal static extern IntPtr GetProcAddress(SafeHandle hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll")]
            internal static extern int FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern int SetDllDirectoryW([MarshalAs(UnmanagedType.LPWStr)] string lpPathName);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern int GetDllDirectoryW(int nBufferLength, StringBuilder lpBuffer);

            internal static string GetDllDirectory()
            {
                StringBuilder buffer = new StringBuilder(260);
                int bufferLen = buffer.Capacity;
                int ret;
                do
                {
                    ret = GetDllDirectoryW(bufferLen, buffer);
                    if (ret != 0 && bufferLen < ret)
                        buffer.EnsureCapacity(bufferLen + 4);
                }
                while (bufferLen < ret);
                return ret == 0 ? null : buffer.ToString();
            }
        }
        #endregion

        #region Posix libdl API
        internal static class Posix
        {
            internal const int RTLD_NOW = 0x0002;
            internal const int RTLD_GLOBAL = 0x0100;

            [DllImport("libdl", EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr DLOpen([MarshalAs(UnmanagedType.LPStr)] string fileName, int flags);

            [DllImport("libdl", EntryPoint = "dlclose", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int DLClose(IntPtr handle);

            internal static string DLError() => Marshal.PtrToStringAnsi(DLErrorPtr());
            [DllImport("libdl", EntryPoint = "dlerror", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr DLErrorPtr();

            [DllImport("libdl", EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr DLSym(SafeHandle handle, [MarshalAs(UnmanagedType.LPStr)] string symbol);
        }
        #endregion
    }
}
