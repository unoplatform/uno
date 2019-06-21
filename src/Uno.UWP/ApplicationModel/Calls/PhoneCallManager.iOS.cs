#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CallKit;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static readonly CXCallObserver _callObserver;

		static PhoneCallManager()
		{
			_callObserver = new CXCallObserver();
			_callObserver.SetDelegate(new CallObserverDelegate(), null);
		}

		public static bool IsCallActive =>
			_callObserver.Calls?.Any(
				c => c != null &&
					 c.HasConnected &&
					 !c.HasEnded) == true;

		public static bool IsCallIncoming =>
			_callObserver.Calls?.Any(
					c => c != null &&
						 !c.HasEnded &&
						 !c.HasConnected &&
						 !c.Outgoing) == true;

		public static event EventHandler<object> CallStateChanged;

		internal void RaiseCallStateChanged() => CallStateChanged?.Invoke(null, null);
	}
}
#endif
