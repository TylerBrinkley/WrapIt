using System;
using System.Collections;
using System.Collections.Generic;

namespace Wrappers
{
    public partial class MyCollectionWrapper : IMyCollection
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.MyCollection"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator MyCollectionWrapper(Company.MyCollection @object) => @object != null ? new MyCollectionWrapper(@object) : null;

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.MyCollection"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.MyCollection(MyCollectionWrapper @object) => @object?.Object;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public Company.MyCollection Object { get; private set; }

        public int Count => Object.Count;

        public DerivedWrapper this[int index] => Object[index];

        IDerived IReadOnlyList<IDerived>.this[int index] => this[index];

        bool ICollection.IsSynchronized => ((ICollection)Object).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection)Object).SyncRoot;

        bool ICollection<IDerived>.IsReadOnly => ((IList)Object).IsReadOnly;

        bool IList.IsFixedSize => ((IList)Object).IsFixedSize;

        bool IList.IsReadOnly => ((IList)Object).IsReadOnly;

        object IList.this[int index] { get => this[index]; set => ((IList)Object)[index] = value is DerivedWrapper o ? o.Object : value; }

        IDerived IList<IDerived>.this[int index] { get => this[index]; set => ((IList)Object)[index] = ((DerivedWrapper)value).Object; }

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public MyCollectionWrapper(Company.MyCollection @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        /// <summary>
        /// Adds a name to the collection.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <returns>An item of type <see cref="T:Company.Derived" /></returns>
        public DerivedWrapper Add(string name) => Object.Add(name);

        IDerived IMyCollection.Add(string name) => Add(name);

        public DerivedWrapper Add(string name, DateTime value) => Object.Add(name, value);

        IDerived IMyCollection.Add(string name, DateTime value) => Add(name, value);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Object.Equals(obj is MyCollectionWrapper o ? o.Object : obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Object.GetHashCode();

        void ICollection.CopyTo(Array array, int index) => ((ICollection)Object).CopyTo(array, index);

        void ICollection<IDerived>.Add(IDerived item) => ((IList)Object).Add(((DerivedWrapper)item).Object);

        void ICollection<IDerived>.Clear() => ((IList)Object).Clear();

        bool ICollection<IDerived>.Contains(IDerived item) => ((IList)Object).Contains(((DerivedWrapper)item).Object);

        void ICollection<IDerived>.CopyTo(IDerived[] array, int arrayIndex)
        {
            if (arrayIndex + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex + Count must be less than or equal to array.Length");
            }

            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        bool ICollection<IDerived>.Remove(IDerived item)
        {
            ((IList)Object).Remove(((DerivedWrapper)item).Object);
            return true;
        }

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

        int IList.Add(object value) => ((IList)Object).Add(value is DerivedWrapper o ? o.Object : value);

        void IList.Clear() => ((IList)Object).Clear();

        bool IList.Contains(object value) => ((IList)Object).Contains(value is DerivedWrapper o ? o.Object : value);

        int IList.IndexOf(object value) => ((IList)Object).IndexOf(value is DerivedWrapper o ? o.Object : value);

        void IList.Insert(int index, object value) => ((IList)Object).Insert(index, value is DerivedWrapper o ? o.Object : value);

        void IList.Remove(object value) => ((IList)Object).Remove(value is DerivedWrapper o ? o.Object : value);

        void IList.RemoveAt(int index) => ((IList)Object).RemoveAt(index);

        int IList<IDerived>.IndexOf(IDerived item) => ((IList)Object).IndexOf(((DerivedWrapper)item).Object);

        void IList<IDerived>.Insert(int index, IDerived item) => ((IList)Object).Insert(index, ((DerivedWrapper)item).Object);

        void IList<IDerived>.RemoveAt(int index) => ((IList)Object).RemoveAt(index);
    }
}