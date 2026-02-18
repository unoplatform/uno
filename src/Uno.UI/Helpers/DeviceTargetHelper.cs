using System;
using Windows.System;

namespace Uno.UI.Helpers;

internal static class DeviceTargetHelper
{
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
	/// This covers native Apple platforms (macOS, iOS, Mac Catalyst) and WebAssembly
	/// running in a browser on Apple devices (macOS, iPad, iPhone).
	/// </summary>
	internal static bool UsesAppleKeyboardLayout() =>
		OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst()
		|| (OperatingSystem.IsBrowser()
			&& Uno.Foundation.WebAssemblyImports.EvalBool(
				"/Mac|iPhone|iPad|iPod/.test(navigator?.platform ?? '')"));

	/// <summary>
	/// Gets the platform-appropriate modifier key for standard commands (Cut, Copy, Paste, etc.).
	/// Returns VirtualKeyModifiers.Windows (Command key) on Apple keyboards,
	/// VirtualKeyModifiers.Control on all others.
	/// </summary>
	internal static VirtualKeyModifiers PlatformCommandModifier { get; } =
		UsesAppleKeyboardLayout() ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;
}
