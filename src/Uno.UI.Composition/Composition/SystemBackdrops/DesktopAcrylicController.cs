using System;

namespace Microsoft.UI.Composition.SystemBackdrops;

public partial class DesktopAcrylicController
#if HAS_UNO_WINUI
	: ISystemBackdropController, IDisposable, ISystemBackdropControllerWithTargets, IClosableNotifier
#endif
{
	/// <summary>
	/// Determines whether the acrylic material is supported on the current operating system.
	/// </summary>
	/// <returns>True if the acrylic material is supported on the current operating system; otherwise, false.</returns>
	public static bool IsSupported() => OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();
}
