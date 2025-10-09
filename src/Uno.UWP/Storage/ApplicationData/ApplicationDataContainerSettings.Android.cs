﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Storage;

partial class ApplicationDataContainerSettings
{
	private class SharedPreferencesPropertySet : IPropertySet
	{
		public SharedPreferencesPropertySet()
		{
		}

		private ISharedPreferences CreatePreferences()
			=> PreferenceManager.GetDefaultSharedPreferences(ApplicationData.GetAndroidAppContext())!;

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
					using var preferences = CreatePreferences();

					preferences
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
		{
			get
			{
				using var preferences = CreatePreferences();

				return preferences
					.All
					.Keys
					.ToArray();
			}
		}

		public ICollection<object> Values
		{
			get
			{
				using var preferences = CreatePreferences();

				return preferences
					.All
					.Values
					.Select(v => DataTypeSerializer.Deserialize(v?.ToString()))
					.ToArray();
			}
		}

		public int Count
		{
			get
			{
				using var preferences = CreatePreferences();

				return preferences
					.All
					.Values
					.Count;
			}
		}

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
				using var preferences = CreatePreferences();

				preferences
					.Edit()
					.PutString(key, DataTypeSerializer.Serialize(value))
					.Commit();
			}
		}

		public void Add(KeyValuePair<string, object> item)
			=> Add(item.Key, item.Value);

		public void Clear()
		{
			using var preferences = CreatePreferences();

			preferences
				.Edit()
				.Clear()
				.Commit();
		}

		public bool Contains(KeyValuePair<string, object> item)
			=> throw new NotSupportedException();

		public bool ContainsKey(string key)
		{
			using var preferences = CreatePreferences();

			return preferences.All.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
			=> throw new NotSupportedException();

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			using var preferences = CreatePreferences();

			return preferences
				.All
				.GetEnumerator();
		}

		public bool Remove(string key)
		{
			using var preferences = CreatePreferences();

			preferences
				.Edit()
				.Remove(key)
				.Commit();

			return true;
		}

		public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

		public bool TryGetValue(string key, out object value)
		{
			using var preferences = CreatePreferences();

			if (preferences.All.TryGetValue(key, out var serializedValue))
			{
				value = DataTypeSerializer.Deserialize(serializedValue?.ToString());
				return true;
			}

			value = null;
			return false;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

}
