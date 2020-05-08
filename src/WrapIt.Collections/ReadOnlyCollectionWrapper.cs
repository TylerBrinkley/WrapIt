using System;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public class ReadOnlyCollectionWrapper<T, TWrapped, TInterface> : EnumerableWrapper<T, TWrapped, TInterface>, IReadOnlyCollection<TInterface>
        where TWrapped : TInterface
    {
        public static ReadOnlyCollectionWrapper<T, TWrapped, TInterface> Create(IReadOnlyCollection<T> collection) => collection switch
        {
            null => null,
            IReadOnlyList<T> v0 => ReadOnlyListWrapper<T, TWrapped, TInterface>.Create(v0),
            _ => new ReadOnlyCollectionWrapper<T, TWrapped, TInterface>(collection)
        };

        public static ReadOnlyCollectionWrapper<T, TWrapped, TInterface> Create(IReadOnlyCollection<TInterface> collection) => collection switch
        {
            null => null,
            IReadOnlyList<TInterface> v0 => ReadOnlyListWrapper<T, TWrapped, TInterface>.Create(v0),
            _ => new ReadOnlyCollectionWrapper<T, TWrapped, TInterface>(collection)
        };

        internal new IReadOnlyCollectionWrapperInternal InternalWrapper => (IReadOnlyCollectionWrapperInternal)base.InternalWrapper;

        public int Count => InternalWrapper.Count;

        public ReadOnlyCollectionWrapper(IReadOnlyCollection<T> collection)
            : base(new StandardReadOnlyCollectionWrapperInternal<IReadOnlyCollection<T>>(collection ?? throw new ArgumentNullException(nameof(collection))))
        {
        }

        public ReadOnlyCollectionWrapper(IReadOnlyCollection<TInterface> collection)
            : base(new CastedReadOnlyCollectionWrapperInternal<IReadOnlyCollection<TInterface>>(collection ?? throw new ArgumentNullException(nameof(collection))))
        {
        }

        internal ReadOnlyCollectionWrapper(IReadOnlyCollectionWrapperInternal internalWrapper)
            : base(internalWrapper)
        {
        }

        internal interface IReadOnlyCollectionWrapperInternal : IEnumerableWrapperInternal, IReadOnlyCollection<TWrapped>
        {
        }

        internal class StandardReadOnlyCollectionWrapperInternal<TCollection> : StandardEnumerableWrapperInternal<TCollection>, IReadOnlyCollectionWrapperInternal
            where TCollection : IReadOnlyCollection<T>
        {
            public int Count => Collection.Count;

            public StandardReadOnlyCollectionWrapperInternal(TCollection collection)
                : base(collection)
            {
            }

            public virtual IReadOnlyCollection<T> ToCollection() => Collection;
        }

        internal class CastedReadOnlyCollectionWrapperInternal<TCollection> : CastedEnumerableWrapperInternal<TCollection>, IReadOnlyCollectionWrapperInternal
            where TCollection : IReadOnlyCollection<TInterface>
        {
            public int Count => Collection.Count;

            public CastedReadOnlyCollectionWrapperInternal(TCollection collection)
                : base(collection)
            {
            }

            public override IEnumerable<T> ToEnumerable() => ToCollection();

            public virtual ICollection<T> ToCollection()
            {
                var list = new List<T>(Count);
                foreach (var item in Collection)
                {
                    list.Add(Conversion<T, TWrapped>.Unwrap((TWrapped)item));
                }
                return list;
            }
        }
    }
}