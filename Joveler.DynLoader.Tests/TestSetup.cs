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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class TestSetup
    {
        public static string SampleDir { get; private set; } = null;
        public static string PackagedStdcallZLibPath { get; private set; } = null;
        public static string PackagedCdeclZLibPath { get; private set; } = null;
        public static string PackagedNgCompatZLibPath { get; private set; } = null;
        public static SimpleZLib ExplicitStdcallZLib { get; private set; } = null;
        public static SimpleZLib ExplicitCdeclZLib { get; private set; } = null;
        public static SimpleZLib ImplicitZLib { get; private set; } = null;
        public static string PackagedMagicPath { get; private set; } = null;
        public static SimpleFileMagic ExplicitMagic { get; private set; } = null;
        public static SimpleFileMagic ImplicitMagic { get; private set; } = null;
        public static SimplePlatform PlatformLib { get; private set; } = null;
        public static string TempUpstreamZLibDir { get; private set; } = null;
        public static string TempUpstreamZLibPath { get; private set; } = null;
        public static string TempNgCompatZLibDir { get; private set; } = null;
        public static string TempNgCompatZLibPath { get; private set; } = null;
        public static SymbolCoexist UpstreamZLib { get; private set; } = null;
        public static SymbolCoexist NgCompatZLib { get; private set; } = null;

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
            const string zlibNgCompatDllName = "zlib1-ng-compat.dll";
            const string magicDllName = "libmagic-1.dll";
            const string zlibSoName = "libz.so";
            const string zlibNgCompatSoName = "libz-ng-compat.so";
            const string zlibDylibName = "libz.dylib";
            const string zlibNgCompatDylibName = "libz-ng-compat.dylib";
            const string magicSoName = "libmagic.so";
            const string magicDylibName = "libmagic.dylib";

            TempUpstreamZLibDir = TestHelper.GetTempDir();
            TempNgCompatZLibDir = TestHelper.GetTempDir();

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
                    arch = "arm";
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
                PackagedStdcallZLibPath = Path.Combine(libDir, zlibStdcallDllName);
                PackagedCdeclZLibPath = Path.Combine(libDir, zlibCdeclDllName);
                PackagedNgCompatZLibPath = Path.Combine(libDir, zlibNgCompatDllName);
                PackagedMagicPath = Path.Combine(libDir, magicDllName);
                implicitLoadPlatform = true;

                TempUpstreamZLibPath = Path.Combine(TempUpstreamZLibDir, zlibCdeclDllName);
                TempNgCompatZLibPath = Path.Combine(TempNgCompatZLibDir, zlibCdeclDllName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                PackagedCdeclZLibPath = Path.Combine(libDir, zlibSoName);
                PackagedNgCompatZLibPath = Path.Combine(libDir, zlibNgCompatSoName);
                PackagedMagicPath = Path.Combine(libDir, magicSoName);
                implicitLoadZLib = true;
                implicitLoadMagic = true;
                implicitLoadPlatform = true;

                TempUpstreamZLibPath = Path.Combine(TempUpstreamZLibDir, zlibSoName);
                TempNgCompatZLibPath = Path.Combine(TempNgCompatZLibDir, zlibSoName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                PackagedCdeclZLibPath = Path.Combine(libDir, zlibDylibName);
                PackagedNgCompatZLibPath = Path.Combine(libDir, zlibNgCompatDylibName);
                PackagedMagicPath = Path.Combine(libDir, magicDylibName);
                implicitLoadZLib = true;

                TempUpstreamZLibPath = Path.Combine(TempUpstreamZLibDir, zlibDylibName);
                TempNgCompatZLibPath = Path.Combine(TempUpstreamZLibDir, zlibDylibName);
            }
            File.Copy(PackagedCdeclZLibPath, TempUpstreamZLibPath);
            File.Copy(PackagedNgCompatZLibPath, TempNgCompatZLibPath);

            ExplicitStdcallZLib = new SimpleZLib();
            ExplicitStdcallZLib.LoadLibrary(PackagedStdcallZLibPath, new SimpleZLibLoadData()
            {
                IsWindowsStdcall = true,
            });
            ExplicitCdeclZLib = new SimpleZLib();
            ExplicitCdeclZLib.LoadLibrary(PackagedCdeclZLibPath, new SimpleZLibLoadData()
            {
                IsWindowsStdcall = false,
            });
            if (implicitLoadZLib)
            {
                ImplicitZLib = new SimpleZLib();
                ImplicitZLib.LoadLibrary();
            }

            UpstreamZLib = new SymbolCoexist();
            UpstreamZLib.LoadLibrary(TempUpstreamZLibPath);
            NgCompatZLib = new SymbolCoexist();
            NgCompatZLib.LoadLibrary(TempNgCompatZLibPath);

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
            UpstreamZLib?.Dispose();
            NgCompatZLib?.Dispose();

            TestHelper.CleanBaseTempDir();
        }
        #endregion

        #region LogEnvironment
        [TestMethod]
        public void LogEnvironment()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine($"OS = {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}");
            b.AppendLine($"Dotnet Runtime = {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine(b.ToString());
        }
        #endregion
    }
}
