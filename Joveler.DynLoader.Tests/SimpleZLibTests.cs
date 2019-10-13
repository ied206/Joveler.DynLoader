﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class SimpleZLibTests
    {
        [TestMethod]
        public unsafe void Adler32()
        {
            // ABC -> 0x018D00C7u
            // XYZ -> 0x0217010Cu
            // ABCXYZ -> 0x05F601D2u
            byte[] first = Encoding.UTF8.GetBytes("ABC");
            byte[] second = Encoding.UTF8.GetBytes("XYZ");
            byte[] full = Encoding.UTF8.GetBytes("ABCXYZ");

            foreach (SimpleZLib z in new SimpleZLib[] { TestSetup.ExplicitZLib, TestSetup.ImplicitZLib })
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

            foreach (SimpleZLib z in new SimpleZLib[] { TestSetup.ExplicitZLib, TestSetup.ImplicitZLib })
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
            foreach (SimpleZLib z in new SimpleZLib[] { TestSetup.ExplicitZLib, TestSetup.ImplicitZLib })
            {
                string verStr = z.ZLibVersion();
                Console.WriteLine(verStr);
                Assert.IsTrue(verStr.Equals("1.2.11", StringComparison.Ordinal));
            }
        }
    }
}
