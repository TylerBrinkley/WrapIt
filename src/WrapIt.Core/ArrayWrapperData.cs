using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class ArrayWrapperData : TypeData
    {
        public ArrayWrapperData()
            : base(typeof(ArrayWrapperData), new TypeName("WrapIt.Collections", "ArrayWrapper"))
        {
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            if (builder.BuildCollectionWrappersAsNecessary)
            {
                BuildStatus = TypeBuildStatus.Building;
                using (var writer = await writerProvider(typeof(Array), "WrapIt.Collections.ArrayWrapper", cancellationToken).ConfigureAwait(false))
                {
                    await writer.WriteAsync(@"using System;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public sealed class ArrayWrapper<T, TWrapped, TInterface> : ListWrapperBase<T, TWrapped, TInterface>
        where TWrapped : TInterface
    {
        public static implicit operator ArrayWrapper<T, TWrapped, TInterface>(T[] array) => array != null ? new ArrayWrapper<T, TWrapped, TInterface>(array) : null;

        public static implicit operator T[](ArrayWrapper<T, TWrapped, TInterface> arrayWrapper) => arrayWrapper?.ToArray();

        public static ArrayWrapper<T, TWrapped, TInterface> Create(T[] array) => array;

        public static ArrayWrapper<T, TWrapped, TInterface> Create(IList<TInterface> list) => list != null ? new ArrayWrapper<T, TWrapped, TInterface>(list) : null;

        internal new IArrayWrapperInternal InternalWrapper => (IArrayWrapperInternal)base.InternalWrapper;

        public ArrayWrapper(T[] array)
            : base(new StandardArrayWrapperInternal(array ?? throw new ArgumentNullException(nameof(array))))
        {
        }

        public ArrayWrapper(IList<TInterface> list)
            : base(new CastedArrayWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public T[] ToArray() => InternalWrapper.ToArray();

        internal interface IArrayWrapperInternal : IListWrapperBaseInternal
        {
            T[] ToArray();
        }

        private sealed class StandardArrayWrapperInternal : StandardCollectionWrapperInternal, IArrayWrapperInternal
        {
            private readonly T[] _array;

            public TWrapped this[int index] { get => Conversion<T, TWrapped>.Wrap(_array[index]); set => _array[index] = Conversion<T, TWrapped>.Unwrap(value); }

            public StandardArrayWrapperInternal(T[] array)
                : base(array)
            {
                _array = array;
            }

            public T[] ToArray() => _array;

            public int IndexOf(TWrapped item) => Array.IndexOf(_array, Conversion<T, TWrapped>.Unwrap(item));

            public void Insert(int index, TWrapped item) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();
        }

        private sealed class CastedArrayWrapperInternal : CastedCollectionWrapperInternal, IArrayWrapperInternal
        {
            private readonly IList<TInterface> _list;

            public TWrapped this[int index] { get => (TWrapped)_list[index]; set => _list[index] = value; }

            public override bool IsReadOnly => true;

            public CastedArrayWrapperInternal(IList<TInterface> list)
                : base(list)
            {
                _list = list;
            }

            public override ICollection<T> ToCollection() => ToArray();

            public T[] ToArray()
            {
                var array = new T[Count];
                var i = 0;
                foreach (var item in _list)
                {
                    array[i] = Conversion<T, TWrapped>.Unwrap((TWrapped)item);
                    ++i;
                }
                return array;
            }

            public int IndexOf(TWrapped item) => _list.IndexOf(item);

            public void Insert(int index, TWrapped item) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();

            public override void Add(TWrapped item) => throw new NotSupportedException();

            public override void Clear() => throw new NotSupportedException();

            public override bool Remove(TWrapped item) => throw new NotSupportedException();
        }
    }
}").ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            BuildStatus = TypeBuildStatus.Built;

            var listWrapperBaseData = builder.GetTypeData(typeof(ListWrapperBaseData), typeDatas);
            if (listWrapperBaseData.BuildStatus != TypeBuildStatus.Built)
            {
                await listWrapperBaseData.BuildAsync(builder, typeDatas, writerProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}