#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
{
	public delegate void DispatcherQueueHandler();
}
