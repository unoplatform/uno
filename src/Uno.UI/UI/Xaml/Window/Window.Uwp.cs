#if !HAS_UNO_WINUI
#nullable enable

using Windows.UI.Xaml;

namespace Windows.UI.Xaml;

partial class Window
{
	private bool _windowCreatedRaised;

	internal Window() : this(Uno.UI.Xaml.WindowType.DesktopXamlSource)
	{
	}

	partial void InitializeWindowingFlavor() => _windowImplementation!.Closed += OnWinUIWindowClosed;

	/// <summary>
	/// Occurs when the window has closed.
	/// </summary>
	public event WindowClosedEventHandler? Closed;

	private void OnWinUIWindowClosed(object sender, WindowEventArgs args) => Closed?.Invoke(this, new Core.CoreWindowEventArgs());

	internal void RaiseCreated()
	{
		if (!_windowCreatedRaised)
		{
			_windowCreatedRaised = true;
			Application.Current.RaiseWindowCreated(this);
		}
	}
}
#endif
