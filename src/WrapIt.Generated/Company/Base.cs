using System;
using System.Collections.Generic;
using Company.OtherNamespace;

namespace Company
{
    /// <summary>
    /// The Base Class.
    /// </summary>
    public class Base
    {
        private readonly DateTime _raccoon = DateTime.Now;

        /// <summary>
        /// The Dog property.
        /// </summary>
        public string Dog { get; set; }

        public IList<Other> InterfaceList { get; set; }

        public Nested NestedProperty { get; set; }

        public DateTime Raccoon => _raccoon;

        public virtual void DoStuff(Other other)
        {
        }

        public override bool Equals(object obj) => base.Equals(obj);

        public void ParamArrayTest(params Other[] others)
        {
        }

        public class Nested
        {
            public DateTimeKind Kind { get; set; }
        }
    }
}