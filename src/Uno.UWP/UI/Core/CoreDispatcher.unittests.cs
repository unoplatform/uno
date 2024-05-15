using Uno.UI.Dispatching;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		public static bool HasThreadAccessOverride
		{
			get => NativeDispatcher.HasThreadAccessOverride;
			set => NativeDispatcher.HasThreadAccessOverride = value;
		}

		public void ProcessEvents(CoreProcessEventsOption options)
			=> _inner.ProcessEvents((Uno.UI.Dispatching.CoreProcessEventsOption)options);
	}
}
