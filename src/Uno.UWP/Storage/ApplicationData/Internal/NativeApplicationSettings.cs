﻿#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

	private NativeApplicationSettings(ApplicationDataLocality locality)
	{
		_locality = locality;

		InitializePlatform();
	}

	internal static NativeApplicationSettings GetForLocality(ApplicationDataLocality locality)
	{
		if (!SupportsLocality())
		{
			locality = ApplicationDataLocality.Local;
		}

		return _instances.GetOrAdd(locality, locality => new NativeApplicationSettings(locality));
	}

	private static partial bool SupportsLocality();

	partial void InitializePlatform();

	public object? this[string key]
	{
		get
		{
			if (TryGetSetting(key, out var value))
			{
				return DeserializeValue(value);
			}

			return null;
		}
		set
		{
			if (value is not null)
			{
				SetSetting(key, SerializeValue(value));
			}
			else
			{
				Remove(key);
			}
		}
	}

	private partial void SetSetting(string key, string value);

	private partial bool TryGetSetting(string key, out string? value);

	internal IEnumerable<string> GetKeysWithPrefix(string prefix) =>
		Keys.Where(kvp => kvp.StartsWith(prefix, StringComparison.InvariantCulture));

	private object? DeserializeValue(string? value) => DataTypeSerializer.Deserialize(value);

	private string SerializeValue(object value) => DataTypeSerializer.Serialize(value);
}
