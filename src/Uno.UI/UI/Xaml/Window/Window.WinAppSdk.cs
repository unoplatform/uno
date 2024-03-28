#nullable enable

using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml;

partial class Window
{
#if HAS_UNO_WINUI
	public Window() : this(Uno.UI.Xaml.WindowType.DesktopXamlSource)
	{
	}

	/// <summary>
	/// Occurs when the window has closed.
	/// </summary>
	public event TypedEventHandler<object, WindowEventArgs> Closed
	{
		add => _windowImplementation.Closed += value;
		remove => _windowImplementation.Closed -= value;
	}
#endif

#if !__IOS__ // This can be added when iOS uses SceneDelegate #8341.
	/// <summary>
	/// Gets or sets a string used for the window title.
	/// </summary>
#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	string Title
	{
		get => _windowImplementation.Title;
		set => _windowImplementation.Title = value;
	}
#endif
}
