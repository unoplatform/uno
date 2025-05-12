#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.Foundation.Collections;
using Windows.Storage;
using static __Uno.Storage.NativeApplicationSettings;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private static partial bool SupportsLocality() => true;

	partial void InitializePlatform() => ReadFromLegacyFile();

	//public object? this[string key]
	//{
	//	get
	//	{
	//		if (NativeMethods.TryGetValue(_locality, key, out var value))
	//		{
	//			return DataTypeSerializer.Deserialize(value);
	//		}
	//		return null;
	//	}
	//	set
	//	{
	//		if (value != null)
	//		{
	//			NativeMethods.SetValue(_locality, key, DataTypeSerializer.Serialize(value));
	//		}
	//		else
	//		{
	//			Remove(key);
	//		}
	//	}
	//}

	public ICollection<string> Keys
	{
		get
		{
			var keys = new List<string>();

			for (int i = 0; i < Count; i++)
			{
				keys.Add(NativeMethods.GetKeyByIndex(_locality, i));
			}

			return keys.AsReadOnly();
		}
	}

	//public ICollection<object> Values
	//{
	//	get
	//	{
	//		var values = new List<object>();

	//		for (int i = 0; i < Count; i++)
	//		{
	//			var rawValue = NativeMethods.GetValueByIndex(_locality, i);
	//			values.Add(DataTypeSerializer.Deserialize(rawValue));
	//		}

	//		return values.AsReadOnly();
	//	}
	//}

	public int Count
		=> NativeMethods.GetCount(_locality);

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
			NativeMethods.SetValue(_locality, key, DataTypeSerializer.Serialize(value));
			//MapChanged?.Invoke(this, null);
		}
	}

	public void Add(KeyValuePair<string, object> item)
		=> Add(item.Key, item.Value);

	public void Clear() => NativeMethods.Clear(_locality);

	public bool Contains(KeyValuePair<string, object> item)
		=> throw new NotSupportedException();

	public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		=> throw new NotSupportedException();

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		List<KeyValuePair<string, object>> kvps = new List<KeyValuePair<string, object>>();

		for (int index = 0; index < Count; index++)
		{
			var key = NativeMethods.GetKeyByIndex(_locality, index);
			var value = NativeMethods.GetValueByIndex(_locality, index);
			kvps.Add(new KeyValuePair<string, object>(key, value));
		}

		return kvps.GetEnumerator();
	}

	private void ReadFromLegacyFile()
	{
		//const string UWPFileName = ".UWPAppSettings";

		//var folder = _locality switch
		//{
		//	ApplicationDataLocality.Local => _owner.LocalFolder,
		//	ApplicationDataLocality.Roaming => _owner.RoamingFolder,
		//	ApplicationDataLocality.LocalCache => _owner.LocalCacheFolder,
		//	ApplicationDataLocality.Temporary => _owner.TemporaryFolder,
		//	_ => throw new ArgumentOutOfRangeException($"Unsupported locality {_locality}"),
		//};

		//var filePath = Path.Combine(folder.Path, UWPFileName);

		//try
		//{
		//	if (File.Exists(filePath))
		//	{
		//		using var reader = new BinaryReader(File.OpenRead(filePath));

		//		var count = reader.ReadInt32();

		//		if (this.Log().IsEnabled(LogLevel.Debug))
		//		{
		//			this.Log().Debug($"Reading {count} settings values");
		//		}

		//		for (var i = 0; i < count; i++)
		//		{
		//			var key = reader.ReadString();
		//			var value = reader.ReadString();

		//			this[key] = value;
		//		}
		//	}
		//	else
		//	{
		//		if (this.Log().IsEnabled(LogLevel.Debug))
		//		{
		//			this.Log().Debug($"File {filePath} does not exist, skipping reading legacy settings");
		//		}
		//	}
		//}
		//catch (Exception e)
		//{
		//	if (this.Log().IsEnabled(LogLevel.Error))
		//	{
		//		this.Log().Error($"Failed to read settings from {filePath}", e);
		//	}
		//}
	}

	private partial void SetSetting(string key, string value)
	{
		throw new NotImplementedException();
	}

	private partial bool TryGetSetting(string key, out string? value)
	{
		if (NativeMethods.TryGetValue(_locality, key, out var innervalue))
		{
			value = innervalue;
			return true;
		}

		value = null;
		return false;
	}

	private partial bool RemoveSetting(string key)
	{
		var ret = NativeMethods.Remove(_locality, key);
		return ret;
	}

	private partial bool ContainsSetting(string key) => NativeMethods.ContainsKey(_locality, key);
}
