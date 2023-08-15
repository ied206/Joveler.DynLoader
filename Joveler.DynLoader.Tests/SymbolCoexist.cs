/*
    Written by Hajin Jang.
    Released under public domain.
*/

using System;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader.Tests
{
    /// <summary>
    /// Sample representation of zlib, designed for loading equal symbol name from independent libraries.
    /// </summary>
    public unsafe sealed class SymbolCoexist : DynLoaderBase
    {
        #region Constructor
        public SymbolCoexist() : base() { }
        #endregion

        #region Properties
        protected override string DefaultLibFileName
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public IntPtr Adler32RawPtr { get; private set; } = IntPtr.Zero;
        public IntPtr Crc32RawPtr { get; private set; } = IntPtr.Zero;
        #endregion

        #region LoadFunctions, ResetFunctions
        /// <inheritdocs/>
        protected override void LoadFunctions()
        {
            Console.WriteLine($"libPath = {LibPath}");

            Adler32RawPtr = GetRawFuncPtr(nameof(adler32));
            Crc32RawPtr = GetRawFuncPtr(nameof(crc32));

            Adler32 = GetFuncPtr<adler32>(nameof(adler32));
            Crc32 = GetFuncPtr<crc32>(nameof(crc32));
            ZLibVersionPtr = GetFuncPtr<zlibVersion>();
        }

        /// <inheritdocs/>
        protected override void ResetFunctions()
        {
            Adler32 = null;
            Crc32 = null;
            ZLibVersionPtr = null;
        }
        #endregion

        #region zlib Function Pointers
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate uint adler32(uint adler, byte* buf, uint len);
        public adler32 Adler32;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate uint crc32(uint crc, byte* buf, uint len);
        public crc32 Crc32;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr zlibVersion();
        internal zlibVersion ZLibVersionPtr;

        public string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
        #endregion
    }
}
