using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class EnumerableWrapperData : TypeData
    {
        public EnumerableWrapperData()
            : base(typeof(EnumerableWrapperData), new TypeName("WrapIt.Collections", "EnumerableWrapper"))
        {
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            if (builder.BuildCollectionWrappersAsNecessary)
            {
                BuildStatus = TypeBuildStatus.Building;
                using (var writer = await writerProvider(typeof(Array), "WrapIt.Collections.EnumerableWrapper", cancellationToken).ConfigureAwait(false))
                {
                    await writer.WriteAsync(@"using System;
using System.Collections;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    internal static class Conversion<T, TWrapped>
    {
        public static readonly Func<T, TWrapped> Wrap = (Func<T, TWrapped>)Delegate.CreateDelegate(typeof(Func<T, TWrapped>), typeof(TWrapped).GetMethod(""op_Implicit"", new[] { typeof(T) }));

        public static readonly Func<TWrapped, T> Unwrap = (Func<TWrapped, T>)Delegate.CreateDelegate(typeof(Func<TWrapped, T>), typeof(TWrapped).GetMethod(""op_Implicit"", new[] { typeof(TWrapped) }));
    }

    public class EnumerableWrapper<T, TWrapped, TInterface> : IEnumerable<TInterface>
        where TWrapped : TInterface
    {
        public static EnumerableWrapper<T, TWrapped, TInterface> Create(IEnumerable<T> enumerable) => enumerable != null ? new EnumerableWrapper<T, TWrapped, TInterface>(enumerable) : null;

        public static EnumerableWrapper<T, TWrapped, TInterface> Create(IEnumerable<TInterface> enumerable) => enumerable != null ? new EnumerableWrapper<T, TWrapped, TInterface>(enumerable) : null;

        internal IEnumerableWrapperInternal InternalWrapper { get; }

        public EnumerableWrapper(IEnumerable<T> enumerable)
            : this(new StandardEnumerableWrapperInternal(enumerable ?? throw new ArgumentNullException(nameof(enumerable))))
        {
        }

        public EnumerableWrapper(IEnumerable<TInterface> enumerable)
            : this(new CastedEnumerableWrapperInternal(enumerable ?? throw new ArgumentNullException(nameof(enumerable))))
        {
        }

        internal EnumerableWrapper(IEnumerableWrapperInternal internalWrapper)
        {
            InternalWrapper = internalWrapper;
        }

        public IEnumerable<T> ToEnumerable() => InternalWrapper.ToEnumerable();

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

        internal class StandardEnumerableWrapperInternal : IEnumerableWrapperInternal
        {
            private readonly IEnumerable<T> _enumerable;

            public StandardEnumerableWrapperInternal(IEnumerable<T> enumerable)
            {
                _enumerable = enumerable;
            }

            public virtual IEnumerable<T> ToEnumerable() => _enumerable;

            public IEnumerator<TWrapped> GetEnumerator()
            {
                foreach (var item in _enumerable)
                {
                    yield return Conversion<T, TWrapped>.Wrap(item);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal class CastedEnumerableWrapperInternal : IEnumerableWrapperInternal
        {
            private readonly IEnumerable<TInterface> _enumerable;

            public CastedEnumerableWrapperInternal(IEnumerable<TInterface> enumerable)
            {
                _enumerable = enumerable;
            }

            public virtual IEnumerable<T> ToEnumerable()
            {
                var list = new List<T>();
                foreach (var item in _enumerable)
                {
                    list.Add(Conversion<T, TWrapped>.Unwrap((TWrapped)item));
                }
                return list;
            }

            public IEnumerator<TWrapped> GetEnumerator()
            {
                foreach (var item in _enumerable)
                {
                    yield return (TWrapped)item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}").ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            BuildStatus = TypeBuildStatus.Built;
        }
    }
}