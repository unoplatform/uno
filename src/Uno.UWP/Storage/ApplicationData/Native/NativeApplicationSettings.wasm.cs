#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Uno.Foundation.Logging;
using Windows.Foundation.Collections;
using Windows.Storage;
using static __Uno.Storage.NativeApplicationSettings;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private static partial bool SupportsLocalityPlatform() => true;

	partial void InitializePlatform() => ReadFromLegacyFile();

	private partial bool ContainsSettingPlatform(string key) => NativeMethods.ContainsKey(_locality, key);

	private partial bool RemoveSettingPlatform(string key)
	{
		var ret = NativeMethods.Remove(_locality, key);
		return ret;
	}

	private partial IEnumerable<string> GetKeysPlatform()
	{
		var keys = new List<string>();

		for (int i = 0; i < Count; i++)
		{
			keys.Add(NativeMethods.GetKeyByIndex(_locality, i));
		}

		return keys.AsReadOnly();
	}

	private partial bool TryGetSettingPlatform(string key, out string? value)
	{
		if (NativeMethods.TryGetValue(_locality, key, out var innerValue))
		{
			value = innerValue;
			return true;
		}

		value = null;
		return false;
	}

	private partial void SetSettingPlatform(string key, string value) =>
		NativeMethods.SetValue(_locality, key, value);

	private void ReadFromLegacyFile()
	{
		const string UWPFileName = ".UWPAppSettings";

		if (ApplicationData.Current is not { } applicationData)
		{
			throw new InvalidOperationException("ApplicationData.Current must be initialized.");
		}

		var folder = _locality switch
		{
			ApplicationDataLocality.Local => applicationData.LocalFolder,
			ApplicationDataLocality.Roaming => applicationData.RoamingFolder,
			ApplicationDataLocality.LocalCache => applicationData.LocalCacheFolder,
			ApplicationDataLocality.Temporary => applicationData.TemporaryFolder,
			_ => throw new ArgumentOutOfRangeException($"Unsupported locality {_locality}"),
		};

		if (folder is null)
		{
			throw new InvalidOperationException($"The folder for locality {_locality} is null.");
		}

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
