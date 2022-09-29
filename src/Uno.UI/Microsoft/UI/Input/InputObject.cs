#nullable disable

#if HAS_UNO_WINUI

namespace Microsoft.UI.Input
{
	public class InputObject
	{
		public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue
			=> Xaml.Window.Current.DispatcherQueue;
	}
}
#endif
