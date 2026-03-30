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
	/// <returns>True if the mica material is supported on the current operating system; otherwise, false.</returns>
	public static bool IsSupported() => OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();
}
