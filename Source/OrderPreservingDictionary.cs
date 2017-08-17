using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace More.Json
{
	public class OrderPreservingDictionary<K, V> : IDictionary<K, V>
	{
		private int[] _hashes = { };
		private readonly List<K> _keys;
		private readonly List<V> _values;

		public OrderPreservingDictionary() : this(8) { }

		public OrderPreservingDictionary(int capacity)
		{
			_keys = new List<K>(capacity);
			_values = new List<V>(capacity);
		}

		public bool IsReadOnly => false;
		public int Count => _keys.Count;

		public ICollection<K> Keys => new ReadOnlyCollection<K>(_keys);
		public ICollection<V> Values => new ReadOnlyCollection<V>(_values);

		public void Add(K key, V val)
		{
			_keys.Add(key);
			_values.Add(val);
		}

		public void Add(KeyValuePair<K, V> entry)
		{
			Add(entry.Key, entry.Value);
		}

		public void Clear()
		{
			_hashes = new int[] { };
			_keys.Clear();
			_values.Clear();
		}

		public void CopyTo(KeyValuePair<K, V>[] array, int offset)
		{
			int max = _keys.Count;
			for (int i = 0; i < max; ++i)
				array[i + offset] = new KeyValuePair<K, V>(_keys[i], _values[i]);
		}

		IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
		{
			int max = _keys.Count();
			for (int i = 0; i < max; ++i)
				yield return new KeyValuePair<K, V>(_keys[i], _values[i]);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			int max = _keys.Count();
			for (int i = 0; i < max; ++i)
				yield return new KeyValuePair<K, V>(_keys[i], _values[i]);
		}

		public V this[K key]
		{
			get
			{
				V result;
				if (TryGetValue(key, out result))
					return result;
				throw new KeyNotFoundException(key.ToString());
			}
			set => Add(key, value);
		}

		public bool ContainsKey(K key)
		{
			V ignored;
			return TryGetValue(key, out ignored);
		}

		public bool TryGetValue(K key, out V val)
		{
			MaybeIndex();

			int hash = key.GetHashCode();
			int max = _hashes.Length;
			// Search in reverse order, so later insertions take precedence
			for (int i = max - 1; i >= 0; --i)
				if (_hashes[i] == hash)
					if (_keys[i].Equals(key))
					{
						val = _values[i];
						return true;
					}
			val = default(V);
			return false;
		}

		public bool Contains(KeyValuePair<K, V> e)
		{
			MaybeIndex();

			K key = e.Key;
			V val = e.Value;
			int hash = key.GetHashCode();
			int max = _hashes.Length;
			for (int i = 0; i < max; ++i)
				if (_hashes[i] == hash)
					if (_keys[i].Equals(key))
						if (_values[i].Equals(val))
							return true;
			return false;
		}

		public bool Remove(K key)
		{
			MaybeIndex();

			int hash = key.GetHashCode();
			int max = _keys.Count;
			for (int i = 0; i < max; ++i)
				if (_hashes[i] == hash)
					if (key.Equals(_keys[i]))
					{
						RemoveStartingFrom(hash, key, i);
						return true;
					}
			return false;
		}

		public bool Remove(KeyValuePair<K, V> e)
		{
			MaybeIndex();

			K key = e.Key;
			V val = e.Value;
			int hash = key.GetHashCode();
			int max = _hashes.Length;
			for (int i = 0; i < max; ++i)
				if (_hashes[i] == hash)
					if (_keys[i].Equals(key))
						if (_values[i].Equals(val))
						{
							RemoveStartingFrom(hash, key, val, i);
							return true;
						}
			return false;
		}

		private void RemoveStartingFrom(int hash, K key, int safe)
		{
			int max = _keys.Count;
			for (int i = safe + 1; i < max; ++i)
				if (_hashes[i] == hash)
					if (key.Equals(_keys[i]))
					{
						_hashes[safe] = _hashes[i];
						_keys[safe] = _keys[i];
						_values[safe] = _values[i];
						++safe;
					}
			Truncate(safe);
		}

		private void RemoveStartingFrom(int hash, K key, V val, int safe)
		{
			int max = _keys.Count;
			for (int i = safe + 1; i < max; ++i)
				if (_hashes[i] == hash)
					if (key.Equals(_keys[i]))
						if (val.Equals(_values[i]))
						{
							_hashes[safe] = _hashes[i];
							_keys[safe] = _keys[i];
							_values[safe] = _values[i];
							++safe;
						}
			Truncate(safe);
		}

		private void MaybeIndex()
		{
			if (_hashes.Length == _keys.Count)
				return;

			var prev = _hashes;
			int max = _keys.Count;
			_hashes = new int[max];
			prev.CopyTo(_hashes, 0);
			for (int i = prev.Length; i < max; ++i)
				_hashes[i] = _keys[i].GetHashCode();
		}

		private void Truncate(int n)
		{
			int max = _keys.Count;
			_keys.RemoveRange(n, max - n);
			_values.RemoveRange(n, max - n);

			var prev = _hashes;
			_hashes = new int[n];
			for (int i = 0; i < n; ++i)
				_hashes[i] = prev[i];
		}
	}
}