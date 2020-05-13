using System;
using System.Collections.Generic;
using OtherNamespace;

namespace Company
{
    public sealed class Derived : Base, IComparable
    {
        private decimal _bird;

        public Base[] Array { get; set; }

        public decimal Bird { set => _bird = value; }

        public Other Cat { get; set; }

        public Collection Collection { get; set; }

        public Other this[int index] => Cat;

        public List<string> Names { get; set; }

        public override void DoStuff(Other other)
        {
        }

        int IComparable.CompareTo(object obj) => 1;
    }
}