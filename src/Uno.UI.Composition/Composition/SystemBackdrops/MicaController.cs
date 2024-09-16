namespace Microsoft.UI.Composition.SystemBackdrops;

public partial class MicaController : ISystemBackdropController, IDisposable, ISystemBackdropControllerWithTargets, IClosableNotifier
{
	/// <summary>
	/// Determines whether the mica material is supported on the current operating system.
	/// </summary>
	/// <remarks>Currently returns false on all targets except for WinUI.</remarks>
	/// <returns>True if the mica material is supported on the current operating system; otherwise, false.</returns>
	public static bool IsSupported() => false;
}
