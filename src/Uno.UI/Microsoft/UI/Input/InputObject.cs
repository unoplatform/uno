#if HAS_UNO_WINUI

namespace Microsoft.UI.Input
{
	public partial class InputObject
	{
		public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue
			=> Xaml.Window.CurrentSafe.DispatcherQueue;
	}
}
#endif
