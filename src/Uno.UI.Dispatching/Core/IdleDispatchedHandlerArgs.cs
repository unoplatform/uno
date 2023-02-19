using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Dispatching
{
	internal partial class IdleDispatchedHandlerArgs
	{
		private IsIdleHandler _handler;

		internal delegate bool IsIdleHandler();

		internal IdleDispatchedHandlerArgs(IsIdleHandler handler)
		{
			_handler = handler;
		}

		/// <summary>
		/// Determines if the dispatcher is currently idle
		/// </summary>
		public bool IsDispatcherIdle => _handler();
	}
}
