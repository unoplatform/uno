using System;
using System.Threading;

namespace Uno.UI.Helpers;

internal static class DeviceTargetHelper
{
	internal static bool IsNonDesktop() =>
		OperatingSystem.IsBrowser() ||
		IsMobile();

	static int haveJLO;

	static bool HaveJLO()
	{
		if (haveJLO != 0)
		{
			return (haveJLO - 1) != 0;
		}
		bool jloExists = Type.GetType("Java.Lang.Object, Mono.Android", throwOnError: false) != null;
		int value = jloExists ? 2 : 1;
		Interlocked.CompareExchange(ref haveJLO, value, 0);
		return (value - 1) != 0;
	}

	internal static bool IsDesktop() =>
		OperatingSystem.IsWindows() ||
		OperatingSystem.IsMacOS() ||
		(OperatingSystem.IsLinux() && !HaveJLO());

	internal static bool IsAndroid() =>
		OperatingSystem.IsAndroid() ||
		(OperatingSystem.IsLinux() && HaveJLO());

	internal static bool IsMobile() =>
		IsAndroid() ||
		IsUIKit();

	internal static bool IsUIKit() =>
		OperatingSystem.IsIOS() ||
		OperatingSystem.IsMacCatalyst() ||
		OperatingSystem.IsTvOS();
}
