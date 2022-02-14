/*
    Copyright (C) 2019-2021 Hajin Jang
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

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class SimpleFileMagicTests
    {
        private static SimpleFileMagic[] _magicLibs;

        [ClassInitialize]
        public static void Init(TestContext _)
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

        [TestMethod]
        public void CreateDispose()
        {
            string libPath = TestSetup.PackagedMagicPath;

            using (SimpleFileMagic m = new SimpleFileMagic())
            {
                m.LoadLibrary(libPath);
                m.MagicVersion();
            }
        }

        [TestMethod]
        public void Manager()
        {
            string libPath = TestSetup.PackagedMagicPath;

            SimpleFileMagicManager manager = new SimpleFileMagicManager();

            bool dupInitGuard = false;
            Assert.IsFalse(manager.PreInitHookCalled);
            Assert.IsFalse(manager.PostInitHookCalled);
            Assert.IsFalse(manager.PreDisposeHookCalled);
            Assert.IsFalse(manager.PostDisposeHookCalled);
            manager.GlobalInit(libPath);
            Assert.IsTrue(manager.PreInitHookCalled);
            Assert.IsTrue(manager.PostInitHookCalled);
            Assert.IsFalse(manager.PreDisposeHookCalled);
            Assert.IsFalse(manager.PostDisposeHookCalled);
            try
            {
                manager.GlobalInit();
            }
            catch (InvalidOperationException)
            {
                dupInitGuard = true;
            }
            Assert.IsTrue(dupInitGuard);

            manager.Lib.MagicVersion();

            bool dupCleanGuard = false;
            Assert.IsTrue(manager.PreInitHookCalled);
            Assert.IsTrue(manager.PostInitHookCalled);
            Assert.IsFalse(manager.PreDisposeHookCalled);
            Assert.IsFalse(manager.PostDisposeHookCalled);
            manager.GlobalCleanup();
            Assert.IsTrue(manager.PreInitHookCalled);
            Assert.IsTrue(manager.PostInitHookCalled);
            Assert.IsTrue(manager.PreDisposeHookCalled);
            Assert.IsTrue(manager.PostDisposeHookCalled);
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
    }
}
