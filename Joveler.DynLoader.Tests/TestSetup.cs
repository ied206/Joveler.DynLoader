/*
    Copyright (C) 2019-2023 Hajin Jang
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
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class TestSetup
    {
        public static string SampleDir { get; private set; }
        public static string PackagedZLibPathStdcall { get; private set; }
        public static string PackagedZLibPathCdecl { get; private set; }
        public static SimpleZLib ExplicitStdcallZLib { get; private set; }
        public static SimpleZLib ExplicitCdeclZLib { get; private set; }
        public static SimpleZLib ImplicitZLib { get; private set; }
        public static string PackagedMagicPath { get; private set; }
        public static SimpleFileMagic ExplicitMagic { get; private set; }
        public static SimpleFileMagic ImplicitMagic { get; private set; }
        public static SimplePlatform PlatformLib { get; private set; }

        #region AssemblyInitalize, AssemblyCleanup
        [AssemblyInitialize]
        public static void AssemblyInitalize(TestContext ctx)
        {
#if NETFRAMEWORK
            string libBaseDir = Path.GetFullPath(TestHelper.GetProgramAbsolutePath());
#else
            string libBaseDir = Path.GetFullPath(Path.Combine(TestHelper.GetProgramAbsolutePath(), "..", "..", ".."));
#endif

            const string zlibStdcallDllName = "zlibwapi.dll";
            const string zlibCdeclDllName = "zlib1.dll";
            const string magicDllName = "libmagic-1.dll";
            const string zlibSoName = "libz.so";
            const string zlibDylibName = "libz.dylib";
            const string magicSoName = "libmagic.so";
            const string magicDylibName = "libmagic.dylib";

            string arch = null;
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    arch = "x86";
                    break;
                case Architecture.X64:
                    arch = "x64";
                    break;
                case Architecture.Arm:
                    arch = "armhf";
                    break;
                case Architecture.Arm64:
                    arch = "arm64";
                    break;
            }

            bool implicitLoadZLib = false;
            bool implicitLoadMagic = false;
            bool implicitLoadPlatform = false;

            string libDir;
#if NETFRAMEWORK
            libDir = Path.Combine(libBaseDir, arch);
#else
            libDir = Path.Combine(libBaseDir, "runtimes");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libDir = Path.Combine(libDir, "win-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libDir = Path.Combine(libDir, "linux-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libDir = Path.Combine(libDir, "osx-");
            libDir += arch;
            libDir = Path.Combine(libDir, "native");
#endif

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                PackagedZLibPathStdcall = Path.Combine(libDir, zlibStdcallDllName);
                PackagedZLibPathCdecl = Path.Combine(libDir, zlibCdeclDllName);
                PackagedMagicPath = Path.Combine(libDir, magicDllName);
                implicitLoadPlatform = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                PackagedZLibPathStdcall = Path.Combine(libDir, zlibSoName);
                PackagedMagicPath = Path.Combine(libDir, magicSoName);
                implicitLoadZLib = true;
                implicitLoadMagic = true;
                implicitLoadPlatform = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                PackagedZLibPathStdcall = Path.Combine(libDir, zlibDylibName);
                PackagedMagicPath = Path.Combine(libDir, magicDylibName);
                implicitLoadZLib = true;
            }

            ExplicitStdcallZLib = new SimpleZLib();
            ExplicitStdcallZLib.LoadLibrary(PackagedZLibPathStdcall, new SimpleZLibLoadData()
            {
                IsWindowsStdcall = true,
            });
            ExplicitCdeclZLib = new SimpleZLib();
            ExplicitCdeclZLib.LoadLibrary(PackagedZLibPathCdecl, new SimpleZLibLoadData()
            {
                IsWindowsStdcall = false,
            });
            if (implicitLoadZLib)
            {
                ImplicitZLib = new SimpleZLib();
                ImplicitZLib.LoadLibrary();
            }

            ExplicitMagic = new SimpleFileMagic();
            ExplicitMagic.LoadLibrary(PackagedMagicPath);
            if (implicitLoadMagic)
            {
                ImplicitMagic = new SimpleFileMagic();
                ImplicitMagic.LoadLibrary();
            }

            if (implicitLoadPlatform)
            {
                PlatformLib = new SimplePlatform();
                PlatformLib.LoadLibrary();
            }
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            ExplicitStdcallZLib?.Dispose();
            ImplicitZLib?.Dispose();
            ExplicitMagic?.Dispose();
            ImplicitMagic?.Dispose();
            PlatformLib?.Dispose();
        }
        #endregion

        #region TestHelper
        public class TestHelper
        {
            public static string GetProgramAbsolutePath()
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                if (Path.GetDirectoryName(path) != null)
                    path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return path;
            }
        }
        #endregion
    }
}
