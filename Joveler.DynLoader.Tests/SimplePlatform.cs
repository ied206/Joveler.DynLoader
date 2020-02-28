/*
    Written by Hajin Jang.
    Released under public domain.
*/

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Joveler.DynLoader.Tests
{
    public sealed class SimplePlatform : DynLoaderBase
    {
        public SimplePlatform() : base() { }
        protected override string DefaultLibFileName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "kernel32.dll";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return "libmagic.so.1";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "libSystem.dylib";
                }

                throw new PlatformNotSupportedException();
            }
        }

        protected override void LoadFunctions()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GetFullPathNamePtrW = GetFuncPtr<GetFullPathNameW>(nameof(GetFullPathNameW));
                GetFullPathNamePtrA = GetFuncPtr<GetFullPathNameA>(nameof(GetFullPathNameA));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                MagicGetPathPtr1 = GetFuncPtr<magic_getpath1>("magic_getpath");
                MagicGetPathPtr2 = GetFuncPtr<magic_getpath2>("magic_getpath");
            }
        }

        protected override void ResetFunctions()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GetFullPathNamePtrW = null;
                GetFullPathNamePtrA = null;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                MagicGetPathPtr1 = null;
                MagicGetPathPtr2 = null;
            }
        }

        public void ForceChangeUnicode(UnicodeConvention newConvention)
        {
            UnicodeConvention = newConvention;
        }

        #region Win32
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private unsafe delegate int GetFullPathNameW(string lpFileName, int nBufferLength, StringBuilder lpBuffer, IntPtr lpFilePart);
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private unsafe delegate int GetFullPathNameA(string lpFileName, int nBufferLength, StringBuilder lpBuffer, IntPtr lpFilePart);
        private GetFullPathNameW GetFullPathNamePtrW;
        private GetFullPathNameA GetFullPathNamePtrA;

        public string GetFullPathName(string lpFileName)
        {
            StringBuilder buffer = new StringBuilder(260);
            int bufferLen = buffer.Capacity;
            int ret;
            do
            {
                switch (UnicodeConvention)
                {
                    case UnicodeConvention.Utf16:
                        ret = GetFullPathNamePtrW(lpFileName, bufferLen, buffer, IntPtr.Zero);
                        break;
                    case UnicodeConvention.Ansi:
                    default:
                        ret = GetFullPathNamePtrA(lpFileName, bufferLen, buffer, IntPtr.Zero);
                        break;
                }

                if (ret != 0 && bufferLen < ret)
                    buffer.EnsureCapacity(bufferLen + 4);
            }
            while (bufferLen < ret);
            return ret == 0 ? null : buffer.ToString();
        }
        #endregion

        #region Posix
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr magic_getpath1(IntPtr magicfile, int action);
        private magic_getpath1 MagicGetPathPtr1;
        public string MagicGetPath1(string magicFile)
        {
            IntPtr magicFilePtr = StringToHGlobalAuto(magicFile);
            try
            {
                IntPtr strPtr = MagicGetPathPtr1(magicFilePtr, -1);
                return PtrToStringAuto(strPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(magicFilePtr);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate IntPtr magic_getpath2(string magicfile, int action);
        private magic_getpath2 MagicGetPathPtr2;
        public string MagicGetPath2(string magicFile)
        {
            IntPtr strPtr = MagicGetPathPtr2(magicFile, -1);
            return PtrToStringAuto(strPtr);
        }
        #endregion
    }
}
