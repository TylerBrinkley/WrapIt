﻿using System.Collections.Generic;

namespace WrapIt.Collections
{
    public abstract class ListWrapperBase<T, TWrapped, TInterface> : CollectionWrapper<T, TWrapped, TInterface>, IList<TInterface>, IReadOnlyList<TInterface>
        where TWrapped : TInterface
    {
        internal new IListWrapperBaseInternal InternalWrapper => (IListWrapperBaseInternal)base.InternalWrapper;

        public TWrapped this[int index] { get => InternalWrapper[index]; set => InternalWrapper[index] = value; }

        TInterface IList<TInterface>.this[int index] { get => this[index]; set => this[index] = (TWrapped)value!; }

        TInterface IReadOnlyList<TInterface>.this[int index] => this[index];

        internal ListWrapperBase(IListWrapperBaseInternal internalWrapper)
            : base(internalWrapper)
        {
        }

        public int IndexOf(TWrapped item) => InternalWrapper.IndexOf(item);

        int IList<TInterface>.IndexOf(TInterface item) => IndexOf((TWrapped)item!);

        void IList<TInterface>.Insert(int index, TInterface item) => InternalWrapper.Insert(index, (TWrapped)item!);

        void IList<TInterface>.RemoveAt(int index) => InternalWrapper.RemoveAt(index);

        internal interface IListWrapperBaseInternal : ICollectionWrapperInternal, IList<TWrapped>
        {
        }

        internal abstract class CastedListWrapperBaseInternal : CastedCollectionWrapperInternal<IList<TInterface>>, IListWrapperBaseInternal
        {
            public TWrapped this[int index] { get => (TWrapped)Collection[index]!; set => Collection[index] = value; }

            protected CastedListWrapperBaseInternal(IList<TInterface> collection)
                : base(collection)
            {
            }

            public int IndexOf(TWrapped item) => Collection.IndexOf(item);

            public virtual void Insert(int index, TWrapped item) => Collection.Insert(index, item);

            public virtual void RemoveAt(int index) => Collection.RemoveAt(index);
        }
    }
}