#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Foundation.Logging;
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
					if (ApplicationDataContainerInterop.TryGetValue(_locality, key, out var value))
					{
						return DataTypeSerializer.Deserialize(value);
					}
					return null;
				}
				set
				{
					if (value != null)
					{
						ApplicationDataContainerInterop.SetValue(_locality, key, DataTypeSerializer.Serialize(value));
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
						keys.Add(ApplicationDataContainerInterop.GetKeyByIndex(_locality, i));
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
						var rawValue = ApplicationDataContainerInterop.GetValueByIndex(_locality, i);
						if (DataTypeSerializer.Deserialize(rawValue) is { } value)
						{
							values.Add(value);
						}
					}

					return values.AsReadOnly();
				}
			}

			public int Count
				=> ApplicationDataContainerInterop.GetCount(_locality);

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
					ApplicationDataContainerInterop.SetValue(_locality, key, DataTypeSerializer.Serialize(value));
					MapChanged?.Invoke(this, null);
				}
			}

			public void Add(KeyValuePair<string, object> item)
				=> Add(item.Key, item.Value);

			public void Clear()
			{
				ApplicationDataContainerInterop.Clear(_locality);
			}

			public bool Contains(KeyValuePair<string, object> item)
				=> throw new NotSupportedException();

			public bool ContainsKey(string key)
				=> ApplicationDataContainerInterop.ContainsKey(_locality, key);

			public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
				=> throw new NotSupportedException();

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			{
				List<KeyValuePair<string, object>> kvps = new List<KeyValuePair<string, object>>();

				for (int index = 0; index < Count; index++)
				{
					var key = ApplicationDataContainerInterop.GetKeyByIndex(_locality, index);
					var value = ApplicationDataContainerInterop.GetValueByIndex(_locality, index);
					kvps.Add(new KeyValuePair<string, object>(key, value));
				}

				return kvps.GetEnumerator();
			}

			public bool Remove(string key)
			{
				var ret = ApplicationDataContainerInterop.Remove(_locality, key);
				return ret;
			}

			public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

			public bool TryGetValue(string key, out object? value)
			{
				if (ApplicationDataContainerInterop.TryGetValue(_locality, key, out var innervalue))
				{
					value = DataTypeSerializer.Deserialize(innervalue);
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

	static partial class ApplicationDataContainerInterop
	{
		internal static bool TryGetValue(ApplicationDataLocality locality, string key, out string? value)
		{
			if (!ContainsKey(locality, key))
			{
				value = null;
				return false;
			}
			else
			{
				value = GetValue(locality.ToStringInvariant(), key);
				return true;
			}
		}

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getValue")]
		private static partial string GetValue(string locality, string key);

		internal static void SetValue(ApplicationDataLocality locality, string key, string value)
			=> SetValue(locality.ToStringInvariant(), key, value);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.setValue")]
		private static partial void SetValue(string locality, string key, string value);

		internal static bool ContainsKey(ApplicationDataLocality locality, string key)
			=> ContainsKey(locality.ToStringInvariant(), key);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.containsKey")]
		private static partial bool ContainsKey(string locality, string key);

		internal static string GetKeyByIndex(ApplicationDataLocality locality, int index)
			=> GetKeyByIndex(locality.ToStringInvariant(), index);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getKeyByIndex")]
		private static partial string GetKeyByIndex(string locality, int index);

		internal static int GetCount(ApplicationDataLocality locality)
			=> GetCount(locality.ToStringInvariant());

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getCount")]
		private static partial int GetCount(string locality);

		internal static void Clear(ApplicationDataLocality locality)
			=> Clear(locality.ToStringInvariant());

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.clear")]
		private static partial void Clear(string locality);

		internal static bool Remove(ApplicationDataLocality locality, string key)
			=> Remove(locality.ToStringInvariant(), key);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.remove")]
		private static partial bool Remove(string locality, string key);

		internal static string GetValueByIndex(ApplicationDataLocality locality, int index)
			=> GetValueByIndex(locality.ToStringInvariant(), index);

		[JSImport("globalThis.Windows.Storage.ApplicationDataContainer.getValueByIndex")]
		private static partial string GetValueByIndex(string locality, int index);
	}
}
