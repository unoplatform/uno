#nullable enable

using System.Collections.Generic;
using System.Linq;
using Foundation;

namespace Uno.Storage;

partial class NativeApplicationSettings
{
	private static partial bool SupportsLocalityPlatform() => false;

	partial void InitializePlatform() { }

	private partial bool ContainsSettingPlatform(string key)
		=> NSUserDefaults.StandardUserDefaults.ToDictionary().ContainsKey((NSString)key);

	private partial bool RemoveSettingPlatform(string key)
	{
		NSUserDefaults.StandardUserDefaults.RemoveObject((NSString)key);
		NSUserDefaults.StandardUserDefaults.Synchronize();
		return true;
	}

	private partial IEnumerable<string> GetKeysPlatform()
		=> NSUserDefaults.StandardUserDefaults.ToDictionary().Keys.Select(k => k.ToString()).ToArray();

	private partial bool TryGetSettingPlatform(string key, out string? value)
	{
		var nsValue = NSUserDefaults.StandardUserDefaults.ValueForKey((NSString)key);
		if (nsValue is not null)
		{
			value = nsValue.ToString();
			return true;
		}
		value = null;
		return false;
	}

	private partial void SetSettingPlatform(string key, string value)
	{
		var nativeObject = NSObject.FromObject(value);
		NSUserDefaults.StandardUserDefaults.SetValueForKey(nativeObject, (NSString)key);
		NSUserDefaults.StandardUserDefaults.Synchronize();
	}
}
