namespace Microsoft.UI.Windowing;

/// <summary>
/// Displays an app window using a full-screen configuration.
/// </summary>
public partial class FullScreenPresenter : AppWindowPresenter
{
	internal FullScreenPresenter() : base(AppWindowPresenterKind.FullScreen)
	{
	}

	/// <summary>
	/// Creates a new instance of FullScreenPresenter.
	/// </summary>
	/// <returns>A new instance of FullScreenPresenter.</returns>
	public static FullScreenPresenter Create() => new FullScreenPresenter();
}
