using System;
using System.Collections.Generic;
using OtherNamespace;

namespace Company
{
    public partial interface IDerived : IBase, IComparable
    {
        IList<IBase> Array { get; set; }
        decimal Bird { set; }
        IOther Cat { get; set; }
        ICollection Collection { get; set; }
        IOther this[int index] { get; }
        List<string> Names { get; set; }
    }
}