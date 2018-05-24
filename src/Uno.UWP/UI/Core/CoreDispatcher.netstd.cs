using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.UI.Core
{
    public sealed partial class CoreDispatcher
    {
		private Timer _timer;

		partial void Initialize()
		{
			_timer = new Timer(_ => DispatchItems());
		}

		// Always reschedule, otherwise we may end up in live-lock.
		public static bool HasThreadAccessOverride { get; set; } = false;
		 
		private bool GetHasThreadAccess() => HasThreadAccessOverride;

		public static CoreDispatcher Main { get; } = new CoreDispatcher();

		partial void EnqueueNative()
		{
			_timer.Change(0, -1);
		}
	}
}