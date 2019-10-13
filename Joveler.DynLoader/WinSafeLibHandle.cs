using System;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader
{
    internal class WinSafeLibHandle : SafeHandle
    {
        public WinSafeLibHandle(string libPath) : base(IntPtr.Zero, true)
        {
            handle = NativeMethods.Win32.LoadLibraryW(libPath);
        }

        /// <inheritdocs />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdocs />
        protected override bool ReleaseHandle()
        {
            int ret = NativeMethods.Win32.FreeLibrary(handle);
            return ret != 0;
        }
    }
}
