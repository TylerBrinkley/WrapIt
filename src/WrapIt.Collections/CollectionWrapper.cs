using System;
using System.Collections;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public class CollectionWrapper<T, TWrapped, TInterface> : EnumerableWrapper<T, TWrapped, TInterface>, ICollection<TInterface>, IReadOnlyCollection<TInterface>, ICollection
        where TWrapped : TInterface
    {
        public static CollectionWrapper<T, TWrapped, TInterface>? Create(ICollection<T>? collection) => collection switch
        {
            null => null,
            T[] o => ArrayWrapper<T, TWrapped, TInterface>.Create(o),
            IList<T> o => ListWrapper<T, TWrapped, TInterface>.Create(o),
            ISet<T> o => SetWrapper<T, TWrapped, TInterface>.Create(o),
            _ => new CollectionWrapper<T, TWrapped, TInterface>(collection)
        };

        public static CollectionWrapper<T, TWrapped, TInterface>? Create(ICollection<TInterface>? collection) => collection switch
        {
            null => null,
            CollectionWrapper<T, TWrapped, TInterface> o => o,
            TInterface[] o => ArrayWrapper<T, TWrapped, TInterface>.Create(o),
            IList<TInterface> o => ListWrapper<T, TWrapped, TInterface>.Create(o),
            ISet<TInterface> o => SetWrapper<T, TWrapped, TInterface>.Create(o),
            _ => new CollectionWrapper<T, TWrapped, TInterface>(collection)
        };

        internal new ICollectionWrapperInternal InternalWrapper => (ICollectionWrapperInternal)base.InternalWrapper;

        public int Count => InternalWrapper.Count;

        bool ICollection<TInterface>.IsReadOnly => InternalWrapper.IsReadOnly;

        object? ICollection.SyncRoot => InternalWrapper.UnderlyingCollection is ICollection c ? c.SyncRoot : null;

        bool ICollection.IsSynchronized => InternalWrapper.UnderlyingCollection is ICollection c && c.IsSynchronized;

        public CollectionWrapper(ICollection<T> collection)
            : base(new StandardCollectionWrapperInternal<ICollection<T>>(collection ?? throw new ArgumentNullException(nameof(collection))))
        {
        }

        public CollectionWrapper(ICollection<TInterface> collection)
            : base(new CastedCollectionWrapperInternal<ICollection<TInterface>>(collection ?? throw new ArgumentNullException(nameof(collection))))
        {
        }

        internal CollectionWrapper(ICollectionWrapperInternal internalWrapper)
            : base(internalWrapper)
        {
        }

        public new ICollection<T> ToCollection() => InternalWrapper.ToCollection();

        public bool Contains(TWrapped item) => InternalWrapper.Contains(item);

        bool ICollection<TInterface>.Contains(TInterface item) => Contains((TWrapped)item!);

        void ICollection<TInterface>.CopyTo(TInterface[] array, int arrayIndex)
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

        void ICollection<TInterface>.Add(TInterface item) => InternalWrapper.Add((TWrapped)item!);

        void ICollection<TInterface>.Clear() => InternalWrapper.Clear();

        bool ICollection<TInterface>.Remove(TInterface item) => InternalWrapper.Remove((TWrapped)item!);

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

        internal interface ICollectionWrapperInternal : IEnumerableWrapperInternal, ICollection<TWrapped>
        {
            ICollection<T> ToCollection();
        }

        internal class StandardCollectionWrapperInternal<TCollection> : StandardEnumerableWrapperInternal<TCollection>, ICollectionWrapperInternal
            where TCollection : ICollection<T>
        {
            public int Count => Collection.Count;

            public virtual bool IsReadOnly => Collection.IsReadOnly;

            public object? SyncRoot => Collection is ICollection c ? c.SyncRoot : null;

            public bool IsSynchronized => Collection is ICollection c && c.IsSynchronized;

            public StandardCollectionWrapperInternal(TCollection collection)
                : base(collection)
            {
            }

            public virtual ICollection<T> ToCollection() => Collection;

            public bool Contains(TWrapped item) => Collection.Contains(Conversion<T, TWrapped>.Unwrap(item));

            public virtual void Add(TWrapped item) => Collection.Add(Conversion<T, TWrapped>.Unwrap(item));

            public virtual void Clear() => Collection.Clear();

            public virtual bool Remove(TWrapped item) => Collection.Remove(Conversion<T, TWrapped>.Unwrap(item));

            void ICollection<TWrapped>.CopyTo(TWrapped[] array, int arrayIndex) => throw new NotSupportedException();
        }

        internal class CastedCollectionWrapperInternal<TCollection> : CastedEnumerableWrapperInternal<TCollection>, ICollectionWrapperInternal
            where TCollection : ICollection<TInterface>
        {
            public int Count => Collection.Count;

            public virtual bool IsReadOnly => Collection.IsReadOnly;

            public object? SyncRoot => Collection is ICollection c ? c.SyncRoot : null;

            public bool IsSynchronized => Collection is ICollection c && c.IsSynchronized;

            public CastedCollectionWrapperInternal(TCollection collection)
                : base(collection)
            {
            }

            public override IEnumerable<T> ToEnumerable() => ToCollection();

            public virtual ICollection<T> ToCollection() => (ICollection<T>)base.ToEnumerable();

            public bool Contains(TWrapped item) => Collection.Contains(item);

            public virtual void Add(TWrapped item) => Collection.Add(item);

            public virtual void Clear() => Collection.Clear();

            public virtual bool Remove(TWrapped item) => Collection.Remove(item);

            void ICollection<TWrapped>.CopyTo(TWrapped[] array, int arrayIndex) => throw new NotSupportedException();
        }
    }
}