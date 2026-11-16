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

		/// <summary>
		/// Lets a host that runs dispatched work inline (the unit tests) pump on an enqueue even when another thread's
		/// enqueue is already pending, so that pending dispatch doesn't defer the work of the current thread.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Func<bool> DispatchBehindPendingOverride;

		private bool GetHasThreadAccess()
		{
			Debug.Assert(HasThreadAccessOverride != null, "HasThreadAccessOverride must be set.");

			return HasThreadAccessOverride();
		}

		partial void EnqueueNative(NativeDispatcherPriority priority)
		{
			Debug.Assert(DispatchOverride != null, "DispatchOverride must be set.");

			DispatchOverride(NativeDispatcher.DispatchItems, priority);
		}

		partial void OnEnqueuedBehindPendingDispatch(NativeDispatcherPriority priority)
		{
			if (DispatchBehindPendingOverride?.Invoke() == true)
			{
				EnqueueNative(priority);
			}
		}
	}
}
