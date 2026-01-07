#nullable enable

using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Preferences;
using Windows.Storage;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private static partial bool SupportsLocalityPlatform() => false;

	partial void InitializePlatform() { }

	private partial bool ContainsSettingPlatform(string key)
	{
		using var preferences = CreatePreferences();
		return preferences.All.ContainsKey(key);
	}

	private partial bool RemoveSettingPlatform(string key)
	{
		using var preferences = CreatePreferences();
		preferences.Edit()?.Remove(key)?.Commit();
		return true;
	}

	private partial IEnumerable<string> GetKeysPlatform()
	{
		using var preferences = CreatePreferences();
		return preferences.All.Keys.ToArray();
	}

	private partial bool TryGetSettingPlatform(string key, out string? value)
	{
		using var preferences = CreatePreferences();
		if (preferences.All.TryGetValue(key, out var serializedValue))
		{
			value = serializedValue?.ToString();
			return true;
		}
		value = null;
		return false;
	}

	private partial void SetSettingPlatform(string key, string value)
	{
		using var preferences = CreatePreferences();
		preferences.Edit()?.PutString(key, value)?.Commit();
	}

	private ISharedPreferences CreatePreferences() =>
		PreferenceManager.GetDefaultSharedPreferences(ApplicationData.GetAndroidAppContext())!;
}
