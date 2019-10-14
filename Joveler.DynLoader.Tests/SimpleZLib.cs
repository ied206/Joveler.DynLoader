using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader.Tests
{
    /// <summary>
    /// Sample representation of zlib, includes only adler32 and crc32 checksum
    /// </summary>
    public unsafe class SimpleZLib : DynLoaderBase
    {
        #region Constructor
        public SimpleZLib() : base() { }
        public SimpleZLib(string libPath) : base(libPath) { }
        #endregion

        #region Properties
        protected override string ErrorMsgInitFirst => "Please init the library first!";
        protected override string ErrorMsgAlreadyInit => "Library was already initialized!";
        protected override string DefaultLibFileName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libz.so";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "libz.dylib";

                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            Adler32 = GetFuncPtr<adler32>(nameof(adler32));
            Crc32 = GetFuncPtr<crc32>(nameof(crc32));
            ZLibVersionPtr = GetFuncPtr<zlibVersion>(nameof(zlibVersion));
        }

        protected override void ResetFunctions()
        {
            Adler32 = null;
            Crc32 = null;
            ZLibVersionPtr = null;
        }
        #endregion

        #region zlib Function Pointers
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public unsafe delegate uint adler32(
            uint adler,
            byte* buf,
            uint len);
        public adler32 Adler32;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public unsafe delegate uint crc32(
            uint crc,
            byte* buf,
            uint len);
        public crc32 Crc32;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate IntPtr zlibVersion();
        private zlibVersion ZLibVersionPtr;
        public string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
        #endregion
    }
}
