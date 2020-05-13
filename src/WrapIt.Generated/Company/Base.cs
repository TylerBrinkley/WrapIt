using System;
using System.Collections.Generic;
using OtherNamespace;

namespace Company
{
    public class Base
    {
        private readonly DateTime _raccoon = DateTime.Now;

        public string Dog { get; set; }

        public IList<Other> InterfaceList { get; set; }

        public DateTime Raccoon => _raccoon;

        public virtual void DoStuff(Other other)
        {
        }

        public override bool Equals(object obj) => base.Equals(obj);
    }
}