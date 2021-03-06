/*
    Copyright (C) 2019 Hajin Jang
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
        public static string BaseDir { get; private set; }
        public static string SampleDir { get; private set; }
        public static string PackagedZLibPath { get; private set; }
        public static SimpleZLib ExplicitZLib { get; private set; }
        public static SimpleZLib ImplicitZLib { get; private set; }
        public static string PackagedMagicPath { get; private set; }
        public static SimpleFileMagic ExplicitMagic { get; private set; }
        public static SimpleFileMagic ImplicitMagic { get; private set; }
        public static SimplePlatform PlatformLib { get; private set; }

        #region AssemblyInitalize, AssemblyCleanup
        [AssemblyInitialize]
        public static void AssemblyInitalize(TestContext ctx)
        {
            BaseDir = Path.GetFullPath(Path.Combine(TestHelper.GetProgramAbsolutePath(), "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");

            const string zlibDllName = "zlibwapi.dll";
            const string zlibSoName = "libz.so";
            const string zlibDylibName = "libz.dylib";
            const string magicDllName = "libmagic-1.dll";
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
                default:
                    throw new PlatformNotSupportedException();
            }

            bool implicitLoadZLib = false;
            bool implicitLoadMagic = false;
            bool implicitLoadPlataform = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                PackagedZLibPath = Path.Combine(arch, zlibDllName);
                PackagedMagicPath = Path.Combine(arch, magicDllName);
                implicitLoadPlataform = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                PackagedZLibPath = Path.Combine(arch, zlibSoName);
                PackagedMagicPath = Path.Combine(arch, magicSoName);
                implicitLoadZLib = true;
                implicitLoadMagic = true;
                implicitLoadPlataform = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                PackagedZLibPath = Path.Combine(arch, zlibDylibName);
                PackagedMagicPath = Path.Combine(arch, magicDylibName);
                implicitLoadZLib = true;
            }

            ExplicitZLib = new SimpleZLib(PackagedZLibPath);
            if (implicitLoadZLib)
                ImplicitZLib = new SimpleZLib();

            ExplicitMagic = new SimpleFileMagic(PackagedMagicPath);
            if (implicitLoadMagic)
                ImplicitMagic = new SimpleFileMagic();

            if (implicitLoadPlataform)
                PlatformLib = new SimplePlatform();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            ExplicitZLib?.Dispose();
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
