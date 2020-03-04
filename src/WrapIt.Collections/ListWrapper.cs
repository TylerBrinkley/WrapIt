using System;
using System.Collections.Generic;
using System.Linq;

namespace WrapIt.Collections
{
    public sealed class ListWrapper<T, TWrapped, TInterface> : ListWrapperBase<T, TWrapped, TInterface>, IList<TInterface>
        where TWrapped : TInterface
    {
        public static implicit operator ListWrapper<T, TWrapped, TInterface>(List<T> list) => list != null ? new ListWrapper<T, TWrapped, TInterface>(list) : null;

        public static implicit operator List<T>(ListWrapper<T, TWrapped, TInterface> listWrapper) => listWrapper?.ToList();

        public static ListWrapper<T, TWrapped, TInterface> Create(IList<T> list) => list != null ? new ListWrapper<T, TWrapped, TInterface>(list) : null;

        public static ListWrapper<T, TWrapped, TInterface> Create(IList<TInterface> list) => list != null ? new ListWrapper<T, TWrapped, TInterface>(list) : null;

        internal new IListWrapperInternal InternalWrapper => (IListWrapperInternal)base.InternalWrapper;

        public ListWrapper(IList<T> list)
            : base(new StandardListWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public ListWrapper(IList<TInterface> list)
            : base(new CastedListWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public List<T> ToList() => InternalWrapper.ToList();

        public void Insert(int index, TWrapped item) => InternalWrapper.Insert(index, item);

        void IList<TInterface>.Insert(int index, TInterface item) => Insert(index, (TWrapped)item);

        public void RemoveAt(int index) => InternalWrapper.RemoveAt(index);

        internal interface IListWrapperInternal : IListWrapperBaseInternal
        {
            List<T> ToList();
        }

        private sealed class StandardListWrapperInternal : StandardCollectionWrapperInternal, IListWrapperInternal
        {
            private readonly IList<T> _list;

            public TWrapped this[int index] { get => Conversion<T, TWrapped>.Wrap(_list[index]); set => _list[index] = Conversion<T, TWrapped>.Unwrap(value); }

            public StandardListWrapperInternal(IList<T> list)
                : base(list)
            {
                _list = list;
            }

            public List<T> ToList() => _list as List<T> ?? _list.ToList();

            public int IndexOf(TWrapped item) => _list.IndexOf(Conversion<T, TWrapped>.Unwrap(item));

            public void Insert(int index, TWrapped item) => _list.Insert(index, Conversion<T, TWrapped>.Unwrap(item));

            public void RemoveAt(int index) => _list.RemoveAt(index);
        }

        private sealed class CastedListWrapperInternal : CastedCollectionWrapperInternal, IListWrapperInternal
        {
            private readonly IList<TInterface> _list;

            public TWrapped this[int index] { get => (TWrapped)_list[index]; set => _list[index] = value; }

            public CastedListWrapperInternal(IList<TInterface> list)
                : base(list)
            {
                _list = list;
            }

            public List<T> ToList() => (List<T>)ToCollection();

            public int IndexOf(TWrapped item) => _list.IndexOf(item);

            public void Insert(int index, TWrapped item) => _list.Insert(index, item);

            public void RemoveAt(int index) => _list.RemoveAt(index);
        }
    }
}