using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader.Tests
{
    /// <summary>
    /// Sample representation of libmagic, includes only version
    /// </summary>
    public unsafe class SimpleFileMagic : DynLoaderBase
    {
        #region Constructor
        public SimpleFileMagic() : base() { }
        public SimpleFileMagic(string libPath) : base(libPath) { }
        #endregion

        #region Properties
        protected override string ErrorMsgInitFirst => "Please init the library first!";
        protected override string ErrorMsgAlreadyInit => "Library was already initialized!";
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
        protected override void LoadFunctions()
        {
            MagicVersion = GetFuncPtr<magic_version>(nameof(magic_version));
        }

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
