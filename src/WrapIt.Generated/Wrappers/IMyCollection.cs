using System;
using System.Collections;
using System.Collections.Generic;

namespace Wrappers
{
    public partial interface IMyCollection : IList, IList<IDerived>, IReadOnlyList<IDerived>
    {
        /// <summary>
        /// Adds a name to the collection.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <returns>An item of type <see cref="T:Company.Derived" /></returns>
        IDerived Add(string name);
        IDerived Add(string name, DateTime value);
    }
}