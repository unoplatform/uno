using System;
using System.Collections.Generic;
using System.Threading;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class ResourceDictionary : DependencyObject
	{
		private readonly Dictionary<object, object> _values = new Dictionary<object, object>();
		private int _resolvingDepth = 0;

		public ResourceDictionary()
		{
		}

		public static Func<string, object> DefaultResolver { get; set; }

		public Uri Source
		{
			get;
			set;
		}

		public IList<ResourceDictionary> MergedDictionaries => Array.Empty<ResourceDictionary>();

		public IDictionary<object, object> ThemeDictionaries { get; } = new Dictionary<object, object>();

		public object Lookup(object key)
		{
			object value;

			var keyName = key.ToString();
			if (!_values.TryGetValue(keyName, out value))
			{
				return DefaultResolver?.Invoke(keyName);
			}

			return value;
		}

		public bool HasKey(object key)
		{
			var keyName = key.ToString();

			return _values.ContainsKey(keyName) || DefaultResolver?.Invoke(keyName) != null;
		}

		public bool Insert(object key, object value)
		{
			_values[key.ToString()] = value;
			return true;
		}

		public bool Remove(object key) => _values.Remove(key.ToString());

		public bool Remove(KeyValuePair<object, object> key) => _values.Remove(key.Key.ToString());

		public void Clear() => _values.Clear();

		public void Add(object key, object value) => _values.Add(key.ToString(), value);

		public bool ContainsKey(object key) => _values.ContainsKey(key.ToString());

		public bool TryGetValue(object key, out object value)
		{
			if (!_values.TryGetValue(key, out value))
			{
				try
				{
					_resolvingDepth++;

					if (_resolvingDepth <= FeatureConfiguration.Xaml.MaxRecursiveResolvingDepth)
					{
						// This prevents a stack overflow while looking for a
						// missing resource in generated xaml code.

						value = DefaultResolver?.Invoke(key.ToString());
					}
				}
				finally
				{
					_resolvingDepth--;
				}

				return value != null;
			}

			return true;
		}

		public object this[object key]
		{
			get
			{
				object value;

				if (!TryGetValue(key, out value))
				{
					return value;
				}

				return value;
			}
			set => _values[key] = value;
		}

		public global::System.Collections.Generic.ICollection<object> Keys => _values.Keys;

		public global::System.Collections.Generic.ICollection<object> Values => _values.Values;

		public void Add(global::System.Collections.Generic.KeyValuePair<object, object> item) => _values.Add(item.Key.ToString(), item.Value);

		public bool Contains(global::System.Collections.Generic.KeyValuePair<object, object> item) => _values.ContainsKey(item.Key.ToString());

		[Uno.NotImplemented]
		public void CopyTo(global::System.Collections.Generic.KeyValuePair<object, object>[] array, int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}

		public int Count => _values.Count;

		public bool IsReadOnly => false;

		public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<object, object>> GetEnumerator() => _values.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();
	}
}
