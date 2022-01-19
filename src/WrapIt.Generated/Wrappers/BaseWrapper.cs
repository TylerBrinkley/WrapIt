using System;
using System.Collections.Generic;
using WrapIt.Collections;
using Wrappers.Base;
using Wrappers.OtherNamespace;

namespace Wrappers
{
    /// <summary>
    /// The Base Class.
    /// </summary>
    public partial class BaseWrapper : IBase
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.Base"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator BaseWrapper(Company.Base @object) => @object switch
        {
            null => null,
            Company.Derived o => (DerivedWrapper)o,
            _ => new BaseWrapper(@object)
        };

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.Base"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.Base(BaseWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public Company.Base Object { get; private set; }

        /// <inheritdoc/>
        public string Dog { get => Object.Dog; set => Object.Dog = value; }

        public ListWrapper<Company.OtherNamespace.Other, OtherWrapper, IOther> InterfaceList { get => ListWrapper<Company.OtherNamespace.Other, OtherWrapper, IOther>.Create(Object.InterfaceList); set => Object.InterfaceList = value?.ToCollection(); }

        IList<IOther> IBase.InterfaceList { get => InterfaceList; set => InterfaceList = ListWrapper<Company.OtherNamespace.Other, OtherWrapper, IOther>.Create(value); }

        public NestedWrapper NestedProperty { get => Object.NestedProperty; set => Object.NestedProperty = value; }

        INested IBase.NestedProperty { get => NestedProperty; set => NestedProperty = (NestedWrapper)value; }

        public DateTime Raccoon => Object.Raccoon;

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public BaseWrapper(Company.Base @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public BaseWrapper()
            : this(new Company.Base())
        {
        }

        public void DoStuff(OtherWrapper other) => Object.DoStuff(other);

        void IBase.DoStuff(IOther other) => DoStuff((OtherWrapper)other);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Object.Equals(obj is BaseWrapper o ? o.Object : obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Object.GetHashCode();

        public void ParamArrayTest(ArrayWrapper<Company.OtherNamespace.Other, OtherWrapper, IOther> others) => Object.ParamArrayTest(others?.ToCollection());

        void IBase.ParamArrayTest(IList<IOther> others) => ParamArrayTest(ArrayWrapper<Company.OtherNamespace.Other, OtherWrapper, IOther>.Create(others));
    }
}