using System;
using System.Collections.Generic;
using WrapIt.Collections;
using Wrappers.OtherNamespace;

namespace Wrappers
{
    /// <inheritdoc cref="IDerived"/>
    public sealed partial class DerivedWrapper : BaseWrapper, IDerived
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.Derived"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator DerivedWrapper(Company.Derived @object) => @object != null ? new DerivedWrapper(@object) : null;

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.Derived"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.Derived(DerivedWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public new Company.Derived Object => (Company.Derived)base.Object;

        /// <inheritdoc cref="IDerived.Array"/>
        public ArrayWrapper<Company.Base, BaseWrapper, IBase> Array { get => Object.Array; set => Object.Array = value?.ToCollection(); }

        IList<IBase> IDerived.Array { get => Array; set => Array = ArrayWrapper<Company.Base, BaseWrapper, IBase>.Create(value); }

        /// <inheritdoc/>
        public decimal Bird { set => Object.Bird = value; }

        private OtherWrapper _cachedProperty;

        /// <inheritdoc cref="IDerived.CachedProperty"/>
        public OtherWrapper CachedProperty { get { var cachedProperty = _cachedProperty; var @object = Object.CachedProperty; return ReferenceEquals(cachedProperty?.Object, @object) ? cachedProperty : _cachedProperty = @object; } set => Object.CachedProperty = value; }

        IOther IDerived.CachedProperty { get => CachedProperty; set => CachedProperty = (OtherWrapper)value; }

        /// <inheritdoc cref="IDerived.Cat"/>
        public OtherWrapper Cat { get => Object.Cat; set => Object.Cat = value; }

        IOther IDerived.Cat { get => Cat; set => Cat = (OtherWrapper)value; }

        /// <inheritdoc cref="IDerived.Collection"/>
        public MyCollectionWrapper Collection { get => Object.Collection; set => Object.Collection = value; }

        IMyCollection IDerived.Collection { get => Collection; set => Collection = (MyCollectionWrapper)value; }

        /// <inheritdoc cref="IDerived.this[int]"/>
        public OtherWrapper this[int index] => Object[index];

        IOther IDerived.this[int index] => this[index];

        /// <inheritdoc/>
        public List<string> Names { get => Object.Names; set => Object.Names = value; }

        /// <inheritdoc cref="IDerived.Point"/>
        public PointWrapper Point { get => Object.Point; set => Object.Point = value; }

        IPoint IDerived.Point { get => Point; set => Point = (PointWrapper)value; }

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public DerivedWrapper(Company.Derived @object)
            : base(@object)
        {
        }

        /// <summary>
        /// Explicit constructor.
        /// </summary>
        /// <param name="names">The names.</param>
        public DerivedWrapper(List<string> names)
            : this(new Company.Derived(names))
        {
        }

        int IComparable.CompareTo(object obj) => ((IComparable)Object).CompareTo(obj is DerivedWrapper o ? o.Object : obj);
    }
}