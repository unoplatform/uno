using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Foundation.Interop;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		/// <summary>
		/// Provide a action that will delegate the dispatch of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Action<Action> DispatchOverride
		{
			get => Uno.UI.Dispatching.CoreDispatcher.DispatchOverride;
			set => Uno.UI.Dispatching.CoreDispatcher.DispatchOverride = value;
		}

		// Always reschedule, otherwise we may end up in live-lock.
		public static bool HasThreadAccessOverride => Uno.UI.Dispatching.CoreDispatcher.HasThreadAccessOverride;
	}
}
