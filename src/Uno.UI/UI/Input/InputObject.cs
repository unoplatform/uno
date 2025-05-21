#if HAS_UNO_WINUI

namespace Microsoft.UI.Input
{
	public partial class InputObject
	{
		public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue
			=> Microsoft.UI.Dispatching.DispatcherQueue.Main;
	}
}
#endif
