using Uno.UI.Dispatching;

namespace Windows.UI.Core
{
	public partial class IdleDispatchedHandlerArgs
	{
		private readonly NativeDispatcher _dispatcher;

		internal IdleDispatchedHandlerArgs(NativeDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		/// <summary>
		/// Determines if the dispatcher is currently idle
		/// </summary>
		public bool IsDispatcherIdle => _dispatcher.IsIdle;
	}
}
