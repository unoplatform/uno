using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Windows.Foundation.Collections;

namespace Windows.Storage
{
	public partial class ApplicationDataContainer
	{
		partial void InitializePartial()
		{
			Values = new NSUserDefaultsPropertySet();
		}

		private class NSUserDefaultsPropertySet : IPropertySet
		{
			private const string UWPFileName = ".UWPAppSettings";
			private Dictionary<string, string> _values = new Dictionary<string, string>();

			public NSUserDefaultsPropertySet()
			{
				ReadFromFile();
			}

			public object this[string key]
			{
				get
				{
					if(!_values.TryGetValue(key, out var value))
					{
						return DataTypeSerializer.Deserialize(value);
					}

					return null;
				}
				set
				{
					if (value != null)
					{
						_values[key] = DataTypeSerializer.Serialize(value);
					}
					else
					{
						Remove(key);
					}

					WriteToFile();
				}
			}

			private void ReadFromFile()
			{
				var folderPath = ApplicationData.Current.LocalFolder.Path;
				var filePath = Path.Combine(folderPath, UWPFileName);

				if (File.Exists(filePath))
				{
					using (var reader = new BinaryReader(File.OpenRead(filePath)))
					{
						var count = reader.ReadInt32();

						Console.WriteLine($"Reading {count} values");

						for (int i = 0; i < count; i++)
						{
							var key = reader.ReadString();
							var value = reader.ReadString();

							_values[key] = value;
						}
					}
				}
				else
				{
					Console.WriteLine($"File {filePath} does not exist, skipping reading settings");
				}
			}

			private void WriteToFile()
			{
				var folderPath = ApplicationData.Current.LocalFolder.Path;
				var filePath = Path.Combine(folderPath, UWPFileName);

				Console.WriteLine($"Writing {_values.Count} settings to {filePath}");

				using (var writer = new BinaryWriter(File.OpenWrite(filePath)))
				{
					writer.Write(_values.Count);

					foreach(var pair in _values)
					{
						writer.Write(pair.Key);
						writer.Write(pair.Value ?? "");
					}
				}
			}

			public ICollection<string> Keys
				=> _values.Keys;

			public ICollection<object> Values
				=> _values.Values.Select(DataTypeSerializer.Deserialize).ToList();

			public int Count
				=> _values.Count;

			public bool IsReadOnly => false;

#pragma warning disable CS0067
			public event MapChangedEventHandler<string, object> MapChanged;
#pragma warning restore CS0067

			public void Add(string key, object value)
			{
				_values.Add(key, DataTypeSerializer.Serialize(value));
				WriteToFile();
			}

			public void Add(KeyValuePair<string, object> item)
				=> throw new NotSupportedException();

			public void Clear()
			{
				_values.Clear();
				WriteToFile();
			}

			public bool Contains(KeyValuePair<string, object> item) 
				=> throw new NotSupportedException();

			public bool ContainsKey(string key)
				=> _values.ContainsKey(key);

			public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
				=> throw new NotSupportedException();

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
				=> throw new NotSupportedException();

			public bool Remove(string key)
			{
				var ret = _values.Remove(key);

				WriteToFile();

				return ret;
			}

			public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

			public bool TryGetValue(string key, out object value)
			{
				if (_values.TryGetValue(key, out var innervalue))
				{
					value = DataTypeSerializer.Deserialize(innervalue);
					return true;
				}

				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
		}
	}
}