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

using System;

namespace Joveler.DynLoader
{
    /// <summary>
    /// The thread-safe manager helps to keep DynLoaderBase instance as a singleton.
    /// Create one static LoadManagerBase instance per library.
    /// </summary>
    /// <remarks>
    /// Dispoable pattern is NOT implemented, because the LoadManagerBase has to be a singleton.
    /// </remarks>
    /// <typeparam name="T">Child class of DynLoaderBase</typeparam>
    public abstract class LoadManagerBase<T> where T : DynLoaderBase
    {
        #region Properties
        /// <summary>
        /// "Please init the library first" error message
        /// </summary>
        protected abstract string ErrorMsgInitFirst { get; }
        /// <summary>
        /// "The library is already loaded" error message
        /// </summary>
        protected abstract string ErrorMsgAlreadyLoaded { get; }
        /// <summary>
        /// DynLoaderBase instance. Access imported extern functions from this instance.
        /// </summary>
        public T? Lib { get; protected set; }
        #endregion

        #region Thread-Safe Load Lock Management
        /// <summary>
        /// Lock object for thread synchronization.
        /// </summary>
        private readonly object _loadLock = new object();
        /// <summary>
        /// Is the library loaded?
        /// </summary>
        public bool Loaded
        {
            get
            {
                lock (_loadLock)
                {
                    return Lib != null;
                }
            }
        }

        /// <summary>
        /// Ensure that the library has been loaded.
        /// </summary>
        public void EnsureLoaded()
        {
            lock (_loadLock)
            {
                if (Lib == null)
                    throw new InvalidOperationException(ErrorMsgInitFirst);
            }
        }

        /// <summary>
        /// Ensure that the library has not been loaded yet.
        /// </summary>
        public void EnsureNotLoaded()
        {
            lock (_loadLock)
            {
                if (Lib != null)
                    throw new InvalidOperationException(ErrorMsgAlreadyLoaded);
            }
        }
        #endregion

        #region CreateLoader
        /// <summary>
        /// Represents parameter-less constructor of DynLoaderBase.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit().
        /// </remarks>
        /// <returns>DynLoaderBase instace</returns>
        protected abstract T CreateLoader();
        /// <summary>
        /// (DEPRECATED) Represents constructor of DynLoaderBase with a `libPath` parameter.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit(string libPath).
        /// </remarks>
        /// <returns>DynLoaderBase instace</returns>
        [Obsolete("Left as ABI compatibility only, remove its override.")]
        protected virtual T CreateLoader(string libPath)
        {
            return CreateLoader();
        }
        #endregion

        #region Hooks
        /// <summary>
        /// Allocate other external resources before CreateLoader() get called.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit().
        /// </remarks>
        protected virtual void PreInitHook() { }
        /// <summary>
        /// Allocate other external resources after CreateLoader() get called.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit().
        /// </remarks>
        protected virtual void PostInitHook() { }
        /// <summary>
        /// Disallocate other external resources before disposing DynLoaderBase instance.
        /// </summary>
        /// <remarks>
        /// Called in GlobalCleanup().
        /// </remarks>
        protected virtual void PreDisposeHook() { }
        /// <summary>
        /// Disallocate other external resources after disposing DynLoaderBase instance.
        /// </summary>
        /// <remarks>
        /// Called in GlobalCleanup().
        /// </remarks>
        protected virtual void PostDisposeHook() { }
        #endregion

        #region GlobalInit
        /// <summary>
        /// Create DynLoaderBase singleton instance in a thread-safe way.
        /// </summary>
        /// <remarks>
        /// Throws an InvalidOperationException when a native library has already been loaded.
        /// </remarks>
        public void GlobalInit()
        {
            GlobalInit(null, null);
        }

        /// <summary>
        /// Create DynLoaderBase singleton instance in a thread-safe way.
        /// </summary>
        /// <remarks>
        /// Throws an InvalidOperationException when a native library has already been loaded.
        /// </remarks>
        /// <param name="libPath">A native library file to load.</param>
        public void GlobalInit(string? libPath)
        {
            GlobalInit(libPath, null);
        }

        /// <summary>
        /// Create DynLoaderBase singleton instance in a thread-safe way, with a custom object.
        /// </summary>
        /// <remarks>
        /// Throws an InvalidOperationException when a native library has already been loaded.
        /// </remarks>
        /// <param name="loadData">Custom object to be passed to <see cref="DynLoaderBase{T}.ParseLoadData()"/>.</param>
        public void GlobalInit(object? loadData)
        {
            GlobalInit(null, loadData);
        }

        /// <summary>
        /// Create DynLoaderBase singleton instance in a thread-safe way, with a custom object.
        /// </summary>
        /// <remarks>
        /// Throws an InvalidOperationException when a native library has already been loaded.
        /// </remarks>
        /// <param name="libPath">A native library file to load.</param>
        /// <param name="loadData">Custom object to be passed to <see cref="DynLoaderBase{T}.ParseLoadData()"/>.</param>
        public void GlobalInit(string? libPath, object? loadData)
        {
            lock (_loadLock)
            {
                if (Lib != null)
                    throw new InvalidOperationException(ErrorMsgAlreadyLoaded);

                try
                {
                    Lib = CreateLoader();
                    PreInitHook();
                    Lib.LoadLibrary(libPath, loadData);
                    PostInitHook();
                }
                catch
                {
                    Lib = null;
                    throw;
                }
            }
        }
        #endregion

        #region GlobalCleanup
        /// <summary>
        /// Dispose DynLoaderBase singleton instance in a thread-safe way.
        /// </summary>
        /// <remarks>
        /// Throws an InvalidOperationException when a native library has not been loaded.
        /// </remarks>
        public void GlobalCleanup()
        {
            lock (_loadLock)
            {
                if (Lib == null)
                    throw new InvalidOperationException(ErrorMsgInitFirst);

                InternalCleanup();
            }
        }

        /// <summary>
        /// Dispose DynLoaderBase singleton instance in a thread-safe way.
        /// </summary>
        /// <remarks>
        /// Use this method to make sure that there is no loaded native library.
        /// </remarks>
        /// <returns>
        /// Returns true when succeeded to unload, and false when a native library has not been loaded.
        /// </returns>
        public bool TryGlobalCleanup()
        {
            lock (_loadLock)
            {
                if (Lib == null)
                    return false;

                InternalCleanup();
                return true;
            }
        }

        private void InternalCleanup()
        {
            PreDisposeHook();
            Lib?.Dispose();
            PostDisposeHook();
            Lib = null;
        }
        #endregion
    }
}
