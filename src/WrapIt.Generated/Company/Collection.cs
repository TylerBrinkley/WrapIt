using System;
using System.Collections;

namespace Company
{
    public class Collection : IEnumerable
    {
        public Derived this[int index] => null;

        /// <summary>
        /// Adds a name to the collection.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <returns>An item of type <see cref="Derived"/></returns>
        public Derived Add(string name) => null;

        public Derived Add(string name, DateTime value) => null;

        public IEnumerator GetEnumerator() => null;
    }
}