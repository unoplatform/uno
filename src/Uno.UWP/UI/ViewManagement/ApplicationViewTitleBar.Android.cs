using Uno.UI.ViewManagement.Helpers;

namespace Windows.UI.ViewManagement;

partial class ApplicationViewTitleBar
{
	public global::Windows.UI.Color? BackgroundColor
	{
		get => StatusBarHelper.BackgroundColor;
		set => StatusBarHelper.BackgroundColor = value;
	}

	public global::Windows.UI.Color? ForegroundColor
	{
		get => StatusBarHelper.ForegroundColor;
		set => StatusBarHelper.ForegroundColor = value;
	}
}
