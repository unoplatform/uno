#if __ANDROID__
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using System;
using Android.Provider;
using System.Collections.Generic;
using Android.OS;

namespace Windows.System
{
	public static partial class Launcher
	{
		private const string MicrosoftSettingsUri = "ms-settings";

		private static readonly Lazy<Dictionary<string, string>> _settingsHandlers = new Lazy<Dictionary<string, string>>(() =>
		{
			var settings = new Dictionary<string, string>()
			{
				{ "sync", Settings.ActionSyncSettings },
				{ "appsfeatures-app", Settings.ActionApplicationDetailsSettings },
				{ "appsfeatures", Settings.ActionApplicationSettings },
				{ "defaultapps", Settings.ActionManageDefaultAppsSettings },
				{ "appsforwebsites", Settings.ActionManageDefaultAppsSettings },
				{ "cortana", Settings.ActionVoiceInputSettings },
				{ "bluetooth", Settings.ActionBluetoothSettings },
				{ "printers", Settings.ActionPrintSettings },
				{ "typing", Settings.ActionHardKeyboardSettings },
				{ "easeofaccess", Settings.ActionAccessibilitySettings },
				{ "network-airplanemode", Settings.ActionAirplaneModeSettings },
				{ "network-celluar", Settings.ActionNetworkOperatorSettings },
				{ "network-wifisettings", Settings.ActionWifiSettings },
				{ "nfctransactions", Settings.ActionNfcSettings },
				{ "network-vpn", Settings.ActionVpnSettings },
				{ "network-wifi", Settings.ActionWifiSettings },
				{ "network", Settings.ActionWirelessSettings },
				{ "personalization", Settings.ActionDisplaySettings },
				{ "privacy", Settings.ActionPrivacySettings },
				{ "about", Settings.ActionDeviceInfoSettings },
				{ "apps-volume", Settings.ActionSoundSettings },
				{ "batterysaver", Settings.ActionBatterySaverSettings },
				{ "display", Settings.ActionDisplaySettings },
				{ "screenrotation", Settings.ActionDisplaySettings },
				{ "quiethours", Settings.ActionZenModePrioritySettings },
				{ "quietmomentshome", Settings.ActionZenModePrioritySettings },
				{ "nightlight", Settings.ActionNightDisplaySettings },
				{ "taskbar", Settings.ActionDisplaySettings },
				{ "notifications", Settings.ActionAppNotificationSettings },
				{ "storage", Settings.ActionInternalStorageSettings },
				{ "sound", Settings.ActionSoundSettings },
				{ "dateandtime", Settings.ActionDateSettings },
				{ "keyboard", Settings.ActionInputMethodSettings },
				{ "regionlanguage", Settings.ActionLocaleSettings },
				{ "developers", Settings.ActionApplicationDevelopmentSettings },
			};
			if (Build.VERSION.SdkInt >= (BuildVersionCodes)28)
			{
#if __ANDROID_28__
				settings.Add("network-datausage", Settings.ActionDataUsageSettings);
#endif
			}
			return settings;
		});



		private static bool CanHandleSpecialUri(Uri uri)
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case MicrosoftSettingsUri: return true;
				default: return false;
			}
		}

		private static Task<bool> HandleSpecialUriAsync(Uri uri)
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case MicrosoftSettingsUri: return HandleSettingsUriAsync(uri);
				default: return LaunchUriActivityAsync(uri);
			}
		}

		private static Task<bool> HandleSettingsUriAsync(Uri uri)
		{
			var settingsString = uri.AbsolutePath.ToLowerInvariant();
			//get exact match first
			_settingsHandlers.Value.TryGetValue(settingsString, out var launchAction);
			if (string.IsNullOrEmpty(launchAction))
			{
				var secondaryMatch = _settingsHandlers.Value
					.Where(handler =>
						settingsString.StartsWith(handler.Key, StringComparison.InvariantCultureIgnoreCase))
					.Select(handler => handler.Value)
					.FirstOrDefault();
				launchAction = secondaryMatch;
			}

			var intent = new Intent(launchAction ?? Settings.ActionSettings);
			StartActivity(intent);
			return Task.FromResult(true);
		}
	}
}
#endif
