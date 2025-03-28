using System;

namespace Windows.UI.Composition.SystemBackdrops;

public partial class MicaController
#if HAS_UNO_WINUI
	: ISystemBackdropController, IDisposable, ISystemBackdropControllerWithTargets, IClosableNotifier
#endif
{
	/// <summary>
	/// Determines whether the mica material is supported on the current operating system.
	/// </summary>
	/// <remarks>Currently returns false on all targets except for WinUI.</remarks>
	/// <returns>True if the mica material is supported on the current operating system; otherwise, false.</returns>
	public static bool IsSupported() => false;
}
