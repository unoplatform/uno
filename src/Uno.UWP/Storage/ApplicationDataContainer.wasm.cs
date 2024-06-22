#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Foundation.Collections;
using static __Windows.Storage.ApplicationDataContainerNative;

namespace Windows.Storage
{
	public partial class ApplicationDataContainer
	{
		partial void InitializePartial(ApplicationData owner)
		{
			Values = new FilePropertySet(owner, Locality);
		}

		private partial class FilePropertySet : IPropertySet
		{
			private readonly ApplicationDataLocality _locality;
			private readonly ApplicationData _owner;

			public FilePropertySet(ApplicationData owner, ApplicationDataLocality locality)
			{
				_locality = locality;
				_owner = owner;

				ReadFromLegacyFile();
			}

			public object? this[string key]
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
						NativeSetValue(_locality.ToStringInvariant(), key, DataTypeSerializer.Serialize(value));
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
					var keys = new List<string>();

					for (int i = 0; i < Count; i++)
					{
						keys.Add(NativeGetKeyByIndex(_locality.ToStringInvariant(), i));
					}

					return keys.AsReadOnly();
				}
			}

			public ICollection<object> Values
			{
				get
				{
					var values = new List<object>();

					for (int i = 0; i < Count; i++)
					{
						var rawValue = NativeGetValueByIndex(_locality.ToStringInvariant(), i);
						values.Add(DataTypeSerializer.Deserialize(rawValue) ?? "");
					}

					return values.AsReadOnly();
				}
			}

			public int Count
				=> NativeGetCount(_locality.ToStringInvariant());

			public bool IsReadOnly => false;

			public event MapChangedEventHandler<string, object>? MapChanged;

			public void Add(string key, object value)
			{
				if (ContainsKey(key))
				{
					throw new ArgumentException("An item with the same key has already been added.");
				}
				if (value != null)
				{
					NativeSetValue(_locality.ToStringInvariant(), key, DataTypeSerializer.Serialize(value));
					MapChanged?.Invoke(this, null);
				}
			}

			public void Add(KeyValuePair<string, object> item)
				=> Add(item.Key, item.Value);

			public void Clear()
			{
				NativeClear(_locality.ToStringInvariant());
			}

			public bool Contains(KeyValuePair<string, object> item)
				=> throw new NotSupportedException();

			public bool ContainsKey(string key)
				=> NativeContainsKey(_locality.ToStringInvariant(), key);

			public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
				=> throw new NotSupportedException();

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			{
				List<KeyValuePair<string, object>> kvps = new List<KeyValuePair<string, object>>();

				for (int index = 0; index < Count; index++)
				{
					var key = NativeGetKeyByIndex(_locality.ToStringInvariant(), index);
					var value = NativeGetValueByIndex(_locality.ToStringInvariant(), index);
					kvps.Add(new KeyValuePair<string, object>(key, value));
				}

				return kvps.GetEnumerator();
			}

			public bool Remove(string key)
			{
				var ret = NativeRemove(_locality.ToStringInvariant(), key);
				return ret;
			}

			public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

			public bool TryGetValue(string key, out object? value)
			{
				if (NativeTryGetValue(_locality.ToStringInvariant(), key) is { } result
										&& result.GetPropertyAsBoolean("hasValue")
										&& result.GetPropertyAsString("value") is { } rawValue)
				{
					value = DataTypeSerializer.Deserialize(rawValue);
					return true;
				}

				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			private void ReadFromLegacyFile()
			{
				const string UWPFileName = ".UWPAppSettings";

				var folder = _locality switch
				{
					ApplicationDataLocality.Local => _owner.LocalFolder,
					ApplicationDataLocality.Roaming => _owner.RoamingFolder,
					ApplicationDataLocality.LocalCache => _owner.LocalCacheFolder,
					ApplicationDataLocality.Temporary => _owner.TemporaryFolder,
					_ => throw new ArgumentOutOfRangeException($"Unsupported locality {_locality}"),
				};

				var filePath = Path.Combine(folder.Path, UWPFileName);

				try
				{
					if (File.Exists(filePath))
					{
						using var reader = new BinaryReader(File.OpenRead(filePath));

						var count = reader.ReadInt32();

						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"Reading {count} settings values");
						}

						for (var i = 0; i < count; i++)
						{
							var key = reader.ReadString();
							var value = reader.ReadString();

							this[key] = value;
						}
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"File {filePath} does not exist, skipping reading legacy settings");
						}
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to read settings from {filePath}", e);
					}
				}
			}
		}
	}
}
