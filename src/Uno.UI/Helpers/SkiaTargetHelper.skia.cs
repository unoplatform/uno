using System;

namespace Uno.UI.Helpers;

internal static class SkiaTargetHelper
{
	internal static bool IsNonDesktop() =>
		OperatingSystem.IsBrowser() ||
		OperatingSystem.IsAndroid() ||
		OperatingSystem.IsIOS() ||
		OperatingSystem.IsMacCatalyst() ||
		OperatingSystem.IsTvOS();
}
