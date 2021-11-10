using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Foundation.Runtime.WebAssembly.Helpers
{
	internal class ActionDisposable : IDisposable
	{
		private readonly Action _action;

		public ActionDisposable(Action action)
		{
			_action = action;
		}

		public void Dispose() => _action?.Invoke();
	}
}
