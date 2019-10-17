#if __ANDROID__
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Android.Content;
using Android.Preferences;
using Uno;
using Uno.UI;
using Windows.Foundation.Collections;

namespace Windows.Storage
{
	public partial class ApplicationDataContainer
	{
		partial void InitializePartial(ApplicationData owner)
		{
			Values = new SharedPreferencesPropertySet();
		}

		private class SharedPreferencesPropertySet : IPropertySet
		{
			private readonly ISharedPreferences _preferences;

			public SharedPreferencesPropertySet()
			{
				_preferences = PreferenceManager.GetDefaultSharedPreferences(ApplicationData.GetAndroidAppContext());
			}

			public object this[string key]
			{
				get
				{
					if (TryGetValue(key, out var value))
					{
						return value;
					}

					return null;
				}
				set
				{
					if (value != null)
					{
						_preferences
							.Edit()
							.PutString(key, DataTypeSerializer.Serialize(value))
							.Commit();
					}
					else
					{
						Remove(key);
					}
				}
			}

			public ICollection<string> Keys
				=> _preferences
				.All
				.Keys
				.ToArray();

			public ICollection<object> Values
				=> _preferences
				.All
				.Values
				.Select(v => DataTypeSerializer.Deserialize(v?.ToString()))
				.ToArray();

			public int Count
				=> _preferences.All.Values.Count;

			public bool IsReadOnly => false;

#pragma warning disable 67 // unused member
			[NotImplemented]
			public event MapChangedEventHandler<string, object> MapChanged;
#pragma warning restore 67 // unused member

			public void Add(string key, object value)
			{
				if (ContainsKey(key))
				{
					throw new ArgumentException("An item with the same key has already been added.");
				}
				if (value != null)
				{
					_preferences
						.Edit()
						.PutString(key, DataTypeSerializer.Serialize(value))
						.Commit();
				}
			}

			public void Add(KeyValuePair<string, object> item)
				=> Add(item.Key, item.Value);

			public void Clear()
			{
				_preferences
					.Edit()
					.Clear()
					.Commit();
			}

			public bool Contains(KeyValuePair<string, object> item)
				=> throw new NotSupportedException();

			public bool ContainsKey(string key)
				=> _preferences.All.ContainsKey(key);

			public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
				=> throw new NotSupportedException();

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
				=> throw new NotSupportedException();

			public bool Remove(string key)
			{
				_preferences
					.Edit()
					.Remove(key)
					.Commit();

				return true;
			}

			public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

			public bool TryGetValue(string key, out object value)
			{
				if (_preferences.All.TryGetValue(key, out var serializedValue))
				{
					value = DataTypeSerializer.Deserialize(serializedValue?.ToString());
					return true;
				}

				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
		}
	}
}
#endif
