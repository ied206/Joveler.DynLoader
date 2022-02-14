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

namespace Joveler.DynLoader.Tests
{
    [TestClass]
    public class GhostErrorTests
    {
        [TestMethod]
        public void LoadModuleError()
        {
            try
            {
                GhostError lib = new GhostError();
                lib.LoadLibrary();
            }
            catch (DllNotFoundException e)
            {
                Console.WriteLine($"{e.GetType()}: {e.Message.Trim()}");
                if (e.InnerException != null)
                {
                    Exception ie = e.InnerException;
                    Console.WriteLine($"{ie.GetType()}: {ie.Message.Trim()}");
                }
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void LoadFunctionError()
        {
            try
            {
                GhostFunction lib = new GhostFunction();
                lib.LoadLibrary(TestSetup.PackagedZLibPath);
            }
            catch (EntryPointNotFoundException e)
            {
                Console.WriteLine($"{e.GetType()}: {e.Message.Trim()}");
                if (e.InnerException != null)
                {
                    Exception ie = e.InnerException;
                    Console.WriteLine($"{ie.GetType()}: {ie.Message.Trim()}");
                }
                return;
            }

            Assert.Fail();
        }
    }
}
