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
		private static CXCallObserver _callObserver = new CXCallObserver();

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
	}
}
#endif
