#if !HAS_UNO_WINUI
#nullable enable

using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml;

partial class Window
{
	private bool _windowCreatedRaised;

#if !HAS_UNO_WINUI
#pragma warning disable CS0067
	/// <summary>
	/// Occurs when the window has closed.
	/// </summary>
	public event WindowClosedEventHandler? Closed;
#pragma warning restore CS0067
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
