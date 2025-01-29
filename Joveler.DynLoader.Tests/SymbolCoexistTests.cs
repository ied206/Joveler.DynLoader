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
using System.Linq;
using System.Text;

namespace Joveler.DynLoader.Tests
{
    /// <summary>
    /// Load two `zlib1.dll` (one from zlib, one from zlib-ng) and test if two can co-exist together.
    /// </summary>
    [TestClass]
    public class SymbolCoexistTests
    {
        private static string[] _libPaths = [];
        private static SymbolCoexist[] _zlibs = [];

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            _libPaths = [TestSetup.TempUpstreamZLibPath, TestSetup.TempNgCompatZLibPath];
            SymbolCoexist?[] libs = [TestSetup.UpstreamZLib, TestSetup.NgCompatZLib];
            _zlibs = libs.Where(z => z != null).Select(z => z!).ToArray();
        }

        [TestMethod]
        public unsafe void CoexistAdler32()
        {
            // ABC -> 0x018D00C7u
            // XYZ -> 0x0217010Cu
            // ABCXYZ -> 0x05F601D2u
            byte[] first = Encoding.UTF8.GetBytes("ABC");
            byte[] second = Encoding.UTF8.GetBytes("XYZ");
            byte[] full = Encoding.UTF8.GetBytes("ABCXYZ");

            foreach (SymbolCoexist z in _zlibs)
            {
                Assert.IsNotNull(z.Adler32);

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
        public unsafe void CoexistCrc32()
        {
            // ABC -> 0xA3830348u
            // XYZ -> 0x7D29F8EDu
            // ABCXYZ -> 0x5C3CA56Fu
            byte[] first = Encoding.UTF8.GetBytes("ABC");
            byte[] second = Encoding.UTF8.GetBytes("XYZ");
            byte[] full = Encoding.UTF8.GetBytes("ABCXYZ");

            foreach (SymbolCoexist z in _zlibs)
            {
                Assert.IsNotNull(z.Crc32);

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
        public void CoexistVersion()
        {
            foreach (SymbolCoexist z in _zlibs)
            {
                string verStr = z.ZLibVersion();
                Console.WriteLine(verStr);
            }
        }

        [TestMethod]
        public void CoexistCreateDispose()
        {
            foreach (string libPath in _libPaths)
            {
                using (SymbolCoexist zlib = new SymbolCoexist())
                {
                    zlib.LoadLibrary(libPath);
                    zlib.ZLibVersion();
                }
            }
        }

        [TestMethod]
        public void CoexistCompareRawPtr()
        {
            SymbolCoexist u = TestSetup.UpstreamZLib ?? throw new ObjectDisposedException(nameof(TestSetup));
            SymbolCoexist n = TestSetup.NgCompatZLib ?? throw new ObjectDisposedException(nameof(TestSetup));

            Console.WriteLine($"{nameof(SymbolCoexist.adler32)}: upstream = 0x{u.Adler32RawPtr:X8}");
            Console.WriteLine($"{nameof(SymbolCoexist.adler32)}: zlib-ng  = 0x{n.Adler32RawPtr:X8}");
            Assert.AreNotEqual(u.Adler32RawPtr, n.Adler32RawPtr);

            Console.WriteLine($"{nameof(SymbolCoexist.crc32)}: upstream = 0x{u.Crc32RawPtr:X8}");
            Console.WriteLine($"{nameof(SymbolCoexist.crc32)}: zlib-ng  = 0x{n.Crc32RawPtr:X8}");
            Assert.AreNotEqual(u.Crc32RawPtr, n.Crc32RawPtr);
        }
    }
}
