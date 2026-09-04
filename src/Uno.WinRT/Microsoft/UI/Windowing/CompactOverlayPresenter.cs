namespace Microsoft.UI.Windowing;

public partial class CompactOverlayPresenter : AppWindowPresenter
{
	internal CompactOverlayPresenter() : base(AppWindowPresenterKind.CompactOverlay)
	{
	}

	public static CompactOverlayPresenter Create() => new CompactOverlayPresenter();
}
