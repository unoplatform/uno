#if !HAS_UNO_WINUI
#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml;

partial class Window
{
	private bool _windowCreatedRaised;

#if DEBUG
	internal Window() : this(Uno.UI.Xaml.WindowType.DesktopXamlSource)
	{
	}
#endif

	partial void InitializeWindowingFlavor()
	{
		_windowImplementation!.Closed += OnWinUIWindowClosed;
	}

	/// <summary>
	/// Occurs when the window has closed.
	/// </summary>
	public event WindowClosedEventHandler? Closed;

	private void OnWinUIWindowClosed(object sender, WindowEventArgs args) => Closed?.Invoke(this, new Core.CoreWindowEventArgs());

	internal void RaiseCreated()
	{
		if (Application.Current is not null && !_windowCreatedRaised)
		{
			_windowCreatedRaised = true;
			Application.Current.RaiseWindowCreated(this);
		}
	}
}
#endif
