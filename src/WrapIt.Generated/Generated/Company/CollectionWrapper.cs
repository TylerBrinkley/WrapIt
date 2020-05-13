using System;
using System.Collections;
using System.Collections.Generic;

namespace Company
{
    public partial class CollectionWrapper : ICollection
    {
        public static implicit operator CollectionWrapper(Company.Collection @object) => @object != null ? new CollectionWrapper(@object) : null;

        public static implicit operator Company.Collection(CollectionWrapper @object) => @object?.Object;

        public Company.Collection Object { get; private set; }

        public DerivedWrapper this[int index] => Object[index];

        IDerived ICollection.this[int index] => this[index];

        public CollectionWrapper(Company.Collection @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public DerivedWrapper Add(string name) => Object.Add(name);

        IDerived ICollection.Add(string name) => Add(name);

        public DerivedWrapper Add(string name, DateTime value) => Object.Add(name, value);

        IDerived ICollection.Add(string name, DateTime value) => Add(name, value);

        public override bool Equals(object obj) => Object.Equals(obj is CollectionWrapper o ? o.Object : obj);

        public override int GetHashCode() => Object.GetHashCode();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IDerived>)this).GetEnumerator();

        public IEnumerator<DerivedWrapper> GetEnumerator()
        {
            foreach (var item in Object)
            {
                yield return (Company.Derived)item;
            }
        }

        IEnumerator<IDerived> IEnumerable<IDerived>.GetEnumerator() => GetEnumerator();
    }
}