using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Storage;

partial class ApplicationDataContainerSettings
{
	private class NSUserDefaultsPropertySet : IPropertySet
	{

		public NSUserDefaultsPropertySet()
		{
		}

		public object this[string key]
		{
			get
			{
				var value = NSUserDefaults.StandardUserDefaults.ValueForKey((NSString)key)?.ToString();

				return DataTypeSerializer.Deserialize(value);
			}
			set
			{
				if (value != null)
				{
					var nativeObject = NSObject.FromObject(DataTypeSerializer.Serialize(value));
					NSUserDefaults.StandardUserDefaults.SetValueForKey(nativeObject, (NSString)key);
				}
				else
				{
					Remove(key);
				}
			}
		}

		public ICollection<string> Keys
			=> NSUserDefaults.StandardUserDefaults.ToDictionary().Keys.Select(k => k.ToString()).ToList();

		public ICollection<object> Values
			=> NSUserDefaults.StandardUserDefaults
			.ToDictionary()
			.Values
			.Select(k => DataTypeSerializer.Deserialize(k?.ToString()))
			.ToList();

		public int Count
			=> (int)NSUserDefaults.StandardUserDefaults.ToDictionary().Count;

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
				NSUserDefaults.StandardUserDefaults.SetValueForKey(nativeObject, (NSString)key);
			}
		}

		public void Add(KeyValuePair<string, object> item)
			=> Add(item.Key, item.Value);

		public void Clear()
		{
			foreach (var pair in NSUserDefaults.StandardUserDefaults.ToDictionary())
			{
				Remove(pair.Key.ToString());
			}
		}

		public bool Contains(KeyValuePair<string, object> item)
			=> throw new NotSupportedException();

		public bool ContainsKey(string key)
			=> NSUserDefaults.StandardUserDefaults.ToDictionary().ContainsKey((NSString)key);

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
			=> throw new NotSupportedException();

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return NSUserDefaults.StandardUserDefaults
				.ToDictionary()
				.Select(k => new KeyValuePair<string, object>(k.Key.ToString(), DataTypeSerializer.Deserialize(k.Key.ToString())))
				.GetEnumerator();
		}

		public bool Remove(string key)
		{
			NSUserDefaults.StandardUserDefaults.RemoveObject((NSString)key);
			NSUserDefaults.StandardUserDefaults.Synchronize();

			return true;
		}

		public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

		public bool TryGetValue(string key, out object value)
		{
			if (NSUserDefaults.StandardUserDefaults.ToDictionary().TryGetValue((NSString)key, out var nsvalue))
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
