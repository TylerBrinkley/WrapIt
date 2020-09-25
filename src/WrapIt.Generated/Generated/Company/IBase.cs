using System;
using System.Collections.Generic;
using OtherNamespace;

namespace Company
{
    /// <summary>
    /// The Base Class.
    /// </summary>
    public partial interface IBase
    {
        /// <summary>
        /// The Dog property.
        /// </summary>
        string Dog { get; set; }
        IList<IOther> InterfaceList { get; set; }
        DateTime Raccoon { get; }

        void DoStuff(IOther other);
    }
}