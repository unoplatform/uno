#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Storage.Internal;
using Windows.Foundation.Collections;

namespace Windows.Storage
{
	public partial class ApplicationDataContainer
	{
		partial void InitializePartial(ApplicationData owner)
		{
			Values = new FilePropertySet(owner, Locality);
		}

		private class FilePropertySet : IPropertySet
		{
			private readonly SettingsFile _file;

			public FilePropertySet(ApplicationData owner, ApplicationDataLocality locality)
				=> _file = new SettingsFile(Path.Combine(owner.GetSettingsFolderPath(), $"{locality}.dat"));

			public object? this[string key]
			{
				get
				{
					if (_file.Values.TryGetValue(key, out var value))
					{
						return DataTypeSerializer.Deserialize(value);
					}

					return null;
				}
				set
				{
					if (value != null)
					{
						_file.Set(key, DataTypeSerializer.Serialize(value));
					}
					else
					{
						Remove(key);
					}
				}
			}

			public ICollection<string> Keys
				=> _file.Values.Keys.ToList();

			public ICollection<object> Values
				=> _file.Values.Values.Select(TryDeserialize).Where(item => item is not null).Select(item => item!).ToList();

			public int Count
				=> _file.Values.Count;

			public bool IsReadOnly => false;

#pragma warning disable CS0067
			public event MapChangedEventHandler<string, object>? MapChanged;
#pragma warning restore CS0067

			public void Add(string key, object value)
			{
				if (ContainsKey(key))
				{
					throw new ArgumentException("An item with the same key has already been added.");
				}
				if (value != null)
				{
					_file.Set(key, DataTypeSerializer.Serialize(value));
				}
			}

			public void Add(KeyValuePair<string, object> item)
				=> Add(item.Key, item.Value);

			public void Clear()
				=> _file.Clear();

			public bool Contains(KeyValuePair<string, object> item)
				=> throw new NotSupportedException();

			public bool ContainsKey(string key)
				=> _file.Values.ContainsKey(key);

			public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
				=> throw new NotSupportedException();

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
				=> _file.Values
					.Select(pair => new KeyValuePair<string, object>(pair.Key, TryDeserialize(pair.Value)!))
					.Where(pair => pair.Value is not null)
					.ToList()
					.GetEnumerator();

			public bool Remove(string key)
				=> _file.Remove(key);

			public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

			public bool TryGetValue(string key, out object? value)
			{
				if (_file.Values.TryGetValue(key, out var innervalue))
				{
					value = DataTypeSerializer.Deserialize(innervalue);
					return true;
				}

				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			// One entry that no longer deserializes must not break enumerating the others.
			private static object? TryDeserialize(string value)
			{
				try
				{
					return DataTypeSerializer.Deserialize(value);
				}
				catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
				{
					return null;
				}
			}
		}
	}
}
