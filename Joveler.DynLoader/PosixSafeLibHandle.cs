using System;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader
{
    internal class PosixSafeLibHandle : SafeHandle
    {
        public PosixSafeLibHandle(string libPath) : base(IntPtr.Zero, true)
        {
            handle = NativeMethods.Posix.DLOpen(libPath, NativeMethods.Posix.RTLD_NOW | NativeMethods.Posix.RTLD_GLOBAL);
        }

        /// <inheritdocs />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdocs />
        protected override bool ReleaseHandle()
        {
            int ret = NativeMethods.Posix.DLClose(handle);
            return ret == 0;
        }
    }
}
