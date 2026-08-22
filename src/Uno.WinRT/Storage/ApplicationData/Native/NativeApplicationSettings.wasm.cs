#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Uno.Foundation.Logging;
using Windows.Storage;
using static __Uno.Storage.NativeApplicationSettings;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	/// <summary>
	/// Flags that the import of the legacy settings file already ran. It carries the reserved internal
	/// prefix, so it stays out of the settings surface.
	/// </summary>
	private static readonly string MigrationKey = ApplicationDataContainer.InternalSettingPrefix + "Migrated";

	private static partial bool SupportsLocalityPlatform() => true;

	private partial Dictionary<string, string> LoadPlatform()
	{
		var settings = new Dictionary<string, string>();

		var count = NativeMethods.GetCount(_locality);
		for (var i = 0; i < count; i++)
		{
			var key = NativeMethods.GetKeyByIndex(_locality, i);
			if (key != MigrationKey)
			{
				settings[key] = NativeMethods.GetValueByIndex(_locality, i);
			}
		}

		if (!NativeMethods.ContainsKey(_locality, MigrationKey) && ImportLegacyFile(settings))
		{
			NativeMethods.SetValue(_locality, MigrationKey, "1");
		}

		return settings;
	}

	private partial void SetSettingPlatform(string key, string value) =>
		NativeMethods.SetValue(_locality, key, value);

	private partial void RemoveSettingsPlatform(IReadOnlyCollection<string> keys)
	{
		foreach (var key in keys)
		{
			NativeMethods.Remove(_locality, key);
		}
	}

	/// <summary>
	/// Imports the settings file written by versions of Uno Platform that persisted to the virtual file
	/// system rather than to local storage.
	/// </summary>
	/// <returns>Whether the import ran to completion, and does not need to be attempted again.</returns>
	private bool ImportLegacyFile(Dictionary<string, string> settings)
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
			if (!File.Exists(filePath))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"File {filePath} does not exist, skipping reading legacy settings");
				}

				return true;
			}

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

				// The file already holds serialized values, so they are stored as they are — going through
				// the public indexer would serialize them a second time. A value the app has since written wins.
				if (!settings.ContainsKey(key))
				{
					settings[key] = value;
					NativeMethods.SetValue(_locality, key, value);
				}
			}

			return true;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to read settings from {filePath}", e);
			}

			return false;
		}
	}
}
