#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
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
			private const string UWPFileName = ".UWPAppSettings";
			private readonly Dictionary<string, string> _values = new Dictionary<string, string>();
			private readonly string _folderPath;
			private readonly string _filePath;

			public FilePropertySet(ApplicationData owner, ApplicationDataLocality locality)
			{
				StorageFolder folder;
				switch (locality)
				{
					case ApplicationDataLocality.Local:
						folder = owner.LocalFolder;
						break;

					case ApplicationDataLocality.Roaming:
						folder = owner.RoamingFolder;
						break;
					case ApplicationDataLocality.LocalCache:
						folder = owner.LocalCacheFolder;
						break;

					case ApplicationDataLocality.Temporary:
						folder = owner.TemporaryFolder;
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(locality));
				}

				_folderPath = folder.Path;
				_filePath = Path.Combine(folder.Path, UWPFileName);

				ReadFromFile();
			}

			public object? this[string key]
			{
				get
				{
					if (_values.TryGetValue(key, out var value))
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
				try
				{

					if (File.Exists(_filePath))
					{
						using (var reader = new BinaryReader(File.OpenRead(_filePath)))
						{
							var count = reader.ReadInt32();

							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().Debug($"Reading {count} settings values");
							}

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
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"File {_filePath} does not exist, skipping reading settings");
						}
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to read settings from {_filePath}", e);
					}
				}
			}

			private void WriteToFile()
			{
				try
				{
					Directory.CreateDirectory(_folderPath);

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Writing {_values.Count} settings to {_filePath}");
					}

					using (var writer = new BinaryWriter(File.OpenWrite(_filePath)))
					{
						writer.Write(_values.Count);

						foreach (var pair in _values)
						{
							writer.Write(pair.Key);
							writer.Write(pair.Value ?? "");
						}
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to write settings to {_filePath}", e);
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
					_values.Add(key, DataTypeSerializer.Serialize(value));
					WriteToFile();
				}
			}

			public void Add(KeyValuePair<string, object> item)
				=> Add(item.Key, item.Value);

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

			public bool TryGetValue(string key, out object? value)
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
