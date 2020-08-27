// Version tracking adapted from Xamarin.Essentials
// https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/VersionTracking/VersionTracking.shared.cs#L8

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Uno.UI.Toolkit.Helpers
{
	public class LaunchTracker
	{
		private const string VersionTrackerPrefix = "Uno.UI.Toolkit.LaunchTracker.";
		private const string VersionsSettingKey = "Versions";
		private const string BuildsSettingKey = "Builds";
		private const string PreviousLaunchDateKey = "PreviousLaunchDate";
		private const string LaunchCountKey = "LaunchCount";

		private static readonly Lazy<LaunchTracker> _current = new Lazy<LaunchTracker>(() => new LaunchTracker());

		private readonly ApplicationDataContainer _settings;
		private readonly Dictionary<string, List<string>> _versionHistory;

		private int _launchCount = -1;

		/// <summary>
		/// Initializes LaunchTracker before first use.
		/// </summary>
		private LaunchTracker()
		{
			_settings = ApplicationData.Current.LocalSettings;

			CurrentLaunchDate = DateTimeOffset.UtcNow;

			if (_settings.Values.TryGetValue(PreviousLaunchDateKey, out var previousLaunchDateValue))
			{
				PreviousLaunchDate = DateTimeOffset.Parse(previousLaunchDateValue.ToString(), CultureInfo.InvariantCulture);
			}

			_settings.Values[PreviousLaunchDateKey] = CurrentLaunchDate.ToString(CultureInfo.InvariantCulture);

			LaunchCount++;

			IsFirstLaunch = LaunchCount == 1;

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

		/// <summary>
		/// Retrieves the current instance.
		/// </summary>
		public static LaunchTracker Current => _current.Value;

		/// <summary>
		/// Ensure this method is called during each launch
		/// of your application to track the launch statistics.
		/// </summary>
		public void Track()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				this.Log().LogInformation("Launch tracker initialized");
			}
		}

		/// <summary>
		/// A value indicating whether the application
		/// was launched for the very first time.
		/// </summary>
		public bool IsFirstLaunch { get; private set; }

		/// <summary>
		/// A value indicating whether the application
		/// was launched for the first time with the current version.
		/// </summary>
		public bool IsFirstLaunchForCurrentVersion { get; private set; }

		/// <summary>
		/// A value indicating whether the application
		/// was launched for the first time with the current build number.
		/// </summary>
		public bool IsFirstLaunchForCurrentBuild { get; private set; }

		/// <summary>
		/// Number of time the application was launched.
		/// </summary>
		public int LaunchCount
		{
			get
			{
				if (_launchCount < 0)
				{
					if (_settings.Values.TryGetValue(LaunchCountKey, out var launchCountValue))
					{
						_launchCount = (int)launchCountValue;
					}
					else
					{
						_launchCount = 0;
					}
				}
				return _launchCount;
			}
			private set
			{
				_launchCount = value;
				_settings.Values[LaunchCountKey] = _launchCount;
			}
		}

		/// <summary>
		/// The date the application started for the current launch.
		/// </summary>
		public DateTimeOffset CurrentLaunchDate { get; set; }

		/// <summary>
		/// The date the application started previously.
		/// </summary>
		public DateTimeOffset? PreviousLaunchDate { get; set; }

		/// <summary>
		/// Current version of the application.
		/// </summary>
		public string CurrentVersion => Package.Current.Id.Version.ToString();

		/// <summary>
		/// Current build of the application.
		/// </summary>
		public string CurrentBuild => Package.Current.Id.Version.Build.ToString(CultureInfo.InvariantCulture);

		/// <summary>
		/// Previous version of the application. Can be <see langword="null"/>.
		/// </summary>
		public string PreviousVersion => GetPrevious(VersionsSettingKey);

		/// <summary>
		/// Previous build of the application. Can be <see langword="null"/>.
		/// </summary>
		public string PreviousBuild => GetPrevious(BuildsSettingKey);

		/// <summary>
		/// First version of the application that was installed on this device.
		/// </summary>
		public string FirstInstalledVersion => _versionHistory[VersionsSettingKey].FirstOrDefault();

		/// <summary>
		/// First build of the application that was installed on this device.
		/// </summary>
		public string FirstInstalledBuild => _versionHistory[BuildsSettingKey].FirstOrDefault();

		/// <summary>
		/// Version history.
		/// </summary>
		public IEnumerable<string> VersionHistory => _versionHistory[VersionsSettingKey].ToArray();

		/// <summary>
		/// Build history.
		/// </summary>
		public IEnumerable<string> BuildHistory => _versionHistory[BuildsSettingKey].ToArray();

		/// <summary>
		/// Checks if this is the first launch for a given version.
		/// </summary>
		/// <param name="version">App version.</param>
		/// <returns>A value indicating whether this is the first launch for the given version.</returns>
		public bool IsFirstLaunchForVersion(string version)
			=> CurrentVersion == version && IsFirstLaunchForCurrentVersion;

		/// <summary>
		/// Checks if this is the first launch for a given build.
		/// </summary>
		/// <param name="version">App version.</param>
		/// <returns>A value indicating whether this is the first launch for the given build.</returns>
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
