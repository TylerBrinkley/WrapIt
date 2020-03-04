using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WrapIt
{
    internal class DictionaryWrapperData : TypeData
    {
        public DictionaryWrapperData()
            : base(typeof(DictionaryWrapperData), new TypeName("WrapIt.Collections", "DictionaryWrapper"))
        {
        }

        public override async Task BuildAsync(WrapperBuilder builder, HashSet<TypeData> typeDatas, Func<Type, string, CancellationToken, Task<TextWriter>> writerProvider, CancellationToken cancellationToken = default)
        {
            if (builder.BuildCollectionWrappersAsNecessary)
            {
                BuildStatus = TypeBuildStatus.Building;
                using (var writer = await writerProvider(typeof(Array), "WrapIt.Collections.DictionaryWrapper", cancellationToken).ConfigureAwait(false))
                {
                    await writer.WriteAsync(@"using System;
using System.Collections;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public sealed class DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface> : IDictionary<TKey, TValueInterface>, IReadOnlyDictionary<TKey, TValueInterface>
        where TValueWrapped : TValueInterface
    {
        public static implicit operator DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface>(Dictionary<TKey, TValue> dictionary) => dictionary != null ? new DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface>(dictionary) : null;

        public static implicit operator Dictionary<TKey, TValue>(DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface> dictionaryWrapper) => dictionaryWrapper?.ToDictionary();

        public static DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface> Create(IDictionary<TKey, TValue> dictionary) => dictionary != null ? new DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface>(dictionary) : null;

        public static DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface> Create(IDictionary<TKey, TValueInterface> dictionary) => dictionary != null ? new DictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface>(dictionary) : null;

        internal IDictionaryWrapperInternal InternalWrapper { get; }

        public TValueWrapped this[TKey key] { get => InternalWrapper[key]; set => InternalWrapper[key] = value; }

        TValueInterface IDictionary<TKey, TValueInterface>.this[TKey key] { get => this[key]; set => this[key] = (TValueWrapped)value; }

        TValueInterface IReadOnlyDictionary<TKey, TValueInterface>.this[TKey key] => this[key];

        public ICollection<TKey> Keys => InternalWrapper.Keys;

        public CollectionWrapper<TValue, TValueWrapped, TValueInterface> Values => InternalWrapper.Values;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValueInterface>.Keys => Keys;

        ICollection<TValueInterface> IDictionary<TKey, TValueInterface>.Values => Values;

        IEnumerable<TValueInterface> IReadOnlyDictionary<TKey, TValueInterface>.Values => Values;

        public int Count => InternalWrapper.Count;

        bool ICollection<KeyValuePair<TKey, TValueInterface>>.IsReadOnly => InternalWrapper.IsReadOnly;

        public DictionaryWrapper(IDictionary<TKey, TValue> dictionary)
        {
            InternalWrapper = new StandardDictionaryWrapper(dictionary ?? throw new ArgumentNullException(nameof(dictionary)));
        }

        public DictionaryWrapper(IDictionary<TKey, TValueInterface> dictionary)
        {
            InternalWrapper = new CastedDictionaryWrapper(dictionary ?? throw new ArgumentNullException(nameof(dictionary)));
        }

        public Dictionary<TKey, TValue> ToDictionary() => InternalWrapper.ToDictionary();

        public IEnumerator<KeyValuePair<TKey, TValueWrapped>> GetEnumerator() => InternalWrapper.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValueInterface>> IEnumerable<KeyValuePair<TKey, TValueInterface>>.GetEnumerator()
        {
            foreach (var item in this)
            {
                yield return new KeyValuePair<TKey, TValueInterface>(item.Key, item.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValueInterface>>)this).GetEnumerator();

        public void Add(TKey key, TValueWrapped value) => InternalWrapper.Add(key, value);

        void IDictionary<TKey, TValueInterface>.Add(TKey key, TValueInterface value) => Add(key, (TValueWrapped)value);

        void ICollection<KeyValuePair<TKey, TValueInterface>>.Add(KeyValuePair<TKey, TValueInterface> item) => Add(item.Key, (TValueWrapped)item.Value);

        public void Clear() => InternalWrapper.Clear();

        bool ICollection<KeyValuePair<TKey, TValueInterface>>.Contains(KeyValuePair<TKey, TValueInterface> item) => TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(Conversion<TValue, TValueWrapped>.Unwrap(value), Conversion<TValue, TValueWrapped>.Unwrap((TValueWrapped)item.Value));

        public bool ContainsKey(TKey key) => InternalWrapper.ContainsKey(key);

        void ICollection<KeyValuePair<TKey, TValueInterface>>.CopyTo(KeyValuePair<TKey, TValueInterface>[] array, int arrayIndex)
        {
            if (arrayIndex + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException(""arrayIndex + Count must be less than or equal to array.Length"");
            }

            foreach (var pair in this)
            {
                array[arrayIndex++] = new KeyValuePair<TKey, TValueInterface>(pair.Key, pair.Value);
            }
        }

        public bool Remove(TKey key) => InternalWrapper.Remove(key);

        bool ICollection<KeyValuePair<TKey, TValueInterface>>.Remove(KeyValuePair<TKey, TValueInterface> item) => ((ICollection<KeyValuePair<TKey, TValueInterface>>)this).Contains(item) && Remove(item.Key);

        public bool TryGetValue(TKey key, out TValueWrapped value) => InternalWrapper.TryGetValue(key, out value);

        bool IDictionary<TKey, TValueInterface>.TryGetValue(TKey key, out TValueInterface value)
        {
            var success = TryGetValue(key, out var wrapped);
            value = wrapped;
            return success;
        }

        bool IReadOnlyDictionary<TKey, TValueInterface>.TryGetValue(TKey key, out TValueInterface value) => ((IDictionary<TKey, TValueInterface>)this).TryGetValue(key, out value);

        internal interface IDictionaryWrapperInternal : IDictionary<TKey, TValueWrapped>
        {
            new CollectionWrapper<TValue, TValueWrapped, TValueInterface> Values { get; }

            Dictionary<TKey, TValue> ToDictionary();
        }

        private sealed class StandardDictionaryWrapper : IDictionaryWrapperInternal
        {
            private readonly IDictionary<TKey, TValue> _dictionary;

            public TValueWrapped this[TKey key] { get => Conversion<TValue, TValueWrapped>.Wrap(_dictionary[key]); set => _dictionary[key] = Conversion<TValue, TValueWrapped>.Unwrap(value); }

            public ICollection<TKey> Keys => _dictionary.Keys;

            public CollectionWrapper<TValue, TValueWrapped, TValueInterface> Values => new CollectionWrapper<TValue, TValueWrapped, TValueInterface>(_dictionary.Values);

            ICollection<TValueWrapped> IDictionary<TKey, TValueWrapped>.Values => throw new NotSupportedException();

            public int Count => _dictionary.Count;

            public bool IsReadOnly => _dictionary.IsReadOnly;

            public StandardDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public void Add(TKey key, TValueWrapped value) => _dictionary.Add(key, Conversion<TValue, TValueWrapped>.Unwrap(value));

            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

            public bool Remove(TKey key) => _dictionary.Remove(key);

            public bool TryGetValue(TKey key, out TValueWrapped value)
            {
                var success = _dictionary.TryGetValue(key, out var actual);
                value = Conversion<TValue, TValueWrapped>.Wrap(actual);
                return success;
            }

            public Dictionary<TKey, TValue> ToDictionary() => _dictionary as Dictionary<TKey, TValue> ?? new Dictionary<TKey, TValue>(_dictionary);

            public ICollection<KeyValuePair<TKey, TValue>> ToCollection() => _dictionary;

            public void Add(KeyValuePair<TKey, TValueWrapped> item) => Add(item.Key, item.Value);

            public void Clear() => _dictionary.Clear();

            public bool Contains(KeyValuePair<TKey, TValueWrapped> item) => throw new NotSupportedException();

            public void CopyTo(KeyValuePair<TKey, TValueWrapped>[] array, int arrayIndex) => throw new NotSupportedException();

            public bool Remove(KeyValuePair<TKey, TValueWrapped> item) => throw new NotSupportedException();

            public IEnumerator<KeyValuePair<TKey, TValueWrapped>> GetEnumerator()
            {
                foreach (var pair in _dictionary)
                {
                    yield return new KeyValuePair<TKey, TValueWrapped>(pair.Key, Conversion<TValue, TValueWrapped>.Wrap(pair.Value));
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CastedDictionaryWrapper : IDictionaryWrapperInternal
        {
            private readonly IDictionary<TKey, TValueInterface> _dictionary;

            public TValueWrapped this[TKey key] { get => (TValueWrapped)_dictionary[key]; set => _dictionary[key] = value; }

            public ICollection<TKey> Keys => _dictionary.Keys;

            public CollectionWrapper<TValue, TValueWrapped, TValueInterface> Values => new CollectionWrapper<TValue, TValueWrapped, TValueInterface>(_dictionary.Values);

            ICollection<TValueWrapped> IDictionary<TKey, TValueWrapped>.Values => throw new NotSupportedException();

            public int Count => _dictionary.Count;

            public bool IsReadOnly => _dictionary.IsReadOnly;

            public CastedDictionaryWrapper(IDictionary<TKey, TValueInterface> dictionary)
            {
                _dictionary = dictionary;
            }

            public ICollection<KeyValuePair<TKey, TValue>> ToCollection() => ToDictionary();

            public void Add(TKey key, TValueWrapped value) => _dictionary.Add(key, value);

            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

            public bool Remove(TKey key) => _dictionary.Remove(key);

            public bool TryGetValue(TKey key, out TValueWrapped value)
            {
                var success = _dictionary.TryGetValue(key, out var v);
                value = (TValueWrapped)v;
                return success;
            }

            public Dictionary<TKey, TValue> ToDictionary()
            {
                var dictionary = new Dictionary<TKey, TValue>(_dictionary.Count);
                foreach (var item in _dictionary)
                {
                    dictionary[item.Key] = Conversion<TValue, TValueWrapped>.Unwrap((TValueWrapped)item.Value);
                }
                return dictionary;
            }

            public void Add(KeyValuePair<TKey, TValueWrapped> item) => Add(item.Key, item.Value);

            public void Clear() => _dictionary.Clear();

            public bool Contains(KeyValuePair<TKey, TValueWrapped> item) => throw new NotSupportedException();

            public void CopyTo(KeyValuePair<TKey, TValueWrapped>[] array, int arrayIndex) => throw new NotSupportedException();

            public bool Remove(KeyValuePair<TKey, TValueWrapped> item) => throw new NotSupportedException();

            public IEnumerator<KeyValuePair<TKey, TValueWrapped>> GetEnumerator()
            {
                foreach (var item in _dictionary)
                {
                    yield return new KeyValuePair<TKey, TValueWrapped>(item.Key, (TValueWrapped)item.Value);
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

            var collectionWrapperData = builder.GetTypeData(typeof(CollectionWrapperData), typeDatas);
            if (collectionWrapperData.BuildStatus != TypeBuildStatus.Built)
            {
                await collectionWrapperData.BuildAsync(builder, typeDatas, writerProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}