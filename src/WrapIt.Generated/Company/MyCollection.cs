using System;
using System.Collections;

namespace Company
{
    public class MyCollection : IList
    {
        public Derived this[int index] => default;

        object IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => default;

        bool ICollection.IsSynchronized => default;

        object ICollection.SyncRoot => default;

        bool IList.IsFixedSize => throw new NotImplementedException();

        bool IList.IsReadOnly => throw new NotImplementedException();

        /// <summary>
        /// Adds a name to the collection.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <returns>An item of type <see cref="Derived"/></returns>
        public Derived Add(string name) => default;

        public Derived Add(string name, DateTime value) => default;

        public IEnumerator GetEnumerator() => default;
        int IList.Add(object value) => throw new NotImplementedException();
        void IList.Clear() => throw new NotImplementedException();
        bool IList.Contains(object value) => throw new NotImplementedException();

        void ICollection.CopyTo(Array array, int index) { }

        int IList.IndexOf(object value) => throw new NotImplementedException();
        void IList.Insert(int index, object value) => throw new NotImplementedException();
        void IList.Remove(object value) => throw new NotImplementedException();
        void IList.RemoveAt(int index) => throw new NotImplementedException();
    }
}