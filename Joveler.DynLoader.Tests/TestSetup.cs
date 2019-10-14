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
        public static SimpleZLib ExplicitZLib { get; private set; }
        public static SimpleZLib ImplicitZLib { get; private set; }
        public static SimpleFileMagic ExplicitMagic { get; private set; }
        public static SimpleFileMagic ImplicitMagic { get; private set; }

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

            string zlibPath = null;
            string magicPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                zlibPath = Path.Combine(arch, zlibDllName);
                magicPath = Path.Combine(arch, magicDllName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                zlibPath = Path.Combine(arch, zlibSoName);
                magicPath = Path.Combine(arch, magicSoName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                zlibPath = Path.Combine(arch, zlibDylibName);
                magicPath = Path.Combine(arch, magicDylibName);
            }

            ExplicitZLib = new SimpleZLib(zlibPath);
            ImplicitZLib = new SimpleZLib();

            ExplicitMagic = new SimpleFileMagic(magicPath);
            ImplicitMagic = new SimpleFileMagic();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            ExplicitMagic.Dispose();
        }
        #endregion

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
    }
}
