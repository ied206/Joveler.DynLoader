using System;

namespace Joveler.DynLoader
{
    /// <summary>
    /// The thread-safe manager helps to keep DynLoaderBase instance as a singleton.
    /// Create one static LoadManagerBase instance per library.
    /// </summary>
    /// <remarks>
    /// Dispoable pattern is NOT implemented, because the LoadManagerBase also have to be a singleton.
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
        public T Lib { get; protected set; }
        #endregion

        #region Thread-Safe Load Lock Management
        private readonly object LoadLock = new object();
        /// <summary>
        /// 
        /// </summary>
        public bool Loaded
        {
            get
            {
                lock (LoadLock)
                {
                    return Lib != null;
                }
            }
        }

        /// <summary>
        /// Ensure that the library have been loaded.
        /// </summary>
        public void EnsureLoaded()
        {
            lock (LoadLock)
            {
                if (Lib == null)
                    throw new InvalidOperationException(ErrorMsgInitFirst);
            }
        }

        /// <summary>
        /// Ensure that the library have not been loaded yet.
        /// </summary>
        public void EnsureNotLoaded()
        {
            lock (LoadLock)
            {
                if (Lib != null)
                    throw new InvalidOperationException(ErrorMsgAlreadyLoaded);
            }
        }
        #endregion

        #region Init and Cleanup Methods
        /// <summary>
        /// Represents parameter-less constructor of DynLoaderBase.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit().
        /// </remarks>
        /// <returns>DynLoaderBase instace</returns>
        protected abstract T CreateLoader();
        /// <summary>
        /// Represents constructor of DynLoaderBase with a `libPath` parameter.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit(string libPath).
        /// </remarks>
        /// <returns>DynLoaderBase instace</returns>
        protected abstract T CreateLoader(string libPath);
        /// <summary>
        /// Allocate other external resources before CreateLoader get called.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit() and GlobalInit(string libPath).
        /// </remarks>
        protected virtual void PreInitHook() { }
        /// <summary>
        /// Allocate other external resources after CreateLoader get called.
        /// </summary>
        /// <remarks>
        /// Called in GlobalInit() and GlobalInit(string libPath).
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

        /// <summary>
        /// Create DynLoaderBase singleton instance in a thread-safe way.
        /// </summary>
        public void GlobalInit()
        {
            lock (LoadLock)
            {
                if (Lib == null)
                {
                    PreInitHook();
                    Lib = CreateLoader();
                    PostInitHook();
                }
                else
                {
                    throw new InvalidOperationException(ErrorMsgInitFirst);
                }
            }
        }

        /// <summary>
        /// Create DynLoaderBase singleton instance in a thread-safe way.
        /// </summary>
        /// <param name="libPath"></param>
        public void GlobalInit(string libPath)
        {
            lock (LoadLock)
            {
                if (Lib != null)
                    throw new InvalidOperationException(ErrorMsgAlreadyLoaded);

                PreInitHook();
                Lib = CreateLoader(libPath);
                PostInitHook();
            }
        }

        /// <summary>
        /// Dispose DynLoaderBase singleton instance in a thread-safe way.
        /// </summary>
        public void GlobalCleanup()
        {
            lock (LoadLock)
            {
                if (Lib == null)
                    throw new InvalidOperationException(ErrorMsgInitFirst);

                PreDisposeHook();
                Lib.Dispose();
                Lib = null;
                PostDisposeHook();
            }
        }
        #endregion
    }
}
