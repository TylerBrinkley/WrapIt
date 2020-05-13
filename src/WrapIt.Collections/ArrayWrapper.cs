using System;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public sealed class ArrayWrapper<T, TWrapped, TInterface> : ListWrapperBase<T, TWrapped, TInterface>
        where TWrapped : TInterface
    {
        public static implicit operator ArrayWrapper<T, TWrapped, TInterface>?(T[]? array) => array != null ? new ArrayWrapper<T, TWrapped, TInterface>(array) : null;

        public static implicit operator T[]?(ArrayWrapper<T, TWrapped, TInterface>? arrayWrapper) => arrayWrapper?.ToCollection();

        public static ArrayWrapper<T, TWrapped, TInterface>? Create(T[]? array) => array;

        public static ArrayWrapper<T, TWrapped, TInterface>? Create(IList<TInterface>? list) => list switch
        {
            null => null,
            ArrayWrapper<T, TWrapped, TInterface> v0 => v0,
            _ => new ArrayWrapper<T, TWrapped, TInterface>(list)
        };

        internal new IArrayWrapperInternal InternalWrapper => (IArrayWrapperInternal)base.InternalWrapper;

        public ArrayWrapper(T[] array)
            : base(new StandardArrayWrapperInternal(array ?? throw new ArgumentNullException(nameof(array))))
        {
        }

        public ArrayWrapper(IList<TInterface> list)
            : base(new CastedArrayWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public new T[] ToCollection() => InternalWrapper.ToArray();

        internal interface IArrayWrapperInternal : IListWrapperBaseInternal
        {
            T[] ToArray();
        }

        private sealed class StandardArrayWrapperInternal : StandardCollectionWrapperInternal<T[]>, IArrayWrapperInternal
        {
            public TWrapped this[int index] { get => Conversion<T, TWrapped>.Wrap(Collection[index]); set => Collection[index] = Conversion<T, TWrapped>.Unwrap(value); }

            public StandardArrayWrapperInternal(T[] collection)
                : base(collection)
            {
            }

            public T[] ToArray() => Collection;

            public int IndexOf(TWrapped item) => Array.IndexOf(Collection, Conversion<T, TWrapped>.Unwrap(item));

            public void Insert(int index, TWrapped item) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();

            public override void Add(TWrapped item) => throw new NotSupportedException();

            public override void Clear() => throw new NotSupportedException();

            public override bool Remove(TWrapped item) => throw new NotSupportedException();
        }

        private sealed class CastedArrayWrapperInternal : CastedListWrapperBaseInternal, IArrayWrapperInternal
        {
            public override bool IsReadOnly => true;

            public CastedArrayWrapperInternal(IList<TInterface> collection)
                : base(collection)
            {
            }

            public override ICollection<T> ToCollection() => ToArray();

            public T[] ToArray()
            {
                var array = new T[Count];
                var i = 0;
                foreach (var item in Collection)
                {
                    array[i] = Conversion<T, TWrapped>.Unwrap((TWrapped)item!);
                    ++i;
                }
                return array;
            }

            public override void Insert(int index, TWrapped item) => throw new NotSupportedException();

            public override void RemoveAt(int index) => throw new NotSupportedException();

            public override void Add(TWrapped item) => throw new NotSupportedException();

            public override void Clear() => throw new NotSupportedException();

            public override bool Remove(TWrapped item) => throw new NotSupportedException();
        }
    }
}