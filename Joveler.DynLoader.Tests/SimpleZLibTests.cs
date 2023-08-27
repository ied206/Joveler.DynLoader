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
using System.Linq;
using System.Text;

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class SimpleZLibTests
    {
        private static SimpleZLib[] _zlibs;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            _zlibs = new SimpleZLib[] { TestSetup.ExplicitStdcallZLib, TestSetup.ExplicitCdeclZLib, TestSetup.ImplicitZLib };
            _zlibs = _zlibs.Where(z => z != null).ToArray();
        }

        [TestMethod]
        public unsafe void Adler32()
        {
            // ABC -> 0x018D00C7u
            // XYZ -> 0x0217010Cu
            // ABCXYZ -> 0x05F601D2u
            byte[] first = Encoding.UTF8.GetBytes("ABC");
            byte[] second = Encoding.UTF8.GetBytes("XYZ");
            byte[] full = Encoding.UTF8.GetBytes("ABCXYZ");

            foreach (SimpleZLib z in _zlibs)
            {
                fixed (byte* firstBuf = first)
                fixed (byte* secondBuf = second)
                fixed (byte* fullBuf = full)
                {
                    uint firstChecksum = z.Adler32(1, firstBuf, (uint)first.Length);
                    Assert.AreEqual(0x018D00C7u, firstChecksum);
                    uint secondChecksum = z.Adler32(1, secondBuf, (uint)second.Length);
                    Assert.AreEqual(0x0217010Cu, secondChecksum);
                    uint finalChecksum = z.Adler32(firstChecksum, secondBuf, (uint)second.Length);
                    Assert.AreEqual(0x05F601D2u, finalChecksum);
                }
            }
        }

        [TestMethod]
        public unsafe void Crc32()
        {
            // ABC -> 0xA3830348u
            // XYZ -> 0x7D29F8EDu
            // ABCXYZ -> 0x5C3CA56Fu
            byte[] first = Encoding.UTF8.GetBytes("ABC");
            byte[] second = Encoding.UTF8.GetBytes("XYZ");
            byte[] full = Encoding.UTF8.GetBytes("ABCXYZ");

            foreach (SimpleZLib z in _zlibs)
            {
                fixed (byte* firstBuf = first)
                fixed (byte* secondBuf = second)
                fixed (byte* fullBuf = full)
                {
                    uint firstChecksum = z.Crc32(0, firstBuf, (uint)first.Length);
                    Assert.AreEqual(0xA3830348u, firstChecksum);
                    uint secondChecksum = z.Crc32(0, secondBuf, (uint)second.Length);
                    Assert.AreEqual(0x7D29F8EDu, secondChecksum);
                    uint finalChecksum = z.Crc32(firstChecksum, secondBuf, (uint)second.Length);
                    Assert.AreEqual(0x5C3CA56Fu, finalChecksum);
                }
            }
        }

        [TestMethod]
        public void Version()
        {
            foreach (SimpleZLib z in _zlibs)
            {
                string verStr = z.ZLibVersion();
                Console.WriteLine(verStr);
            }
        }

        [TestMethod]
        public void StdcallCreateDispose()
        {
            string libPath = TestSetup.PackagedStdcallZLibPath;
            using (SimpleZLib zlib = new SimpleZLib())
            {
                zlib.LoadLibrary(libPath);
                zlib.ZLibVersion();
            }
        }

        [TestMethod]
        public void CdeclCreateDispose()
        {
            string libPath = TestSetup.PackagedCdeclZLibPath;
            using (SimpleZLib zlib = new SimpleZLib())
            {
                zlib.LoadLibrary(libPath);
                zlib.ZLibVersion();
            }
        }

        [TestMethod]
        public void StdcallManager()
        {
            string libPath = TestSetup.PackagedStdcallZLibPath;
            ManagerTemplate(libPath, true);
        }

        [TestMethod]
        public void CdeclManager()
        {
            string libPath = TestSetup.PackagedCdeclZLibPath;
            ManagerTemplate(libPath, false);
        }

        private static void ManagerTemplate(string libPath, bool isWindowsStdcall)
        {
            SimpleZLibManager manager = new SimpleZLibManager();
            SimpleZLibLoadData loadData = new SimpleZLibLoadData()
            {
                IsWindowsStdcall = isWindowsStdcall,
            };

            bool dupInitGuard = false;
            manager.GlobalInit(libPath, loadData);
            try
            {
                manager.GlobalInit(loadData);
            }
            catch (InvalidOperationException)
            {
                dupInitGuard = true;
            }
            Assert.IsTrue(dupInitGuard);

            Console.WriteLine(manager.Lib.ZLibVersion());
            Console.WriteLine($"UnknownRawPtr = 0x{manager.Lib.UnknownRawPtr:X8}");
            Console.WriteLine($"DeflateRawPtr = 0x{manager.Lib.DeflateRawPtr:X8}");

            Assert.IsFalse(manager.Lib.HasUnknownSymbol);
            Assert.IsTrue(manager.Lib.HasCrc32Symbol);
            Assert.AreEqual(IntPtr.Zero, manager.Lib.UnknownRawPtr);
            Assert.AreNotEqual(IntPtr.Zero, manager.Lib.DeflateRawPtr);

            bool dupCleanGuard = false;
            manager.GlobalCleanup();
            try
            {
                manager.GlobalCleanup();
            }
            catch (InvalidOperationException)
            {
                dupCleanGuard = true;
            }
            Assert.IsTrue(dupCleanGuard);
        }

        [TestMethod]
        public void DllNotFoundRetry()
        {
            string existLibPath = TestSetup.PackagedCdeclZLibPath;
            string libDir = Path.GetDirectoryName(existLibPath);
            string notExistLibPath = Path.Combine(libDir, "404_NOT_FOUND.dll");
            Console.WriteLine($"First-load  libPath: {notExistLibPath}");
            Console.WriteLine($"Second-load libPath: {existLibPath}");
            
            SimpleZLibManager manager = new SimpleZLibManager();
            SimpleZLibLoadData loadData = new SimpleZLibLoadData()
            {
                IsWindowsStdcall = false,
            };

            bool catched = false;
            try
            {
                manager.GlobalInit(notExistLibPath, loadData);
            }
            catch (DllNotFoundException)
            {
                catched = true;
                manager.GlobalInit(existLibPath, loadData);
            }
            Assert.IsTrue(catched);
        }
    }
}
