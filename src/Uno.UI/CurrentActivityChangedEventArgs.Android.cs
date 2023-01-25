using System;
using System.Linq;

namespace Uno.UI
{
	public class CurrentActivityChangedEventArgs : EventArgs
	{
		internal CurrentActivityChangedEventArgs(BaseActivity current)
		{
			Current = current;
		}

		/// <summary>
		/// Gets the new current activity, if any.
		/// </summary>
		public BaseActivity Current { get; }
	}
}
