#if !HAS_UNO_WINUI
#nullable enable

using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml;

partial class Window
{
	private bool _windowCreatedRaised;

#if !HAS_UNO_WINUI
	/// <summary>
	/// Occurs when the window has closed.
	/// </summary>
	public event WindowClosedEventHandler Closed;
#endif

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
