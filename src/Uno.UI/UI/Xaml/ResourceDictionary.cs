using System;
using System.Collections.Generic;
using System.Threading;
using Uno.UI;
using Uno.Extensions;

namespace Windows.UI.Xaml
{
	public partial class ResourceDictionary : DependencyObject
	{
		private readonly Dictionary<object, object> _values = new Dictionary<object, object>();

		public ResourceDictionary()
		{
		}

		public Uri Source
		{
			get;
			set;
		}

		public IList<ResourceDictionary> MergedDictionaries { get; } = new List<ResourceDictionary>();

		public IDictionary<object, object> ThemeDictionaries { get; } = new Dictionary<object, object>();

		public object Lookup(object key)
		{
			object value;

			var keyName = key;
			if (!_values.TryGetValue(keyName, out value))
			{
				return null;
			}

			return value;
		}

		public bool HasKey(object key)
		{
			var keyName = key;

			return _values.ContainsKey(keyName);
		}

		public bool Insert(object key, object value)
		{
			_values[key] = value;
			return true;
		}

		public bool Remove(object key) => _values.Remove(key);

		public bool Remove(KeyValuePair<object, object> key) => _values.Remove(key.Key);

		public void Clear() => _values.Clear();

		public void Add(object key, object value) => _values.Add(key, value);

		public bool ContainsKey(object key) => _values.ContainsKey(key) || ContainsKeyMerged(key) || ContainsKeyTheme(key);

		private bool TryGetValue(object key, out object value)
		{
			if (_values.TryGetValue(key, out value))
			{
				return true;
			}

			if (GetFromMerged(key, out value))
			{
				return true;
			}

			return GetFromTheme(key, out value);
		}

		public object this[object key]
		{
			get
			{
				object value;
				TryGetValue(key, out value);

				return value;
			}
			set => _values[key] = value;
		}

		private bool GetFromMerged(object key, out object value)
		{
			// Check last dictionary first - //https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/resourcedictionary-and-xaml-resource-references#merged-resource-dictionaries
			for (int i = MergedDictionaries.Count - 1; i >= 0; i--)
			{
				if (MergedDictionaries[i].TryGetValue(key, out value))
				{
					return true;
				}
			}

			value = null;

			return false;
		}

		private bool ContainsKeyMerged(object key)
		{
			for (int i = MergedDictionaries.Count - 1; i >= 0; i--)
			{
				if (MergedDictionaries[i].ContainsKey(key))
				{
					return true;
				}
			}

			return false;
		}

		private ResourceDictionary GetThemeDictionary() => GetThemeDictionary(Themes.Active) ?? GetThemeDictionary(Themes.Default);

		private ResourceDictionary GetThemeDictionary(string theme)
		{
			if (ThemeDictionaries.TryGetValue(theme, out var dict))
			{
				return dict as ResourceDictionary;
			}

			return null;
		}

		private bool GetFromTheme(object key, out object value)
		{
			var dict = GetThemeDictionary();

			if (dict != null && dict.TryGetValue(key, out value))
			{
				return true;
			}

			return GetFromThemeMerged(key, out value);
		}

		private bool GetFromThemeMerged(object key, out object value)
		{
			for (int i = MergedDictionaries.Count - 1; i >= 0; i--)
			{
				if (MergedDictionaries[i].GetFromTheme(key, out value))
				{
					return true;
				}
			}

			value = null;

			return false;
		}


		private bool ContainsKeyTheme(object key)
		{
			return GetThemeDictionary()?.ContainsKey(key) ?? ContainsKeyThemeMerged(key);
		}

		private bool ContainsKeyThemeMerged(object key)
		{
			for (int i = MergedDictionaries.Count - 1; i >= 0; i--)
			{
				if (MergedDictionaries[i].ContainsKeyTheme(key))
				{
					return true;
				}
			}

			return false;
		}

		public global::System.Collections.Generic.ICollection<object> Keys => _values.Keys;

		public global::System.Collections.Generic.ICollection<object> Values => _values.Values;

		public void Add(global::System.Collections.Generic.KeyValuePair<object, object> item) => _values.Add(item.Key, item.Value);

		public bool Contains(global::System.Collections.Generic.KeyValuePair<object, object> item) => _values.ContainsKey(item.Key);

		[Uno.NotImplemented]
		public void CopyTo(global::System.Collections.Generic.KeyValuePair<object, object>[] array, int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}

		public int Count => _values.Count;

		public bool IsReadOnly => false;

		public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<object, object>> GetEnumerator() => _values.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();

		private static class Themes
		{
			public const string Default = "Default";
			public static string Active
			{
				get
				{
					if (Application.Current == null)
					{
						return "Light";
					}

					var custom = ApplicationHelper.RequestedCustomTheme;

					if (!custom.IsNullOrEmpty())
					{
						return custom;
					}

					switch (Application.Current.RequestedTheme)
					{
						case ApplicationTheme.Light:
							return "Light";
						case ApplicationTheme.Dark:
							return "Dark";
						default:
							throw new InvalidOperationException($"Theme {Application.Current.RequestedTheme} is not valid");
					}
				}
			}
		}
	}
}
