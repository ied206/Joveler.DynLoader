using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class SimpleFileMagicTests
    {
        private static SimpleFileMagic[] _magicLibs;

        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            _magicLibs = new SimpleFileMagic[] { TestSetup.ExplicitMagic, TestSetup.ImplicitMagic };
            _magicLibs = _magicLibs.Where(m => m != null).ToArray();
        }

        [TestMethod]
        public void Version()
        {
            foreach (SimpleFileMagic m in _magicLibs)
            {
                int verInt = m.MagicVersion();
                Console.WriteLine(verInt);
            }
        }
    }
}
