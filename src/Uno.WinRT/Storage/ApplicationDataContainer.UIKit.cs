using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Foundation;
using Uno.Storage.Internal;
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
			public object this[string key]
			{
				get
				{
					var value = UnoUserDefaults.Instance.ValueForKey((NSString)key)?.ToString();

					return DataTypeSerializer.Deserialize(value);
				}
				set
				{
					if (value != null)
					{
						var nativeObject = NSObject.FromObject(DataTypeSerializer.Serialize(value));
						UnoUserDefaults.Instance.SetValueForKey(nativeObject, (NSString)key);
					}
					else
					{
						Remove(key);
					}
				}
			}

			public ICollection<string> Keys
				=> UnoUserDefaults.Instance
				.ToDictionary()
				.Keys
				.Select(key => key.ToString())
				.ToList();

			public ICollection<object> Values
				=> UnoUserDefaults.Instance
				.ToDictionary()
				.Values
				.Select(value => DataTypeSerializer.Deserialize(value?.ToString()))
				.ToList();

			public int Count
				=> (int)UnoUserDefaults.Instance.ToDictionary().Count;

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
					UnoUserDefaults.Instance.SetValueForKey(nativeObject, (NSString)key);
				}
			}

			public void Add(KeyValuePair<string, object> item)
				=> Add(item.Key, item.Value);

			public void Clear()
			{
				foreach (var pair in UnoUserDefaults.Instance.ToDictionary())
				{
					UnoUserDefaults.Instance.RemoveObject(pair.Key.ToString());
				}

				UnoUserDefaults.Instance.Synchronize();
			}

			public bool Contains(KeyValuePair<string, object> item)
				=> throw new NotSupportedException();

			public bool ContainsKey(string key)
				=> UnoUserDefaults.Instance.ToDictionary().ContainsKey((NSString)key);

			public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
				=> throw new NotSupportedException();

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			{
				return UnoUserDefaults.Instance
					.ToDictionary()
					.Select(pair => new KeyValuePair<string, object>(pair.Key.ToString(), DataTypeSerializer.Deserialize(pair.Value?.ToString())))
					.GetEnumerator();
			}

			public bool Remove(string key)
			{
				if (!ContainsKey(key))
				{
					return false;
				}

				UnoUserDefaults.Instance.RemoveObject(key);
				UnoUserDefaults.Instance.Synchronize();

				return true;
			}

			public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

			public bool TryGetValue(string key, out object value)
			{
				if (UnoUserDefaults.Instance.ToDictionary().TryGetValue((NSString)key, out var nativeValue))
				{
					value = DataTypeSerializer.Deserialize(nativeValue?.ToString());
					return true;
				}

				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
