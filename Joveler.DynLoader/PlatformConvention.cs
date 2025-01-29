/*
    Copyright (C) 2019-present Hajin Jang
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

namespace Joveler.DynLoader
{
    #region ILP32, LP64 and LLP64 Data Models
    /// <summary>
    /// The data model of the platform.
    /// </summary>
    public enum PlatformDataModel
    {
        /// <summary>
        /// The data model of 64bit POSIX.
        /// <para>In C, int = 32bit, long = 64bit, pointer = 64bit.</para>
        /// </summary>
        LP64 = 0,
        /// <summary>
        /// The data model of 64bit Windows.
        /// <para>In C, int = 32bit, long = 32bit, long long = 64bit, pointer = 64bit.</para>
        /// </summary>
        LLP64 = 1,
        /// <summary>
        /// The data model of 32bit Windows and 32bit POSIX.
        /// <para>In C, int = 32bit, long = 32bit, pointer = 32bit.</para>
        /// </summary>
        ILP32 = 2,
    }

    /// <summary>
    /// Size of the long type of the platform.
    /// </summary>
    public enum PlatformLongSize
    {
        /// <summary>
        /// In C, long is 64bit.
        /// <para>The size of the long in 64bit POSIX (LP64).</para>
        /// </summary>
        Long64 = 0,
        /// <summary>
        /// In C, long is 32bit.
        /// <para>The size of the long in 32bit Windows (ILP32) and POSIX (LLP64).</para>
        /// </summary>
        Long32 = 1,
    }
    #endregion

    #region Platform Bitness (for size_t handling)
    /// <summary>
    /// The bitness of the Platform. Equal to the size of address space and size_t.
    /// </summary>
    public enum PlatformBitness
    {
        /// <summary>
        /// Platform is 32bit.
        /// </summary>
        Bit32 = 0,
        /// <summary>
        /// Platform is 64bit.
        /// </summary>
        Bit64 = 1,
    }
    #endregion

    #region Default Unicode Encoding Convention
    /// <summary>
    /// Default unicode encoding convention of the platform. 
    /// <para>Some native libraries does not follow default unicode encoding convention of the platform, be careful!</para>
    /// </summary>
    public enum UnicodeConvention
    {
        /// <summary>
        /// Default unicode encoding of POSIX.
        /// </summary>
        Utf8 = 0,
        /// <summary>
        /// Default non-unicode encoding of Windows.
        /// </summary>
        Ansi = 0,
        /// <summary>
        /// Default unicode encoding of Windows.
        /// </summary>
        Utf16 = 1,
    }
    #endregion
}
