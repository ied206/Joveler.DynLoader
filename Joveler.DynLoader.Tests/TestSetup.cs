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

        #region AssemblyInitalize, AssemblyCleanup
        [AssemblyInitialize]
        public static void AssemblyInitalize(TestContext ctx)
        {
            BaseDir = Path.GetFullPath(Path.Combine(TestHelper.GetProgramAbsolutePath(), "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");

            const string x64 = "x64";
            const string x86 = "x86";
            const string armhf = "armhf";
            const string arm64 = "arm64";

            const string dllName = "zlibwapi.dll";
            const string soName = "libz.so";
            const string dylibName = "libz.dylib";

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        libPath = Path.Combine(x86, dllName);
                        break;
                    case Architecture.X64:
                        libPath = Path.Combine(x64, dllName);
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        libPath = Path.Combine(x64, soName);
                        break;
                    case Architecture.Arm:
                        libPath = Path.Combine(armhf, soName);
                        break;
                    case Architecture.Arm64:
                        libPath = Path.Combine(arm64, soName);
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        libPath = Path.Combine(x64, dylibName);
                        break;
                }
            }

            if (libPath == null)
                throw new PlatformNotSupportedException();

            ExplicitZLib = new SimpleZLib(libPath);
            ImplicitZLib = new SimpleZLib();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            ExplicitZLib.Dispose();
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
