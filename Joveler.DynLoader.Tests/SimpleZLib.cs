/*
    Written by Hajin Jang.
    Released under public domain.
*/

using System;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader.Tests
{
    public class SimpleZLibLoadData
    {
        public bool IsWindowsStdcall { get; set; } = true;
    }

    /// <summary>
    /// Sample representation of zlib, includes only adler32 and crc32 checksum
    /// </summary>
    public unsafe sealed class SimpleZLib : DynLoaderBase
    {
        #region Constructor
        public SimpleZLib() : base() { }
        #endregion

        #region Properties
        protected override string DefaultLibFileName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libz.so.1";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "libz.dylib";

                throw new PlatformNotSupportedException();
            }
        }

        private bool _isWindowsStdcall = true;

        public bool HasUnknownSymbol { get; private set; } = true;
        public bool HasCrc32Symbol { get; private set; } = false;
        public IntPtr DeflateRawPtr { get; private set; } = IntPtr.Zero;
        #endregion

        #region Stdcall and Cdecl
        internal Stdcall _stdcall = new Stdcall();
        internal Cdecl _cdecl = new Cdecl();
        #endregion

        #region ParseCustomData
        protected override void ParseLoadData(object data)
        {
            if (!(data is SimpleZLibLoadData loadData))
                return;

            _isWindowsStdcall = loadData.IsWindowsStdcall;
            Console.WriteLine($"libPath = {LibPath}");
            Console.WriteLine($"isWindowsStdcall = {_isWindowsStdcall}");
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        /// <inheritdocs/>
        protected override void LoadFunctions()
        {
            HasUnknownSymbol = HasFuncSymbol("UnknownSymbol");
            HasCrc32Symbol = HasFuncSymbol(nameof(Cdecl.crc32));
            DeflateRawPtr = GetRawFuncPtr("deflate");

            if (_isWindowsStdcall)
            {
                _stdcall.Adler32 = GetFuncPtr<Stdcall.adler32>(nameof(Stdcall.adler32));
                _stdcall.Crc32 = GetFuncPtr<Stdcall.crc32>(nameof(Stdcall.crc32));
                _stdcall.ZLibVersionPtr = GetFuncPtr<Stdcall.zlibVersion>();
            }
            else
            {
                _cdecl.Adler32 = GetFuncPtr<Cdecl.adler32>(nameof(Cdecl.adler32));
                _cdecl.Crc32 = GetFuncPtr<Cdecl.crc32>(nameof(Cdecl.crc32));
                _cdecl.ZLibVersionPtr = GetFuncPtr<Cdecl.zlibVersion>();
            }
        }

        /// <inheritdocs/>
        protected override void ResetFunctions()
        {
            if (_isWindowsStdcall)
            {
                _stdcall.Adler32 = null;
                _stdcall.Crc32 = null;
                _stdcall.ZLibVersionPtr = null;
            }
            else
            {
                _cdecl.Adler32 = null;
                _cdecl.Crc32 = null;
                _cdecl.ZLibVersionPtr = null;
            }
        }
        #endregion

        #region zlib Function Pointers
        public unsafe uint Adler32(uint adler, byte* buf, uint len)
        {
            if (_isWindowsStdcall)
                return _stdcall.Adler32(adler, buf, len);
            else
                return _cdecl.Adler32(adler, buf, len);
        }

        public unsafe uint Crc32(uint crc, byte* buf, uint len)
        {
            if (_isWindowsStdcall)
                return _stdcall.Crc32(crc, buf, len);
            else
                return _cdecl.Crc32(crc, buf, len);
        }

        public string ZLibVersion()
        {
            IntPtr strPtr = IntPtr.Zero;
            if (_isWindowsStdcall)
                strPtr = _stdcall.ZLibVersionPtr();
            else
                strPtr = _cdecl.ZLibVersionPtr();
            return Marshal.PtrToStringAnsi(strPtr);
        }

        internal class Stdcall
        {
            private const CallingConvention CallConv = CallingConvention.Winapi;

            [UnmanagedFunctionPointer(CallConv)]
            public unsafe delegate uint adler32(uint adler, byte* buf, uint len);
            public adler32 Adler32;

            [UnmanagedFunctionPointer(CallConv)]
            public unsafe delegate uint crc32(uint crc, byte* buf, uint len);
            public crc32 Crc32;

            [UnmanagedFunctionPointer(CallConv)]
            public delegate IntPtr zlibVersion();
            internal zlibVersion ZLibVersionPtr;
            
        }
        internal class Cdecl
        {
            private const CallingConvention CallConv = CallingConvention.Cdecl;

            [UnmanagedFunctionPointer(CallConv)]
            public unsafe delegate uint adler32(uint adler, byte* buf, uint len);
            public adler32 Adler32;

            [UnmanagedFunctionPointer(CallConv)]
            public unsafe delegate uint crc32(uint crc, byte* buf, uint len);
            public crc32 Crc32;

            [UnmanagedFunctionPointer(CallConv)]
            public delegate IntPtr zlibVersion();
            internal zlibVersion ZLibVersionPtr;
        }
        #endregion
    }
}
