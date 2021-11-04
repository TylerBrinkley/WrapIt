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
            ICollection<T> o => CollectionWrapper<T, TWrapped, TInterface>.Create(o),
            IReadOnlyCollection<T> o => ReadOnlyCollectionWrapper<T, TWrapped, TInterface>.Create(o),
            _ => new EnumerableWrapper<T, TWrapped, TInterface>(enumerable)
        };

        public static EnumerableWrapper<T, TWrapped, TInterface>? Create(IEnumerable<TInterface>? enumerable) => enumerable switch
        {
            null => null,
            EnumerableWrapper<T, TWrapped, TInterface> o => o,
            ICollection<TInterface> o => CollectionWrapper<T, TWrapped, TInterface>.Create(o),
            IReadOnlyCollection<TInterface> o => ReadOnlyCollectionWrapper<T, TWrapped, TInterface>.Create(o),
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
            object UnderlyingCollection { get; }
            IEnumerable<T> ToEnumerable();
        }

        internal class StandardEnumerableWrapperInternal<TCollection> : IEnumerableWrapperInternal
            where TCollection : IEnumerable<T>
        {
            protected readonly TCollection Collection;

            object IEnumerableWrapperInternal.UnderlyingCollection => Collection;

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

            object IEnumerableWrapperInternal.UnderlyingCollection => Collection;

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