using System;
using System.Collections.Generic;
using OtherNamespace;
using WrapIt.Collections;

namespace Company
{
    public partial class BaseWrapper : IBase
    {
        public static implicit operator BaseWrapper(Company.Base @object) => @object switch
        {
            null => null,
            Company.Derived v0 => (DerivedWrapper)v0,
            _ => new BaseWrapper(@object)
        };

        public static implicit operator Company.Base(BaseWrapper @object) => @object?.Object;

        public Company.Base Object { get; private set; }

        public string Dog { get => Object.Dog; set => Object.Dog = value; }

        public ListWrapper<OtherNamespace.Other, OtherWrapper, IOther> InterfaceList { get => ListWrapper<OtherNamespace.Other, OtherWrapper, IOther>.Create(Object.InterfaceList); set => Object.InterfaceList = value?.ToCollection(); }

        IList<IOther> IBase.InterfaceList { get => InterfaceList; set => InterfaceList = ListWrapper<OtherNamespace.Other, OtherWrapper, IOther>.Create(value); }

        public DateTime Raccoon => Object.Raccoon;

        public BaseWrapper(Company.Base @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public void DoStuff(OtherWrapper other) => Object.DoStuff(other);

        void IBase.DoStuff(IOther other) => DoStuff((OtherWrapper)other);

        public override bool Equals(object obj) => Object.Equals(obj is BaseWrapper o ? o.Object : obj);

        public override int GetHashCode() => Object.GetHashCode();
    }
}