/*
    Written by Hajin Jang.
    Released under public domain.
*/

using System;

namespace Joveler.DynLoader.Tests
{
    public class SimpleFileMagicManager : LoadManagerBase<SimpleFileMagic>
    {
        public bool PreInitHookCalled { get; private set; }
        public bool PostInitHookCalled { get; private set; }
        public bool PreDisposeHookCalled { get; private set; }
        public bool PostDisposeHookCalled { get; private set; }

        protected override string ErrorMsgInitFirst => "Please init the libmagic first!";
        protected override string ErrorMsgAlreadyLoaded => "libmagic is already loaded.";

        protected override SimpleFileMagic CreateLoader()
        {
            return new SimpleFileMagic();
        }

        protected override void PreInitHook()
        {
            PreInitHookCalled = true;
        }

        protected override void PostInitHook()
        {
            if (!PreInitHookCalled)
                throw new InvalidOperationException();
            PostInitHookCalled = true;
        }
        protected override void PreDisposeHook()
        {
            PreDisposeHookCalled = true;
        }

        protected override void PostDisposeHook()
        {
            if (!PreDisposeHookCalled)
                throw new InvalidOperationException();
            PostDisposeHookCalled = true;
        }
    }
}
