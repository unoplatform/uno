using System;

namespace Uno.UI.Helpers;

internal static class SkiaTargetHelper
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
		OperatingSystem.IsIOS() ||
		OperatingSystem.IsMacCatalyst() ||
		OperatingSystem.IsTvOS();
}
