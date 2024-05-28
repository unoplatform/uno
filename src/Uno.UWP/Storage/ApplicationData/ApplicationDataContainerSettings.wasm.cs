namespace Windows.Storage;

partial class ApplicationDataContainerSettings
{
	partial void InitializePartial() => ReadFromLegacyFile();

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
				values.Add(DataTypeSerializer.Deserialize(rawValue));
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

class ApplicationDataContainerInterop
{
	#region TryGetValue
	internal static bool TryGetValue(ApplicationDataLocality locality, string key, out string? value)
	{
		var parms = new ApplicationDataContainer_TryGetValueParams
		{
			Key = key,
			Locality = locality.ToStringInvariant()
		};

		var ret = (ApplicationDataContainer_TryGetValueReturn)TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:tryGetValue", parms, typeof(ApplicationDataContainer_TryGetValueReturn));

		value = ret.Value;

		return ret.HasValue;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_TryGetValueParams
	{
		public string Key;
		public string Locality;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_TryGetValueReturn
	{
		public string? Value;
		public bool HasValue;
	}
	#endregion

	#region SetValue
	internal static void SetValue(ApplicationDataLocality locality, string key, string value)
	{
		var parms = new ApplicationDataContainer_SetValueParams
		{
			Key = key,
			Value = value,
			Locality = locality.ToStringInvariant()
		};

		TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:setValue", parms);
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_SetValueParams
	{
		public string Key;
		public string Value;
		public string Locality;
	}

	#endregion

	#region ContainsKey
	internal static bool ContainsKey(ApplicationDataLocality locality, string key)
	{
		var parms = new ApplicationDataContainer_ContainsKeyParams
		{
			Key = key,
			Locality = locality.ToStringInvariant()
		};

		var ret = (ApplicationDataContainer_ContainsKeyReturn)TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:containsKey", parms, typeof(ApplicationDataContainer_ContainsKeyReturn));
		return ret.ContainsKey;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_ContainsKeyParams
	{
		public string Key;
		public string Value;
		public string Locality;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_ContainsKeyReturn
	{
		public bool ContainsKey;
	}
	#endregion

	#region GetKeyByIndex
	internal static string GetKeyByIndex(ApplicationDataLocality locality, int index)
	{
		var parms = new ApplicationDataContainer_GetKeyByIndexParams
		{
			Locality = locality.ToStringInvariant(),
			Index = index
		};

		var ret = (ApplicationDataContainer_GetKeyByIndexReturn)TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:getKeyByIndex", parms, typeof(ApplicationDataContainer_GetKeyByIndexReturn));
		return ret.Value;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_GetKeyByIndexParams
	{
		public string Locality;
		public int Index;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_GetKeyByIndexReturn
	{
		public string Value;
	}
	#endregion

	#region GetCount

	internal static int GetCount(ApplicationDataLocality locality)
	{
		var parms = new ApplicationDataContainer_GetCountParams
		{
			Locality = locality.ToStringInvariant()
		};

		var ret = (ApplicationDataContainer_GetCountReturn)TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:getCount", parms, typeof(ApplicationDataContainer_GetCountReturn));
		return ret.Count;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_GetCountParams
	{
		public string Locality;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_GetCountReturn
	{
		public int Count;
	}
	#endregion

	#region Clear

	internal static void Clear(ApplicationDataLocality locality)
	{
		var parms = new ApplicationDataContainer_ClearParams
		{
			Locality = locality.ToStringInvariant()
		};

		TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:clear", parms);
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_ClearParams
	{
		public string Locality;
	}

	#endregion

	#region Remove

	internal static bool Remove(ApplicationDataLocality locality, string key)
	{
		var parms = new ApplicationDataContainer_RemoveParams
		{
			Locality = locality.ToStringInvariant(),
			Key = key
		};

		var ret = (ApplicationDataContainer_RemoveReturn)TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:remove", parms, typeof(ApplicationDataContainer_RemoveReturn));
		return ret.Removed;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_RemoveParams
	{
		public string Locality;
		public string Key;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct ApplicationDataContainer_RemoveReturn
	{
		public bool Removed;
	}

	#endregion

	#region GetValueByIndex

	internal static string GetValueByIndex(ApplicationDataLocality locality, int index)
	{
		var parms = new ApplicationDataContainer_GetValueByIndexParams
		{
			Locality = locality.ToStringInvariant(),
			Index = index
		};

		var ret = (ApplicationDataContainer_GetValueByIndexReturn)TSInteropMarshaller.InvokeJS("UnoStatic_Windows_Storage_ApplicationDataContainer:getValueByIndex", parms, typeof(ApplicationDataContainer_GetValueByIndexReturn));
		return ret.Value;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct ApplicationDataContainer_GetValueByIndexParams
	{
		public string Locality;
		public int Index;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct ApplicationDataContainer_GetValueByIndexReturn
	{
		public string Value;
	}
	#endregion
}
