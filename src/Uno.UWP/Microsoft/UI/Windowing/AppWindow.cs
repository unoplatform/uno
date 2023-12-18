#if HAS_UNO_WINUI
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Windowing;

public partial class AppWindow
{
	internal AppWindow()
	{
	}

	internal static AppWindow Instance { get; } = new AppWindow();

#if !__IOS__
	public string Title
	{
		get => ApplicationView.GetForCurrentView().Title;
		set => ApplicationView.GetForCurrentView().Title = value;
	}
#endif

	public AppWindowTitleBar TitleBar { get; } = new AppWindowTitleBar();

	public static AppWindow GetFromWindowId(WindowId windowId) => Instance;
}
#endif
