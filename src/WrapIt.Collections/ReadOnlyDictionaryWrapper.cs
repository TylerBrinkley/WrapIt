using System;
using System.Collections;
using System.Collections.Generic;

namespace WrapIt.Collections
{
    public sealed class ReadOnlyDictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface> : IReadOnlyDictionary<TKey, TValueInterface>
        where TValueWrapped : TValueInterface
    {
        public static ReadOnlyDictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface> Create(IReadOnlyDictionary<TKey, TValue> dictionary) => dictionary != null ? new ReadOnlyDictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface>(dictionary) : null;

        public static ReadOnlyDictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface> Create(IReadOnlyDictionary<TKey, TValueInterface> dictionary) => dictionary != null ? new ReadOnlyDictionaryWrapper<TKey, TValue, TValueWrapped, TValueInterface>(dictionary) : null;

        internal IReadOnlyDictionaryWrapperInternal InternalWrapper { get; }

        public TValueWrapped this[TKey key] => InternalWrapper[key];

        TValueInterface IReadOnlyDictionary<TKey, TValueInterface>.this[TKey key] => this[key];

        public IEnumerable<TKey> Keys => InternalWrapper.Keys;

        public EnumerableWrapper<TValue, TValueWrapped, TValueInterface> Values => InternalWrapper.Values;

        IEnumerable<TValueInterface> IReadOnlyDictionary<TKey, TValueInterface>.Values => Values;

        public int Count => InternalWrapper.Count;

        public ReadOnlyDictionaryWrapper(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            InternalWrapper = new StandardReadOnlyDictionaryWrapper(dictionary ?? throw new ArgumentNullException(nameof(dictionary)));
        }

        public ReadOnlyDictionaryWrapper(IReadOnlyDictionary<TKey, TValueInterface> dictionary)
        {
            InternalWrapper = new CastedReadOnlyDictionaryWrapper(dictionary ?? throw new ArgumentNullException(nameof(dictionary)));
        }

        public IReadOnlyDictionary<TKey, TValue> ToDictionary() => InternalWrapper.ToDictionary();

        public IEnumerator<KeyValuePair<TKey, TValueWrapped>> GetEnumerator() => InternalWrapper.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValueInterface>> IEnumerable<KeyValuePair<TKey, TValueInterface>>.GetEnumerator()
        {
            foreach (var item in this)
            {
                yield return new KeyValuePair<TKey, TValueInterface>(item.Key, item.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValueInterface>>)this).GetEnumerator();

        public bool ContainsKey(TKey key) => InternalWrapper.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValueWrapped value) => InternalWrapper.TryGetValue(key, out value);

        bool IReadOnlyDictionary<TKey, TValueInterface>.TryGetValue(TKey key, out TValueInterface value)
        {
            var success = TryGetValue(key, out var wrapped);
            value = wrapped;
            return success;
        }

        internal interface IReadOnlyDictionaryWrapperInternal : IReadOnlyDictionary<TKey, TValueWrapped>
        {
            new EnumerableWrapper<TValue, TValueWrapped, TValueInterface> Values { get; }

            IReadOnlyDictionary<TKey, TValue> ToDictionary();
        }

        private sealed class StandardReadOnlyDictionaryWrapper : IReadOnlyDictionaryWrapperInternal
        {
            private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;

            public TValueWrapped this[TKey key] => Conversion<TValue, TValueWrapped>.Wrap(_dictionary[key]);

            public IEnumerable<TKey> Keys => _dictionary.Keys;

            public EnumerableWrapper<TValue, TValueWrapped, TValueInterface> Values => EnumerableWrapper<TValue, TValueWrapped, TValueInterface>.Create(_dictionary.Values);

            IEnumerable<TValueWrapped> IReadOnlyDictionary<TKey, TValueWrapped>.Values => throw new NotSupportedException();

            public int Count => _dictionary.Count;

            public StandardReadOnlyDictionaryWrapper(IReadOnlyDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

            public bool TryGetValue(TKey key, out TValueWrapped value)
            {
                var success = _dictionary.TryGetValue(key, out var actual);
                value = Conversion<TValue, TValueWrapped>.Wrap(actual);
                return success;
            }

            public IReadOnlyDictionary<TKey, TValue> ToDictionary() => _dictionary;

            public IReadOnlyCollection<KeyValuePair<TKey, TValue>> ToCollection() => _dictionary;

            public IEnumerator<KeyValuePair<TKey, TValueWrapped>> GetEnumerator()
            {
                foreach (var pair in _dictionary)
                {
                    yield return new KeyValuePair<TKey, TValueWrapped>(pair.Key, Conversion<TValue, TValueWrapped>.Wrap(pair.Value));
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CastedReadOnlyDictionaryWrapper : IReadOnlyDictionaryWrapperInternal
        {
            private readonly IReadOnlyDictionary<TKey, TValueInterface> _dictionary;

            public TValueWrapped this[TKey key] => (TValueWrapped)_dictionary[key];

            public IEnumerable<TKey> Keys => _dictionary.Keys;

            public EnumerableWrapper<TValue, TValueWrapped, TValueInterface> Values => EnumerableWrapper<TValue, TValueWrapped, TValueInterface>.Create(_dictionary.Values);

            IEnumerable<TValueWrapped> IReadOnlyDictionary<TKey, TValueWrapped>.Values => throw new NotSupportedException();

            public int Count => _dictionary.Count;

            public CastedReadOnlyDictionaryWrapper(IReadOnlyDictionary<TKey, TValueInterface> dictionary)
            {
                _dictionary = dictionary;
            }

            public IReadOnlyCollection<KeyValuePair<TKey, TValue>> ToCollection() => ToDictionary();

            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

            public bool TryGetValue(TKey key, out TValueWrapped value)
            {
                var success = _dictionary.TryGetValue(key, out var v);
                value = (TValueWrapped)v;
                return success;
            }

            public IReadOnlyDictionary<TKey, TValue> ToDictionary()
            {
                var dictionary = new Dictionary<TKey, TValue>(_dictionary.Count);
                foreach (var item in _dictionary)
                {
                    dictionary[item.Key] = Conversion<TValue, TValueWrapped>.Unwrap((TValueWrapped)item.Value);
                }
                return dictionary;
            }

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
}