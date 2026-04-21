#nullable enable

using Foundation;
using UIKit;

namespace Uno.Helpers.Theming;

internal static partial class SystemThemeHelper
{
	private static NSObject? _darkerSystemColorsChangedObserver;

	internal static SystemTheme GetSystemTheme()
	{
		//Ensure the current device is running 12.0 or higher, because `TraitCollection.UserInterfaceStyle` was introduced in iOS 12.0
		if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
		{
			if (UIScreen.MainScreen.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark)
			{
				return SystemTheme.Dark;
			}
		}
		return SystemTheme.Light;
	}

	static partial void ObserveThemeChangesPlatform()
	{
		if (_darkerSystemColorsChangedObserver is not null)
		{
			return;
		}

		_darkerSystemColorsChangedObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			UIApplication.DarkerSystemColorsStatusDidChangeNotification,
			_ => UIApplication.SharedApplication.BeginInvokeOnMainThread(RefreshHighContrast));
	}

	static partial void GetIsHighContrastEnabledPlatform(ref bool result)
	{
		result = UIAccessibility.DarkerSystemColorsEnabled;
	}

	static partial void GetHighContrastSchemeNamePlatform(ref string result)
	{
		if (UIAccessibility.DarkerSystemColorsEnabled)
		{
			result = GetMobileHighContrastSchemeName(GetSystemTheme());
		}
	}

	static partial void GetHighContrastSystemColorsPlatform(ref HighContrastSystemColors? result)
	{
		if (UIAccessibility.DarkerSystemColorsEnabled)
		{
			result = GetMobileHighContrastSystemColors(GetSystemTheme());
		}
	}
}
