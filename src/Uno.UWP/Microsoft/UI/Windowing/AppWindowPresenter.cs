namespace Microsoft.UI.Windowing;

public partial class AppWindowPresenter
{
	/// <summary>
	/// Displays an app window using a pre-defined configuration appropriate for the type of window.
	/// </summary>
	internal AppWindowPresenter(AppWindowPresenterKind kind) => Kind = kind;

	/// <summary>
	/// Gets a value that indicates the kind of presenter the app window is using.
	/// </summary>
	public AppWindowPresenterKind Kind { get; }
}
