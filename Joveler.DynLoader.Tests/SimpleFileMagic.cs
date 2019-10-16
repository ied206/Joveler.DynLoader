/*
    Written by Hajin Jang.
    Released under public domain.
*/

using System;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader.Tests
{
    /// <summary>
    /// Sample representation of libmagic, includes only version
    /// </summary>
    public unsafe sealed class SimpleFileMagic : DynLoaderBase
    {
        #region Constructor
        public SimpleFileMagic() : base() { }
        public SimpleFileMagic(string libPath) : base(libPath) { }
        #endregion

        #region Properties
        protected override string DefaultLibFileName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libmagic.so.1";

                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        /// <inheritdocs/>
        protected override void LoadFunctions()
        {
            MagicVersion = GetFuncPtr<magic_version>(nameof(magic_version));
        }

        /// <inheritdocs/>
        protected override void ResetFunctions()
        {
            MagicVersion = null;
        }
        #endregion

        #region zlib Function Pointers
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int magic_version();
        public magic_version MagicVersion;
        #endregion
    }
}
