using System;
using System.ComponentModel;

using Uno.UI.Dispatching;

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
			get => NativeDispatcher.DispatchOverride;
			set => NativeDispatcher.DispatchOverride = value;
		}

		// Always reschedule, otherwise we may end up in live-lock.
		public static bool HasThreadAccessOverride => NativeDispatcher.HasThreadAccessOverride;
	}
}
