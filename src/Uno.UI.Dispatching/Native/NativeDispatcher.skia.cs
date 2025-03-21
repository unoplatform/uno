using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Action<Action, NativeDispatcherPriority> DispatchOverride;

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Func<bool> HasThreadAccessOverride;

		private bool GetHasThreadAccess()
		{
			Trace.Assert(HasThreadAccessOverride != null, "HasThreadAccessOverride must be set.");

			return HasThreadAccessOverride();
		}

		partial void EnqueueNative(NativeDispatcherPriority priority)
		{
			Trace.Assert(DispatchOverride != null, "DispatchOverride must be set.");

			DispatchOverride(NativeDispatcher.DispatchItems, priority);
		}
	}
}
