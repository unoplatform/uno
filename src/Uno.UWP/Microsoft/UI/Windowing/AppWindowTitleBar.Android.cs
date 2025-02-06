#if HAS_UNO_WINUI
using Uno.UI.ViewManagement.Helpers;

namespace Microsoft.UI.Windowing;

partial class AppWindowTitleBar
{
	public static bool IsCustomizationSupported() => true;

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
#endif
