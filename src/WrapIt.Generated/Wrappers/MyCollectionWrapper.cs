using System;
using System.Collections;
using System.Collections.Generic;

namespace Wrappers
{
    /// <inheritdoc cref="IMyCollection"/>
    public partial class MyCollectionWrapper : IMyCollection, IList, IReadOnlyList<IDerived>
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

        /// <inheritdoc/>
        public int Count => Object.Count;

        /// <inheritdoc cref="IReadOnlyList{IDerived}.this[int]"/>
        public DerivedWrapper this[int index] => Object[index];

        IDerived IReadOnlyList<IDerived>.this[int index] => this[index];

        bool ICollection<IDerived>.IsReadOnly => ((IList)Object).IsReadOnly;

        IDerived IList<IDerived>.this[int index] { get => this[index]; set => ((IList)Object)[index] = ((DerivedWrapper)value).Object; }

        bool ICollection.IsSynchronized => ((ICollection)Object).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection)Object).SyncRoot;

        bool IList.IsFixedSize => ((IList)Object).IsFixedSize;

        bool IList.IsReadOnly => ((IList)Object).IsReadOnly;

        object IList.this[int index] { get => this[index]; set => ((IList)Object)[index] = value is DerivedWrapper o ? o.Object : value; }

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public MyCollectionWrapper(Company.MyCollection @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public MyCollectionWrapper()
            : this(new Company.MyCollection())
        {
        }

        /// <inheritdoc cref="IMyCollection.Add(string)"/>
        public DerivedWrapper Add(string name) => Object.Add(name);

        IDerived IMyCollection.Add(string name) => Add(name);

        /// <inheritdoc cref="IMyCollection.Add(string, DateTime)"/>
        public DerivedWrapper Add(string name, DateTime value) => Object.Add(name, value);

        IDerived IMyCollection.Add(string name, DateTime value) => Add(name, value);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Object.Equals(obj is MyCollectionWrapper o ? o.Object : obj);

        /// <inheritdoc cref="IEnumerable{IDerived}.GetEnumerator()"/>
        public IEnumerator<DerivedWrapper> GetEnumerator()
        {
            foreach (var item in Object)
            {
                yield return (Company.Derived)item;
            }
        }

        IEnumerator<IDerived> IEnumerable<IDerived>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override int GetHashCode() => Object.GetHashCode();

        void ICollection<IDerived>.Add(IDerived item) => ((IList)Object).Add(((DerivedWrapper)item).Object);

        void ICollection<IDerived>.Clear() => ((IList)Object).Clear();

        bool ICollection<IDerived>.Contains(IDerived item) => ((IList)Object).Contains(((DerivedWrapper)item).Object);

        void ICollection<IDerived>.CopyTo(IDerived[] array, int arrayIndex)
        {
            if ((uint)arrayIndex + Count > array.Length)
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

        int IList<IDerived>.IndexOf(IDerived item) => ((IList)Object).IndexOf(((DerivedWrapper)item).Object);

        void IList<IDerived>.Insert(int index, IDerived item) => ((IList)Object).Insert(index, ((DerivedWrapper)item).Object);

        void IList<IDerived>.RemoveAt(int index) => ((IList)Object).RemoveAt(index);

        void ICollection.CopyTo(Array array, int index)
        {
            if ((uint)index + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException("index + Count must be less than or equal to array.Length");
            }

            foreach (var item in this)
            {
                array.SetValue(item, index++);
            }
        }

        int IList.Add(object value) => ((IList)Object).Add(value is DerivedWrapper o ? o.Object : value);

        void IList.Clear() => ((IList)Object).Clear();

        bool IList.Contains(object value) => ((IList)Object).Contains(value is DerivedWrapper o ? o.Object : value);

        int IList.IndexOf(object value) => ((IList)Object).IndexOf(value is DerivedWrapper o ? o.Object : value);

        void IList.Insert(int index, object value) => ((IList)Object).Insert(index, value is DerivedWrapper o ? o.Object : value);

        void IList.Remove(object value) => ((IList)Object).Remove(value is DerivedWrapper o ? o.Object : value);

        void IList.RemoveAt(int index) => ((IList)Object).RemoveAt(index);
    }
}