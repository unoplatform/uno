using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Foundation;
using Windows.Foundation.Collections;

namespace Windows.Storage
{
	public partial class ApplicationDataContainer
	{
		partial void InitializePartial(ApplicationData owner)
		{
			Values = new NSUserDefaultsPropertySet();
		}

		private class NSUserDefaultsPropertySet : IPropertySet
		{
			private static readonly NSUserDefaults _userDefaults = new NSUserDefaults("UnoApplicationData", NSUserDefaultsType.SuiteName);
			private static bool _migrated;
			private const string MigrationKey = "__uno_migrated";

			public NSUserDefaultsPropertySet()
			{
				MigrateIfNeeded();
			}

			private static void MigrateIfNeeded()
			{
				if (_migrated)
				{
					return;
				}

				_migrated = true;

				if (_userDefaults.BoolForKey(MigrationKey))
				{
					return;
				}

				var standardDefaults = NSUserDefaults.StandardUserDefaults;
				foreach (var pair in standardDefaults.ToDictionary())
				{
					var value = pair.Value?.ToString();
					if (value != null && IsUnoSerializedValue(value))
					{
						_userDefaults.SetValueForKey(pair.Value, pair.Key);
						standardDefaults.RemoveObject(pair.Key.ToString());
					}
				}

				_userDefaults.SetBool(true, MigrationKey);
				_userDefaults.Synchronize();
				standardDefaults.Synchronize();
			}

			private static bool IsUnoSerializedValue(string value)
			{
				var index = value.IndexOf(':');
				if (index <= 0)
				{
					return false;
				}

				var typeName = value.Substring(0, index);
				return DataTypeSerializer.SupportedTypes.Any(t => t.FullName == typeName);
			}

			public object this[string key]
			{
				get
				{
					var value = _userDefaults.ValueForKey((NSString)key)?.ToString();

					return DataTypeSerializer.Deserialize(value);
				}
				set
				{
					if (value != null)
					{
						var nativeObject = NSObject.FromObject(DataTypeSerializer.Serialize(value));
						_userDefaults.SetValueForKey(nativeObject, (NSString)key);
					}
					else
					{
						Remove(key);
					}
				}
			}

			public ICollection<string> Keys
				=> _userDefaults.ToDictionary().Keys.Select(k => k.ToString()).ToList();

			public ICollection<object> Values
				=> _userDefaults
				.ToDictionary()
				.Values
				.Select(k => DataTypeSerializer.Deserialize(k?.ToString()))
				.ToList();

			public int Count
				=> (int)_userDefaults.ToDictionary().Count;

			public bool IsReadOnly => false;

#pragma warning disable CS0067
			public event MapChangedEventHandler<string, object> MapChanged;
#pragma warning restore CS0067

			public void Add(string key, object value)
			{
				if (ContainsKey(key))
				{
					throw new ArgumentException("An item with the same key has already been added.");
				}
				if (value != null)
				{
					var nativeObject = NSObject.FromObject(DataTypeSerializer.Serialize(value));
					_userDefaults.SetValueForKey(nativeObject, (NSString)key);
				}
			}

			public void Add(KeyValuePair<string, object> item)
				=> Add(item.Key, item.Value);

			public void Clear()
			{
				foreach (var pair in _userDefaults.ToDictionary())
				{
					Remove(pair.Key.ToString());
				}
			}

			public bool Contains(KeyValuePair<string, object> item)
				=> throw new NotSupportedException();

			public bool ContainsKey(string key)
				=> _userDefaults.ToDictionary().ContainsKey((NSString)key);

			public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
				=> throw new NotSupportedException();

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			{
				return _userDefaults
					.ToDictionary()
					.Select(k => new KeyValuePair<string, object>(k.Key.ToString(), DataTypeSerializer.Deserialize(k.Value?.ToString())))
					.GetEnumerator();
			}

			public bool Remove(string key)
			{
				if (!ContainsKey(key))
				{
					return false;
				}

				_userDefaults.RemoveObject((NSString)key);
				_userDefaults.Synchronize();

				return true;
			}

			public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

			public bool TryGetValue(string key, out object value)
			{
				if (_userDefaults.ToDictionary().TryGetValue((NSString)key, out var nsvalue))
				{
					value = DataTypeSerializer.Deserialize(nsvalue?.ToString());
					return true;
				}

				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
