using System;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public sealed class SetWrapper<T, TWrapped, TInterface> : CollectionWrapper<T, TWrapped, TInterface>, ISet<TInterface>
        where TWrapped : TInterface
    {
        public static implicit operator SetWrapper<T, TWrapped, TInterface>?(HashSet<T>? set) => set != null ? new SetWrapper<T, TWrapped, TInterface>(set) : null;

        public static implicit operator HashSet<T>?(SetWrapper<T, TWrapped, TInterface>? setWrapper) => setWrapper?.ToCollection();

        public static SetWrapper<T, TWrapped, TInterface>? Create(ISet<T>? list) => list != null ? new SetWrapper<T, TWrapped, TInterface>(list) : null;

        public static SetWrapper<T, TWrapped, TInterface>? Create(ISet<TInterface>? list) => list switch
        {
            null => null,
            SetWrapper<T, TWrapped, TInterface> v0 => v0,
            _ => new SetWrapper<T, TWrapped, TInterface>(list)
        };

        internal new ISetWrapperInternal InternalWrapper => (ISetWrapperInternal)base.InternalWrapper;

        public SetWrapper(ISet<T> list)
            : base(new StandardSetWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public SetWrapper(ISet<TInterface> list)
            : base(new CastedSetWrapperInternal(list ?? throw new ArgumentNullException(nameof(list))))
        {
        }

        public new HashSet<T> ToCollection() => InternalWrapper.ToSet();

        public bool Add(TWrapped item) => InternalWrapper.Add(item);

        bool ISet<TInterface>.Add(TInterface item) => Add((TWrapped)item!);

        public void UnionWith(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.UnionWith(other);

        void ISet<TInterface>.UnionWith(IEnumerable<TInterface> other) => UnionWith(Create(other)!);

        public void IntersectWith(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.IntersectWith(other);

        void ISet<TInterface>.IntersectWith(IEnumerable<TInterface> other) => IntersectWith(Create(other)!);

        public void ExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.ExceptWith(other);

        void ISet<TInterface>.ExceptWith(IEnumerable<TInterface> other) => ExceptWith(Create(other)!);

        public void SymmetricExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.SymmetricExceptWith(other);

        void ISet<TInterface>.SymmetricExceptWith(IEnumerable<TInterface> other) => SymmetricExceptWith(Create(other)!);

        public bool IsSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.IsSubsetOf(other);

        bool ISet<TInterface>.IsSubsetOf(IEnumerable<TInterface> other) => IsSubsetOf(Create(other)!);

        public bool IsSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.IsSupersetOf(other);

        bool ISet<TInterface>.IsSupersetOf(IEnumerable<TInterface> other) => IsSupersetOf(Create(other)!);

        public bool IsProperSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.IsProperSupersetOf(other);

        bool ISet<TInterface>.IsProperSupersetOf(IEnumerable<TInterface> other) => IsProperSupersetOf(Create(other)!);

        public bool IsProperSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.IsProperSubsetOf(other);

        bool ISet<TInterface>.IsProperSubsetOf(IEnumerable<TInterface> other) => IsProperSubsetOf(Create(other)!);

        public bool Overlaps(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.Overlaps(other);

        bool ISet<TInterface>.Overlaps(IEnumerable<TInterface> other) => Overlaps(Create(other)!);

        public bool SetEquals(EnumerableWrapper<T, TWrapped, TInterface> other) => InternalWrapper.SetEquals(other);

        bool ISet<TInterface>.SetEquals(IEnumerable<TInterface> other) => SetEquals(Create(other)!);

        public void Clear() => InternalWrapper.Clear();

        public bool Remove(TWrapped item) => InternalWrapper.Remove(item);

        internal interface ISetWrapperInternal : ICollectionWrapperInternal
        {
            new bool Add(TWrapped item);
            void ExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other);
            void IntersectWith(EnumerableWrapper<T, TWrapped, TInterface> other);
            bool IsProperSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other);
            bool IsProperSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other);
            bool IsSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other);
            bool IsSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other);
            bool Overlaps(EnumerableWrapper<T, TWrapped, TInterface> other);
            bool SetEquals(EnumerableWrapper<T, TWrapped, TInterface> other);
            void SymmetricExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other);
            HashSet<T> ToSet();
            void UnionWith(EnumerableWrapper<T, TWrapped, TInterface> other);
        }

        private sealed class StandardSetWrapperInternal : StandardCollectionWrapperInternal<ISet<T>>, ISetWrapperInternal
        {
            public StandardSetWrapperInternal(ISet<T> collection)
                : base(collection)
            {
            }

            public new bool Add(TWrapped item) => Collection.Add(Conversion<T, TWrapped>.Unwrap(item));

            public void ExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.ExceptWith(other.ToCollection());

            public void IntersectWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IntersectWith(other.ToCollection());

            public bool IsProperSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsProperSubsetOf(other.ToCollection());

            public bool IsProperSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsProperSupersetOf(other.ToCollection());

            public bool IsSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsSubsetOf(other.ToCollection());

            public bool IsSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsSupersetOf(other.ToCollection());

            public bool Overlaps(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.Overlaps(other.ToCollection());

            public bool SetEquals(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.SetEquals(other.ToCollection());

            public void SymmetricExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.SymmetricExceptWith(other.ToCollection());

            public HashSet<T> ToSet() => Collection as HashSet<T> ?? new HashSet<T>(Collection);

            public void UnionWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.UnionWith(other.ToCollection());
        }

        private sealed class CastedSetWrapperInternal : CastedCollectionWrapperInternal<ISet<TInterface>>, ISetWrapperInternal
        {
            public CastedSetWrapperInternal(ISet<TInterface> collection)
                : base(collection)
            {
            }

            public new bool Add(TWrapped item) => Collection.Add(item);

            public void ExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.ExceptWith(other);

            public void IntersectWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IntersectWith(other);

            public bool IsProperSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsProperSubsetOf(other);

            public bool IsProperSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsProperSupersetOf(other);

            public bool IsSubsetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsSubsetOf(other);

            public bool IsSupersetOf(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.IsSupersetOf(other);

            public bool Overlaps(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.Overlaps(other);

            public bool SetEquals(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.SetEquals(other);

            public void SymmetricExceptWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.SymmetricExceptWith(other);

            public override ICollection<T> ToCollection() => ToSet();

            public HashSet<T> ToSet()
            {
                var set = new HashSet<T>();
                foreach (var item in Collection)
                {
                    set.Add(Conversion<T, TWrapped>.Unwrap((TWrapped)item!));
                }
                return set;
            }

            public void UnionWith(EnumerableWrapper<T, TWrapped, TInterface> other) => Collection.UnionWith(other);
        }
    }
}