using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Core
{
    public partial class IdleDispatchedHandlerArgs
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
