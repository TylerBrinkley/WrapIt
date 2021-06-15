using System;
using System.Collections.Generic;
using Wrappers.Base;
using Wrappers.OtherNamespace;

namespace Wrappers
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
        INested NestedProperty { get; set; }
        DateTime Raccoon { get; }

        void DoStuff(IOther other);
        void ParamArrayTest(IList<IOther> others);
    }
}