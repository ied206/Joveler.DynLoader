using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class SimpleFileMagicTests
    {
        [TestMethod]
        public void Version()
        {
            foreach (SimpleFileMagic m in new SimpleFileMagic[] { TestSetup.ExplicitMagic, TestSetup.ImplicitMagic })
            {
                int verInt = m.MagicVersion();
                Console.WriteLine(verInt);
            }
        }
    }
}
