#nullable enable

using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Windows.Storage;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private const string PreferencesName = "UnoApplicationData";

	/// <summary>
	/// Flags that the migration from the default shared preferences already ran. It lives in the same
	/// preferences file as the user data, so it must be kept out of the settings surface.
	/// </summary>
	private const string MigrationKey = "__uno_migrated";

	private static readonly object _migrationGate = new();

	private static ISharedPreferences? _preferences;

	private static partial bool SupportsLocalityPlatform() => false;

	partial void InitializePlatform() => _ = Preferences;

	/// <summary>
	/// Settings used to live in the default shared preferences, the file the AndroidX preference screens and
	/// other libraries write to. They are moved once into a preferences file owned by Uno Platform, so that
	/// enumerating or clearing application settings no longer sees — or deletes — keys Uno does not own.
	/// </summary>
	private static ISharedPreferences Preferences
	{
		get
		{
			if (_preferences is not null)
			{
				return _preferences;
			}

			// The lock ensures a concurrent caller waits for the migration to complete instead of
			// observing a partially migrated store.
			lock (_migrationGate)
			{
				if (_preferences is null)
				{
					var context = ApplicationData.GetAndroidAppContext();
					var preferences = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)!;

					if (!preferences.GetBoolean(MigrationKey, false))
					{
						Migrate(context, preferences);
					}

					_preferences = preferences;
				}
			}

			return _preferences;
		}
	}

	private static void Migrate(Context context, ISharedPreferences preferences)
	{
		using var legacyPreferences = context.GetSharedPreferences(context.PackageName + "_preferences", FileCreationMode.Private)!;

		var editor = preferences.Edit()!;
		var legacyEditor = legacyPreferences.Edit()!;

		var legacyEntries = legacyPreferences.All;
		if (legacyEntries is not null)
		{
			foreach (var pair in legacyEntries)
			{
				if (pair.Value?.ToString() is { } value && DataTypeSerializer.IsSerializedValue(value))
				{
					editor.PutString(pair.Key, value);
					legacyEditor.Remove(pair.Key);
				}
			}
		}

		editor.PutBoolean(MigrationKey, true);
		editor.Commit();
		legacyEditor.Commit();
	}

	private static bool IsMigrationKey(string key) => key == MigrationKey;

	private partial bool ContainsSettingPlatform(string key)
		=> !IsMigrationKey(key) && (Preferences.All?.ContainsKey(key) ?? false);

	private partial bool RemoveSettingPlatform(string key)
	{
		var exists = ContainsSettingPlatform(key);
		if (exists)
		{
			Preferences.Edit()?.Remove(key)?.Commit();
		}

		return exists;
	}

	private partial IEnumerable<string> GetKeysPlatform()
		=> Preferences.All?.Keys.Where(k => !IsMigrationKey(k)).ToArray() ?? [];

	private partial bool TryGetSettingPlatform(string key, out string? value)
	{
		if (!IsMigrationKey(key) && Preferences.All?.TryGetValue(key, out var serializedValue) == true)
		{
			value = serializedValue?.ToString();
			return true;
		}

		value = null;
		return false;
	}

	private partial void SetSettingPlatform(string key, string value)
		=> Preferences.Edit()?.PutString(key, value)?.Commit();
}
