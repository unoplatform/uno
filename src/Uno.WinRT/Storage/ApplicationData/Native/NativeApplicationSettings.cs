#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Uno.Storage;

/// <summary>
/// Provides access to raw application settings.
/// </summary>
/// <remarks>
/// The settings are read from the native store once and kept in memory, writing through on every change.
/// Reading a single setting, or enumerating them, would otherwise walk the whole native store — a JNI map on
/// Android, the user defaults dictionary on Apple platforms, local storage on WebAssembly — on every access.
/// This means changes made to the store behind Uno Platform's back, by native code, are not observed.
/// </remarks>
internal partial class NativeApplicationSettings
{
	private static readonly ConcurrentDictionary<ApplicationDataLocality, NativeApplicationSettings> _instances = new();

	private readonly ApplicationDataLocality _locality;
	private readonly object _gate = new();

	private Dictionary<string, string>? _settings;

	private NativeApplicationSettings(ApplicationDataLocality locality)
	{
		_locality = locality;
	}

	internal static NativeApplicationSettings GetForLocality(ApplicationDataLocality locality)
	{
		if (!SupportsLocalityPlatform())
		{
			locality = ApplicationDataLocality.Local;
		}

		return _instances.GetOrAdd(locality, locality => new NativeApplicationSettings(locality));
	}

	private Dictionary<string, string> Settings
	{
		get
		{
			lock (_gate)
			{
				return _settings ??= LoadPlatform();
			}
		}
	}

	public IEnumerable<string> Keys
	{
		get
		{
			lock (_gate)
			{
				return Settings.Keys.ToArray();
			}
		}
	}

	public object? this[string key]
	{
		get
		{
			lock (_gate)
			{
				return Settings.TryGetValue(key, out var value) ? DeserializeValue(value) : null;
			}
		}
		set
		{
			if (value is null)
			{
				Remove(key);
				return;
			}

			var serializedValue = SerializeValue(value);

			lock (_gate)
			{
				Settings[key] = serializedValue;
				SetSettingPlatform(key, serializedValue);
			}
		}
	}

	public bool Remove(string key)
	{
		lock (_gate)
		{
			if (!Settings.Remove(key))
			{
				return false;
			}

			RemoveSettingsPlatform([key]);
			return true;
		}
	}

	public bool TryGetValue(string key, out object? value)
	{
		lock (_gate)
		{
			if (Settings.TryGetValue(key, out var stringValue))
			{
				value = DeserializeValue(stringValue);
				return true;
			}
		}

		value = null;
		return false;
	}

	public bool ContainsKey(string key)
	{
		lock (_gate)
		{
			return Settings.ContainsKey(key);
		}
	}

	public void RemoveKeys(Predicate<string> shouldRemove)
	{
		lock (_gate)
		{
			var keysToRemove = Settings.Keys.Where(k => shouldRemove(k)).ToArray();
			if (keysToRemove.Length == 0)
			{
				return;
			}

			foreach (var key in keysToRemove)
			{
				Settings.Remove(key);
			}

			// Removed in one go: persisting key by key rewrites the whole store once per key on some platforms.
			RemoveSettingsPlatform(keysToRemove);
		}
	}

	internal void RemoveKeysWithPrefix(string prefix) =>
		RemoveKeys(k => k.StartsWith(prefix, StringComparison.Ordinal));

	internal IEnumerable<string> GetKeys(Predicate<string> shouldInclude) =>
		Keys.Where(k => shouldInclude(k));

	internal IEnumerable<string> GetKeysWithPrefix(string prefix) =>
		GetKeys(k => k.StartsWith(prefix, StringComparison.Ordinal));

	private static object? DeserializeValue(string? value) => DataTypeSerializer.Deserialize(value);

	private static string SerializeValue(object value) => DataTypeSerializer.Serialize(value);

	private static partial bool SupportsLocalityPlatform();

	/// <summary>
	/// Reads every setting currently held by the native store.
	/// </summary>
	private partial Dictionary<string, string> LoadPlatform();

	private partial void SetSettingPlatform(string key, string value);

	private partial void RemoveSettingsPlatform(IReadOnlyCollection<string> keys);
}
