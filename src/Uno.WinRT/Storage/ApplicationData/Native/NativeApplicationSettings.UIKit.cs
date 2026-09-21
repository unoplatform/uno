#nullable enable

using System.Collections.Generic;
using Foundation;
using Windows.Storage;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private const string SuiteName = "UnoApplicationData";

	/// <summary>
	/// Flags that the migration from the standard user defaults already ran. It lives in the same suite as the
	/// user data, and carries the reserved internal prefix so that it stays out of the settings surface.
	/// </summary>
	private const string MigrationKey = ApplicationDataContainer.InternalSettingPrefix + "Migrated";

	private static readonly NSUserDefaults _userDefaults = new(SuiteName, NSUserDefaultsType.SuiteName);
	private static readonly object _migrationGate = new();

	private static bool _migrated;

	private static partial bool SupportsLocalityPlatform() => false;

	/// <summary>
	/// Settings used to live in the shared standard user defaults, next to the keys owned by the OS, Apple
	/// frameworks and native libraries. They are moved once into a suite owned by Uno Platform, so that
	/// enumerating or clearing application settings no longer sees — or deletes — unrelated native keys.
	/// </summary>
	private static void MigrateIfNeeded()
	{
		if (_migrated)
		{
			return;
		}

		// The lock ensures a concurrent caller waits for the migration to complete instead of
		// observing a partially migrated container.
		lock (_migrationGate)
		{
			if (_migrated)
			{
				return;
			}

			if (!_userDefaults.BoolForKey(MigrationKey))
			{
				Migrate();
			}

			_migrated = true;
		}
	}

	private static void Migrate()
	{
		var standardDefaults = NSUserDefaults.StandardUserDefaults;
		foreach (var pair in standardDefaults.ToDictionary())
		{
			if (pair.Key is not NSString key)
			{
				continue;
			}

			var value = pair.Value?.ToString();
			if (value is not null && DataTypeSerializer.IsSerializedValue(value))
			{
				_userDefaults[key.ToString()] = pair.Value!;
				standardDefaults.RemoveObject(key.ToString());
			}
		}

		_userDefaults.SetBool(true, MigrationKey);
	}

	private partial Dictionary<string, string> LoadPlatform()
	{
		MigrateIfNeeded();

		var settings = new Dictionary<string, string>();

		foreach (var pair in _userDefaults.ToDictionary())
		{
			var key = pair.Key?.ToString();
			if (key is not null && key != MigrationKey && pair.Value?.ToString() is { } value)
			{
				settings[key] = value;
			}
		}

		return settings;
	}

	private partial void SetSettingPlatform(string key, string value)
		=> _userDefaults.SetString(value, key);

	private partial void RemoveSettingsPlatform(IReadOnlyCollection<string> keys)
	{
		foreach (var key in keys)
		{
			_userDefaults.RemoveObject(key);
		}
	}
}
