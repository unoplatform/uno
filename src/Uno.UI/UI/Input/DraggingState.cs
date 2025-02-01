// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public enum DraggingState
	{
		Started,
		Continuing,
		Completed,
	}
}
#endif
