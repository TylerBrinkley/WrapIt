using System;
using System.Collections.Generic;
using Wrappers.OtherNamespace;

namespace Wrappers
{
    public partial interface IDerived : IBase, IComparable
    {
        IList<IBase> Array { get; set; }
        decimal Bird { set; }
        IOther CachedProperty { get; set; }
        IOther Cat { get; set; }
        ICollection Collection { get; set; }
        IOther this[int index] { get; }
        List<string> Names { get; set; }
        IPoint Point { get; set; }
    }
}