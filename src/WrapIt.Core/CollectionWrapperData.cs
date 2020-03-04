using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class CollectionWrapperData : TypeData
    {
        public CollectionWrapperData()
            : base(typeof(CollectionWrapperData), new TypeName("WrapIt.Collections", "CollectionWrapper"))
        {
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            if (builder.BuildCollectionWrappersAsNecessary)
            {
                BuildStatus = TypeBuildStatus.Building;
                using (var writer = await writerProvider(typeof(Array), "WrapIt.Collections.CollectionWrapper", cancellationToken).ConfigureAwait(false))
                {
                    await writer.WriteAsync(@"using System;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public class CollectionWrapper<T, TWrapped, TInterface> : EnumerableWrapper<T, TWrapped, TInterface>, ICollection<TInterface>, IReadOnlyCollection<TInterface>
        where TWrapped : TInterface
    {
        public static CollectionWrapper<T, TWrapped, TInterface> Create(ICollection<T> collection) => collection != null ? new CollectionWrapper<T, TWrapped, TInterface>(collection) : null;

        public static CollectionWrapper<T, TWrapped, TInterface> Create(ICollection<TInterface> collection) => collection != null ? new CollectionWrapper<T, TWrapped, TInterface>(collection) : null;

        internal new ICollectionWrapperInternal InternalWrapper => (ICollectionWrapperInternal)base.InternalWrapper;

        public int Count => InternalWrapper.Count;

        bool ICollection<TInterface>.IsReadOnly => InternalWrapper.IsReadOnly;

        public CollectionWrapper(ICollection<T> collection)
            : base(new StandardCollectionWrapperInternal(collection ?? throw new ArgumentNullException(nameof(collection))))
        {
        }

        public CollectionWrapper(ICollection<TInterface> collection)
            : base(new CastedCollectionWrapperInternal(collection ?? throw new ArgumentNullException(nameof(collection))))
        {
        }

        internal CollectionWrapper(ICollectionWrapperInternal internalWrapper)
            : base(internalWrapper)
        {
        }

        public ICollection<T> ToCollection() => InternalWrapper.ToCollection();

        IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator()
        {
            foreach (var item in this)
            {
                yield return item;
            }
        }

        public bool Contains(TWrapped item) => InternalWrapper.Contains(item);

        bool ICollection<TInterface>.Contains(TInterface item) => Contains((TWrapped)item);

        void ICollection<TInterface>.CopyTo(TInterface[] array, int arrayIndex)
        {
            if (arrayIndex + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException(""arrayIndex + Count must be less than or equal to array.Length"");
            }

                foreach (var item in this)
                {
                    array[arrayIndex++] = item;
                }
            }

            void ICollection<TInterface>.Add(TInterface item) => InternalWrapper.Add((TWrapped) item);

            void ICollection<TInterface>.Clear() => InternalWrapper.Clear();

            bool ICollection<TInterface>.Remove(TInterface item) => InternalWrapper.Remove((TWrapped) item);

        internal interface ICollectionWrapperInternal : IEnumerableWrapperInternal, ICollection<TWrapped>
        {
            ICollection<T> ToCollection();
        }

        internal class StandardCollectionWrapperInternal : StandardEnumerableWrapperInternal, ICollectionWrapperInternal
        {
            private readonly ICollection<T> _collection;

            public int Count => _collection.Count;

            public virtual bool IsReadOnly => _collection.IsReadOnly;

            public StandardCollectionWrapperInternal(ICollection<T> collection)
                : base(collection)
            {
                _collection = collection;
            }

            public virtual ICollection<T> ToCollection() => _collection;

            public bool Contains(TWrapped item) => _collection.Contains(Conversion<T, TWrapped>.Unwrap(item));

            public virtual void Add(TWrapped item) => _collection.Add(Conversion<T, TWrapped>.Unwrap(item));

            public virtual void Clear() => _collection.Clear();

            public virtual bool Remove(TWrapped item) => _collection.Remove(Conversion<T, TWrapped>.Unwrap(item));

            void ICollection<TWrapped>.CopyTo(TWrapped[] array, int arrayIndex) => throw new NotSupportedException();
        }

        internal class CastedCollectionWrapperInternal : CastedEnumerableWrapperInternal, ICollectionWrapperInternal
        {
            private readonly ICollection<TInterface> _collection;

            public int Count => _collection.Count;

            public virtual bool IsReadOnly => _collection.IsReadOnly;

            public CastedCollectionWrapperInternal(ICollection<TInterface> collection)
                : base(collection)
            {
                _collection = collection;
            }

            public override IEnumerable<T> ToEnumerable() => ToCollection();

            public virtual ICollection<T> ToCollection()
            {
                var list = new List<T>(Count);
                foreach (var item in _collection)
                {
                    list.Add(Conversion<T, TWrapped>.Unwrap((TWrapped)item));
                }
                return list;
            }

            public bool Contains(TWrapped item) => _collection.Contains(item);

            public virtual void Add(TWrapped item) => _collection.Add(item);

            public virtual void Clear() => _collection.Clear();

            public virtual bool Remove(TWrapped item) => _collection.Remove(item);

            void ICollection<TWrapped>.CopyTo(TWrapped[] array, int arrayIndex) => throw new NotSupportedException();
        }
    }
}").ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            BuildStatus = TypeBuildStatus.Built;

            var enumerableWrapperData = builder.GetTypeData(typeof(EnumerableWrapperData), typeDatas);
            if (enumerableWrapperData.BuildStatus != TypeBuildStatus.Built)
            {
                await enumerableWrapperData.BuildAsync(builder, typeDatas, writerProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}