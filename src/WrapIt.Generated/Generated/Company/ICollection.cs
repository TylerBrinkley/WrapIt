using System;
using System.Collections;
using System.Collections.Generic;

namespace Company
{
    public partial interface ICollection : IEnumerable, IEnumerable<IDerived>
    {
        IDerived this[int index] { get; }

        IDerived Add(string name);
        IDerived Add(string name, DateTime value);
    }
}