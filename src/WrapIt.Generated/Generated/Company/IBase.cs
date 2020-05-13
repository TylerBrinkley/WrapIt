using System;
using System.Collections.Generic;
using OtherNamespace;

namespace Company
{
    public partial interface IBase
    {
        string Dog { get; set; }
        IList<IOther> InterfaceList { get; set; }
        DateTime Raccoon { get; }

        void DoStuff(IOther other);
    }
}