using System;
using System.Collections.Generic;
using Company.OtherNamespace;

namespace Company
{
    public sealed class Derived : Base, IComparable
    {
        private decimal _bird;

        public Base[] Array { get; set; }

        public decimal Bird { set => _bird = value; }

        public Other CachedProperty { get; set; }

        public Other Cat { get; set; }

        public MyCollection Collection { get; set; }

        public Other this[int index] => Cat;

        public List<string> Names { get; set; }

        public Point Point { get; set; }

        /// <summary>
        /// Explicit constructor.
        /// </summary>
        /// <param name="names">The names.</param>
        public Derived(List<string> names)
        {
            Names = names;
        }

        public override void DoStuff(Other other)
        {
        }

        int IComparable.CompareTo(object obj) => 1;
    }
}