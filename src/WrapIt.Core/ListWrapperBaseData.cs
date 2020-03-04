using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class ListWrapperBaseData : TypeData
    {
        public ListWrapperBaseData()
            : base(typeof(ListWrapperBaseData), new TypeName("WrapIt.Collections", "ListWrapperBase"))
        {
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            if (builder.BuildCollectionWrappersAsNecessary)
            {
                BuildStatus = TypeBuildStatus.Building;
                using (var writer = await writerProvider(typeof(Array), "WrapIt.Collections.ListWrapperBase", cancellationToken).ConfigureAwait(false))
                {
                    await writer.WriteAsync(@"using System.Collections.Generic;

namespace WrapIt.Collections
{
    public abstract class ListWrapperBase<T, TWrapped, TInterface> : CollectionWrapper<T, TWrapped, TInterface>, IList<TInterface>, IReadOnlyList<TInterface>
        where TWrapped : TInterface
    {
        internal new IListWrapperBaseInternal InternalWrapper => (IListWrapperBaseInternal)base.InternalWrapper;

        public TWrapped this[int index] { get => InternalWrapper[index]; set => InternalWrapper[index] = value; }

        TInterface IList<TInterface>.this[int index] { get => this[index]; set => this[index] = (TWrapped)value; }

        TInterface IReadOnlyList<TInterface>.this[int index] => this[index];

        internal ListWrapperBase(IListWrapperBaseInternal internalWrapper)
            : base(internalWrapper)
        {
        }

        public int IndexOf(TWrapped item) => InternalWrapper.IndexOf(item);

        int IList<TInterface>.IndexOf(TInterface item) => IndexOf((TWrapped)item);

        void IList<TInterface>.Insert(int index, TInterface item) => InternalWrapper.Insert(index, (TWrapped)item);

        void IList<TInterface>.RemoveAt(int index) => InternalWrapper.RemoveAt(index);

        internal interface IListWrapperBaseInternal : ICollectionWrapperInternal, IList<TWrapped>
        {
        }
    }
}").ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            BuildStatus = TypeBuildStatus.Built;

            var collectionWrapperData = builder.GetTypeData(typeof(CollectionWrapperData), typeDatas);
            if (collectionWrapperData.BuildStatus != TypeBuildStatus.Built)
            {
                await collectionWrapperData.BuildAsync(builder, typeDatas, writerProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}