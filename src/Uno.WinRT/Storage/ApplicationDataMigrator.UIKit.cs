#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Uno.Storage.Internal;
using Windows.Storage;

namespace Uno.Storage;

/// <summary>
/// Brings application settings written by Uno Platform versions prior to 7.0 into the storage
/// used since 7.0.
/// </summary>
public static class ApplicationDataMigrator
{
	private static readonly HashSet<string> _legacyTypeNames =
		new(DataTypeSerializer.SupportedTypes.Select(type => type.FullName!), StringComparer.Ordinal);

	/// <summary>
	/// Moves application settings written by Uno Platform versions prior to 7.0 out of
	/// <see cref="NSUserDefaults.StandardUserDefaults"/> and into the dedicated <c>UnoApplicationData</c>
	/// suite that has backed <see cref="ApplicationData.LocalSettings"/> and
	/// <see cref="ApplicationData.RoamingSettings"/> since 7.0.
	/// </summary>
	/// <returns>The number of legacy entries taken out of the standard user defaults.</returns>
	/// <remarks>
	/// Nothing moves unless this is called, so an app that never shipped on a pre-7.0 version of Uno
	/// Platform never needs it. Call it during startup, before the settings are first read.
	/// <para>
	/// Repeat calls are safe: a key that already exists in the destination keeps its current (newer)
	/// value, and an install with nothing to migrate is a no-op that returns 0.
	/// </para>
	/// </remarks>
	public static int MigrateSettings()
	{
		var source = NSUserDefaults.StandardUserDefaults;
		var target = UnoUserDefaults.Instance;
		var targetDomain = UnoUserDefaults.Domain;

		List<string>? legacyKeys = null;

		foreach (var pair in source.ToDictionary())
		{
			if (pair.Key is not NSString key || pair.Value is null || pair.Value.ToString() is not { } valueText || !IsLegacyValue(valueText))
			{
				continue;
			}

			// Anything already written through the 7.0 API is newer than what was left behind, so it wins.
			if (!targetDomain.ContainsKey(key))
			{
				target.SetValueForKey(pair.Value, key);
			}

			(legacyKeys ??= new List<string>()).Add(key.ToString());
		}

		if (legacyKeys is null)
		{
			return 0;
		}

		// NSUserDefaults has no transaction. Committing every write before the first delete is what makes
		// an interrupted migration recoverable: a crash can leave an entry in both places - and the next
		// call finishes the job - but never in neither.
		target.Synchronize();

		foreach (var legacyKey in legacyKeys)
		{
			source.RemoveObject(legacyKey);
		}

		source.Synchronize();

		return legacyKeys.Count;
	}

	private static bool IsLegacyValue(string value)
	{
		var separatorIndex = value.IndexOf(':');

		return separatorIndex > 0 && _legacyTypeNames.Contains(value.Substring(0, separatorIndex));
	}
}
