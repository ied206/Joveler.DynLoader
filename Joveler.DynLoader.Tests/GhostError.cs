/*
    Written by Hajin Jang.
    Released under public domain.
*/

using System;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader.Tests
{
    /// <summary>
    /// The loader tries to call non-existent library
    /// </summary>
    public unsafe sealed class GhostError : DynLoaderBase
    {
        #region Constructor
        public GhostError() : base() { }
        #endregion

        #region Properties
        protected override string DefaultLibFileName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "ᄒᆞᆫ글ḀḘ韓國Ghost.dll";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "ᄒᆞᆫ글ḀḘ韓國Ghost.so";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "ᄒᆞᆫ글ḀḘ韓國Ghost.dylib";

                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        /// <inheritdocs/>
        protected override void LoadFunctions()
        {
        }

        /// <inheritdocs/>
        protected override void ResetFunctions()
        {
        }
        #endregion
    }

    /// <summary>
    /// The loader tries to call non-existent library
    /// </summary>
    public unsafe sealed class GhostFunction : DynLoaderBase
    {
        #region Constructor
        public GhostFunction() : base() { }
        #endregion

        #region Properties
        protected override string DefaultLibFileName => throw new PlatformNotSupportedException();
        #endregion

        #region LoadFunctions, ResetFunctions
        /// <inheritdocs/>
        protected override void LoadFunctions()
        {
            NotExist = GetFuncPtr<not_exist>();
        }

        /// <inheritdocs/>
        protected override void ResetFunctions()
        {
            NotExist = null;
        }
        #endregion

        #region Ghost Function Pointers
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public unsafe delegate void not_exist();
        public not_exist? NotExist;
        #endregion
    }
}
