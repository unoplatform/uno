#if __ANDROID__
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Windows.Foundation;
using AndroidSettings = Android.Provider.Settings;

namespace Windows.System
{
	public partial class Launcher
	{
		private const string MicrosoftUriPrefix = "ms-";
		private const string MicrosoftSettingsUri = "ms-settings";

		private static readonly (string uriPrefix, string intent)[] _settingsHandlers = new (string uriPrefix, string intent)[]
		{
			("sync", AndroidSettings.ActionSyncSettings),
			("appsfeatures-app", AndroidSettings.ActionApplicationDetailsSettings),
			("appsfeatures", AndroidSettings.ActionApplicationSettings),
			("defaultapps", AndroidSettings.ActionManageDefaultAppsSettings),
			("appsforwebsites", AndroidSettings.ActionManageDefaultAppsSettings),
			("cortana", AndroidSettings.ActionVoiceInputSettings),
			("bluetooth", AndroidSettings.ActionBluetoothSettings),
			("printers", AndroidSettings.ActionPrintSettings),
			("typing", AndroidSettings.ActionHardKeyboardSettings),
			("easeofaccess", AndroidSettings.ActionAccessibilitySettings),
			("network-airplanemode", AndroidSettings.ActionAirplaneModeSettings),
			("network-celluar", AndroidSettings.ActionNetworkOperatorSettings),
			("network-datausage", AndroidSettings.ActionDataUsageSettings),
			("network-wifisettings", AndroidSettings.ActionWifiSettings),
			("nfctransactions", AndroidSettings.ActionNfcSettings),
			("network-vpn", AndroidSettings.ActionVpnSettings),
			("network-wifi", AndroidSettings.ActionWifiSettings),
			("network", AndroidSettings.ActionWirelessSettings),
			("personalization", AndroidSettings.ActionDisplaySettings),
			("privacy", AndroidSettings.ActionPrivacySettings),
			("about", AndroidSettings.ActionDeviceInfoSettings),
			("apps-volume", AndroidSettings.ActionSoundSettings),
			("batterysaver", AndroidSettings.ActionBatterySaverSettings),
			("display", AndroidSettings.ActionDisplaySettings),
			("screenrotation", AndroidSettings.ActionDisplaySettings),
			("quiethours", AndroidSettings.ActionZenModePrioritySettings),
			("quietmomentshome", AndroidSettings.ActionZenModePrioritySettings),
			("nightlight", AndroidSettings.ActionNightDisplaySettings),
			("taskbar", AndroidSettings.ActionDisplaySettings),
			("notifications", AndroidSettings.ActionAppNotificationSettings),
			("storage", AndroidSettings.ActionInternalStorageSettings),
			("sound", AndroidSettings.ActionSoundSettings),
			("dateandtime", AndroidSettings.ActionDateSettings),
			("keyboard", AndroidSettings.ActionInputMethodSettings),
			("regionlanguage", AndroidSettings.ActionLocaleSettings),			
			("developers", AndroidSettings.ActionApplicationDevelopmentSettings),
		};

		static Launcher()
		{

		}

		public static async Task<bool> LaunchUriAsync(Uri uri)
		{
			if (uri == null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			if (Uno.UI.ContextHelper.Current == null)
			{
				throw new InvalidOperationException(
					"LaunchUriAsync was called too early in application lifetime. " +
					"App context needs to be initialized");
			}

			if (IsSpecialUri(uri) && await HandleSpecialUriAsync(uri))
			{
				return true;
			}

			return await LaunchUriInternalAsync(uri);
		}

		private static Task<bool> LaunchUriInternalAsync(Uri uri)
		{
			var androidUri = Android.Net.Uri.Parse(uri.OriginalString);
			var intent = new Intent(Intent.ActionView, androidUri);

			StartActivity(intent);
			return Task.FromResult(true);
		}

		private static bool IsSpecialUri(Uri uri) => uri.Scheme.StartsWith(MicrosoftUriPrefix, StringComparison.InvariantCultureIgnoreCase);

		private static Task<bool> HandleSpecialUriAsync(Uri uri)
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case MicrosoftSettingsUri: return HandleSettingsUriAsync(uri);
				default: return LaunchUriInternalAsync(uri);
			}
		}

		private static Task<bool> HandleSettingsUriAsync(Uri uri)
		{
			var settingsString = uri.AbsolutePath.ToLowerInvariant();
			//get exact match first
			var bestMatch = _settingsHandlers
				.Where(handler => handler.uriPrefix == settingsString)
				.Select(handler => handler.intent)
				.FirstOrDefault();
			var launchAction = bestMatch;
			if (launchAction == null)
			{
				var secondaryMatch = _settingsHandlers
					.Where(handler =>
						settingsString.StartsWith(handler.uriPrefix, StringComparison.InvariantCultureIgnoreCase))
					.Select(handler => handler.intent)
					.FirstOrDefault();
				launchAction = secondaryMatch;
			}

			var intent = new Intent(launchAction ?? AndroidSettings.ActionSettings);
			StartActivity(intent);
			return Task.FromResult(true);
		}

		private static void StartActivity(Intent intent) => ((Activity)Uno.UI.ContextHelper.Current).StartActivity(intent);
	}
}
#endif
