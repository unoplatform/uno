#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
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
	/// <summary>
	/// Moves application settings written by Uno Platform versions prior to 7.0 out of
	/// <see cref="NSUserDefaults.StandardUserDefaults"/> and into the dedicated <c>UnoApplicationData</c>
	/// suite that has backed <see cref="ApplicationData.LocalSettings"/> and
	/// <see cref="ApplicationData.RoamingSettings"/> since 7.0.
	/// </summary>
	/// <returns>The number of legacy entries taken out of the standard user defaults.</returns>
	/// <exception cref="IOException">
	/// The suite could not be saved. Nothing was removed from the standard user defaults, so a later call can retry.
	/// </exception>
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
		var standard = NSUserDefaults.StandardUserDefaults;

		return LegacySettingsMigration.Migrate(
			new NSUserDefaultsStore(standard, standard.ToDictionary()),
			new NSUserDefaultsStore(UnoUserDefaults.Instance, UnoUserDefaults.Domain));
	}

	private sealed class NSUserDefaultsStore : ISettingsMigrationStore
	{
		private readonly NSUserDefaults _defaults;
		private readonly NSDictionary _domain;

		public NSUserDefaultsStore(NSUserDefaults defaults, NSDictionary domain)
		{
			_defaults = defaults;
			_domain = domain;
		}

		public IEnumerable<KeyValuePair<string, object>> Entries
		{
			get
			{
				foreach (var pair in _domain)
				{
					if (pair.Key is NSString key && key.ToString() is { } name && pair.Value is { } value)
					{
						yield return new(name, value);
					}
				}
			}
		}

		public bool ContainsKey(string key) => _domain.ContainsKey((NSString)key);

		public void SetValue(string key, object value) => _defaults.SetValueForKey((NSObject)value, (NSString)key);

		public void Remove(string key) => _defaults.RemoveObject(key);

		public bool Synchronize() => _defaults.Synchronize();
	}
}
