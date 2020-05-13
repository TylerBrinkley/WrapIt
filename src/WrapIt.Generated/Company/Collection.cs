using System;
using System.Collections;

namespace Company
{
    public class Collection : IEnumerable
    {
        public Derived this[int index] => null;

        public Derived Add(string name) => null;

        public Derived Add(string name, DateTime value) => null;

        public IEnumerator GetEnumerator() => null;
    }
}