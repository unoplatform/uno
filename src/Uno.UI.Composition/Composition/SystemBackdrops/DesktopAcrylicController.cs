using System;

namespace Windows.UI.Composition.SystemBackdrops;

public partial class DesktopAcrylicController
#if HAS_UNO_WINUI
	: ISystemBackdropController, IDisposable, ISystemBackdropControllerWithTargets, IClosableNotifier
#endif
{
	/// <summary>
	/// Determines whether the acrylic material is supported on the current operating system.
	/// </summary>
	/// <remarks>Currently returns false on all targets except for WinUI.</remarks>
	/// <returns>True if the acrylic material is supported on the current operating system; otherwise, false.</returns>
	public static bool IsSupported() => false;
}
