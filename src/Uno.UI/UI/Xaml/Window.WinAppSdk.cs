#nullable enable
#if HAS_UNO_WINUI

using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml;

public sealed partial class Window
{
#if !__IOS__ // This can be added when iOS uses SceneDelegate #8341.
	/// <summary>
	/// Gets or sets a string used for the window title.
	/// </summary>
	public string Title
	{
		get => ApplicationView.GetForCurrentView().Title;
		set => ApplicationView.GetForCurrentView().Title = value;
	}
#endif
}
#endif
