using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.UI.Core
{
    public sealed partial class CoreDispatcher
    {
		private Timer _timer;

		/// <summary>
		/// Provide a action that will delegate the dispach of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Action<Action> DispatchOverride;

		partial void Initialize()
		{
			if (DispatchOverride == null)
			{
				_timer = new Timer(_ => DispatchItems());
			}
		}

		// Always reschedule, otherwise we may end up in live-lock.
		public static bool HasThreadAccessOverride { get; set; } = false;
		 
		private bool GetHasThreadAccess() => HasThreadAccessOverride;

		public static CoreDispatcher Main { get; } = new CoreDispatcher();

		partial void EnqueueNative()
		{
			if (DispatchOverride == null)
			{
				_timer.Change(0, -1);
			}
			else
			{
				DispatchOverride(() => DispatchItems());
			}
		}
	}
}
