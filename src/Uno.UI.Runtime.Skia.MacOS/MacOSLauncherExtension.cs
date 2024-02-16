using Windows.System;

using Uno.Foundation.Extensibility;
using Uno.Extensions.System;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSLauncherExtension : ILauncherExtension
{
	private const string MicrosoftSettingsUri = "ms-settings";

	private static readonly MacOSLauncherExtension _instance = new();

	private MacOSLauncherExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(ILauncherExtension), _ => _instance);

	public Task<bool> LaunchUriAsync(Uri uri)
	{
		// null check is done by the caller

		if (Launcher.IsSpecialUri(uri) && CanHandleSpecialUri(uri))
		{
			return Task.FromResult(HandleSpecialUri(uri));
		}

		return Task.FromResult(NativeUno.uno_application_open_url(uri.AbsoluteUri));
	}

	public Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType)
	{
		var canOpenUri = !Launcher.IsSpecialUri(uri) ? NativeUno.uno_application_query_url_support(uri.AbsoluteUri) : CanHandleSpecialUri(uri);

		var supportStatus = canOpenUri ? LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;
		return Task.FromResult(supportStatus);
	}

	// copied and adapted from src/Uno.UWP/System/Launcher.macOS.SpecialUris.cs
	private static readonly Lazy<Dictionary<string, string>> _settingsHandlers = new(() =>
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

	private static bool HandleSpecialUri(Uri uri)
	{
		switch (uri.Scheme.ToLowerInvariant())
		{
			case MicrosoftSettingsUri: return HandleSettingsUri(uri);
			default: throw new InvalidOperationException("This special URI is not supported on macOS");
		}
	}

	private static bool HandleSettingsUri(Uri uri)
	{
		var settingsString = uri.AbsolutePath.ToLowerInvariant();
		// get exact match first
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

		var url = string.IsNullOrEmpty(launchAction) ? "/System/Applications/System Preferences.app" : $"/System/Library/PreferencePanes/{launchAction}.prefPane";
		return NativeUno.uno_application_open_url(url);
	}
}
