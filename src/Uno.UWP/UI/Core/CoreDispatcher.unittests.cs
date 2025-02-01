using Uno.UI.Dispatching;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		public void ProcessEvents(CoreProcessEventsOption options)
			=> _inner.ProcessEvents((Uno.UI.Dispatching.CoreProcessEventsOption)options);
	}
}
