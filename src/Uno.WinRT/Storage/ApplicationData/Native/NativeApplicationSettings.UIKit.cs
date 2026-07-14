#nullable enable

using System.Collections.Generic;
using System.Linq;
using Foundation;
using Windows.Storage;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private const string SuiteName = "UnoApplicationData";

	/// <summary>
	/// Flags that the migration from the standard user defaults already ran. It lives in the same suite as the
	/// user data, so it must be kept out of the settings surface.
	/// </summary>
	private const string MigrationKey = ApplicationDataContainer.InternalSettingPrefix + "Migrated";

	private static readonly NSUserDefaults _userDefaults = new(SuiteName, NSUserDefaultsType.SuiteName);
	private static readonly object _migrationGate = new();

	private static bool _migrated;

	private static partial bool SupportsLocalityPlatform() => false;

	partial void InitializePlatform() => MigrateIfNeeded();

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

	private static bool IsMigrationKey(string key) => key == MigrationKey;

	private partial bool ContainsSettingPlatform(string key)
		=> !IsMigrationKey(key) && _userDefaults[key] is not null;

	private partial bool RemoveSettingPlatform(string key)
	{
		var exists = ContainsSettingPlatform(key);
		if (exists)
		{
			_userDefaults.RemoveObject(key);
		}

		return exists;
	}

	private partial IEnumerable<string> GetKeysPlatform()
		=> _userDefaults
			.ToDictionary()
			.Keys
			.Select(k => k.ToString())
			.Where(k => !IsMigrationKey(k))
			.ToArray();

	private partial bool TryGetSettingPlatform(string key, out string? value)
	{
		if (!IsMigrationKey(key) && _userDefaults[key] is { } nsValue)
		{
			value = nsValue.ToString();
			return true;
		}

		value = null;
		return false;
	}

	private partial void SetSettingPlatform(string key, string value)
		=> _userDefaults.SetString(value, key);
}
