#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Uno.Storage.Internal;

/// <summary>
/// A settings store the legacy migration reads from or writes to.
/// </summary>
internal interface ISettingsMigrationStore
{
	IEnumerable<KeyValuePair<string, object>> Entries { get; }

	bool ContainsKey(string key);

	void SetValue(string key, object value);

	void Remove(string key);

	/// <summary>
	/// Persists pending changes, returning <see langword="false"/> when they could not be saved.
	/// </summary>
	bool Synchronize();
}

/// <summary>
/// Moves settings serialized by pre-7.0 Uno Platform versions from one store to another.
/// </summary>
internal static class LegacySettingsMigration
{
	private static readonly HashSet<string> _legacyTypeNames =
		new(DataTypeSerializer.SupportedTypes.Select(type => type.FullName!), StringComparer.Ordinal);

	internal static int Migrate(ISettingsMigrationStore source, ISettingsMigrationStore target)
	{
		List<string>? legacyKeys = null;

		foreach (var (key, value) in source.Entries.ToList())
		{
			if (value.ToString() is not { } valueText || !IsLegacyValue(valueText))
			{
				continue;
			}

			// Anything already written through the 7.0 API is newer than what was left behind, so it wins.
			if (!target.ContainsKey(key))
			{
				target.SetValue(key, value);
			}

			(legacyKeys ??= new List<string>()).Add(key);
		}

		if (legacyKeys is null)
		{
			return 0;
		}

		// There is no transaction, so nothing is deleted until the copy is durably saved: an interrupted
		// or failed migration can leave an entry in both places - and the next call finishes the job -
		// but never in neither.
		if (!target.Synchronize())
		{
			throw new IOException("The migrated application settings could not be saved. The original values were left in place.");
		}

		foreach (var legacyKey in legacyKeys)
		{
			source.Remove(legacyKey);
		}

		if (!source.Synchronize() && typeof(LegacySettingsMigration).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(LegacySettingsMigration).Log().LogWarning(
				"The legacy application settings were migrated, but removing them from their original location could not be saved. They will be removed again on the next migration.");
		}

		return legacyKeys.Count;
	}

	private static bool IsLegacyValue(string value)
	{
		var separatorIndex = value.IndexOf(':');

		return separatorIndex > 0 && _legacyTypeNames.Contains(value.Substring(0, separatorIndex));
	}
}
