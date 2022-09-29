#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Core
{
    public partial class IdleDispatchedHandlerArgs
    {
		private Uno.UI.Dispatching.IdleDispatchedHandlerArgs _originHandler;

		internal IdleDispatchedHandlerArgs(Uno.UI.Dispatching.IdleDispatchedHandlerArgs c)
		{
			_originHandler = c;
		}

		/// <summary>
		/// Determines if the dispatcher is currently idle
		/// </summary>
		public bool IsDispatcherIdle => _originHandler.IsDispatcherIdle;
    }
}
