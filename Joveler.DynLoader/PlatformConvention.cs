﻿/*
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

namespace Joveler.DynLoader
{
    #region ILP32, LP64 and LLP64 Data Models
    /// <summary>
    /// Data model of the platform.
    /// </summary>
    public enum PlatformDataModel
    {
        LP64 = 0, // POSIX 64bit
        LLP64 = 1, // Windows 64bit
        ILP32 = 2, // Windows, POSIX 32bit 
    }

    /// <summary>
    /// Size of the `long` type of the platform.
    /// </summary>
    public enum PlatformLongSize
    {
        Long64 = 0, // POSIX 64bit (LP64)
        Long32 = 1, // Windows, POSIX 32bit (ILP32, LLP64)
    }
    #endregion

    #region Default Unicode Encoding Convention
    /// <summary>
    /// Default unicode encoding convention of the platform. 
    /// </summary>
    /// <remarks>
    /// Some native libraries does not follow default unicode encoding convention of the platform, so be careful.
    /// </remarks>
    public enum UnicodeConvention
    {
        Utf8 = 0, // POSIX
        Ansi = 0, // Windows
        Utf16 = 1, // Windows
    }
    #endregion
}