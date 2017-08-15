using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace More.Json
{
    public class OrderPreservingDictionary<K, V> : IDictionary<K, V>
    {
        private readonly List<KeyValuePair<K, V>> _entries;

        public OrderPreservingDictionary() : this(8) { }

        public OrderPreservingDictionary(int capacity)
        {
            _entries = new List<KeyValuePair<K, V>>(capacity);
        }

        public bool IsReadOnly => false;
        public int Count => _entries.Count;

        public ICollection<K> Keys => (from e in _entries select e.Key).ToArray<K>();
        public ICollection<V> Values => (from e in _entries select e.Value).ToArray<V>();

        public void Add(K key, V val)
        {
            _entries.Add(new KeyValuePair<K, V>(key, val));
        }

        public void Add(KeyValuePair<K, V> entry)
        {
            _entries.Add(entry);
        }

        public bool Remove(KeyValuePair<K, V> e)
        {
            return _entries.Remove(e);
        }

        public bool Contains(KeyValuePair<K, V> e)
        {
            return _entries.Contains(e);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int i)
        {
            _entries.CopyTo(array, i);
        }

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        public V this[K key]
        {
            get
            {
                foreach (var e in _entries)
                    if (key.Equals(e.Key))
                        return e.Value;
                throw new KeyNotFoundException($"Key not found: {key}");
            }
            set => Add(key, value);
        }

        public bool TryGetValue(K key, out V val)
        {
            foreach (var e in _entries)
                if (key.Equals(e.Key))
                {
                    val = e.Value;
                    return true;
                }
            val = default(V);
            return false;
        }

        public bool ContainsKey(K key)
        {
            foreach (var e in _entries)
                if (key.Equals(e.Key))
                    return true;
            return false;
        }

        public bool Remove(K key)
        {
            for (int i = 0; i < _entries.Count; ++i)
                if (key.Equals(_entries[i].Key))
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            return false;
        }
    }
}