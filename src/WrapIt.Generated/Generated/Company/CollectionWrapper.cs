using System;
using System.Collections;
using System.Collections.Generic;

namespace Company
{
    public partial class CollectionWrapper : ICollection
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.Collection"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator CollectionWrapper(Company.Collection @object) => @object != null ? new CollectionWrapper(@object) : null;

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.Collection"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.Collection(CollectionWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public Company.Collection Object { get; private set; }

        public DerivedWrapper this[int index] => Object[index];

        IDerived ICollection.this[int index] => this[index];

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public CollectionWrapper(Company.Collection @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        /// <summary>
        /// Adds a name to the collection.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <returns>An item of type <see cref="T:Company.Derived" /></returns>
        public DerivedWrapper Add(string name) => Object.Add(name);

        IDerived ICollection.Add(string name) => Add(name);

        public DerivedWrapper Add(string name, DateTime value) => Object.Add(name, value);

        IDerived ICollection.Add(string name, DateTime value) => Add(name, value);

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Object.Equals(obj is CollectionWrapper o ? o.Object : obj);

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => Object.GetHashCode();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IDerived>)this).GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
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