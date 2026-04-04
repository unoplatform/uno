using System;

namespace Microsoft.UI.Composition.SystemBackdrops;

public partial class MicaController
#if HAS_UNO_WINUI
	: ISystemBackdropController, IDisposable, ISystemBackdropControllerWithTargets, IClosableNotifier
#endif
{
	/// <summary>
	/// Determines whether the mica material is supported on the current operating system.
	/// </summary>
	/// <remarks>
	/// The current Skia Win32 implementation uses <c>DWMWA_SYSTEMBACKDROP_TYPE</c>, which is only
	/// supported on Windows 11 build 22621 and later.
	/// </remarks>
	/// <returns>True if the mica material is supported on the current operating system; otherwise, false.</returns>
	public static bool IsSupported() =>
		OperatingSystem.IsMacOS()
		|| OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);
}
