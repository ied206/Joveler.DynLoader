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
    public class SimplePlatformTests
    {
        [TestMethod]
        public void PerOS()
        {
            SimplePlatform p = TestSetup.PlatformLib;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsNotNull(p);

                // Assumption: No known non-unicode encoding support ancient Korean, non-ASCII latin, Chinese characters at once.
                const string uniFile = "ᄒᆞᆫ글ḀḘ韓國.txt";
                const string asciiFile = "ASCII.txt";
                string bclUniFullPath = Path.GetFullPath(uniFile);
                string bclAsciiFullPath = Path.GetFullPath(asciiFile);

                // UTF-16 mode (automatic)
                string nativeAsciiFullPath = p.GetFullPathName(asciiFile);
                Assert.IsTrue(nativeAsciiFullPath.Equals(bclAsciiFullPath, StringComparison.Ordinal));
                string nativeUniFullPath = p.GetFullPathName(uniFile);
                Assert.IsTrue(nativeUniFullPath.Equals(bclUniFullPath, StringComparison.Ordinal));

                // UTF-8 mode (manual)
                p.ForceChangeUnicode(UnicodeConvention.Utf8);
                nativeAsciiFullPath = p.GetFullPathName(asciiFile);
                Assert.IsTrue(nativeAsciiFullPath.Equals(bclAsciiFullPath, StringComparison.Ordinal));
                nativeUniFullPath = p.GetFullPathName(uniFile);
                Assert.IsFalse(nativeUniFullPath.Equals(bclUniFullPath, StringComparison.Ordinal));

                // UTF-16 mode (manual)
                p.ForceChangeUnicode(UnicodeConvention.Utf16);
                nativeAsciiFullPath = p.GetFullPathName(asciiFile);
                Assert.IsTrue(nativeAsciiFullPath.Equals(bclAsciiFullPath, StringComparison.Ordinal));
                nativeUniFullPath = p.GetFullPathName(uniFile);
                Assert.IsTrue(nativeUniFullPath.Equals(bclUniFullPath, StringComparison.Ordinal));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.IsNotNull(p);

                string magicFile1 = p.MagicGetPath1(null);
                string magicFile2 = p.MagicGetPath2(null);
                Assert.IsTrue(magicFile1.Equals(magicFile2, StringComparison.Ordinal));
            }
            else
            {
                Assert.IsNull(p);
            }

        }
    }
}
