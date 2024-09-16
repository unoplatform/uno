using System;

namespace Microsoft.UI.Composition.SystemBackdrops;

public partial class DesktopAcrylicController : ISystemBackdropController, IDisposable, ISystemBackdropControllerWithTargets, IClosableNotifier
{
	/// <summary>
	/// Determines whether the acrylic material is supported on the current operating system.
	/// </summary>
	/// <remarks>Currently returns false on all targets except for WinUI.</remarks>
	/// <returns>True if the acrylic material is supported on the current operating system; otherwise, false.</returns>
	public static bool IsSupported() => false;
}
