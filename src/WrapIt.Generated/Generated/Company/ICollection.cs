using System;
using System.Collections;
using System.Collections.Generic;

namespace Company
{
    public partial interface ICollection : IEnumerable, IEnumerable<IDerived>
    {
        IDerived this[int index] { get; }

        /// <summary>
        /// Adds a name to the collection.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <returns>An item of type <see cref="T:Company.Derived" /></returns>
        IDerived Add(string name);
        IDerived Add(string name, DateTime value);
    }
}