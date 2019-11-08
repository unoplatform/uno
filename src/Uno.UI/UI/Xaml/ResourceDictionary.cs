using System;
using System.Collections.Generic;
using System.Threading;
using Uno.UI;
using Uno.Extensions;
using System.ComponentModel;

namespace Windows.UI.Xaml
{
	public partial class ResourceDictionary : DependencyObject, IDependencyObjectParse, IDictionary<object, object>
	{
		private readonly Dictionary<object, object> _values = new Dictionary<object, object>();

		public ResourceDictionary()
		{
		}

		private Uri _source;
		public Uri Source
		{
			get => _source;
			set
			{
				if (!IsParsing) // If we're parsing, the Source is being set as a 'FYI', don't try to resolve it
				{
					var sourceDictionary = ResourceResolver.RetrieveDictionaryForSource(value);

					CopyFrom(sourceDictionary);
				}

				_source = value;
			}
		}

		public IList<ResourceDictionary> MergedDictionaries { get; } = new List<ResourceDictionary>();

		private IDictionary<object, object> _themeDictionaries;
		public IDictionary<object, object> ThemeDictionaries { get => _themeDictionaries = _themeDictionaries ?? new ResourceDictionary(); }
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

		public void Add(object key, object value)
		{
			if (value is ResourceInitializer resourceInitializer)
			{
				_values.Add(key, new LazyInitializer(ResourceResolver.CurrentScope, resourceInitializer));
			}
			else
			{
				_values.Add(key, value);
			}
		}

		public bool ContainsKey(object key) => _values.ContainsKey(key) || ContainsKeyMerged(key) || ContainsKeyTheme(key);

		public bool TryGetValue(object key, out object value)
		{
			if (_values.TryGetValue(key, out value))
			{
				TryMaterializeLazy(key, ref value);
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
			set => Add(key, value);
		}

		/// <summary>
		/// If retrieved element is a <see cref="LazyInitializer"/> stub, materialize the actual object and replace the stub.
		/// </summary>
		private void TryMaterializeLazy(object key, ref object value)
		{
			if (value is LazyInitializer lazyInitializer)
			{
				object newValue = null;
#if !HAS_EXPENSIVE_TRYFINALLY
				try
#endif
				{
					_values.Remove(key); // Temporarily remove the key to make this method safely reentrant, if it's a framework- or application-level theme dictionary
					ResourceResolver.PushNewScope(lazyInitializer.CurrentScope);
					newValue = lazyInitializer.Initializer();
				}
#if !HAS_EXPENSIVE_TRYFINALLY
				finally
#endif
				{
					value = newValue;
					_values[key] = newValue; // If Initializer threw an exception this will push null, to avoid running buggy initialization again and again (and avoid surfacing initializer to consumer code)
					ResourceResolver.PopScope();
				}
			}
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
			object dict = null;
			if (_themeDictionaries?.TryGetValue(theme, out dict) ?? false)
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

		/// <summary>
		/// Copy another dictionary's contents, this is used when setting the <see cref="Source"/> property
		/// </summary>
		private void CopyFrom(ResourceDictionary source)
		{
			_values.Clear();
			MergedDictionaries.Clear();
			_themeDictionaries?.Clear();

			_values.AddRange(source);
			MergedDictionaries.AddRange(source.MergedDictionaries);
			if (source._themeDictionaries != null)
			{
				ThemeDictionaries.AddRange(source.ThemeDictionaries);
			}
		}

		public global::System.Collections.Generic.ICollection<object> Keys => _values.Keys;

		public global::System.Collections.Generic.ICollection<object> Values => _values.Values;

		public void Add(global::System.Collections.Generic.KeyValuePair<object, object> item) => Add(item.Key, item.Value);

		public bool Contains(global::System.Collections.Generic.KeyValuePair<object, object> item) => _values.ContainsKey(item.Key);

		[Uno.NotImplemented]
		public void CopyTo(global::System.Collections.Generic.KeyValuePair<object, object>[] array, int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}

		public int Count => _values.Count;

		public bool IsReadOnly => false;

		private bool _isParsing;
		/// <summary>
		/// True if the element is in the process of being parsed from Xaml.
		/// </summary>
		/// <remarks>This property shouldn't be set from user code. It's public to allow being set from generated code.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool IsParsing
		{
			get => _isParsing;
			set
			{
				if (!value)
				{
					throw new InvalidOperationException($"{nameof(IsParsing)} should never be set from user code.");
				}

				_isParsing = value;
				if (_isParsing)
				{
					ResourceResolver.PushSourceToScope(this);
				}
			}
		}

		public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<object, object>> GetEnumerator() => _values.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();

		public void CreationComplete()
		{
			if (!IsParsing)
			{
				throw new InvalidOperationException($"Called without matching {nameof(IsParsing)} call. This method should never be called from user code.");
			}

			_isParsing = false;
			ResourceResolver.PopSourceFromScope();
		}

		public delegate object ResourceInitializer();

		/// <summary>
		/// Allows resources to be initialized on-demand using correct scope.
		/// </summary>
		private struct LazyInitializer
		{
			public XamlScope CurrentScope { get; }
			public ResourceInitializer Initializer { get; }

			public LazyInitializer(XamlScope currentScope, ResourceInitializer initializer)
			{
				CurrentScope = currentScope;
				Initializer = initializer;
			}
		}

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
