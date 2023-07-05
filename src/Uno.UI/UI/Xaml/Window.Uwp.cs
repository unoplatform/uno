#if !HAS_UNO_WINUI
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml;

partial class Window
{
	private bool _windowCreatedRaised;

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
