#if HAS_UNO_WINUI
namespace Microsoft.UI.Windowing;

partial class AppWindowTitleBar
{
	public static bool IsCustomizationSupported() => true;

	public global::Windows.UI.Color? BackgroundColor
	{
		get => TitleBarHelper.BackgroundColor;
		set => TitleBarHelper.BackgroundColor = value;
	}
}
#endif
