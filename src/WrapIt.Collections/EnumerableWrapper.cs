using System;
using System.Collections;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public class EnumerableWrapper<T, TWrapped, TInterface> : IEnumerable<TInterface>
        where TWrapped : TInterface
    {
        public static EnumerableWrapper<T, TWrapped, TInterface>? Create(IEnumerable<T>? enumerable) => enumerable switch
        {
            null => null,
            ICollection<T> v0 => CollectionWrapper<T, TWrapped, TInterface>.Create(v0),
            IReadOnlyCollection<T> v1 => ReadOnlyCollectionWrapper<T, TWrapped, TInterface>.Create(v1),
            _ => new EnumerableWrapper<T, TWrapped, TInterface>(enumerable)
        };

        public static EnumerableWrapper<T, TWrapped, TInterface>? Create(IEnumerable<TInterface>? enumerable) => enumerable switch
        {
            null => null,
            EnumerableWrapper<T, TWrapped, TInterface> v0 => v0,
            ICollection<TInterface> v1 => CollectionWrapper<T, TWrapped, TInterface>.Create(v1),
            IReadOnlyCollection<TInterface> v2 => ReadOnlyCollectionWrapper<T, TWrapped, TInterface>.Create(v2),
            _ => new EnumerableWrapper<T, TWrapped, TInterface>(enumerable)
        };

        internal IEnumerableWrapperInternal InternalWrapper { get; }

        public EnumerableWrapper(IEnumerable<T> enumerable)
            : this(new StandardEnumerableWrapperInternal<IEnumerable<T>>(enumerable ?? throw new ArgumentNullException(nameof(enumerable))))
        {
        }

        public EnumerableWrapper(IEnumerable<TInterface> enumerable)
            : this(new CastedEnumerableWrapperInternal<IEnumerable<TInterface>>(enumerable ?? throw new ArgumentNullException(nameof(enumerable))))
        {
        }

        internal EnumerableWrapper(IEnumerableWrapperInternal internalWrapper)
        {
            InternalWrapper = internalWrapper;
        }

        public IEnumerable<T> ToCollection() => InternalWrapper.ToEnumerable();

        public IEnumerator<TWrapped> GetEnumerator() => InternalWrapper.GetEnumerator();

        IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator()
        {
            foreach (var item in this)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TInterface>)this).GetEnumerator();

        internal interface IEnumerableWrapperInternal : IEnumerable<TWrapped>
        {
            IEnumerable<T> ToEnumerable();
        }

        internal class StandardEnumerableWrapperInternal<TCollection> : IEnumerableWrapperInternal
            where TCollection : IEnumerable<T>
        {
            protected readonly TCollection Collection;

            public StandardEnumerableWrapperInternal(TCollection enumerable)
            {
                Collection = enumerable;
            }

            public virtual IEnumerable<T> ToEnumerable() => Collection;

            public IEnumerator<TWrapped> GetEnumerator()
            {
                foreach (var item in Collection)
                {
                    yield return Conversion<T, TWrapped>.Wrap(item);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal class CastedEnumerableWrapperInternal<TCollection> : IEnumerableWrapperInternal
            where TCollection : IEnumerable<TInterface>
        {
            protected readonly TCollection Collection;

            public CastedEnumerableWrapperInternal(TCollection collection)
            {
                Collection = collection;
            }

            public virtual IEnumerable<T> ToEnumerable()
            {
                var list = new List<T>();
                foreach (var item in Collection)
                {
                    list.Add(Conversion<T, TWrapped>.Unwrap((TWrapped)item!));
                }
                return list;
            }

            public IEnumerator<TWrapped> GetEnumerator()
            {
                foreach (var item in Collection)
                {
                    yield return (TWrapped)item!;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}