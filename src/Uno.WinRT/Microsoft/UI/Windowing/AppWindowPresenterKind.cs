namespace Microsoft.UI.Windowing;

/// <summary>
/// Defines constants that specify the kind of presenter the app window uses.
/// </summary>
public enum AppWindowPresenterKind
{
	/// <summary>
	/// The app window uses the system default presenter.
	/// </summary>
	Default = 0,

	/// <summary>
	/// The app window uses a compact overlay (picture-in-picture) presenter.
	/// </summary>
	CompactOverlay = 1,

	/// <summary>
	/// The app window uses a full screen presenter.
	/// </summary>
	FullScreen = 2,

	/// <summary>
	/// The app window uses an overlapped presenter.
	/// </summary>
	Overlapped = 3,
}
