#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Windows.Storage;

namespace Uno.Storage;

/// <summary>
/// Provides access to raw application settings.
/// </summary>
internal partial class NativeApplicationSettings : INativeApplicationSettings
{
	private static readonly ConcurrentDictionary<ApplicationDataLocality, NativeApplicationSettings> _instances = new();

	private readonly ApplicationDataLocality _locality;

	public IEnumerable<string> Keys => GetKeysPlatform();

	private NativeApplicationSettings(ApplicationDataLocality locality)
	{
		_locality = locality;

		InitializePlatform();
	}

	internal static NativeApplicationSettings GetForLocality(ApplicationDataLocality locality)
	{
		if (!SupportsLocalityPlatform())
		{
			locality = ApplicationDataLocality.Local;
		}

		return _instances.GetOrAdd(locality, locality => new NativeApplicationSettings(locality));
	}

	private static partial bool SupportsLocalityPlatform();

	partial void InitializePlatform();

	public object? this[string key]
	{
		get
		{
			if (TryGetSettingPlatform(key, out var value))
			{
				return DeserializeValue(value);
			}

			return null;
		}
		set
		{
			if (value is not null)
			{
				SetSettingPlatform(key, SerializeValue(value));
			}
			else
			{
				RemoveSettingPlatform(key);
			}
		}
	}

	public bool Remove(string key) => RemoveSettingPlatform(key);

	public bool TryGetValue(string key, out object? value)
	{
		if (TryGetSettingPlatform(key, out var stringValue))
		{
			value = DeserializeValue(stringValue);
			return true;
		}
		value = null;
		return false;
	}

	public bool ContainsKey(string key) => ContainsSettingPlatform(key);

	public void RemoveKeys(Predicate<string> shouldRemove)
	{
		var keysToRemove = Keys.Where(k => shouldRemove(k)).ToList();
		foreach (var key in keysToRemove)
		{
			RemoveSettingPlatform(key);
		}
	}

	private partial bool ContainsSettingPlatform(string key);

	private partial void SetSettingPlatform(string key, string value);

	private partial bool TryGetSettingPlatform(string key, out string? value);

	private partial bool RemoveSettingPlatform(string key);

	private partial IEnumerable<string> GetKeysPlatform();

	internal IEnumerable<string> GetKeysWithPrefix(string prefix) =>
		Keys.Where(kvp => kvp.StartsWith(prefix, StringComparison.InvariantCulture));

	internal IEnumerable<string> GetKeys(Predicate<string> shouldInclude) =>
		Keys.Where(kvp => shouldInclude(kvp));

	internal IEnumerable<object?> GetValues(IEnumerable<string> keys) =>
		keys.Select(k => this[k]);

	private object? DeserializeValue(string? value) => DataTypeSerializer.Deserialize(value);

	private string SerializeValue(object value) => DataTypeSerializer.Serialize(value);

	internal void RemoveKeysWithPrefix(string internalSettingPrefix) =>
		RemoveKeys(k => k.StartsWith(internalSettingPrefix, StringComparison.Ordinal));
}
