// Code adapted from Xamarin.Essentials
// https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/VersionTracking/VersionTracking.shared.cs#L8

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Uno.UI.Toolkit.Helpers
{
	public class VersionTracker
	{
		private const string VersionTrackerPrefix = "Uno.UI.Toolkit.VersionTracker.";
		private const string VersionsSettingKey = "Versions";
		private const string BuildsSettingKey = "Builds";

		private static readonly Lazy<VersionTracker> _current = new Lazy<VersionTracker>(() => new VersionTracker());

		private readonly ApplicationDataContainer _settings;
		private readonly Dictionary<string, List<string>> _versionHistory;

		private VersionTracker()
		{
			_settings = ApplicationData.Current.LocalSettings;
			
			IsFirstLaunch =
				!_settings.Values.ContainsKey(GetSettingKey(VersionsSettingKey)) ||
				!_settings.Values.ContainsKey(GetSettingKey(BuildsSettingKey));

			if (IsFirstLaunch)
			{
				_versionHistory = new Dictionary<string, List<string>>
				{
					{ VersionsSettingKey, new List<string>() },
					{ BuildsSettingKey, new List<string>() }
				};
			}
			else
			{
				_versionHistory = new Dictionary<string, List<string>>
				{
					{ VersionsSettingKey, ReadHistory(VersionsSettingKey).ToList() },
					{ BuildsSettingKey, ReadHistory(BuildsSettingKey).ToList() }
				};
			}

			IsFirstLaunchForCurrentVersion = !_versionHistory[VersionsSettingKey].Contains(CurrentVersion);
			if (IsFirstLaunchForCurrentVersion)
			{
				_versionHistory[VersionsSettingKey].Add(CurrentVersion);
			}

			IsFirstLaunchForCurrentBuild = !_versionHistory[BuildsSettingKey].Contains(CurrentBuild);
			if (IsFirstLaunchForCurrentBuild)
			{
				_versionHistory[BuildsSettingKey].Add(CurrentBuild);
			}

			if (IsFirstLaunchForCurrentVersion || IsFirstLaunchForCurrentBuild)
			{
				WriteHistory(VersionsSettingKey, _versionHistory[VersionsSettingKey]);
				WriteHistory(BuildsSettingKey, _versionHistory[BuildsSettingKey]);
			}
		}

		public static VersionTracker Current => _current.Value;

		[Preserve]
		public void Track()
		{
		}

		public bool IsFirstLaunch { get; private set; }

		public bool IsFirstLaunchForCurrentVersion { get; private set; }

		public bool IsFirstLaunchForCurrentBuild { get; private set; }

		public string CurrentVersion => Package.Current.Id.Version.ToString();

		public string CurrentBuild => Package.Current.Id.Version.Build.ToString(CultureInfo.InvariantCulture);

		public string PreviousVersion => GetPrevious(VersionsSettingKey);

		public string PreviousBuild => GetPrevious(BuildsSettingKey);

		public string FirstInstalledVersion => _versionHistory[VersionsSettingKey].FirstOrDefault();

		public string FirstInstalledBuild => _versionHistory[BuildsSettingKey].FirstOrDefault();

		public IEnumerable<string> VersionHistory => _versionHistory[VersionsSettingKey].ToArray();

		public IEnumerable<string> BuildHistory => _versionHistory[BuildsSettingKey].ToArray();

		public bool IsFirstLaunchForVersion(string version)
			=> CurrentVersion == version && IsFirstLaunchForCurrentVersion;

		public bool IsFirstLaunchForBuild(string build)
			=> CurrentBuild == build && IsFirstLaunchForCurrentBuild;

		private string[] ReadHistory(string key)
			=> _settings.Values[GetSettingKey(key)]?.ToString()?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

		private void WriteHistory(string key, IEnumerable<string> history)
			=> _settings.Values[GetSettingKey(key)] = string.Join("|", history);

		private string GetPrevious(string key)
		{
			var trail = _versionHistory[GetSettingKey(key)];
			return (trail.Count >= 2) ? trail[trail.Count - 2] : null;
		}

		private string GetSettingKey(string key) => VersionTrackerPrefix + key;
	}
}
