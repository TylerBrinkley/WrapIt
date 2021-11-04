using System;
using System.Collections.Generic;
using System.Linq;

namespace WrapIt.Collections
{
    public sealed class ListWrapper<T, TWrapped, TInterface> : ListWrapperBase<T, TWrapped, TInterface>, IList<TInterface>
        where TWrapped : TInterface
    {
        public static implicit operator ListWrapper<T, TWrapped, TInterface>?(List<T>? list) => list != null ? new ListWrapper<T, TWrapped, TInterface>(list) : null;

        public static implicit operator List<T>?(ListWrapper<T, TWrapped, TInterface>? listWrapper) => listWrapper?.ToCollection();

        public static ListWrapper<T, TWrapped, TInterface>? Create(IList<T>? list) => list != null ? new ListWrapper<T, TWrapped, TInterface>(list) : null;

        public static ListWrapper<T, TWrapped, TInterface>? Create(IList<TInterface>? list) => list switch
        {
            null => null,
            ListWrapper<T, TWrapped, TInterface> o => o,
            _ => new ListWrapper<T, TWrapped, TInterface>(list)
        };

        internal new IListWrapperInternal InternalWrapper => (IListWrapperInternal)base.InternalWrapper;

        public ListWrapper(IList<T> list)
            : base(new StandardListWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public ListWrapper(IList<TInterface> list)
            : base(new CastedListWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public new List<T> ToCollection() => InternalWrapper.ToList();

        public void Add(TWrapped item) => InternalWrapper.Add(item);

        public void Clear() => InternalWrapper.Clear();

        public bool Remove(TWrapped item) => InternalWrapper.Remove(item);

        public void Insert(int index, TWrapped item) => InternalWrapper.Insert(index, item);

        void IList<TInterface>.Insert(int index, TInterface item) => Insert(index, (TWrapped)item!);

        public void RemoveAt(int index) => InternalWrapper.RemoveAt(index);

        internal interface IListWrapperInternal : IListWrapperBaseInternal
        {
            List<T> ToList();
        }

        private sealed class StandardListWrapperInternal : StandardCollectionWrapperInternal<IList<T>>, IListWrapperInternal
        {
            public TWrapped this[int index] { get => Conversion<T, TWrapped>.Wrap(Collection[index]); set => Collection[index] = Conversion<T, TWrapped>.Unwrap(value); }

            public StandardListWrapperInternal(IList<T> collection)
                : base(collection)
            {
            }

            public List<T> ToList() => Collection as List<T> ?? Collection.ToList();

            public int IndexOf(TWrapped item) => Collection.IndexOf(Conversion<T, TWrapped>.Unwrap(item));

            public void Insert(int index, TWrapped item) => Collection.Insert(index, Conversion<T, TWrapped>.Unwrap(item));

            public void RemoveAt(int index) => Collection.RemoveAt(index);
        }

        private sealed class CastedListWrapperInternal : CastedListWrapperBaseInternal, IListWrapperInternal
        {
            public CastedListWrapperInternal(IList<TInterface> collection)
                : base(collection)
            {
            }

            public List<T> ToList() => (List<T>)ToCollection();
        }
    }
}