using System;
using System.Collections.Generic;
using System.Text;

namespace Joveler.DynLoader.Tests
{
    public class SimpleZLibManager : LoadManagerBase<SimpleZLib>
    {
        protected override string ErrorMsgInitFirst => "Please init the zlib first!";
        protected override string ErrorMsgAlreadyLoaded => "zlib is already loaded.";

        protected override SimpleZLib CreateLoader()
        {
            return new SimpleZLib();
        }

        protected override SimpleZLib CreateLoader(string libPath)
        {
            return new SimpleZLib(libPath);
        }
    }
}
