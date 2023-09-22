#nullable enable
#if HAS_UNO_WINUI
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml;

public sealed partial class Window
{
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

#pragma warning disable CS0067
	/// <summary>
	/// Occurs when the window has closed.
	/// </summary>
	public event TypedEventHandler<object, WindowEventArgs>? Closed;
#pragma warning restore CS0067
}
#endif
