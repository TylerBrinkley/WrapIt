using System;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public sealed class ReadOnlyListWrapper<T, TWrapped, TInterface> : ReadOnlyCollectionWrapper<T, TWrapped, TInterface>, IReadOnlyList<TInterface>
        where TWrapped : TInterface
    {
        public static ReadOnlyListWrapper<T, TWrapped, TInterface>? Create(IReadOnlyList<T>? list) => list != null ? new ReadOnlyListWrapper<T, TWrapped, TInterface>(list) : null;

        public static ReadOnlyListWrapper<T, TWrapped, TInterface>? Create(IReadOnlyList<TInterface>? list) => list switch
        {
            null => null,
            ReadOnlyListWrapper<T, TWrapped, TInterface> v0 => v0,
            _ => new ReadOnlyListWrapper<T, TWrapped, TInterface>(list)
        };

        internal new IReadOnlyListWrapperInternal InternalWrapper => (IReadOnlyListWrapperInternal)base.InternalWrapper;

        public TInterface this[int index] => InternalWrapper[index];

        public ReadOnlyListWrapper(IReadOnlyList<T> list)
            : base(new StandardReadOnlyListWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public ReadOnlyListWrapper(IReadOnlyList<TInterface> list)
            : base(new CastedReadOnlyListWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public new IReadOnlyList<T> ToCollection() => InternalWrapper.ToList();

        internal interface IReadOnlyListWrapperInternal : IReadOnlyCollectionWrapperInternal, IReadOnlyList<TWrapped>
        {
            IReadOnlyList<T> ToList();
        }

        private sealed class StandardReadOnlyListWrapperInternal : StandardReadOnlyCollectionWrapperInternal<IReadOnlyList<T>>, IReadOnlyListWrapperInternal
        {
            public TWrapped this[int index] => Conversion<T, TWrapped>.Wrap(Collection[index]);

            public StandardReadOnlyListWrapperInternal(IReadOnlyList<T> collection)
                : base(collection)
            {
            }

            public IReadOnlyList<T> ToList() => Collection;
        }

        private sealed class CastedReadOnlyListWrapperInternal : CastedReadOnlyCollectionWrapperInternal<IReadOnlyList<TInterface>>, IReadOnlyListWrapperInternal
        {
            public TWrapped this[int index] => (TWrapped)Collection[index]!;

            public CastedReadOnlyListWrapperInternal(IReadOnlyList<TInterface> collection)
                : base(collection)
            {
            }

            public IReadOnlyList<T> ToList() => (IReadOnlyList<T>)ToCollection();
        }
    }
}