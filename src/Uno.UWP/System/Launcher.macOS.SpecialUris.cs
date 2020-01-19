#if __MACOS__
using Foundation;
using AppKit;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Windows.System
{
	public static partial class Launcher
	{
		private const string MicrosoftSettingsUri = "ms-settings";

		private static readonly Lazy<Dictionary<string, string>> _settingsHandlers = new Lazy<Dictionary<string, string>>(() =>
		{
			var settings = new Dictionary<string, string>()
			{
				{ "signinoptions-launchfaceenrollment", "TouchID" },
				{ "signinoptions-launchfingerprintenrollment", "TouchID" },
				{ "signinoptions", "Accounts" },
				{ "emailandaccounts", "InternetAccounts" },
				{ "tabletmode", "Expose" },
				{ "personalization-start", "Expose" },
				{ "personalization-background", "DesktopScreenEffectsPref" },
				{ "personalization", "Appearance" },
				{ "bluetooth", "Bluetooth" },
				{ "dateandtime", "DateAndTime" },
				{ "region", "Localization" },
				{ "typing", "Keyboard" },
				{ "display", "Displays" },
				{ "screenrotation", "Displays" },
				{ "taskbar", "Dock" },
				{ "batterysaver", "EnergySaver" },
				{ "powersleep", "EnergySaver" },
				{ "otherusers", "FamilySharingPrefPane" },
				{ "mousetouchpad", "Mouse" },
				{ "devices-touchpad", "Trackpad" },
				{ "network", "Network" },
				{ "privacy-notifications", "Notifications" },
				{ "printers", "PrintAndFax" },
				{ "privacy", "Security" },
				{ "crossdevice", "SharingPref" },
				{ "quiethours", "ScreenTime" },
				{ "quietmomentshome", "ScreenTime" },
				{ "sound", "Sound" },
				{ "windowsupdate", "SoftwareUpdate" },
				{ "cortana-windowssearch", "Spotlight" },
				{ "cortana", "Speech" },
				{ "storage", "StartupDisk" },
				{ "backup", "TimeMachine" },
				{ "easeofaccess", "UniversalAccessPref" }
			};
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
				default: throw new InvalidOperationException("This special URI is not supported on iOS");
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
			NSUrl url;
			if (string.IsNullOrEmpty(launchAction))
			{
				url = NSUrl.CreateFileUrl(new string[] { "/System/Applications/System Preferences.app" });
			}
			else
			{
				url = NSUrl.CreateFileUrl(new string[] { $@"/System/Library/PreferencePanes/{launchAction}.prefPane" });
			}
			NSWorkspace.SharedWorkspace.OpenUrl(url);
			return Task.FromResult(true);
		}	
	}
}
#endif
