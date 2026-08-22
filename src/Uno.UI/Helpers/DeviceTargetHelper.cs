using System;
using Uno.Foundation.Logging;
using Windows.System;

namespace Uno.UI.Helpers;

internal static class DeviceTargetHelper
{
	private static readonly Lazy<bool> _usesAppleKeyboardLayout = new(() =>
		OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsTvOS() ||
		(OperatingSystem.IsBrowser() &&
			Uno.Foundation.WebAssemblyImports.EvalBool("/Mac|iPhone|iPad|iPod/.test(navigator?.platform ?? '')")));

	private static readonly Lazy<BrowserHostPlatform> _browserHost = new(GetBrowserHostPlatform);

	internal static bool IsNonDesktop() =>
		OperatingSystem.IsBrowser() ||
		IsMobile();

	internal static bool IsDesktop() =>
		OperatingSystem.IsWindows() ||
		OperatingSystem.IsLinux() ||
		OperatingSystem.IsMacOS();

	internal static bool IsMobile() =>
		OperatingSystem.IsAndroid() ||
		IsUIKit();

	internal static bool IsUIKit() =>
		OperatingSystem.IsIOS() ||
		OperatingSystem.IsMacCatalyst() ||
		OperatingSystem.IsTvOS();

	/// <summary>
	/// Returns true when the keyboard layout follows Apple conventions (using Command key).
	/// This covers native Apple platforms (macOS, iOS, Mac Catalyst, tvOS) and WebAssembly
	/// running in a browser on Apple devices (macOS, iPhone, iPad, iPod).
	/// </summary>
	internal static bool UsesAppleKeyboardLayout => _usesAppleKeyboardLayout.Value;

	/// <summary>
	/// Gets the platform-appropriate modifier key for standard commands (Cut, Copy, Paste, etc.).
	/// Returns VirtualKeyModifiers.Windows (Command key) on Apple keyboards,
	/// VirtualKeyModifiers.Control on all others.
	/// </summary>
	internal static VirtualKeyModifiers PlatformCommandModifier =>
		UsesAppleKeyboardLayout ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;

	/// <summary>
	/// The mobile OS hosting the browser, for features that must follow the host device's interaction
	/// conventions: on WebAssembly the OS APIs only ever report "browser", never the underlying device.
	/// </summary>
	internal static BrowserHostPlatform BrowserHost => _browserHost.Value;

	private static BrowserHostPlatform GetBrowserHostPlatform()
	{
		if (!OperatingSystem.IsBrowser())
		{
			return BrowserHostPlatform.Other;
		}

		try
		{
			return GetBrowserHostPlatform(
				Uno.Foundation.WebAssemblyImports.EvalString("navigator?.userAgent ?? ''"),
				Uno.Foundation.WebAssemblyImports.EvalString("navigator?.platform ?? ''"),
				Uno.Foundation.WebAssemblyImports.EvalBool("(navigator?.maxTouchPoints ?? 0) > 0"));
		}
		catch (Exception e)
		{
			// Probing navigator can fail (e.g. a Content-Security-Policy without 'unsafe-eval'). Falling back
			// to desktop conventions is preferable to faulting every caller.
			if (typeof(DeviceTargetHelper).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(DeviceTargetHelper).Log().Debug($"Unable to detect the browser host platform: {e}");
			}

			return BrowserHostPlatform.Other;
		}
	}

	/// <remarks>
	/// iPadOS 13+ browsers request the desktop site by default and report a Mac user agent and platform,
	/// so touch capability is all that tells an iPad apart from a Mac.
	/// </remarks>
	internal static BrowserHostPlatform GetBrowserHostPlatform(string userAgent, string platform, bool isTouchCapable)
	{
		if (userAgent.Contains("Android", StringComparison.Ordinal))
		{
			return BrowserHostPlatform.Android;
		}

		if (userAgent.Contains("iPhone", StringComparison.Ordinal)
			|| userAgent.Contains("iPad", StringComparison.Ordinal)
			|| userAgent.Contains("iPod", StringComparison.Ordinal))
		{
			return BrowserHostPlatform.iOS;
		}

		if (isTouchCapable
			&& (platform.Contains("Mac", StringComparison.Ordinal) || userAgent.Contains("Macintosh", StringComparison.Ordinal)))
		{
			return BrowserHostPlatform.iOS;
		}

		return BrowserHostPlatform.Other;
	}
}
