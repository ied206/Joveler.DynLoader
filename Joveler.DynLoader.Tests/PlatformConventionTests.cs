/*
    Copyright (C) 2020 Hajin Jang
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class PlatformConventionTests
    {
        #region PlatformDataModel
        [TestMethod]
        public void DataModel()
        {
            DynLoaderBase instance = new EmptyLoader();
            instance.LoadLibrary();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                    case Architecture.Arm:
                        Assert.AreEqual(PlatformDataModel.ILP32, instance.PlatformDataModel);
                        break;
                    case Architecture.X64:
                    case Architecture.Arm64:
                        Assert.AreEqual(PlatformDataModel.LLP64, instance.PlatformDataModel);
                        break;
                }
            }
            else
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                    case Architecture.Arm:
                        Assert.AreEqual(PlatformDataModel.ILP32, instance.PlatformDataModel);
                        break;
                    case Architecture.X64:
                    case Architecture.Arm64:
                        Assert.AreEqual(PlatformDataModel.LP64, instance.PlatformDataModel);
                        break;
                }
            }
        }
        #endregion

        #region PlatformLongSize
        [TestMethod]
        public void LongSize()
        {
            DynLoaderBase instance = new EmptyLoader();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.AreEqual(PlatformLongSize.Long32, instance.PlatformLongSize);
            }
            else
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                    case Architecture.Arm:
                        Assert.AreEqual(PlatformLongSize.Long32, instance.PlatformLongSize);
                        break;
                    case Architecture.X64:
                    case Architecture.Arm64:
                        Assert.AreEqual(PlatformLongSize.Long64, instance.PlatformLongSize);
                        break;
                }
            }
        }
        #endregion

        #region PlatformBitness
        [TestMethod]
        public void Bitness()
        {
            DynLoaderBase instance = new EmptyLoader();

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                case Architecture.Arm:
                    Assert.AreEqual(PlatformBitness.Bit32, instance.PlatformBitness);
                    break;
                case Architecture.X64:
                case Architecture.Arm64:
                    Assert.AreEqual(PlatformBitness.Bit64, instance.PlatformBitness);
                    break;
            }
        }
        #endregion

        #region Unicode
        [TestMethod]
        public void Unicode()
        {
            DynLoaderBase defaultInstance = new EmptyLoader();
            DynLoaderBase customInstance = new EncLoader();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.AreEqual(UnicodeConvention.Utf16, defaultInstance.UnicodeConvention);
                Assert.IsTrue(defaultInstance.UnicodeEncoding.Equals(Encoding.Unicode));

                Assert.AreEqual(UnicodeConvention.Ansi, customInstance.UnicodeConvention);
                Assert.IsTrue(customInstance.UnicodeEncoding.Equals(new UTF8Encoding(false)));
            }
            else
            {
                Assert.AreEqual(UnicodeConvention.Utf8, defaultInstance.UnicodeConvention);
                Assert.IsTrue(defaultInstance.UnicodeEncoding.Equals(new UTF8Encoding(false)));

                Assert.AreEqual(UnicodeConvention.Utf8, customInstance.UnicodeConvention);
                Assert.IsTrue(customInstance.UnicodeEncoding.Equals(new UTF8Encoding(false)));
            }
        }
        #endregion

        #region StringPtr
        [TestMethod]
        public void StringPtr()
        {
            DynLoaderBase defaultInstance = new EmptyLoader();
            DynLoaderBase customInstance = new EncLoader();

            const string str = "Joveler.DynLoader";
            IntPtr ansiUtf8Ptr = Marshal.StringToHGlobalAnsi(str); // ANSI on Windows, UTF8 on POSIX
            IntPtr utf16Ptr = Marshal.StringToHGlobalUni(str);
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // PtrToStringAuto
                    string fromUniStr = defaultInstance.PtrToStringAuto(utf16Ptr);
                    Assert.IsTrue(str.Equals(fromUniStr, StringComparison.Ordinal));
                    string fromAnsiStr = customInstance.PtrToStringAuto(ansiUtf8Ptr);
                    Assert.IsTrue(str.Equals(fromAnsiStr, StringComparison.Ordinal));

                    // StringToHGlobalAuto
                    IntPtr fromUniPtr = defaultInstance.StringToHGlobalAuto(str);
                    try
                    {
                        fromUniStr = Marshal.PtrToStringUni(fromUniPtr);
                        Assert.IsTrue(str.Equals(fromUniStr, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(fromUniPtr);
                    }
                    IntPtr fromAnsiPtr = customInstance.StringToHGlobalAuto(str);
                    try
                    {
                        fromAnsiStr = Marshal.PtrToStringAnsi(fromAnsiPtr);
                        Assert.IsTrue(str.Equals(fromAnsiStr, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(fromAnsiPtr);
                    }

                    // StringToCoTaskMemAuto
                    fromUniPtr = defaultInstance.StringToCoTaskMemAuto(str);
                    try
                    {
                        fromUniStr = Marshal.PtrToStringUni(fromUniPtr);
                        Assert.IsTrue(str.Equals(fromUniStr, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(fromUniPtr);
                    }
                    fromAnsiPtr = customInstance.StringToCoTaskMemAuto(str);
                    try
                    {
                        fromAnsiStr = Marshal.PtrToStringAnsi(fromAnsiPtr);
                        Assert.IsTrue(str.Equals(fromAnsiStr, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(fromAnsiPtr);
                    }
                }
                else
                {
                    // PtrToStringAuto
                    string fromUtf8Str = defaultInstance.PtrToStringAuto(ansiUtf8Ptr);
                    Assert.IsTrue(str.Equals(fromUtf8Str, StringComparison.Ordinal));
                    fromUtf8Str = customInstance.PtrToStringAuto(ansiUtf8Ptr);
                    Assert.IsTrue(str.Equals(fromUtf8Str, StringComparison.Ordinal));

                    // StringToHGlobalAuto
                    IntPtr fromUtf8Ptr = defaultInstance.StringToHGlobalAuto(str);
                    try
                    {
                        fromUtf8Str = Marshal.PtrToStringAnsi(fromUtf8Ptr);
                        Assert.IsTrue(str.Equals(fromUtf8Str, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(fromUtf8Ptr);
                    }
                    fromUtf8Ptr = customInstance.StringToHGlobalAuto(str);
                    try
                    {
                        fromUtf8Str = Marshal.PtrToStringAnsi(fromUtf8Ptr);
                        Assert.IsTrue(str.Equals(fromUtf8Str, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(fromUtf8Ptr);
                    }

                    // StringToCoTaskMemAuto
                    fromUtf8Ptr = defaultInstance.StringToCoTaskMemAuto(str);
                    try
                    {
                        fromUtf8Str = Marshal.PtrToStringAnsi(fromUtf8Ptr);
                        Assert.IsTrue(str.Equals(fromUtf8Str, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(fromUtf8Ptr);
                    }
                    fromUtf8Ptr = customInstance.StringToCoTaskMemAuto(str);
                    try
                    {
                        fromUtf8Str = Marshal.PtrToStringAnsi(fromUtf8Ptr);
                        Assert.IsTrue(str.Equals(fromUtf8Str, StringComparison.Ordinal));
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(fromUtf8Ptr);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ansiUtf8Ptr);
                Marshal.FreeHGlobal(utf16Ptr);
            }

        }
        #endregion
    }

    #region EmptyLoader (inherits DynLoaderBase)
    public class EmptyLoader : DynLoaderBase
    {
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
                    return "libpthread.so.0";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "libSystem.dylib";
                }

                throw new PlatformNotSupportedException();
            }
        }

        protected override void LoadFunctions() { }

        protected override void ResetFunctions() { }
    }

    public class EncLoader : DynLoaderBase
    {
        public EncLoader() : base()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                UnicodeConvention = UnicodeConvention.Ansi;
            else
                UnicodeConvention = UnicodeConvention.Utf8;
        }

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
                    return "libpthread.so.0";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "libSystem.dylib";
                }

                throw new PlatformNotSupportedException();
            }
        }

        protected override void LoadFunctions() { }

        protected override void ResetFunctions() { }
    }
    #endregion
}
